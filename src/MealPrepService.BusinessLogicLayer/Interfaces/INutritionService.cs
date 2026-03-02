using MealPrepService.BusinessLogicLayer.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MealPrepService.BusinessLogicLayer.Interfaces
{
    public interface INutritionService
    {
        Task<NutritionResultDto> CalculateAsync(List<string> ingredients);
    }
}
