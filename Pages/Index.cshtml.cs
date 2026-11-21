using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QamishloDeliveryWeb.Pages
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // =====================================================
        // WORKER
        // =====================================================

        public int WorkerID { get; set; }

        public string WorkerName { get; set; } =
            "Delivery Worker";

        public string WorkerPhone { get; set; } =
            "";

        public double RestaurantLatitude { get; set; } = 36.120858;
        public double RestaurantLongitude { get; set; } = 44.061717;


        // =====================================================
        // ORDERS
        // =====================================================

        public List<DeliveryOrder> Orders { get; set; } =
            new List<DeliveryOrder>();

        public string OrdersLoadError { get; set; } = "";

        public List<TransferWorkerOption> TransferWorkers { get; set; } =
            new List<TransferWorkerOption>();


        // =====================================================
        // TODAY STATISTICS
        // =====================================================

        public int DeliveredOrdersToday { get; set; }

        public decimal TotalAmountToday { get; set; }


        // =====================================================
        // CURRENT ORDERS STATISTICS
        // =====================================================

        public int AssignedOrdersCount
        {
            get
            {
                return Orders.Count;
            }
        }

        public int ActiveDeliveriesCount
        {
            get
            {
                return Orders.Count(x =>
                    x.Status == "Preparing" ||
                    x.Status == "Ready" ||
                    x.Status == "OutForDelivery"
                );
            }
        }


        // =====================================================
        // GET
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            LoadWorkerSession();
            await LoadRestaurantCoordinatesAsync();

            if (WorkerID == 0 ||
                string.IsNullOrWhiteSpace(HttpContext.Session.GetString("DeliveryWorkerUsername")) ||
                string.IsNullOrWhiteSpace(HttpContext.Session.GetString("DeliveryWorkerPassword")))
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Login");
            }

            await LoadOrdersAsync();
            await LoadTodayStatisticsAsync();
            await LoadTransferTargetsAsync();

            return Page();
        }


        // =====================================================
        // LOAD WORKER SESSION
        // =====================================================

        
        private async Task LoadRestaurantCoordinatesAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("QamishloAPI");
                var resp = await client.GetAsync("api/settings");
                if (!resp.IsSuccessStatusCode) return;
                using var doc = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var root = doc.RootElement;
                if (root.TryGetProperty("restaurantLatitude", out var latEl) && latEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var lat = latEl.GetDouble();
                    if (Math.Abs(lat) > 0.001) RestaurantLatitude = lat;
                }
                if (root.TryGetProperty("restaurantLongitude", out var lngEl) && lngEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    var lng = lngEl.GetDouble();
                    if (Math.Abs(lng) > 0.001) RestaurantLongitude = lng;
                }
            }
            catch { }
        }

        private void LoadWorkerSession()
        {
            WorkerID =
                HttpContext.Session.GetInt32(
                    "DeliveryWorkerID"
                ) ?? 0;

            // V9: Auto-restore session from 30-day persistent cookie
            if (WorkerID == 0 && Request.Cookies.TryGetValue("QamishloDelivery.Remember", out var remB64) && !string.IsNullOrWhiteSpace(remB64))
            {
                try
                {
                    var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(remB64));
                    using var doc = System.Text.Json.JsonDocument.Parse(raw);
                    var r = doc.RootElement;
                    int cid = r.TryGetProperty("id", out var ie) ? ie.GetInt32() : 0;
                    string u = r.TryGetProperty("u", out var ue) ? ue.GetString() ?? "" : "";
                    string p = r.TryGetProperty("p", out var pe) ? pe.GetString() ?? "" : "";
                    string nm = r.TryGetProperty("name", out var ne) ? ne.GetString() ?? "عامل توصيل" : "عامل توصيل";
                    string ph = r.TryGetProperty("phone", out var phe) ? phe.GetString() ?? "" : "";
                    if (cid > 0 && !string.IsNullOrWhiteSpace(u) && !string.IsNullOrWhiteSpace(p))
                    {
                        HttpContext.Session.SetInt32("DeliveryWorkerID", cid);
                        HttpContext.Session.SetString("DeliveryWorkerName", nm);
                        HttpContext.Session.SetString("DeliveryWorkerPhone", ph);
                        HttpContext.Session.SetString("DeliveryWorkerUsername", u);
                        HttpContext.Session.SetString("DeliveryWorkerPassword", p);
                        WorkerID = cid;
                    }
                }
                catch { }
            }

            WorkerName =
                HttpContext.Session.GetString(
                    "DeliveryWorkerName"
                ) ?? "Delivery Worker";

            WorkerPhone =
                HttpContext.Session.GetString(
                    "DeliveryWorkerPhone"
                ) ?? "";
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


        // =====================================================
        // LOAD ORDERS
        // =====================================================

        private async Task LoadOrdersAsync()
        {
            try
            {
                HttpClient client = CreateWorkerApiClient();

                HttpResponseMessage response =
                    await client.GetAsync(
                        $"api/DeliveryWorkers/{WorkerID}/orders"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    string detail = await response.Content.ReadAsStringAsync();
                    if (detail.Length > 250) detail = detail[..250];
                    Orders = new List<DeliveryOrder>();
                    OrdersLoadError = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "Your delivery-worker session has expired. Please sign in again."
                        : $"Could not load orders ({(int)response.StatusCode}): {detail}";
                    TempData["StatusError"] = OrdersLoadError;
                    return;
                }

                WorkerOrdersResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<WorkerOrdersResponse>();

                if (
                    result != null &&
                    result.Success &&
                    result.Orders != null
                )
                {
                    // Delivered orders are kept exclusively in /Reports so the driver only sees active orders here
                    Orders = result.Orders
                        .Where(o => !string.Equals(o.Status, "Delivered", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    Orders = new List<DeliveryOrder>();
                    OrdersLoadError = result?.Message ?? "The API response did not contain a valid order list.";
                    TempData["StatusError"] = OrdersLoadError;
                }
            }
            catch (Exception ex)
            {
                Orders =
                    new List<DeliveryOrder>();

                OrdersLoadError = "Connection Error: " + ex.Message;
                TempData["StatusError"] = OrdersLoadError;
            }
        }


        // =====================================================
        // LOAD ACTIVE WORKERS FOR ORDER TRANSFER
        // =====================================================

        private async Task LoadTransferTargetsAsync()
        {
            try
            {
                using HttpClient client = CreateWorkerApiClient();
                using HttpResponseMessage response = await client.GetAsync(
                    $"api/deliveryworkers/{WorkerID}/transfer-targets");

                if (!response.IsSuccessStatusCode)
                {
                    string detail = await response.Content.ReadAsStringAsync();
                    if (detail.Length > 250) detail = detail[..250];
                    TransferWorkers = new List<TransferWorkerOption>();
                    TempData["TransferLoadError"] =
                        $"Could not load transfer targets ({(int)response.StatusCode}): {detail}";
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        TempData["StatusError"] = "Your worker session expired. Please sign in again.";
                    return;
                }

                TransferWorkerTargetsResponse? result =
                    await response.Content.ReadFromJsonAsync<TransferWorkerTargetsResponse>();

                TransferWorkers = result?.Success == true && result.Workers != null
                    ? result.Workers
                    : new List<TransferWorkerOption>();
            }
            catch
            {
                TransferWorkers = new List<TransferWorkerOption>();
            }
        }


        // =====================================================
        // LOAD TODAY STATISTICS
        // =====================================================

        private async Task LoadTodayStatisticsAsync()
        {
            try
            {
                HttpClient client = CreateWorkerApiClient();

                HttpResponseMessage response =
                    await client.GetAsync(
                        $"api/DeliveryWorkers/{WorkerID}/today-stats"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    DeliveredOrdersToday = 0;
                    TotalAmountToday = 0;

                    return;
                }

                TodayStatisticsResponse? result =
                    await response.Content
                        .ReadFromJsonAsync<TodayStatisticsResponse>();

                if (
                    result != null &&
                    result.Success
                )
                {
                    DeliveredOrdersToday =
                        result.DeliveredOrders;

                    TotalAmountToday =
                        result.TotalAmount;
                }
                else
                {
                    DeliveredOrdersToday = 0;
                    TotalAmountToday = 0;
                }
            }
            catch
            {
                DeliveredOrdersToday = 0;
                TotalAmountToday = 0;
            }
        }


        // =====================================================
        // UPDATE ORDER STATUS
        // =====================================================

        public async Task<IActionResult> OnPostStatusAsync(
            int orderID,
            string status)
        {
            LoadWorkerSession();

            if (WorkerID == 0)
            {
                return RedirectToPage("/Login");
            }

            if (
                orderID <= 0 ||
                string.IsNullOrWhiteSpace(status)
            )
            {
                TempData["StatusError"] =
                    "Invalid order or status.";

                return RedirectToPage();
            }

            string[] allowedStatuses =
            {
                "Preparing",
                "Ready",
                "OutForDelivery",
                "Delivered"
            };

            if (
                !allowedStatuses.Contains(
                    status,
                    StringComparer.OrdinalIgnoreCase
                )
            )
            {
                TempData["StatusError"] =
                    "Invalid order status.";

                return RedirectToPage();
            }

            try
            {
                HttpClient client = CreateWorkerApiClient();

                var request = new
                {
                    Status = status
                };

                HttpResponseMessage response =
                    await client.PostAsJsonAsync(
                        $"api/DeliveryWorkers/{WorkerID}/orders/{orderID}/status",
                        request
                    );

                if (response.IsSuccessStatusCode)
                {
                    TempData["StatusSuccess"] =
                        $"Order #{orderID} changed to {status}.";

                    return RedirectToPage();
                }

                string error =
                    await response.Content
                        .ReadAsStringAsync();

                TempData["StatusError"] =
                    $"API Error ({(int)response.StatusCode}): {error}";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["StatusError"] =
                    "Connection Error: " +
                    ex.Message;

                return RedirectToPage();
            }
        }


        // =====================================================
        // TRANSFER ORDER TO ANOTHER ACTIVE WORKER
        // =====================================================

        public async Task<IActionResult> OnPostTransferAsync(
            int orderID,
            int newWorkerID,
            string? notes)
        {
            LoadWorkerSession();
            if (WorkerID <= 0)
                return RedirectToPage("/Login");

            string username = HttpContext.Session.GetString("DeliveryWorkerUsername") ?? "";
            string password = HttpContext.Session.GetString("DeliveryWorkerPassword") ?? "";
            if (orderID <= 0 || newWorkerID <= 0 || newWorkerID == WorkerID ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["StatusError"] = "Invalid transfer details or expired login. Please sign in again.";
                return RedirectToPage();
            }

            try
            {
                using HttpClient client = CreateWorkerApiClient();
                var request = new
                {
                    NewWorkerID = newWorkerID,
                    Username = username,
                    Password = password,
                    Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
                };

                using HttpResponseMessage response = await client.PostAsJsonAsync(
                    $"api/deliveryworkers/{WorkerID}/orders/{orderID}/transfer",
                    request);

                string body = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    TempData["StatusSuccess"] = $"Order #{orderID} was transferred successfully.";
                }
                else
                {
                    string detail = body.Length > 350 ? body[..350] : body;
                    TempData["StatusError"] = $"Transfer failed ({(int)response.StatusCode}): {detail}";
                }
            }
            catch (Exception ex)
            {
                TempData["StatusError"] = "Could not connect to the restaurant server: " + ex.Message;
            }

            return RedirectToPage();
        }



        public sealed class GpsLocationPayload
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public async Task<IActionResult> OnPostUpdateLocationAsync()
        {
            LoadWorkerSession();
            if (WorkerID <= 0)
                return new JsonResult(new { success = false, message = "Not authenticated" }) { StatusCode = 401 };

            GpsLocationPayload? payload = null;
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                if (double.TryParse(form["latitude"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var la) &&
                    double.TryParse(form["longitude"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ln))
                {
                    payload = new GpsLocationPayload { Latitude = la, Longitude = ln };
                }
            }
            else
            {
                try { payload = await Request.ReadFromJsonAsync<GpsLocationPayload>(); } catch { }
            }

            if (payload == null)
                return new JsonResult(new { success = false, message = "Invalid coordinates" }) { StatusCode = 400 };

            try
            {
                using HttpClient client = CreateWorkerApiClient();
                using HttpResponseMessage resp = await client.PostAsJsonAsync(
                    $"api/deliveryworkers/{WorkerID}/location",
                    new { Latitude = payload.Latitude, Longitude = payload.Longitude });
                string body = await resp.Content.ReadAsStringAsync();
                return new ContentResult
                {
                    Content = string.IsNullOrWhiteSpace(body) ? "{\"success\":true}" : body,
                    ContentType = "application/json",
                    StatusCode = (int)resp.StatusCode
                };
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnGetActiveOrdersJsonAsync()
        {
            LoadWorkerSession();
            if (WorkerID <= 0)
                return new JsonResult(new { success = false, count = 0, orderIds = Array.Empty<int>() });

            await LoadOrdersAsync();
            var ids = Orders.Select(o => o.OrderID).ToList();
            return new JsonResult(new { success = true, count = ids.Count, orderIds = ids });
        }

        // =====================================================
        // WHATSAPP
        // =====================================================

        public string GetWhatsAppUrl(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "#";
            }

            string cleanPhone =
                new string(
                    phone
                        .Where(char.IsDigit)
                        .ToArray()
                );

            if (cleanPhone.StartsWith("0"))
            {
                cleanPhone =
                    "964" +
                    cleanPhone.Substring(1);
            }
            else if (
                cleanPhone.StartsWith("750") ||
                cleanPhone.StartsWith("751") ||
                cleanPhone.StartsWith("752") ||
                cleanPhone.StartsWith("770") ||
                cleanPhone.StartsWith("771") ||
                cleanPhone.StartsWith("780") ||
                cleanPhone.StartsWith("781")
            )
            {
                cleanPhone =
                    "964" + cleanPhone;
            }

            return
                "https://wa.me/" +
                cleanPhone;
        }
    }


    // =========================================================
    // API ORDERS RESPONSE
    // =========================================================

    public class WorkerOrdersResponse
    {
        public bool Success { get; set; }

        public int WorkerID { get; set; }

        public int Count { get; set; }

        public List<DeliveryOrder>? Orders { get; set; }

        public string? Message { get; set; }
    }


    // =========================================================
    // TODAY STATISTICS RESPONSE
    // =========================================================

    public class TodayStatisticsResponse
    {
        public bool Success { get; set; }

        public int WorkerID { get; set; }

        public string Date { get; set; } =
            "";

        public int DeliveredOrders { get; set; }

        public decimal TotalAmount { get; set; }
    }


    public class TransferWorkerTargetsResponse
    {
        public bool Success { get; set; }
        public List<TransferWorkerOption>? Workers { get; set; }
    }

    public class TransferWorkerOption
    {
        public int DeliveryWorkerID { get; set; }
        public string FullName { get; set; } = "";
        public string? Phone { get; set; }
    }


    // =========================================================
    // DELIVERY ORDER & ITEMS
    // =========================================================

    public class DeliveryOrderItem
    {
        public int OrderItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class DeliveryOrder
    {
        public int OrderID { get; set; }

        public int DailyOrderNumber { get; set; }

        public string OrderNumber { get; set; } = "";

        public int? CustomerID { get; set; }

        public string CustomerName { get; set; } =
            "";

        public string CustomerPhone { get; set; } =
            "";

        public int? DeliveryWorkerID { get; set; }

        public int? DeliveryZoneID { get; set; }

        public string ZoneName { get; set; } =
            "";

        public DateTime OrderDate { get; set; }

        public string OrderType { get; set; } =
            "";

        public string Status { get; set; } =
            "";

        public decimal Subtotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public string DeliveryAddress { get; set; } =
            "";

        public string Notes { get; set; } =
            "";

        public List<DeliveryOrderItem> Items { get; set; } =
            new List<DeliveryOrderItem>();

        public string DisplayOrderCode =>
            !string.IsNullOrWhiteSpace(OrderNumber)
                ? OrderNumber
                : DailyOrderNumber > 0
                    ? $"Q-{DailyOrderNumber}"
                    : $"#{OrderID}";

        public string CleanNotes =>
            string.IsNullOrWhiteSpace(Notes)
                ? ""
                : Regex.Replace(Notes, @"Location:\s*-?\d+(?:\.\d+)?\s*,\s*-?\d+(?:\.\d+)?", "", RegexOptions.IgnoreCase).Trim();

        public string ItemsSummaryText =>
            Items != null && Items.Count > 0
                ? string.Join(" • ", Items.Select(i => $"{i.ProductName} × {i.Quantity}"))
                : "";
    }
}