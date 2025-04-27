using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using PersonalDiaryApp.UI.Helpers;
using PersonalDiaryApp.UI.Models;
using System.Net.Http.Headers;
using System.Text;

namespace PersonalDiaryApp.UI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ✅ Giriş Sayfası (GET)
        [HttpGet]
        public IActionResult Login()
        {
            var token = HttpContext.Session.GetString("token");

            if (!string.IsNullOrEmpty(token))
            {
                return View("AlreadyLoggedIn"); // Zaten giriş yaptıysa yönlendirme
            }

            return View();
        }

        // ✅ Giriş İşlemi (POST)
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var client = _httpClientFactory.CreateClient("DiaryApi");

            var content = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/Auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var tokenObj = JsonConvert.DeserializeObject<TokenResponse>(json);

                HttpContext.Session.SetString("token", tokenObj.Token);
                HttpContext.Session.SetString("email", model.Email);
                return RedirectToAction("Index", "Home");
            }

            // ❌ Başarısız giriş → hata mesajı göster
            TempData["LoginError"] = "Kullanıcı adı veya parola yanlış!";
            return View(model);
        }


        // ✅ Kayıt Sayfası (GET)
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ✅ Kayıt İşlemi (POST)
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            
            var client = _httpClientFactory.CreateClient("DiaryApi");
            var json = JsonConvert.SerializeObject(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/Auth/register", content);


            if (!response.IsSuccessStatusCode)
            {
                // 1) JSON’u oku ve ApiError listesine dönüştür
                var raw = await response.Content.ReadAsStringAsync();
                List<ApiError>? errors = null;
                try
                {
                    errors = JsonConvert.DeserializeObject<List<ApiError>>(raw);
                }
                catch
                {
                    // Beklenmedik formatta döndü, tek bir hata olarak gösterelim
                }

                if (errors != null && errors.Any())
                {
                    // 2) Kodlara göre Türkçe karşılık üretelim
                    foreach (var err in errors)
                    {
                        string userMessage = err.Code switch
                        {
                            "PasswordRequiresNonAlphanumeric" =>
                                "Şifre en az bir özel karakter içermelidir (örn. @, #, !, vb.).",
                            "PasswordRequiresDigit" =>
                                "Şifre en az bir rakam (0–9) içermelidir.",
                            "DuplicateUserName" =>
                                "Bu kullanıcı adı zaten kullanımda. Lütfen farklı bir e-posta ile deneyin.",
                            "DuplicateEmail" =>
                                "Bu e-posta adresi zaten kayıtlı.",
                            _ =>
                                err.Description // eğer tanıdık kod yoksa orijinal açıklamayı göster
                        };

                        ModelState.AddModelError(string.Empty, userMessage);
                    }
                }
                else
                {
                    // JSON parse edilemediyse raw metni yazarız
                    ModelState.AddModelError(string.Empty, "Kayıt başarısız. " + raw);
                }

                return View(model);
            }

            // Başarılıysa giriş sayfasına yönlendir
            return RedirectToAction("Login");
        }

        // ✅ Çıkış Yap
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        // POST: /Auth/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            // 1) Session’dan token’ı al
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            // 2) API istemcisi oluştur
            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 3) API’ye DELETE isteği at
            var response = await client.DeleteAsync("api/Auth/delete-account");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Hata"] = "Hesabınız silinirken bir hata oluştu.";
                return RedirectToAction("Index", "Home");
            }

            // 4) Oturumu temizle ve ana sayfaya yönlendir
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

    }
}
