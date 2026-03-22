using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Globalization;

namespace Meal_Preparation_System_RazorPage.Services
{
    public class GoogleSheetsExportService : IGoogleSheetsExportService
    {
        private readonly IConfiguration _configuration;

        public GoogleSheetsExportService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task ExportDashboardReportAsync(DashboardExportPayload payload)
        {
            var spreadsheetId = _configuration["GoogleSheets:SpreadsheetId"];
            var sheetName = _configuration["GoogleSheets:SheetName"] ?? "DashboardReport";
            var serviceAccountJson = _configuration["GoogleSheets:ServiceAccountJson"];
            var serviceAccountFile = _configuration["GoogleSheets:ServiceAccountFile"];

            if (string.IsNullOrWhiteSpace(spreadsheetId))
                throw new InvalidOperationException("GoogleSheets:SpreadsheetId is not configured.");

            GoogleCredential credential;
            if (!string.IsNullOrWhiteSpace(serviceAccountJson))
            {
                credential = GoogleCredential.FromJson(serviceAccountJson);
            }
            else if (!string.IsNullOrWhiteSpace(serviceAccountFile) && File.Exists(serviceAccountFile))
            {
                credential = GoogleCredential.FromFile(serviceAccountFile);
            }
            else
            {
                throw new InvalidOperationException("Google Sheets credentials not found. Configure GoogleSheets:ServiceAccountJson or GoogleSheets:ServiceAccountFile.");
            }

            if (credential.IsCreateScopedRequired)
            {
                credential = credential.CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MealPrep Admin Dashboard Export"
            });

            await EnsureSheetExistsAsync(service, spreadsheetId, sheetName);

            var values = BuildSheetValues(payload);
            var safeSheetName = EscapeSheetName(sheetName);
            var clearRequest = service.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, $"'{safeSheetName}'!A:Z");
            await clearRequest.ExecuteAsync();

            var body = new ValueRange { Values = values };
            var updateRequest = service.Spreadsheets.Values.Update(body, spreadsheetId, $"'{safeSheetName}'!A1");
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await updateRequest.ExecuteAsync();
        }

        private static async Task EnsureSheetExistsAsync(SheetsService service, string spreadsheetId, string sheetName)
        {
            var spreadsheet = await service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();
            var exists = spreadsheet.Sheets?.Any(s =>
                string.Equals(s.Properties?.Title, sheetName, StringComparison.OrdinalIgnoreCase)) == true;

            if (exists)
            {
                return;
            }

            var addSheetRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new()
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties
                            {
                                Title = sheetName
                            }
                        }
                    }
                }
            };

            await service.Spreadsheets.BatchUpdate(addSheetRequest, spreadsheetId).ExecuteAsync();
        }

        private static string EscapeSheetName(string sheetName)
        {
            return sheetName.Replace("'", "''");
        }

        private static IList<IList<object>> BuildSheetValues(DashboardExportPayload payload)
        {
            var rows = new List<IList<object>>
            {
                new List<object> { "MealPrep Admin Dashboard Report" },
                new List<object> { "Generated At", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) },
                new List<object>()
            };

            rows.Add(new List<object> { "Overview" });
            rows.Add(new List<object> { "Total Customers", payload.Stats.TotalCustomers });
            rows.Add(new List<object> { "Active Subscriptions", payload.Stats.ActiveSubscriptions });
            rows.Add(new List<object> { "Pending Orders", payload.Stats.PendingOrders });
            rows.Add(new List<object> { "Today Revenue", payload.Stats.TodayRevenue });
            rows.Add(new List<object> { "Current Month Revenue", payload.Stats.CurrentMonthRevenue });
            rows.Add(new List<object> { "Current Year Revenue", payload.Stats.CurrentYearRevenue });
            rows.Add(new List<object>());

            rows.Add(new List<object> { "Current Month Breakdown" });
            rows.Add(new List<object> { "Subscription Revenue", payload.CurrentMonthReport?.TotalSubscriptionRevenue ?? 0m });
            rows.Add(new List<object> { "Order Revenue", payload.CurrentMonthReport?.TotalOrderRevenue ?? 0m });
            rows.Add(new List<object> { "Total Revenue", payload.CurrentMonthReport?.TotalRevenue ?? payload.Stats.CurrentMonthRevenue });
            rows.Add(new List<object>());

            rows.Add(new List<object> { "Revenue Trend" });
            rows.Add(new List<object> { "Month", "Revenue" });
            rows.AddRange(payload.RevenueTrend.Select(item => new List<object> { item.Label, item.Revenue }));
            rows.Add(new List<object>());

            rows.Add(new List<object> { "Top Selling Dishes" });
            rows.Add(new List<object> { "Dish", "Quantity Sold", "Revenue" });
            rows.AddRange(payload.TopSellingDishes.Select(item => new List<object>
            {
                item.RecipeName,
                item.TotalQuantitySold,
                item.TotalRevenue
            }));

            return rows;
        }
    }
}
