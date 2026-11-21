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

        public string WorkerName { get; private set; } = "Delivery Worker";

        public int DeliveredToday { get; private set; }

        public decimal TotalAmountToday { get; private set; }

        public int TotalDeliveredAllTime => DeliveredOrders.Count;

        public decimal TotalAmountAllTime => DeliveredOrders.Sum(o => o.TotalAmount);

        public List<DeliveryOrder> DeliveredOrders { get; private set; } = new();

        public bool OrdersLoaded { get; private set; }

        public string ErrorMessage { get; private set; } = "";

        public decimal AverageOrderValue =>
            DeliveredToday > 0
                ? TotalAmountToday / DeliveredToday
                : (DeliveredOrders.Count > 0 ? TotalAmountAllTime / DeliveredOrders.Count : 0);

        public async Task<IActionResult> OnGetAsync()
        {
            WorkerID = HttpContext.Session.GetInt32("DeliveryWorkerID") ?? 0;
            WorkerName = HttpContext.Session.GetString("DeliveryWorkerName") ?? "Delivery Worker";

            if (WorkerID == 0 ||
                string.IsNullOrWhiteSpace(HttpContext.Session.GetString("DeliveryWorkerUsername")) ||
                string.IsNullOrWhiteSpace(HttpContext.Session.GetString("DeliveryWorkerPassword")))
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Login");
            }

            await LoadTodayStatisticsAsync();
            await LoadDeliveredOrdersAsync();

            return Page();
        }

        private HttpClient CreateWorkerApiClient()
        {
            HttpClient client = _httpClientFactory.CreateClient("QamishloAPI");
            string username = HttpContext.Session.GetString("DeliveryWorkerUsername") ?? "";
            string password = HttpContext.Session.GetString("DeliveryWorkerPassword") ?? "";

            client.DefaultRequestHeaders.Remove("X-Delivery-Worker-Username");
            client.DefaultRequestHeaders.Remove("X-Delivery-Worker-Password");

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                client.DefaultRequestHeaders.Add("X-Delivery-Worker-Username", username);
                client.DefaultRequestHeaders.Add("X-Delivery-Worker-Password", password);
            }

            return client;
        }

        private async Task LoadTodayStatisticsAsync()
        {
            try
            {
                using HttpClient client = CreateWorkerApiClient();
                using HttpResponseMessage response = await client.GetAsync(
                    $"api/DeliveryWorkers/{WorkerID}/today-stats");

                if (!response.IsSuccessStatusCode)
                    return;

                var result = await response.Content.ReadFromJsonAsync<TodayStatisticsResponse>();
                if (result?.Success == true)
                {
                    DeliveredToday = result.DeliveredOrders;
                    TotalAmountToday = result.TotalAmount;
                }
            }
            catch
            {
            }
        }

        private async Task LoadDeliveredOrdersAsync()
        {
            try
            {
                using HttpClient client = CreateWorkerApiClient();

                // 1. Call canonical /history endpoint (supported across all API versions)
                string[] candidateEndpoints = new[]
                {
                    $"api/DeliveryWorkers/{WorkerID}/history?pageSize=100",
                    $"api/DeliveryWorkers/{WorkerID}/delivered-orders",
                    $"api/DeliveryWorkers/{WorkerID}/orders"
                };

                foreach (string endpoint in candidateEndpoints)
                {
                    using HttpResponseMessage response = await client.GetAsync(endpoint);
                    if (!response.IsSuccessStatusCode)
                        continue;

                    var result = await response.Content.ReadFromJsonAsync<WorkerOrdersResponse>();
                    if (result?.Success == true && result.Orders != null)
                    {
                        DeliveredOrders = result.Orders
                            .Where(order => string.Equals(order.Status, "Delivered", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(order => order.OrderDate)
                            .ThenByDescending(order => order.OrderID)
                            .ToList();

                        OrdersLoaded = true;
                        ErrorMessage = "";
                        return;
                    }
                }

                ErrorMessage = "Could not load the delivered order history from the server.";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not connect to the restaurant server: " + ex.Message;
            }
        }
    }
}
