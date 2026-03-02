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

            sb.AppendLine("You are a nutrition expert. Analyze the following ingredients and return ONLY a valid JSON response with no additional text.");
            sb.AppendLine();
            sb.AppendLine("Return JSON in this EXACT format:");
            sb.AppendLine("""
{
  "ingredients": [
    {
      "name": "chicken breast",
      "amount": 250,
      "unit": "g",
      "calories": 275,
      "protein_g": 52,
      "carbs_g": 0,
      "fat_g": 6
    }
  ],
  "totals": {
    "calories": 275,
    "protein_g": 52,
    "carbs_g": 0,
    "fat_g": 6
  },
  "advice": "Good protein source with minimal carbs."
}
""");

            sb.AppendLine("Analyze these ingredients and provide nutritional values per 100g or as stated:");
            foreach (var item in ingredients)
            {
                sb.AppendLine($"- {item}");
            }

            sb.AppendLine();
            sb.AppendLine("Important:");
            sb.AppendLine("- Return ONLY the JSON, no markdown, no code blocks, no explanations");
            sb.AppendLine("- All numeric values must be accurate");
            sb.AppendLine("- Advice should be a single sentence with practical nutrition tips");

            return sb.ToString();
        }

        private NutritionResultDto ParseResponse(string responseText)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseText);

                var aiText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(aiText))
                    throw new InvalidOperationException("AI returned empty response.");

                using var jsonDoc = JsonDocument.Parse(aiText);

                var result = new NutritionResultDto();

                // Parse totals
                if (jsonDoc.RootElement.TryGetProperty("totals", out var totalsElement))
                {
                    result.TotalCalories = GetSingleValue(totalsElement, "calories");
                    result.TotalProteinG = GetSingleValue(totalsElement, "protein_g");
                    result.TotalCarbsG = GetSingleValue(totalsElement, "carbs_g");
                    result.TotalFatG = GetSingleValue(totalsElement, "fat_g");
                }
                else
                {
                    throw new InvalidOperationException("Missing 'totals' in AI response.");
                }

                // Parse advice
                if (jsonDoc.RootElement.TryGetProperty("advice", out var adviceElement))
                {
                    result.Advice = adviceElement.GetString() ?? "";
                }

                // Parse ingredients
                if (jsonDoc.RootElement.TryGetProperty("ingredients", out var ingredientsArray))
                {
                    if (ingredientsArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ingredient in ingredientsArray.EnumerateArray())
                        {
                            var ingredientDto = new IngredientNutritionDto
                            {
                                Name = GetStringValue(ingredient, "name"),
                                Amount = GetSingleValue(ingredient, "amount"),
                                Unit = GetStringValue(ingredient, "unit"),
                                Calories = GetSingleValue(ingredient, "calories"),
                                ProteinG = GetSingleValue(ingredient, "protein_g"),
                                CarbsG = GetSingleValue(ingredient, "carbs_g"),
                                FatG = GetSingleValue(ingredient, "fat_g")
                            };

                            result.Ingredients.Add(ingredientDto);
                        }
                    }
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse AI response as JSON.", ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new InvalidOperationException("Required field missing in AI response.", ex);
            }
        }

        private static float GetSingleValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.Number)
                    return property.GetSingle();
            }
            return 0f;
        }

        private static string GetStringValue(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.GetString() ?? "";
            }
            return "";
        }
    }
}
