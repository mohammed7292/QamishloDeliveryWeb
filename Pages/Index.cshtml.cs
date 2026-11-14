using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QamishloDeliveryWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // =====================================================
        // WORKER
        // =====================================================

        public int WorkerID { get; set; }

        public string WorkerName { get; set; } =
            "Delivery Worker";

        public string WorkerPhone { get; set; } =
            "";


        // =====================================================
        // ORDERS
        // =====================================================

        public List<DeliveryOrder> Orders { get; set; } =
            new List<DeliveryOrder>();

        public List<DeliveryOrder> TodayOrders { get; set; } =
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

            if (WorkerID == 0 ||
                string.IsNullOrWhiteSpace(HttpContext.Session.GetString("DeliveryWorkerUsername")) ||
                string.IsNullOrWhiteSpace(HttpContext.Session.GetString("DeliveryWorkerPassword")))
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/Login");
            }

            await LoadOrdersAsync();
            await LoadTodayOrdersAsync();
            await LoadTodayStatisticsAsync();
            await LoadTransferTargetsAsync();

            return Page();
        }


        // =====================================================
        // LOAD WORKER SESSION
        // =====================================================

        private void LoadWorkerSession()
        {
            WorkerID =
                HttpContext.Session.GetInt32(
                    "DeliveryWorkerID"
                ) ?? 0;

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
                    Orders = result.Orders;
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
        // LOAD ALL ORDERS ASSIGNED TO THIS DRIVER TODAY
        // =====================================================

        private async Task LoadTodayOrdersAsync()
        {
            try
            {
                using HttpClient client = CreateWorkerApiClient();
                using HttpResponseMessage response = await client.GetAsync(
                    $"api/deliveryworkers/{WorkerID}/today-orders");
                if (!response.IsSuccessStatusCode)
                {
                    TodayOrders = new List<DeliveryOrder>();
                    return;
                }
                WorkerOrdersResponse? result = await response.Content.ReadFromJsonAsync<WorkerOrdersResponse>();
                TodayOrders = result?.Success == true && result.Orders != null
                    ? result.Orders
                    : new List<DeliveryOrder>();
            }
            catch
            {
                TodayOrders = new List<DeliveryOrder>();
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
        // END SHIFT
        // =====================================================

        public async Task<IActionResult> OnPostEndShiftAsync()
        {
            LoadWorkerSession();
            if (WorkerID <= 0) return RedirectToPage("/Login");
            try
            {
                using HttpClient client = CreateWorkerApiClient();
                using HttpResponseMessage response = await client.PostAsync(
                    $"api/deliveryworkers/{WorkerID}/end-shift", null);
                string content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "End-shift request for worker {WorkerID} returned HTTP {StatusCode}: {Body}",
                        WorkerID, (int)response.StatusCode, SafeLogBody(content));
                    string apiMessage = TryReadApiMessage(content);
                    TempData["StatusError"] = !string.IsNullOrWhiteSpace(apiMessage)
                        ? apiMessage
                        : response.StatusCode == System.Net.HttpStatusCode.Conflict
                            ? "لا يمكن إنهاء الدوام قبل تسليم الطلبات النشطة أو تحويلها."
                            : $"تعذر إنهاء الدوام (HTTP {(int)response.StatusCode}). حاول مجدداً.";
                    return RedirectToPage();
                }
                HttpContext.Session.Clear();
                TempData["LoginSuccess"] = "تم تسجيل نهاية الدوام بنجاح.";
                return RedirectToPage("/Login");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "End-shift request timed out for worker {WorkerID}.", WorkerID);
                TempData["StatusError"] = "انتهت مهلة الاتصال أثناء إنهاء الدوام. لم نتمكن من تأكيد العملية؛ حدّث الصفحة وتحقق من حالة الدوام.";
                return RedirectToPage();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "End-shift API connection failed for worker {WorkerID}.", WorkerID);
                TempData["StatusError"] = "تعذر الاتصال بخادم المطعم أثناء إنهاء الدوام. تحقق من الإنترنت وحاول مجدداً.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected end-shift error for worker {WorkerID}.", WorkerID);
                TempData["StatusError"] = "حدث خطأ غير متوقع أثناء إنهاء الدوام.";
                return RedirectToPage();
            }
        }

        // Browser geolocation is forwarded through this same-origin Razor handler.
        public async Task<IActionResult> OnPostLocationAsync(double latitude, double longitude)
        {
            LoadWorkerSession();
            if (WorkerID <= 0) return Unauthorized();
            if (!double.IsFinite(latitude) || !double.IsFinite(longitude) ||
                latitude is < -90 or > 90 || longitude is < -180 or > 180)
                return BadRequest(new { success = false, message = "إحداثيات GPS غير صالحة." });
            try
            {
                using HttpClient client = CreateWorkerApiClient();
                using HttpResponseMessage response = await client.PostAsJsonAsync(
                    $"api/deliveryworkers/{WorkerID}/location",
                    new { Latitude = latitude, Longitude = longitude });
                string body = await response.Content.ReadAsStringAsync();
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    ContentType = "application/json",
                    Content = string.IsNullOrWhiteSpace(body) ? "{}" : body
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "GPS forwarding timed out for worker {WorkerID}.", WorkerID);
                return StatusCode(504, new { success = false, message = "انتهت مهلة إرسال الموقع. سيُعاد الإرسال عند توفر الاتصال." });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "GPS forwarding failed for worker {WorkerID}.", WorkerID);
                return StatusCode(503, new { success = false, message = "تعذر الاتصال بخادم المطعم لإرسال الموقع." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected GPS forwarding error for worker {WorkerID}.", WorkerID);
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء معالجة موقع GPS." });
            }
        }

        private static string SafeLogBody(string body)
            => string.IsNullOrWhiteSpace(body) ? string.Empty : body[..Math.Min(body.Length, 500)];

        private static string TryReadApiMessage(string body)
        {
            try
            {
                using var json = System.Text.Json.JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty("message", out var message) &&
                    message.ValueKind == System.Text.Json.JsonValueKind.String)
                    return message.GetString() ?? string.Empty;
            }
            catch { }
            return string.Empty;
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
    // DELIVERY ORDER
    // =========================================================

    public class DeliveryOrder
    {
        public int OrderID { get; set; }

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
    }
}