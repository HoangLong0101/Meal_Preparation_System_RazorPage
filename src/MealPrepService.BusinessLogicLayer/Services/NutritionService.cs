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

            sb.AppendLine("Return ONLY valid JSON.");
            sb.AppendLine("""
        {
          "ingredients": [],
          "totals": {},
          "advice": ""
        }
        """);

            sb.AppendLine("Ingredients:");
            foreach (var item in ingredients)
            {
                sb.AppendLine(item.Replace("{", "").Replace("}", ""));
            }

            return sb.ToString();
        }

        private NutritionResultDto ParseResponse(string responseText)
        {
            using var doc = JsonDocument.Parse(responseText);

            var aiText = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            using var jsonDoc = JsonDocument.Parse(aiText!);

            var result = new NutritionResultDto();

            var totals = jsonDoc.RootElement.GetProperty("totals");

            result.TotalCalories = totals.GetProperty("calories").GetSingle();
            result.TotalProteinG = totals.GetProperty("protein_g").GetSingle();
            result.TotalCarbsG = totals.GetProperty("carbs_g").GetSingle();
            result.TotalFatG = totals.GetProperty("fat_g").GetSingle();

            result.Advice = jsonDoc.RootElement.GetProperty("advice").GetString() ?? "";

            return result;
        }
    }
}
