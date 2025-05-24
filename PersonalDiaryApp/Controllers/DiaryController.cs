using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalDiaryApp.Data;
using PersonalDiaryApp.DTOs;
using PersonalDiaryApp.Entities;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace PersonalDiaryApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DiaryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DiaryController> _logger;

        public DiaryController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            ILogger<DiaryController> logger)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _logger = logger;
        }

        // Günlük Listeleme (Sadece Kullanıcıya Ait)
        [HttpGet]
        public async Task<IActionResult> GetAll(
            int page = 1,
            int pageSize = 5,
            bool plainText = false    // HTML mi, düz metin mi
        )
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var query = _context.Diaries
                .Where(x => x.UserId == userId)
                .Include(x => x.Photos)
                .OrderByDescending(x => x.CreatedDate);

            var totalCount = await query.CountAsync();
            var diaries = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = diaries.Select(x => new DiaryDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = plainText ? StripHtml(x.Content) : x.Content,
                CreatedDate = x.CreatedDate,
                PhotoUrls = x.Photos.Select(p => p.PhotoUrl).ToList()
            }).ToList();

            return Ok(new
            {
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                Data = result
            });
        }

        // HTML etiketlerini temizleyip düz metin döner
        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            var noTags = Regex.Replace(html, "<.*?>", string.Empty);
            return WebUtility.HtmlDecode(noTags).Trim();
        }

        // Günlük Oluşturma
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] DiaryCreateDto dto)
        {
            // Girdiği HTML'i düz metne çevir


            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var diary = new Diary
            {
                Title = dto.Title,
                Content = dto.Content,
                UserId = userId,
                IsFavorite = dto.IsFavorite,
                CreatedDate = DateTime.Now,
                Photos = new List<DiaryPhoto>()
            };

            var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploadPath);
            if (dto.Photos != null)
            {
                foreach (var photo in dto.Photos)
                {
                    if (!photo.ContentType.StartsWith("image/"))
                        return BadRequest("Sadece resim dosyası yüklenebilir.");
                    if (photo.Length > 2 * 1024 * 1024)
                        return BadRequest("Max 2MB resim yüklenebilir.");

                    var fileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
                    var fullPath = Path.Combine(uploadPath, fileName);
                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await photo.CopyToAsync(stream);

                    diary.Photos.Add(new DiaryPhoto { PhotoUrl = $"/uploads/{fileName}" });
                }
            }

            _context.Diaries.Add(diary);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Günlük başarıyla oluşturuldu." });
        }

        // Günlük Silme
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var diary = await _context.Diaries
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (diary == null)
                return NotFound("Günlük bulunamadı");

            _context.Diaries.Remove(diary);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Günlük silindi. ID: {id}");
            return Ok("Günlük silindi.");
        }

        // Günlük Güncelleme
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DiaryUpdateDto dto)
        {

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var diary = await _context.Diaries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (diary == null)
                return NotFound("Günlük bulunamadı");

            diary.Title = dto.Title;
            diary.Content = dto.Content;
            diary.CreatedDate = DateTime.Now;
            diary.IsFavorite = dto.IsFavorite;
            await _context.SaveChangesAsync();
            return Ok("Günlük başarıyla güncellendi.");
        }

        // Tek günlük detayı
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var diary = await _context.Diaries
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
            if (diary == null)
                return NotFound("Günlük bulunamadı");

            var result = new DiaryDetailDto
            {
                Id = diary.Id,
                Title = diary.Title,
                Content = StripHtml(diary.Content),
                CreatedDate = diary.CreatedDate,
                IsFavorite = diary.IsFavorite,
                Photos = diary.Photos.Select(p => new DiaryPhotoDto
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl
                }).ToList()
            };

            return Ok(result);
        }

        // Arama
        // GET: api/Diary/search
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            string keyword,
            bool plainText = false   // ← Burada düz metin kontrolü
        )
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı oturumu bulunamadı." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var diaries = await _context.Diaries
                .Where(x => x.UserId == userId &&
                            (x.Title.Contains(keyword) || x.Content.Contains(keyword)))
                .Include(x => x.Photos)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var result = diaries.Select(x => new DiaryDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = plainText
                            ? StripHtml(x.Content) // düz metin
                            : x.Content,           // ham HTML
                CreatedDate = x.CreatedDate,
                PhotoUrls = x.Photos.Select(p => p.PhotoUrl).ToList()
            }).ToList();

            return Ok(result);
        }

        // Fotoğraf silme
        [HttpDelete("photo/{photoId}")]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var photo = await _context.DiaryPhotos
                .Include(p => p.Diary)
                .FirstOrDefaultAsync(p => p.Id == photoId && p.Diary.UserId == userId);
            if (photo == null)
                return NotFound("Fotoğraf bulunamadı veya yetkiniz yok.");

            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", photo.PhotoUrl.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.DiaryPhotos.Remove(photo);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Fotoğraf silindi. Fotoğraf ID: {photoId}");
            return Ok("Fotoğraf başarıyla silindi.");
        }

        // Çoklu fotoğraf ekleme
        [HttpPut("photo/update-or-add-multiple/{diaryId}")]
        public async Task<IActionResult> AddMultiplePhotos(int diaryId, List<IFormFile> newPhotos)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = user.Id;
            var diary = await _context.Diaries
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.Id == diaryId && x.UserId == userId);
            if (diary == null)
                return NotFound("Günlük bulunamadı veya yetkiniz yok.");

            if (newPhotos == null || !newPhotos.Any())
                return BadRequest("Fotoğraf yüklenmedi.");

            var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploadPath);
            foreach (var photo in newPhotos)
            {
                if (!photo.ContentType.StartsWith("image/")) continue;
                if (photo.Length > 2 * 1024 * 1024) continue;

                var newFileName = Guid.NewGuid() + Path.GetExtension(photo.FileName);
                var newFullPath = Path.Combine(uploadPath, newFileName);
                using var stream = new FileStream(newFullPath, FileMode.Create);
                await photo.CopyToAsync(stream);
                diary.Photos.Add(new DiaryPhoto { PhotoUrl = $"/uploads/{newFileName}" });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Çoklu fotoğraf eklendi. Günlük ID: {diaryId}");
            return Ok("Fotoğraflar başarıyla eklendi.");
        }

        // Favorilere ekle/çıkar ve listele
        [HttpPost("favorite/{id}")]
        public async Task<IActionResult> AddToFavorites(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var diary = await _context.Diaries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (diary == null)
                return NotFound("Günlük bulunamadı");

            diary.IsFavorite = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Günlük favorilere eklendi. Id: {id}");
            return Ok("Günlük favorilere eklendi.");
        }

        [HttpPost("unfavorite/{id}")]
        public async Task<IActionResult> RemoveFromFavorites(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var diary = await _context.Diaries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (diary == null)
                return NotFound("Günlük bulunamadı");

            diary.IsFavorite = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Günlük favoritenden çıkarıldı. Id: {id}");
            return Ok("Günlük favorilerden çıkarıldı.");
        }
        // Favori Günlükler Listeleme
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites(
         
            bool plainText = false    // ← Buradan kontrol ediyoruz
        )
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Kullanıcı oturumu bulunamadı." });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var query = _context.Diaries
                .Where(x => x.UserId == userId && x.IsFavorite)
                .Include(x => x.Photos)
                .OrderByDescending(x => x.CreatedDate);

            var totalCount = await query.CountAsync();
            var diaries = await query
              
                .ToListAsync();

            var result = diaries.Select(x => new DiaryDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = plainText
                            ? StripHtml(x.Content)  // düz metin
                            : x.Content,            // HTML
                CreatedDate = x.CreatedDate,
                PhotoUrls = x.Photos.Select(p => p.PhotoUrl).ToList()
            }).ToList();

            return Ok(new
            {
                TotalCount = totalCount,
               
                Data = result
            });
        }

    }
}
