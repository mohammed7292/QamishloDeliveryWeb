using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QamishloDeliveryWeb.Pages
{
    public class ReportsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ReportsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public int WorkerID { get; private set; }

        public string WorkerName { get; private set; } =
            "Delivery Worker";

        public int DeliveredToday { get; private set; }

        public decimal TotalAmountToday { get; private set; }

        public List<DeliveryOrder> DeliveredOrders { get; private set; } =
            new();

        public bool OrdersLoaded { get; private set; }

        public string ErrorMessage { get; private set; } = "";

        public decimal AverageOrderValue =>
            DeliveredToday > 0
                ? TotalAmountToday / DeliveredToday
                : 0;

        public async Task<IActionResult> OnGetAsync()
        {
            WorkerID =
                HttpContext.Session.GetInt32("DeliveryWorkerID") ?? 0;

            WorkerName =
                HttpContext.Session.GetString("DeliveryWorkerName")
                ?? "Delivery Worker";

            if (WorkerID == 0)
            {
                return RedirectToPage("/Login");
            }

            await LoadTodayStatisticsAsync();
            await LoadDeliveredOrdersAsync();

            return Page();
        }

        private async Task LoadTodayStatisticsAsync()
        {
            try
            {
                var client =
                    _httpClientFactory.CreateClient("QamishloAPI");

                var response = await client.GetAsync(
                    $"api/DeliveryWorkers/{WorkerID}/today-stats");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage =
                        "Could not load today's statistics.";
                    return;
                }

                var result =
                    await response.Content
                        .ReadFromJsonAsync<TodayStatisticsResponse>();

                if (result?.Success == true)
                {
                    DeliveredToday = result.DeliveredOrders;
                    TotalAmountToday = result.TotalAmount;
                }
                else
                {
                    ErrorMessage =
                        "Today's statistics were not available.";
                }
            }
            catch
            {
                ErrorMessage =
                    "Could not connect to the restaurant server.";
            }
        }

        private async Task LoadDeliveredOrdersAsync()
        {
            try
            {
                var client =
                    _httpClientFactory.CreateClient("QamishloAPI");

                var response = await client.GetAsync(
                    $"api/DeliveryWorkers/{WorkerID}/orders");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage =
                        "Could not load the order history.";
                    return;
                }

                var result =
                    await response.Content
                        .ReadFromJsonAsync<WorkerOrdersResponse>();

                if (result?.Success == true && result.Orders != null)
                {
                    DeliveredOrders = result.Orders
                        .Where(order =>
                            string.Equals(
                                order.Status,
                                "Delivered",
                                StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(order => order.OrderDate)
                        .ToList();

                    OrdersLoaded = true;
                }
                else
                {
                    ErrorMessage =
                        "The order history was not available.";
                }
            }
            catch
            {
                ErrorMessage =
                    "Could not connect to the restaurant server.";
            }
        }
    }
}