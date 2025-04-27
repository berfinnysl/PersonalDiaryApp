using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PersonalDiaryApp.UI.Helpers;
using PersonalDiaryApp.UI.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text;

namespace PersonalDiaryApp.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Home/Index
        // Günlük Listeleme
        public async Task<IActionResult> Index()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Hata"] = "Lütfen giriş yapınız.";
                return RedirectToAction("Login", "Auth");
            }

            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("api/Diary");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Hata = "Günlükler yüklenemedi.";
                return View(new List<DiaryViewModel>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert
                .DeserializeObject<DiaryListResponse>(json)!;
            var gunlukler = responseData.Data;

            return View(gunlukler);
        }

        // GET: /Home/Create
        // Günlük Ekleme Formu
        [HttpGet]
        public IActionResult Create()
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Hata"] = "Lütfen giriş yapınız.";
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        // POST: /Home/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DiaryCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 📸 Çoklu fotoğraf desteği için formData oluşturuyoruz:
            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(model.Title), "Title");
            form.Add(new StringContent(model.Content), "Content");
            form.Add(new StringContent(model.IsFavorite.ToString()), "IsFavorite");

            // 📸 Çoklu fotoğraf gönderimi
            if (model.Photos != null && model.Photos.Any())
            {
                foreach (var photo in model.Photos)
                {
                    if (photo.Length > 0)
                    {
                        var streamContent = new StreamContent(photo.OpenReadStream());
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType);
                        // **Burada "Photos" çoğul olmalı!**
                        form.Add(streamContent, "Photos", photo.FileName);
                    }
                }
            }

            var response = await client.PostAsync("api/Diary", form);
            if (response.IsSuccessStatusCode)
                return RedirectToAction("Index");

            ViewBag.Hata = "Günlük kaydedilemedi.";
            return View(model);
        }



        // GET: /Home/Detail/{id}
        // Günlük Detayı Gösterme
        // GET: /Home/Detail/5
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var res = await client.GetAsync($"api/Diary/{id}");
            if (!res.IsSuccessStatusCode)
            {
                TempData["Hata"] = "Günlük detayı yüklenemedi.";
                return RedirectToAction("Index");
            }
           

            var json = await res.Content.ReadAsStringAsync();
            var dto = JsonConvert.DeserializeObject<DiaryDetailDto>(json)!;

            var baseUrl = client.BaseAddress!.ToString().TrimEnd('/');
            var vm = new DiaryViewModel
            {
                Id = dto.Id,
                Title = dto.Title,
                Content = dto.Content,
                CreatedDate = dto.CreatedDate,
                IsFavorite = dto.IsFavorite,

                // burada Photos listesinden 2 ayrı listeye
                PhotoUrls = dto.Photos
                       .Select(p => p.PhotoUrl.StartsWith("http")
                                    ? p.PhotoUrl
                                    : $"{baseUrl}{p.PhotoUrl}")
                       .ToList(),

                PhotoIds = dto.Photos
                       .Select(p => p.Id)
                       .ToList()
            };

            return View(vm);
        }

        // POST: /Home/DeletePhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int photoId, int diaryId)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync($"api/Diary/photo/{photoId}");
            if (!response.IsSuccessStatusCode)
                TempData["Error"] = "Fotoğraf silinemedi.";

            return RedirectToAction("Detail", new { id = diaryId });
        }





        // GET: /Home/Edit/{id}
        // Günlüğü Düzenleme Formu
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 1) API’den detayı çek
            var res = await client.GetAsync($"api/Diary/{id}");
            if (!res.IsSuccessStatusCode) return NotFound();

            var json = await res.Content.ReadAsStringAsync();
            // API tarafında DiaryDetailDto’ya PhotoUrls dönüyorsanız:
            var vm = JsonConvert.DeserializeObject<DiaryViewModel>(json)!;

            // 2) Edit ViewModel’e aktar
            var editVm = new DiaryEditViewModel
            {
                Id = vm.Id,
                Title = vm.Title,
                Content = vm.Content,
                IsFavorite = vm.IsFavorite,
                ExistingPhotoUrls = vm.PhotoUrls  // ← Burada mutlaka atama yapın!
            };

            return View(editVm);
        }


        // POST: /Home/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DiaryEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 1️⃣ Günlüğün metinsel kısmını güncelle
            var updateDto = new
            {
                id = model.Id,
                title = model.Title,
                content = model.Content,
                isFavorite = model.IsFavorite
            };

            var jsonPayload = JsonConvert.SerializeObject(updateDto);
            var stringContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var updateRes = await client.PutAsync($"api/Diary/{model.Id}", stringContent);

            if (!updateRes.IsSuccessStatusCode)
            {
                ViewBag.Hata = "Günlük güncellenemedi.";
                return View(model);
            }

            // 2️⃣ 📸 Çoklu fotoğraf ekleme (tek tek gönderiyoruz)
            if (model.Photos != null && model.Photos.Any())
            {
                using var form = new MultipartFormDataContent();

                foreach (var photo in model.Photos)
                {
                    var streamContent = new StreamContent(photo.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(photo.ContentType);
                    form.Add(streamContent, "newPhotos", photo.FileName);
                }

                var photoRes = await client.PutAsync($"api/Diary/photo/update-or-add-multiple/{model.Id}", form);

                if (!photoRes.IsSuccessStatusCode)
                    TempData["FotoHata"] = "Fotoğraflar eklenirken hata oldu.";
            }


            return RedirectToAction("Detail", new { id = model.Id });
        }





        [HttpGet]
        public async Task<IActionResult> Favorites(int page = 1, int pageSize = 5)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // API favorites endpoint'inden dönüyor
            var response = await client.GetAsync($"api/Diary/favorites");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Hata = "Favori günlükler yüklenemedi.";
                return View("Index", new DiaryListViewModel());
            }

            var json = await response.Content.ReadAsStringAsync();

            // 1) JSON diziyi önce doğrudan liste olarak al
            var list = JsonConvert
                .DeserializeObject<List<DiaryViewModel>>(json)!;
            var baseUrl = client.BaseAddress!.ToString().TrimEnd('/');
            foreach (var d in list)
            {
                d.PhotoUrls = d.PhotoUrls
                    .Select(rel => $"{baseUrl}{rel}")
                    .ToList();
            }


            // 2) Sonra bunu DiaryListViewModel’e sar
            var vm = new DiaryListViewModel
            {
                Items = list,
                CurrentPage = 1,
                PageSize = list.Count,
                TotalCount = list.Count
            };

            // 3) Index.cshtml’i DiaryListViewModel beklediği için bu modeli gönder
            return View("Index", vm);
        }



        [HttpPost]
        public async Task<IActionResult> Unfavorite(int id)
        {
            var token = HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync($"https://localhost:44353/api/Diary/unfavorite/{id}", null);

            if (response.IsSuccessStatusCode)
            {
                TempData["Message"] = "Günlük favorilerden çıkarıldı.";
            }
            else
            {
                TempData["Error"] = "Favorilerden çıkarılamadı.";
            }

            return RedirectToAction("Favorites");
        }
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5)
        {
            // 1) Token kontrolü
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Hata"] = "Lütfen giriş yapınız.";
                return RedirectToAction("Login", "Auth");
            }

            // 2) HttpClient ayarı
            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // 3) Sayfalı API çağrısı
            var response = await client.GetAsync($"api/Diary?page={page}&pageSize={pageSize}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Hata = "Günlükler yüklenemedi.";
                // Boş liste ve 0 sayfa bilgisi ile dönelim
                return View(new DiaryListViewModel());
            }

            // 4) JSON’u oku ve API’den gelen listeyi modelle eşleştir
            var json = await response.Content.ReadAsStringAsync();
            var apiResult = JsonConvert.DeserializeObject<DiaryListResponse>(json)!;

            var vm = new DiaryListViewModel
            {
                Items = apiResult.Data,
                CurrentPage = apiResult.CurrentPage,
                PageSize = apiResult.PageSize,
                TotalCount = apiResult.TotalCount
            };
            var baseUrl = client.BaseAddress!.ToString().TrimEnd('/');
            foreach (var diary in vm.Items)
            {
                diary.PhotoUrls = diary.PhotoUrls
                    .Select(rel => $"{baseUrl}{rel}")
                    .ToList();
            }
            // 5) View’e yeni VM’i gönder
            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> Search(string keyword)
        {
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // API'de arama endpointine istek
            var response = await client.GetAsync($"api/Diary/search?keyword={keyword}");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Hata = "Arama sonuçları getirilemedi.";
                return View("Index", new DiaryListViewModel());
            }

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<DiaryViewModel>>(json)!;

            // Arama sonuçlarını DiaryListViewModel ile sarmalayalım
            var vm = new DiaryListViewModel
            {
                Items = list,
                CurrentPage = 1,
                PageSize = list.Count,
                TotalCount = list.Count
            };

            ViewBag.AramaKelimesi = keyword;
            return View("Index", vm);
        }
        // GET /Home/Delete/5
       // POST: /Home/Delete/5
[HttpPost]
[ValidateAntiForgeryToken]
[ActionName("Delete")]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    // API’ye POST Delete isteği atın
    var client = _httpClientFactory.CreateClient("DiaryApi");
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", HttpContext.Session.GetString("token"));

    await client.DeleteAsync($"api/Diary/{id}");
    return RedirectToAction("Index");
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["LoginError"] = "Lütfen tüm alanları doldurun.";
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("DiaryApi");
            var response = await client.PostAsJsonAsync("api/Auth/login", model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                if (result != null && result.Success && !string.IsNullOrEmpty(result.Data))
                {
                    // 1) Token kaydet
                    HttpContext.Session.SetString("token", result.Data);

                    // 2) Ad Soyad bilgisi varsa API’de dön, yoksa e-posta’yı koy:
                    //    (API’niz Login response’a name de döndürüyorsa o DTO’ya ekleyin ve buradan çekin.)
                    // Şimdilik e-mail’i gösterelim:
                    HttpContext.Session.SetString("name", model.Email);

                    return RedirectToAction("Index");
                }
            }

            TempData["LoginError"] = "Kullanıcı adı veya parola yanlış!";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            // 1) Token kontrolü
            var token = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Auth");

            // 2) API çağrısı
            var client = _httpClientFactory.CreateClient("DiaryApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await client.DeleteAsync("api/Auth/account");
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = "Hesabınız silinirken bir hata oluştu.";
                return RedirectToAction("Index");
            }

            // 3) Oturumu temizle ve kullanıcıyı kayıt/giriş sayfasına gönder
            HttpContext.Session.Clear();
            TempData["Message"] = "Hesabınız başarıyla silindi.";
            return RedirectToAction("Register", "Auth");
        }

    }
}
