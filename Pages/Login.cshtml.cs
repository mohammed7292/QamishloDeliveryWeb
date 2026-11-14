using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace QamishloDeliveryWeb.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IHttpClientFactory httpClientFactory, ILogger<LoginModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty]
        public string Username { get; set; } = "";

        [BindProperty]
        public string Password { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage =
                    "Please enter username and password.";

                return Page();
            }

            try
            {
                using var client = _httpClientFactory.CreateClient("QamishloAPI");

                var request = new DeliveryLoginRequest
                {
                    Username = Username.Trim(),
                    Password = Password
                };

                using var response = await client.PostAsJsonAsync(
                    "api/DeliveryWorkers/login", request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة، أو أن الحساب غير نشط.";
                    return Page();
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Delivery login API returned HTTP {StatusCode}.", (int)response.StatusCode);
                    ErrorMessage = "خادم المطعم غير متاح حالياً. حاول بعد قليل.";
                    return Page();
                }

                var result = await response.Content.ReadFromJsonAsync<DeliveryLoginResponse>();

                if (result == null || !result.Success)
                {
                    ErrorMessage =
                        result?.Message ??
                        "Invalid username or password.";

                    return Page();
                }

                HttpContext.Session.SetInt32(
                    "DeliveryWorkerID",
                    result.DeliveryWorkerID);

                HttpContext.Session.SetString(
                    "DeliveryWorkerName",
                    result.FullName ?? "");

                HttpContext.Session.SetString(
                    "DeliveryWorkerUsername",
                    result.Username ?? Username.Trim());
                HttpContext.Session.SetString(
                    "DeliveryWorkerPassword",
                    Password);
                HttpContext.Session.SetString(
                    "DeliveryWorkerPhone",
                    result.Phone ?? "");

                return RedirectToPage("/Index");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Delivery login request timed out.");
                ErrorMessage = "انتهت مهلة الاتصال بخادم المطعم. تحقق من الإنترنت وحاول مجدداً.";
                return Page();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Delivery login could not reach the restaurant API.");
                ErrorMessage = "تعذر الاتصال بخادم المطعم. تحقق من اتصال الإنترنت وحاول مجدداً.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected delivery login error.");
                ErrorMessage = "حدث خطأ غير متوقع أثناء تسجيل الدخول.";
                return Page();
            }
        }
    }

    public class DeliveryLoginRequest
    {
        public string Username { get; set; } = "";

        public string Password { get; set; } = "";
    }

    public class DeliveryLoginResponse
    {
        public bool Success { get; set; }

        public int DeliveryWorkerID { get; set; }

        public string? FullName { get; set; }

        public string? Username { get; set; }

        public string? Phone { get; set; }

        public string? Message { get; set; }
    }
}