# Meal Preparation System

A comprehensive meal preparation and planning system built with ASP.NET Core Razor Pages and AI-powered recommendations.

## Features

- **AI-Powered Meal Recommendations**: Get intelligent meal suggestions based on your health profile and preferences
- **Personalized Nutrition**: Recommendations tailored to your dietary needs and health goals
- **Allergen Safety**: Automatically filters out recipes containing your allergens
- **Weekly Meal Planning**: Generate complete meal plans for multiple days
- **Meal Diversity**: Ensures variety by avoiding recently consumed recipes
- **Nutritional Tracking**: View detailed nutritional information for each recommendation

## Technology Stack

- **Frontend**: ASP.NET Core Razor Pages (.NET 9)
- **Backend**: Business Logic Layer with service pattern
- **Data Access**: Entity Framework Core with Unit of Work and Repository patterns
- **AI Integration**: OpenAI GPT for intelligent recommendations
- **Payment Processing**: VNPay integration

## Project Structure

```
Meal_Preparation_System_RazorPage/
„¥„Ÿ„Ÿ Pages/                          # Razor Pages
„    „¥„Ÿ„Ÿ Index.cshtml               # Home page
„    „¥„Ÿ„Ÿ Recommendations.cshtml     # Meal recommendations page
„    „¥„Ÿ„Ÿ MealPlanner.cshtml         # Weekly meal planner
„    „¤„Ÿ„Ÿ Shared/                    # Shared layouts and partials
„¥„Ÿ„Ÿ wwwroot/                       # Static files (CSS, JS, images)
„¤„Ÿ„Ÿ Program.cs                     # Application configuration

MealPrepService.BusinessLogicLayer/
„¥„Ÿ„Ÿ Interfaces/                    # Service interfaces
„¥„Ÿ„Ÿ Services/                      # Business logic implementations
„¤„Ÿ„Ÿ DTOs/                          # Data Transfer Objects

MealPrepService.DataAccessLayer/
„¥„Ÿ„Ÿ Entities/                      # Database entities
„¤„Ÿ„Ÿ Repositories/                  # Data access repositories
```

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server or SQL Server Express
- OpenAI API key (for AI recommendations)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/YOUR_USERNAME/Meal_Preparation_System.git
   cd Meal_Preparation_System
   ```

2. Update the connection string in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=MealPrepDB;Trusted_Connection=True;"
     }
   }
   ```

3. Configure AI settings in `appsettings.json`:
   ```json
   {
     "AI": {
       "UseRealAI": true,
       "OpenAIKey": "your-openai-api-key-here"
     }
   }
   ```

4. Run database migrations:
   ```bash
   dotnet ef database update
   ```

5. Run the application:
   ```bash
   cd Meal_Preparation_System_RazorPage
   dotnet run
   ```

6. Navigate to `https://localhost:5001` in your browser

## Usage

### Get Meal Recommendations

1. Navigate to the **Recommendations** page
2. Enter your Customer ID
3. Choose between:
   - **General Recommendations**: Get a variety of meal suggestions
   - **Specific Recommendations**: Select a date and meal type (breakfast, lunch, dinner)
4. View your personalized recommendations with nutritional information

### Generate a Meal Plan

1. Navigate to the **Meal Planner** page
2. Enter your Customer ID
3. Select start date and number of days (1-30)
4. Click **Generate Meal Plan** to create a complete meal plan with breakfast, lunch, and dinner for each day

## Configuration

### Service Registration

All services are registered in `Program.cs` using dependency injection:

- **Repositories**: Unit of Work pattern with specialized repositories
- **Business Services**: Account, Order, Menu, Revenue, AI services
- **AI Engine**: Recommendation engine with profile analyzer

### Session Configuration

Session timeout is set to 30 minutes and can be configured in `Program.cs`.

## Architecture

### Razor Pages Pattern

Each page consists of two files:
- `.cshtml`: The view (HTML + Razor syntax)
- `.cshtml.cs`: The PageModel (controller logic)

### PageModel Structure

```csharp
public class MyPageModel : PageModel
{
    // Dependency injection
    private readonly IMyService _myService;
    
    public MyPageModel(IMyService myService)
    {
        _myService = myService;
    }
    
    // Bind properties for form data
    [BindProperty]
    public string MyProperty { get; set; }
    
    // Display properties
    public List<MyData> Data { get; set; }
    
    // GET handler
    public async Task OnGetAsync()
    {
        // Load data
    }
    
    // POST handler
    public async Task<IActionResult> OnPostAsync()
    {
        // Process form submission
        return Page();
    }
}
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contact

Project Link: [https://github.com/YOUR_USERNAME/Meal_Preparation_System](https://github.com/YOUR_USERNAME/Meal_Preparation_System)

## Acknowledgments

- Built with ASP.NET Core Razor Pages
- AI recommendations powered by OpenAI GPT
- Bootstrap for UI components
