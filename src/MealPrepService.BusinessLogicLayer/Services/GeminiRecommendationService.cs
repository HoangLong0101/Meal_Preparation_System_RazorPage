using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;

namespace MealPrepService.BusinessLogicLayer.Services
{
    /// <summary>
    /// Google Gemini-powered recommendation service
    /// </summary>
    public class GeminiRecommendationService : ILLMService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiRecommendationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiEndpoint;

        public GeminiRecommendationService(
            IConfiguration configuration,
            ILogger<GeminiRecommendationService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClientFactory.CreateClient();

            // Load configuration
            _apiKey = _configuration["AI:Gemini:ApiKey"] 
                ?? throw new InvalidOperationException("Gemini API key not configured");
            _model = _configuration["AI:Gemini:Model"] ?? "gemini-1.5-flash";
            
            // All Gemini models use v1beta API
            var apiVersion = "v1beta";
            
            // Gemini API endpoint format
            _apiEndpoint = $"https://generativelanguage.googleapis.com/{apiVersion}/models/{_model}:generateContent?key={_apiKey}";
            
            _logger.LogInformation("Gemini service initialized with model: {Model}, API version: {ApiVersion}", _model, apiVersion);
        }

        public string GetModelName() => _model;

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                _logger.LogInformation("Checking Gemini service availability with model: {Model}", _model);
                
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogError("Gemini API key is null or empty");
                    return false;
                }
                
                _logger.LogInformation("API key configured (length: {Length})", _apiKey.Length);

                // Simple health check
                var testRequest = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = "test" }
                            }
                        }
                    }
                };

                _logger.LogInformation("Making test request to Gemini API");
                var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, testRequest);
                
                _logger.LogInformation("Gemini API response: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                }
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini service health check failed");
                return false;
            }
        }

        public async Task<AIRecommendationResponse> GenerateRecommendationsAsync(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations)
        {
            try
            {
                var prompt = BuildRecommendationPrompt(context, candidateRecipes, maxRecommendations);
                var response = await CallGeminiAsync(prompt);
                
                return ParseRecommendationResponse(response, candidateRecipes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI recommendations");
                throw;
            }
        }

        public async Task<AIRecommendationResponse> GenerateRecommendationsAsync(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations,
            List<Guid> recentRecipeIds,
            DateTime targetDate,
            string mealType)
        {
            try
            {
                var prompt = BuildDiversityAwarePrompt(context, candidateRecipes, maxRecommendations, recentRecipeIds, targetDate, mealType);
                var response = await CallGeminiAsync(prompt);
                
                return ParseRecommendationResponse(response, candidateRecipes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate diversity-aware AI recommendations");
                throw;
            }
        }

        public async Task<string> GenerateRecommendationReasoningAsync(Recipe recipe, CustomerContext context)
        {
            try
            {
                var prompt = BuildReasoningPrompt(recipe, context);
                return await CallGeminiAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate reasoning for recipe {RecipeId}", recipe.Id);
                return "This recipe matches your dietary preferences and nutritional goals.";
            }
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            try
            {
                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 16384,
                        responseMimeType = "application/json"
                    }
                };

                _logger.LogDebug("Sending request to Gemini API");
                var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API returned error: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Gemini API request failed: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Received response from Gemini API");

                var jsonDoc = JsonDocument.Parse(responseContent);
                
                // Parse Gemini response format
                if (jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    
                    // Log finish reason to check if response was truncated
                    if (firstCandidate.TryGetProperty("finishReason", out var finishReason))
                    {
                        _logger.LogInformation("Gemini finish reason: {FinishReason}", finishReason.GetString());
                    }
                    
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        // For thinking models (e.g. gemini-2.5-flash), the response may contain
                        // a "thought" part followed by the actual content part.
                        // Iterate from the last part to find the actual (non-thought) content.
                        string? resultText = null;
                        for (int i = parts.GetArrayLength() - 1; i >= 0; i--)
                        {
                            var part = parts[i];
                            // Skip thought parts (thinking model internal reasoning)
                            if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                                continue;
                            
                            if (part.TryGetProperty("text", out var textElement))
                            {
                                resultText = textElement.GetString();
                                break;
                            }
                        }
                        
                        // Fallback: if all parts are thoughts, use the last part's text
                        if (resultText == null)
                        {
                            var lastPart = parts[parts.GetArrayLength() - 1];
                            if (lastPart.TryGetProperty("text", out var fallbackText))
                                resultText = fallbackText.GetString();
                        }
                        
                        return resultText ?? string.Empty;
                    }
                }

                _logger.LogWarning("Unable to parse Gemini response");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
        }

        private string BuildDiversityAwarePrompt(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations,
            List<Guid> recentRecipeIds,
            DateTime targetDate,
            string mealType)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("You are a professional nutritionist and meal planning expert. Create diverse, varied meal recommendations that avoid repetition.");
            sb.AppendLine();
            
            // Customer Profile
            sb.AppendLine("## Customer Profile");
            sb.AppendLine($"Name: {context.Customer.FullName}");
            
            if (context.HealthProfile != null)
            {
                sb.AppendLine($"Calorie Goal: {context.HealthProfile.CalorieGoal ?? 2000} cal/day");
                sb.AppendLine($"Dietary Restrictions: {context.HealthProfile.DietaryRestrictions ?? "None"}");
                sb.AppendLine($"Health Notes: {context.HealthProfile.HealthNotes ?? "General wellness"}");
                
                if (!string.IsNullOrWhiteSpace(context.HealthProfile.FoodPreferences))
                {
                    sb.AppendLine($"Food Preferences: {context.HealthProfile.FoodPreferences}");
                }
            }
            
            if (context.Allergies.Any())
            {
                sb.AppendLine($"Allergies: {string.Join(", ", context.Allergies.Select(a => a.AllergyName))}");
                sb.AppendLine("⚠️ CRITICAL: Never recommend recipes containing these allergens!");
            }
            
            sb.AppendLine();
            
            // Diversity Requirements
            sb.AppendLine("## DIVERSITY REQUIREMENTS (CRITICAL)");
            sb.AppendLine($"Target: {mealType} for {targetDate:yyyy-MM-dd}");
            
            if (recentRecipeIds.Any())
            {
                var recentRecipeNames = candidateRecipes
                    .Where(r => recentRecipeIds.Contains(r.Id))
                    .Select(r => r.RecipeName)
                    .ToList();
                
                if (recentRecipeNames.Any())
                {
                    sb.AppendLine("🚫 FORBIDDEN RECIPES (used in last 3 days):");
                    foreach (var recipeName in recentRecipeNames)
                    {
                        sb.AppendLine($"  - {recipeName}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("YOU MUST EXCLUDE THESE FROM YOUR RECOMMENDATIONS!");
                }
            }
            
            sb.AppendLine();
            
            // Available Recipes
            sb.AppendLine($"## Available Recipes ({candidateRecipes.Count} options)");
            foreach (var recipe in candidateRecipes)
            {
                var isRecent = recentRecipeIds.Contains(recipe.Id);
                var marker = isRecent ? "❌" : "✓";
                
                sb.AppendLine($"{marker} Recipe #{recipe.Id}:");
                sb.AppendLine($"   Name: {recipe.RecipeName}");
                sb.AppendLine($"   Calories: {recipe.TotalCalories}");
                sb.AppendLine($"   Protein: {recipe.ProteinG}g, Carbs: {recipe.CarbsG}g, Fat: {recipe.FatG}g");
                
                if (recipe.RecipeIngredients != null && recipe.RecipeIngredients.Any())
                {
                    sb.AppendLine($"   Ingredients: {string.Join(", ", recipe.RecipeIngredients.Select(ri => ri.Ingredient.IngredientName))}");
                }
                
                sb.AppendLine();
            }
            
            // Instructions
            sb.AppendLine("## Task");
            sb.AppendLine($"Select exactly {maxRecommendations} recipes that:");
            sb.AppendLine("1. NEVER include forbidden recipes (marked with ❌)");
            sb.AppendLine("2. Match the customer's dietary needs and preferences");
            sb.AppendLine("3. Are appropriate for the meal type");
            sb.AppendLine("4. Provide variety and nutritional balance");
            sb.AppendLine("5. Avoid allergens");
            sb.AppendLine();
            sb.AppendLine("Respond ONLY with a JSON array of recipe IDs and scores:");
            sb.AppendLine("[");
            sb.AppendLine("  {\"recipeId\": \"guid-here\", \"score\": 95, \"reason\": \"brief explanation\"},");
            sb.AppendLine("  ...");
            sb.AppendLine("]");
            
            return sb.ToString();
        }

        private string BuildRecommendationPrompt(
            CustomerContext context,
            List<Recipe> candidateRecipes,
            int maxRecommendations)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("You are a professional nutritionist and meal planning expert.");
            sb.AppendLine();
            
            // Customer Profile
            sb.AppendLine("## Customer Profile");
            sb.AppendLine($"Name: {context.Customer.FullName}");
            
            if (context.HealthProfile != null)
            {
                sb.AppendLine($"Calorie Goal: {context.HealthProfile.CalorieGoal ?? 2000} cal/day");
                sb.AppendLine($"Dietary Restrictions: {context.HealthProfile.DietaryRestrictions ?? "None"}");
                sb.AppendLine($"Health Notes: {context.HealthProfile.HealthNotes ?? "General wellness"}");
                
                if (!string.IsNullOrWhiteSpace(context.HealthProfile.FoodPreferences))
                {
                    sb.AppendLine($"Food Preferences: {context.HealthProfile.FoodPreferences}");
                }
            }
            
            if (context.Allergies.Any())
            {
                sb.AppendLine($"Allergies: {string.Join(", ", context.Allergies.Select(a => a.AllergyName))}");
                sb.AppendLine("⚠️ CRITICAL: Never recommend recipes containing these allergens!");
            }
            
            sb.AppendLine();
            
            // Available Recipes
            sb.AppendLine($"## Available Recipes ({candidateRecipes.Count} options)");
            foreach (var recipe in candidateRecipes)
            {
                sb.AppendLine($"Recipe #{recipe.Id}:");
                sb.AppendLine($"   Name: {recipe.RecipeName}");
                sb.AppendLine($"   Calories: {recipe.TotalCalories}");
                sb.AppendLine($"   Protein: {recipe.ProteinG}g, Carbs: {recipe.CarbsG}g, Fat: {recipe.FatG}g");
                
                if (recipe.RecipeIngredients != null && recipe.RecipeIngredients.Any())
                {
                    sb.AppendLine($"   Ingredients: {string.Join(", ", recipe.RecipeIngredients.Select(ri => ri.Ingredient.IngredientName))}");
                }
                
                sb.AppendLine();
            }
            
            // Instructions
            sb.AppendLine("## Task");
            sb.AppendLine($"Select the top {maxRecommendations} recipes that best match the customer's needs.");
            sb.AppendLine("Consider: nutritional balance, dietary restrictions, preferences, and allergens.");
            sb.AppendLine();
            sb.AppendLine("Respond ONLY with a JSON array:");
            sb.AppendLine("[");
            sb.AppendLine("  {\"recipeId\": \"guid-here\", \"score\": 95, \"reason\": \"brief explanation\"},");
            sb.AppendLine("  ...");
            sb.AppendLine("]");
            
            return sb.ToString();
        }

        private string BuildReasoningPrompt(Recipe recipe, CustomerContext context)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Generate a brief, friendly explanation (2-3 sentences) for why this recipe is recommended:");
            sb.AppendLine();
            sb.AppendLine($"Recipe: {recipe.RecipeName}");
            sb.AppendLine($"Calories: {recipe.TotalCalories}");
            sb.AppendLine($"Nutrition: {recipe.ProteinG}g protein, {recipe.CarbsG}g carbs, {recipe.FatG}g fat");
            
            if (context.HealthProfile != null)
            {
                sb.AppendLine($"Customer's Calorie Goal: {context.HealthProfile.CalorieGoal}");
                sb.AppendLine($"Dietary Preferences: {context.HealthProfile.DietaryRestrictions}");
            }
            
            return sb.ToString();
        }

        private AIRecommendationResponse ParseRecommendationResponse(string response, List<Recipe> candidateRecipes)
        {
            var result = new AIRecommendationResponse();
            
            try
            {
                // Extract JSON from response (might have markdown formatting)
                var jsonStart = response.IndexOf('[');
                var jsonEnd = response.LastIndexOf(']');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonString = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    
                    using var doc = JsonDocument.Parse(jsonString);
                    var recommendations = doc.RootElement;
                    
                    foreach (var item in recommendations.EnumerateArray())
                    {
                        if (item.TryGetProperty("recipeId", out var recipeIdElement))
                        {
                            var recipeIdStr = recipeIdElement.GetString();
                            if (Guid.TryParse(recipeIdStr, out var recipeId))
                            {
                                var recipe = candidateRecipes.FirstOrDefault(r => r.Id == recipeId);
                                if (recipe != null)
                                {
                                    var score = item.TryGetProperty("score", out var scoreElement) 
                                        ? scoreElement.GetInt32() : 50;
                                    var reason = item.TryGetProperty("reason", out var reasonElement)
                                        ? reasonElement.GetString() ?? string.Empty : string.Empty;
                                    
                                    result.RecommendedRecipes.Add(new AIRecommendedRecipe
                                    {
                                        RecipeId = recipeId,
                                        RecipeName = recipe.RecipeName,
                                        ConfidenceScore = score,
                                        Reasoning = reason
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse recommendation response");
            }
            
            return result;
        }

        public async Task<List<AIGeneratedRecipe>> GenerateRecipeSuggestionsAsync(
            string cuisineType,
            string dietaryRestrictions,
            int calorieTarget,
            int count = 5)
        {
            try
            {
                var prompt = BuildRecipeGenerationPrompt(cuisineType, dietaryRestrictions, calorieTarget, count);
                var response = await CallGeminiAsync(prompt);
                
                return ParseGeneratedRecipes(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate recipe suggestions");
                throw;
            }
        }

        private string BuildRecipeGenerationPrompt(string cuisineType, string dietaryRestrictions, int calorieTarget, int count)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("You are a professional chef and nutritionist. Generate creative, delicious, and nutritious recipe suggestions.");
            sb.AppendLine();
            sb.AppendLine("## Requirements:");
            sb.AppendLine($"- Cuisine Type: {cuisineType}");
            sb.AppendLine($"- Dietary Restrictions: {dietaryRestrictions}");
            sb.AppendLine($"- Target Calories per serving: {calorieTarget} kcal (±100 kcal is acceptable)");
            sb.AppendLine($"- Number of recipes to generate: {count}");
            sb.AppendLine();
            sb.AppendLine("## Guidelines:");
            sb.AppendLine("1. Each recipe should be complete with clear cooking instructions");
            sb.AppendLine("2. Include realistic ingredient quantities");
            sb.AppendLine("3. Provide nutritional estimates (calories, protein, carbs, fat)");
            sb.AppendLine("4. Ensure recipes respect the dietary restrictions");
            sb.AppendLine("5. Make recipes practical and achievable for home cooking");
            sb.AppendLine();
            sb.AppendLine("## Output Format (JSON only, no markdown):");
            sb.AppendLine("[");
            sb.AppendLine("  {");
            sb.AppendLine("    \"recipeName\": \"Recipe Name\",");
            sb.AppendLine("    \"instructions\": \"Step 1: Do this.\\nStep 2: Do that...\",");
            sb.AppendLine("    \"estimatedCalories\": 500,");
            sb.AppendLine("    \"estimatedProtein\": 30,");
            sb.AppendLine("    \"estimatedCarbs\": 50,");
            sb.AppendLine("    \"estimatedFat\": 15,");
            sb.AppendLine("    \"cuisineType\": \"Vietnamese\",");
            sb.AppendLine("    \"dietaryInfo\": \"Gluten-free, High protein\",");
            sb.AppendLine("    \"ingredients\": [");
            sb.AppendLine("      {\"ingredientName\": \"Chicken breast\", \"quantity\": 200, \"unit\": \"gram\"},");
            sb.AppendLine("      {\"ingredientName\": \"Rice\", \"quantity\": 100, \"unit\": \"gram\"}");
            sb.AppendLine("    ]");
            sb.AppendLine("  }");
            sb.AppendLine("]");
            
            return sb.ToString();
        }

        private List<AIGeneratedRecipe> ParseGeneratedRecipes(string response)
        {
            var recipes = new List<AIGeneratedRecipe>();
            
            try
            {
                // Extract JSON from response
                var jsonStart = response.IndexOf('[');
                var jsonEnd = response.LastIndexOf(']');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonString = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    
                    using var doc = JsonDocument.Parse(jsonString);
                    var recipesArray = doc.RootElement;
                    
                    foreach (var recipeElement in recipesArray.EnumerateArray())
                    {
                        var recipe = new AIGeneratedRecipe();
                        
                        if (recipeElement.TryGetProperty("recipeName", out var name))
                            recipe.RecipeName = name.GetString() ?? string.Empty;
                        
                        if (recipeElement.TryGetProperty("instructions", out var instructions))
                            recipe.Instructions = instructions.GetString() ?? string.Empty;
                        
                        if (recipeElement.TryGetProperty("estimatedCalories", out var calories))
                            recipe.EstimatedCalories = (float)calories.GetDouble();
                        
                        if (recipeElement.TryGetProperty("estimatedProtein", out var protein))
                            recipe.EstimatedProtein = (float)protein.GetDouble();
                        
                        if (recipeElement.TryGetProperty("estimatedCarbs", out var carbs))
                            recipe.EstimatedCarbs = (float)carbs.GetDouble();
                        
                        if (recipeElement.TryGetProperty("estimatedFat", out var fat))
                            recipe.EstimatedFat = (float)fat.GetDouble();
                        
                        if (recipeElement.TryGetProperty("cuisineType", out var cuisine))
                            recipe.CuisineType = cuisine.GetString() ?? string.Empty;
                        
                        if (recipeElement.TryGetProperty("dietaryInfo", out var dietary))
                            recipe.DietaryInfo = dietary.GetString() ?? string.Empty;
                        
                        if (recipeElement.TryGetProperty("ingredients", out var ingredients))
                        {
                            foreach (var ing in ingredients.EnumerateArray())
                            {
                                var ingredient = new RecipeIngredientSuggestion();
                                
                                if (ing.TryGetProperty("ingredientName", out var ingName))
                                    ingredient.IngredientName = ingName.GetString() ?? string.Empty;
                                
                                if (ing.TryGetProperty("quantity", out var qty))
                                    ingredient.Quantity = (float)qty.GetDouble();
                                
                                if (ing.TryGetProperty("unit", out var unit))
                                    ingredient.Unit = unit.GetString() ?? string.Empty;
                                
                                recipe.Ingredients.Add(ingredient);
                            }
                        }
                        
                        recipes.Add(recipe);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse generated recipes");
            }
            
            return recipes;
        }

        public async Task<DailyMealSuggestionResponse> GenerateDailyMealSuggestionsAsync(
            CustomerContext context,
            DailyMealRequest request,
            List<Recipe> availableRecipes)
        {
            try
            {
                _logger.LogInformation("Generating daily meal suggestions for customer, meal type: {MealType}", 
                    request.MealType);

                // Build context-aware prompt
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine($"You are an AI expert in nutrition and cuisine. Your task is to suggest suitable meals for the user.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("USER INFORMATION:");
                
                if (context.HealthProfile != null)
                {
                    promptBuilder.AppendLine($"- Age: {context.HealthProfile.Age}");
                    promptBuilder.AppendLine($"- Gender: {context.HealthProfile.Gender}");
                    promptBuilder.AppendLine($"- Weight: {context.HealthProfile.Weight} kg");
                    promptBuilder.AppendLine($"- Height: {context.HealthProfile.Height} cm");
                    if (!string.IsNullOrEmpty(context.HealthProfile.DietaryRestrictions))
                        promptBuilder.AppendLine($"- Diet: {context.HealthProfile.DietaryRestrictions}");
                    if (context.HealthProfile.CalorieGoal > 0)
                        promptBuilder.AppendLine($"- Calorie goal: {context.HealthProfile.CalorieGoal} cal/day");
                    if (!string.IsNullOrEmpty(context.HealthProfile.FoodPreferences))
                        promptBuilder.AppendLine($"- Food preferences: {context.HealthProfile.FoodPreferences}");
                }
                
                if (context.Allergies?.Any() == true)
                {
                    var allergyNames = context.Allergies.Select(a => a.AllergyName);
                    promptBuilder.AppendLine($"- Allergies: {string.Join(", ", allergyNames)}");
                }
                
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("CURRENT REQUEST:");
                promptBuilder.AppendLine($"- Meal type: {request.MealType}");
                promptBuilder.AppendLine($"- Preparation method: {request.PreparationMethod}");
                if (!string.IsNullOrEmpty(request.CurrentMood))
                    promptBuilder.AppendLine($"- Current mood: {request.CurrentMood}");
                if (request.Budget.HasValue)
                    promptBuilder.AppendLine($"- Budget: {request.Budget:N0} VND");
                if (request.AvailableTime.HasValue)
                    promptBuilder.AppendLine($"- Available time: {request.AvailableTime} minutes");
                if (!string.IsNullOrEmpty(request.SpecificRequest))
                    promptBuilder.AppendLine($"- Specific request: {request.SpecificRequest}");
                if (request.PrioritizeHealthy)
                    promptBuilder.AppendLine("- Prioritize healthy options");
                if (request.NumberOfPeople > 1)
                    promptBuilder.AppendLine($"- Number of people: {request.NumberOfPeople}");
                if (request.FamilyMembers?.Any() == true)
                {
                    promptBuilder.AppendLine("- Family members:");
                    foreach (var member in request.FamilyMembers)
                    {
                        var portion = member.Portion > 0 ? member.Portion : 1;
                        var note = string.IsNullOrWhiteSpace(member.Note) ? "" : $" ({member.Note})";
                        promptBuilder.AppendLine($"  • {member.Role} - {portion} portion(s){note}");
                    }
                }

                promptBuilder.AppendLine();
                
                if (availableRecipes?.Any() == true)
                {
                    promptBuilder.AppendLine($"RECIPE DATABASE ({availableRecipes.Count} recipes available):");
                    foreach (var recipe in availableRecipes.Take(20))
                    {
                        // Estimate prep time based on calories (simple heuristic: 500 cal = 30 min)
                        var estimatedPrepTime = (int)(recipe.TotalCalories / 500.0f * 30);
                        var estimatedPrice = estimatedPrepTime * 15000; // 15k per 30 min
                        
                        promptBuilder.AppendLine($"- ID: {recipe.Id}, Name: {recipe.RecipeName}, Calories: {recipe.TotalCalories}, " +
                            $"Protein: {recipe.ProteinG}g, Carbs: {recipe.CarbsG}g, Fat: {recipe.FatG}g, " +
                            $"Estimated time: {estimatedPrepTime} min, Estimated price: {estimatedPrice:N0} VND");
                    }
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("TASK:");
                if (request.NumberOfPeople > 1)
                {
                    promptBuilder.AppendLine($"Suggest EXACTLY 2 suitable meals for {request.NumberOfPeople} people with COMPLETE ingredient lists.");
                    promptBuilder.AppendLine($"Scale ingredient quantities for {request.NumberOfPeople} servings.");
                    if (request.FamilyMembers?.Any() == true)
                    {
                        promptBuilder.AppendLine("Consider ALL family members' dietary needs, ages, and health conditions when suggesting meals.");
                        promptBuilder.AppendLine("Ensure each listed family member has a dedicated portion based on their requested portion count.");
                    }
                }
                else
                {
                    promptBuilder.AppendLine("Suggest EXACTLY 2 suitable meals with COMPLETE ingredient lists.");
                }
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("RETURN JSON IN THIS FORMAT:");
                promptBuilder.AppendLine("{\"overallReasoning\":\"brief reason\",\"suggestions\":[{\"recipeId\":null,\"mealName\":\"Meal Name\",\"description\":\"10-word description\",\"estimatedCalories\":500,\"estimatedPrice\":50000,\"prepTime\":30,\"reasoning\":\"Brief reason\",\"isFromDatabase\":false,\"proteinG\":25,\"carbsG\":45,\"fatG\":15,\"instructions\":\"Step 1... Step 2...\",\"ingredients\":[{\"name\":\"ingredient\",\"quantity\":200,\"unit\":\"g\"}],\"perPersonPortions\":[{\"person\":\"You\",\"portions\":1,\"quantityGuide\":\"about 1 bowl / 250g\",\"estimatedCalories\":500,\"note\":\"Optional short note\"}]}]}");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("IMPORTANT:");
                promptBuilder.AppendLine("- Each meal MUST have 5-8 ingredients (proteins, vegetables, seasonings, oils, sauces)");
                promptBuilder.AppendLine("- Include ALL ingredients needed to cook the complete meal");
                promptBuilder.AppendLine("- suggestions array MUST contain EXACTLY 2 options");
                promptBuilder.AppendLine("- For EACH option, perPersonPortions MUST include one entry for each person/family member");
                promptBuilder.AppendLine("- quantityGuide must describe practical per-person serving size (e.g., grams, bowl, plate)");
                promptBuilder.AppendLine("- Keep instructions under 200 characters");
                promptBuilder.AppendLine("- Keep reasoning under 50 characters");

                var prompt = promptBuilder.ToString();
                var responseText = await CallGeminiAsync(prompt);

                return ParseDailyMealSuggestions(responseText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate daily meal suggestions");
                return new DailyMealSuggestionResponse
                {
                    Reasoning = "Unable to generate suggestions due to system error",
                    Suggestions = new List<DailyMealSuggestion>()
                };
            }
        }

        public async Task<InventoryMealSuggestionResponse> GenerateInventoryBasedMealAsync(
            CustomerContext context,
            InventoryMealRequest request,
            List<Recipe> availableRecipes)
        {
            try
            {
                _logger.LogInformation("Generating inventory-based meal suggestions, {IngredientCount} ingredients available", 
                    request.AvailableIngredients.Count);

                // Sort ingredients by expiry date (soonest first) if prioritizing expiring items
                var sortedIngredients = request.PrioritizeExpiring 
                    ? request.AvailableIngredients
                        .OrderBy(i => i.DaysUntilExpiry)
                        .ToList()
                    : request.AvailableIngredients.ToList();

                var expiredIngredients = sortedIngredients.Where(i => i.IsExpired).ToList();
                var expiringIngredients = sortedIngredients.Where(i => i.IsExpiringSoon).ToList();
                var freshIngredients = sortedIngredients.Where(i => !i.IsExpired && !i.IsExpiringSoon).ToList();

                // Build context-aware prompt
                var promptBuilder = new StringBuilder();
                promptBuilder.AppendLine("You are an AI expert in nutrition and reducing food waste. Your task is to suggest meals using available ingredients, PRIORITIZING items close to expiration.");
                promptBuilder.AppendLine();

                // User health context
                promptBuilder.AppendLine("USER INFORMATION:");
                if (context.HealthProfile != null)
                {
                    promptBuilder.AppendLine($"- Age: {context.HealthProfile.Age}");
                    promptBuilder.AppendLine($"- Gender: {context.HealthProfile.Gender}");
                    if (!string.IsNullOrEmpty(context.HealthProfile.DietaryRestrictions))
                        promptBuilder.AppendLine($"- Dietary restrictions: {context.HealthProfile.DietaryRestrictions}");
                    if (context.HealthProfile.CalorieGoal > 0)
                        promptBuilder.AppendLine($"- Calorie goal: {context.HealthProfile.CalorieGoal} cal/day");
                }

                if (context.Allergies?.Any() == true)
                {
                    var allergyNames = context.Allergies.Select(a => a.AllergyName);
                    promptBuilder.AppendLine($"- Allergies (MUST AVOID): {string.Join(", ", allergyNames)}");
                }
                promptBuilder.AppendLine();

                // Inventory section - prioritize expiring items
                promptBuilder.AppendLine("AVAILABLE INVENTORY (sorted by expiry date - USE EXPIRING ITEMS FIRST!):");

                if (expiredIngredients.Any())
                {
                    promptBuilder.AppendLine("🔴 RECENTLY EXPIRED (use at your discretion, still edible if within 1-2 days):");
                    foreach (var ing in expiredIngredients)
                    {
                        promptBuilder.AppendLine($"  - {ing.Name}: {ing.AvailableAmount} {ing.Unit} (expired {Math.Abs(ing.DaysUntilExpiry)} days ago)");
                    }
                    promptBuilder.AppendLine();
                }

                if (expiringIngredients.Any())
                {
                    promptBuilder.AppendLine("⚠️ EXPIRING SOON (PRIORITY - must use these!):");
                    foreach (var ing in expiringIngredients)
                    {
                        promptBuilder.AppendLine($"  - {ing.Name}: {ing.AvailableAmount} {ing.Unit} (EXPIRES IN {ing.DaysUntilExpiry} DAYS!)");
                    }
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("Other available ingredients:");
                foreach (var ing in freshIngredients)
                {
                    promptBuilder.AppendLine($"  - {ing.Name}: {ing.AvailableAmount} {ing.Unit} (expires in {ing.DaysUntilExpiry} days)");
                }
                promptBuilder.AppendLine();



                // Request details
                promptBuilder.AppendLine("REQUEST:");
                promptBuilder.AppendLine($"- Meal type: {request.MealType}");
                if (request.MaxPrepTime.HasValue)
                    promptBuilder.AppendLine($"- Max prep time: {request.MaxPrepTime} minutes");
                promptBuilder.AppendLine($"- Generate shopping list for missing items: {(request.GenerateShoppingList ? "Yes" : "No")}");
                
                if (!string.IsNullOrWhiteSpace(request.MealPreference))
                {
                    promptBuilder.AppendLine();
                    promptBuilder.AppendLine("🎯 USER'S MEAL PREFERENCE (IMPORTANT - try to match this!):");
                    promptBuilder.AppendLine($"   \"{request.MealPreference}\"");
                }
                promptBuilder.AppendLine();

                // Available recipes from database
                if (availableRecipes?.Any() == true)
                {
                    promptBuilder.AppendLine($"RECIPE DATABASE ({availableRecipes.Count} recipes):");
                    foreach (var recipe in availableRecipes.Take(15))
                    {
                        promptBuilder.AppendLine($"- ID: {recipe.Id}, Name: {recipe.RecipeName}, Calories: {recipe.TotalCalories}");
                    }
                    promptBuilder.AppendLine();
                }

                promptBuilder.AppendLine("TASK:");
                promptBuilder.AppendLine("1. Suggest 2 meals that MAXIMIZE use of EXPIRING ingredients first");
                promptBuilder.AppendLine("2. If user provided meal preference, try to match it while using available ingredients");
                promptBuilder.AppendLine("3. For each meal, list which inventory items are used and how much");
                promptBuilder.AppendLine("4. List any ESSENTIAL missing ingredients needed (optional ingredients not needed)");
                promptBuilder.AppendLine("5. Calculate inventory match percentage (how much can be made from available items)");
                promptBuilder.AppendLine();

                promptBuilder.AppendLine("RETURN JSON IN THIS EXACT FORMAT:");
                promptBuilder.AppendLine(@"{
  ""reasoning"": ""Brief explanation of why these meals were chosen, emphasizing use of expiring items"",
  ""usedExpiringIngredients"": [""ingredient1"", ""ingredient2""],
  ""suggestions"": [
    {
      ""recipeId"": null,
      ""mealName"": ""Meal Name"",
      ""description"": ""Short description"",
      ""estimatedCalories"": 500,
      ""prepTime"": 30,
      ""reasoning"": ""Why this meal uses expiring items well"",
      ""isFromDatabase"": false,
      ""proteinG"": 25,
      ""carbsG"": 45,
      ""fatG"": 15,
      ""instructions"": ""Step 1... Step 2..."",
      ""inventoryMatchPercentage"": 80,
      ""ingredientsFromInventory"": [
        {""ingredientId"": """", ""name"": ""Chicken"", ""requiredAmount"": 200, ""availableAmount"": 300, ""unit"": ""g"", ""isExpiringSoon"": true, ""daysUntilExpiry"": 2}
      ],
      ""missingIngredients"": [
        {""ingredientName"": ""Soy Sauce"", ""neededAmount"": 30, ""unit"": ""ml"", ""isEssential"": true, ""substituteNote"": ""Can substitute with fish sauce""}
      ]
    }
  ]
}");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("IMPORTANT: Prioritize using expiring ingredients! Keep instructions under 200 chars.");

                var prompt = promptBuilder.ToString();
                var responseText = await CallGeminiAsync(prompt);

                return ParseInventoryMealSuggestions(responseText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate inventory-based meal suggestions");
                return new InventoryMealSuggestionResponse
                {
                    Reasoning = "Unable to generate suggestions due to system error",
                    Suggestions = new List<InventoryMealSuggestion>()
                };
            }
        }

        private InventoryMealSuggestionResponse ParseInventoryMealSuggestions(string responseText)
        {
            var response = new InventoryMealSuggestionResponse();
            
            try
            {
                _logger.LogInformation("Parsing inventory meal suggestions, response length: {Length}", responseText?.Length ?? 0);
                
                var jsonText = ExtractJsonFromResponse(responseText);
                
                if (jsonText == "{}" || string.IsNullOrWhiteSpace(jsonText))
                {
                    _logger.LogWarning("Empty JSON extracted for inventory suggestions");
                    return response;
                }
                
                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("reasoning", out var reasoning))
                    response.Reasoning = reasoning.GetString() ?? string.Empty;

                if (root.TryGetProperty("usedExpiringIngredients", out var usedExpiring))
                {
                    foreach (var item in usedExpiring.EnumerateArray())
                    {
                        response.UsedExpiringIngredients.Add(item.GetString() ?? string.Empty);
                    }
                }

                if (root.TryGetProperty("suggestions", out var suggestions))
                {
                    foreach (var suggestionElement in suggestions.EnumerateArray())
                    {
                        var suggestion = new InventoryMealSuggestion();

                        if (suggestionElement.TryGetProperty("recipeId", out var recipeId))
                        {
                            var recipeIdStr = recipeId.GetString();
                            if (!string.IsNullOrEmpty(recipeIdStr) && Guid.TryParse(recipeIdStr, out var guid))
                                suggestion.RecipeId = guid;
                        }

                        if (suggestionElement.TryGetProperty("mealName", out var name))
                            suggestion.MealName = name.GetString() ?? string.Empty;

                        if (suggestionElement.TryGetProperty("description", out var desc))
                            suggestion.Description = desc.GetString() ?? string.Empty;

                        if (suggestionElement.TryGetProperty("estimatedCalories", out var cal))
                            suggestion.EstimatedCalories = (float)cal.GetDouble();

                        if (suggestionElement.TryGetProperty("prepTime", out var time))
                            suggestion.PrepTime = time.GetInt32();

                        if (suggestionElement.TryGetProperty("reasoning", out var reasonText))
                            suggestion.Reasoning = reasonText.GetString() ?? string.Empty;

                        if (suggestionElement.TryGetProperty("isFromDatabase", out var fromDb))
                            suggestion.IsFromDatabase = fromDb.GetBoolean();

                        if (suggestionElement.TryGetProperty("proteinG", out var protein))
                            suggestion.ProteinG = (float)protein.GetDouble();

                        if (suggestionElement.TryGetProperty("carbsG", out var carbs))
                            suggestion.CarbsG = (float)carbs.GetDouble();

                        if (suggestionElement.TryGetProperty("fatG", out var fat))
                            suggestion.FatG = (float)fat.GetDouble();

                        if (suggestionElement.TryGetProperty("instructions", out var inst))
                            suggestion.Instructions = inst.GetString();

                        if (suggestionElement.TryGetProperty("inventoryMatchPercentage", out var matchPct))
                            suggestion.InventoryMatchPercentage = matchPct.GetInt32();

                        // Parse ingredients from inventory
                        if (suggestionElement.TryGetProperty("ingredientsFromInventory", out var invIngredients))
                        {
                            suggestion.IngredientsFromInventory = new List<MealIngredientUsage>();
                            foreach (var ing in invIngredients.EnumerateArray())
                            {
                                var usage = new MealIngredientUsage();
                                
                                if (ing.TryGetProperty("ingredientId", out var ingId))
                                {
                                    var idStr = ingId.GetString();
                                    if (!string.IsNullOrEmpty(idStr) && Guid.TryParse(idStr, out var ingGuid))
                                        usage.IngredientId = ingGuid;
                                }
                                
                                if (ing.TryGetProperty("name", out var ingName))
                                    usage.Name = ingName.GetString() ?? string.Empty;
                                
                                if (ing.TryGetProperty("requiredAmount", out var reqAmt))
                                    usage.RequiredAmount = (float)reqAmt.GetDouble();
                                
                                if (ing.TryGetProperty("availableAmount", out var avlAmt))
                                    usage.AvailableAmount = (float)avlAmt.GetDouble();
                                
                                if (ing.TryGetProperty("unit", out var unit))
                                    usage.Unit = unit.GetString() ?? string.Empty;
                                
                                if (ing.TryGetProperty("isExpiringSoon", out var expSoon))
                                    usage.IsExpiringSoon = expSoon.GetBoolean();
                                
                                if (ing.TryGetProperty("daysUntilExpiry", out var days))
                                    usage.DaysUntilExpiry = days.GetInt32();
                                
                                suggestion.IngredientsFromInventory.Add(usage);
                            }
                        }

                        // Parse missing ingredients (shopping list)
                        if (suggestionElement.TryGetProperty("missingIngredients", out var missingIngredients))
                        {
                            suggestion.MissingIngredients = new List<ShoppingListItem>();
                            foreach (var missing in missingIngredients.EnumerateArray())
                            {
                                var item = new ShoppingListItem();
                                
                                if (missing.TryGetProperty("ingredientName", out var missingName))
                                    item.IngredientName = missingName.GetString() ?? string.Empty;
                                
                                if (missing.TryGetProperty("neededAmount", out var neededAmt))
                                    item.NeededAmount = (float)neededAmt.GetDouble();
                                
                                if (missing.TryGetProperty("unit", out var missingUnit))
                                    item.Unit = missingUnit.GetString() ?? string.Empty;
                                
                                if (missing.TryGetProperty("isEssential", out var essential))
                                    item.IsEssential = essential.GetBoolean();
                                
                                if (missing.TryGetProperty("substituteNote", out var subNote))
                                    item.SubstituteNote = subNote.GetString();
                                
                                suggestion.MissingIngredients.Add(item);
                            }
                        }

                        response.Suggestions.Add(suggestion);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse inventory meal suggestions");
            }
            
            return response;
        }

        private DailyMealSuggestionResponse ParseDailyMealSuggestions(string responseText)
        {
            var response = new DailyMealSuggestionResponse();
            
            try
            {
                _logger.LogInformation("Raw AI response length: {Length} chars", responseText?.Length ?? 0);
                _logger.LogDebug("Full AI response: {Response}", responseText);
                
                var jsonText = ExtractJsonFromResponse(responseText);
                
                _logger.LogInformation("Extracted JSON length: {Length} chars", jsonText?.Length ?? 0);
                
                if (jsonText == "{}" || string.IsNullOrWhiteSpace(jsonText))
                {
                    _logger.LogWarning("Empty JSON extracted, returning empty response");
                    return response;
                }
                
                var jsonDoc = JsonDocument.Parse(jsonText);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("overallReasoning", out var reasoning))
                    response.Reasoning = reasoning.GetString() ?? string.Empty;

                if (root.TryGetProperty("suggestions", out var suggestions))
                {
                    foreach (var suggestionElement in suggestions.EnumerateArray())
                    {
                        var suggestion = new DailyMealSuggestion();

                        if (suggestionElement.TryGetProperty("recipeId", out var recipeId))
                        {
                            var recipeIdStr = recipeId.GetString();
                            if (!string.IsNullOrEmpty(recipeIdStr) && Guid.TryParse(recipeIdStr, out var guid))
                                suggestion.RecipeId = guid;
                        }

                        if (suggestionElement.TryGetProperty("mealName", out var name))
                            suggestion.MealName = name.GetString() ?? string.Empty;

                        if (suggestionElement.TryGetProperty("description", out var desc))
                            suggestion.Description = desc.GetString() ?? string.Empty;

                        if (suggestionElement.TryGetProperty("estimatedCalories", out var cal))
                            suggestion.EstimatedCalories = (float)cal.GetDouble();

                        if (suggestionElement.TryGetProperty("estimatedPrice", out var price))
                            suggestion.EstimatedPrice = (float)price.GetDouble();

                        if (suggestionElement.TryGetProperty("prepTime", out var time))
                            suggestion.PrepTime = time.GetInt32();

                        if (suggestionElement.TryGetProperty("reasoning", out var reasonText))
                            suggestion.Reasoning = reasonText.GetString() ?? string.Empty;

                        if (suggestionElement.TryGetProperty("isFromDatabase", out var fromDb))
                            suggestion.IsFromDatabase = fromDb.GetBoolean();

                        if (suggestionElement.TryGetProperty("proteinG", out var protein))
                            suggestion.ProteinG = (float)protein.GetDouble();

                        if (suggestionElement.TryGetProperty("carbsG", out var carbs))
                            suggestion.CarbsG = (float)carbs.GetDouble();

                        if (suggestionElement.TryGetProperty("fatG", out var fat))
                            suggestion.FatG = (float)fat.GetDouble();

                        if (suggestionElement.TryGetProperty("instructions", out var inst))
                            suggestion.Instructions = inst.GetString();

                        if (suggestionElement.TryGetProperty("ingredients", out var ingredients))
                        {
                            suggestion.Ingredients = new List<SuggestionIngredient>();
                            foreach (var ing in ingredients.EnumerateArray())
                            {
                                var ingredient = new SuggestionIngredient();
                                
                                if (ing.TryGetProperty("name", out var ingName))
                                    ingredient.Name = ingName.GetString() ?? string.Empty;
                                
                                if (ing.TryGetProperty("quantity", out var qty))
                                    ingredient.Quantity = (float)qty.GetDouble();
                                
                                if (ing.TryGetProperty("unit", out var unit))
                                    ingredient.Unit = unit.GetString() ?? string.Empty;
                                
                                suggestion.Ingredients.Add(ingredient);
                            }
                        }

                        if (suggestionElement.TryGetProperty("perPersonPortions", out var perPersonPortions))
                        {
                            suggestion.PerPersonPortions = new List<SuggestionPersonPortion>();
                            foreach (var portionElement in perPersonPortions.EnumerateArray())
                            {
                                var portion = new SuggestionPersonPortion();

                                if (portionElement.TryGetProperty("person", out var person))
                                    portion.Person = person.GetString() ?? string.Empty;

                                if (portionElement.TryGetProperty("portions", out var portions))
                                    portion.Portions = portions.GetInt32();

                                if (portionElement.TryGetProperty("quantityGuide", out var quantityGuide))
                                    portion.QuantityGuide = quantityGuide.GetString() ?? string.Empty;

                                if (portionElement.TryGetProperty("estimatedCalories", out var estimatedCalories))
                                    portion.EstimatedCalories = (float)estimatedCalories.GetDouble();

                                if (portionElement.TryGetProperty("note", out var note))
                                    portion.Note = note.GetString();

                                if (portion.Portions <= 0)
                                    portion.Portions = 1;

                                suggestion.PerPersonPortions.Add(portion);
                            }
                        }

                        response.Suggestions.Add(suggestion);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse daily meal suggestions");
            }
            
            return response;
        }

        private string ExtractJsonFromResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return "{}";
            
            // Remove markdown code blocks if present
            var text = responseText.Trim();
            
            // Handle ```json ... ``` format
            if (text.Contains("```json"))
            {
                var start = text.IndexOf("```json") + 7;
                var end = text.IndexOf("```", start);
                if (end > start)
                {
                    text = text.Substring(start, end - start).Trim();
                }
            }
            // Handle ``` ... ``` format (without json marker)
            else if (text.StartsWith("```") && text.EndsWith("```"))
            {
                text = text.Substring(3, text.Length - 6).Trim();
            }
            
            // Try to find JSON object (starts with {)
            var objectStart = text.IndexOf('{');
            var objectEnd = text.LastIndexOf('}');
            
            if (objectStart >= 0 && objectEnd > objectStart)
            {
                return text.Substring(objectStart, objectEnd - objectStart + 1);
            }
            
            // Try to find JSON array (starts with [)
            var arrayStart = text.IndexOf('[');
            var arrayEnd = text.LastIndexOf(']');
            
            if (arrayStart >= 0 && arrayEnd > arrayStart)
            {
                return text.Substring(arrayStart, arrayEnd - arrayStart + 1);
            }
            
            // Return empty object if no JSON found
            _logger.LogWarning("No valid JSON found in response: {Response}", 
                text.Length > 200 ? text.Substring(0, 200) : text);
            return "{}";
        }
    }
}
