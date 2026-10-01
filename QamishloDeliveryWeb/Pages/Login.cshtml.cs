using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

namespace QamishloDeliveryWeb.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LoginModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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
                var client =
                    _httpClientFactory.CreateClient(
                        "QamishloAPI");

                var request = new DeliveryLoginRequest
                {
                    Username = Username.Trim(),
                    Password = Password
                };

                var response =
                    await client.PostAsJsonAsync(
                        "api/DeliveryWorkers/login",
                        request);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage =
                        "Invalid username or password.";

                    return Page();
                }

                var result =
                    await response.Content
                        .ReadFromJsonAsync<DeliveryLoginResponse>();

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
                    result.Username ?? "");
                HttpContext.Session.SetString(
                     "DeliveryWorkerPhone",
                      result.Phone ?? "");

                return RedirectToPage("/Index");
            }
            catch (Exception)
            {
                ErrorMessage =
                    "Cannot connect to the restaurant server.";

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