using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class NutritionService : INutritionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public NutritionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<NutritionResultDto> CalculateAsync(List<string> ingredients)
        {
            if (ingredients == null || !ingredients.Any())
                throw new ArgumentException("At least one ingredient required.");

            if (ingredients.Count > 10)
                throw new ArgumentException("Maximum 10 ingredients allowed.");

            var apiKey = _configuration["AI:Gemini:ApiKey"];
            var model = _configuration["AI:Gemini:Model"] ?? "gemini-1.5-flash";

            var endpoint =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var prompt = BuildPrompt(ingredients);

            var request = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            if (!response.IsSuccessStatusCode)
                throw new Exception("AI service unavailable.");

            var responseText = await response.Content.ReadAsStringAsync();

            return ParseResponse(responseText);
        }

        private string BuildPrompt(List<string> ingredients)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a professional nutrition analyst. Analyze the following ingredients and:");
            sb.AppendLine("1. Calculate detailed nutrition for each ingredient");
            sb.AppendLine("2. Rate the overall meal quality (Poor / Below Average / Average / Good / Excellent)");
            sb.AppendLine("3. Suggest 2-5 ingredients the user should ADD to make this a complete, balanced meal");
            sb.AppendLine("   - Focus on missing macronutrients (protein, carbs, fat, fiber)");
            sb.AppendLine("   - Consider missing food groups (vegetables, grains, protein source, healthy fats)");
            sb.AppendLine("   - Each suggestion must include a category: Protein, Vegetable, Grain, Fruit, Dairy, Healthy Fat, or Fiber");
            sb.AppendLine();
            sb.AppendLine("Return ONLY valid JSON matching this exact schema:");
            sb.AppendLine("""
        {
          "ingredients": [
            {
              "name": "ingredient name",
              "amount": 100,
              "unit": "g",
              "calories": 200,
              "protein_g": 10.5,
              "carbs_g": 25.0,
              "fat_g": 5.0
            }
          ],
          "totals": {
            "calories": 200,
            "protein_g": 10.5,
            "carbs_g": 25.0,
            "fat_g": 5.0
          },
          "meal_rating": "Good",
          "suggestions": [
            {
              "ingredient": "broccoli",
              "amount": "150g",
              "reason": "Adds fiber, vitamin C, and micronutrients missing from main protein",
              "category": "Vegetable"
            }
          ],
          "advice": "Brief overall nutrition advice about the meal and suggestions"
        }
        """);

            sb.AppendLine("Ingredients to analyze:");
            foreach (var item in ingredients)
            {
                sb.AppendLine("- " + item.Replace("{", "").Replace("}", ""));
            }

            return sb.ToString();
        }

        private NutritionResultDto ParseResponse(string responseText)
        {
            using var doc = JsonDocument.Parse(responseText);

            // Gemini 2.5 Flash may include a "thought" part before the actual text.
            // Iterate parts backwards to find the last non-thought text part.
            var parts = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            string? aiText = null;
            for (int i = parts.GetArrayLength() - 1; i >= 0; i--)
            {
                var part = parts[i];
                if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                    continue;
                if (part.TryGetProperty("text", out var textProp))
                {
                    aiText = textProp.GetString();
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(aiText))
                throw new Exception("AI returned no content.");

            using var jsonDoc = JsonDocument.Parse(aiText);

            var result = new NutritionResultDto();

            var totals = jsonDoc.RootElement.GetProperty("totals");

            result.TotalCalories = GetFloatSafe(totals, "calories");
            result.TotalProteinG = GetFloatSafe(totals, "protein_g");
            result.TotalCarbsG = GetFloatSafe(totals, "carbs_g");
            result.TotalFatG = GetFloatSafe(totals, "fat_g");

            result.Advice = jsonDoc.RootElement.TryGetProperty("advice", out var adviceProp)
                ? adviceProp.GetString() ?? ""
                : "";

            result.MealRating = jsonDoc.RootElement.TryGetProperty("meal_rating", out var ratingProp)
                ? ratingProp.GetString() ?? ""
                : "";

            if (jsonDoc.RootElement.TryGetProperty("suggestions", out var suggestionsArr))
            {
                foreach (var sug in suggestionsArr.EnumerateArray())
                {
                    result.Suggestions.Add(new MealImprovementDto
                    {
                        Ingredient = sug.TryGetProperty("ingredient", out var ing) ? ing.GetString() ?? "" : "",
                        Amount = sug.TryGetProperty("amount", out var amt) ? amt.GetString() ?? "" : "",
                        Reason = sug.TryGetProperty("reason", out var rsn) ? rsn.GetString() ?? "" : "",
                        Category = sug.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : ""
                    });
                }
            }

            if (jsonDoc.RootElement.TryGetProperty("ingredients", out var ingredientsArr))
            {
                foreach (var ing in ingredientsArr.EnumerateArray())
                {
                    result.Ingredients.Add(new IngredientNutritionDto
                    {
                        Name = ing.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Amount = GetFloatSafe(ing, "amount"),
                        Unit = ing.TryGetProperty("unit", out var u) ? u.GetString() ?? "" : "",
                        Calories = GetFloatSafe(ing, "calories"),
                        ProteinG = GetFloatSafe(ing, "protein_g"),
                        CarbsG = GetFloatSafe(ing, "carbs_g"),
                        FatG = GetFloatSafe(ing, "fat_g")
                    });
                }
            }

            return result;
        }

        private static float GetFloatSafe(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return 0f;
            return prop.ValueKind switch
            {
                JsonValueKind.Number => prop.GetSingle(),
                JsonValueKind.String => float.TryParse(prop.GetString(), out var v) ? v : 0f,
                _ => 0f
            };
        }
    }
}
