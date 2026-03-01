namespace MealPrepService.BusinessLogicLayer.DTOs;

public class AllergyDto
{
    public Guid Id { get; set; }
    public string AllergyName { get; set; } = string.Empty;
}

public class CreateAllergyDto
{
    public string AllergyName { get; set; } = string.Empty;
}

public class UpdateAllergyDto
{
    public Guid Id { get; set; }
    public string AllergyName { get; set; } = string.Empty;
}
