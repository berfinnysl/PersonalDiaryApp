using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalDiaryApp.Data;
using PersonalDiaryApp.DTOs;
using PersonalDiaryApp.Entities;
using System.Security.Claims;

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
        private readonly ILogger<DiaryController> _logger; // DOĞRU YERİ BURASI

        public DiaryController(AppDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env, ILogger<DiaryController> logger)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _logger = logger;
        }

        // Günlük Listeleme (Sadece Kullanıcıya Ait)
        [HttpGet]
        public async Task<IActionResult> GetAll(int page = 1, int pageSize = 5)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

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
                Content = x.Content,
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
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] DiaryCreateDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var diary = new Diary
            {
                Title = dto.Title,
                Content = dto.Content,
                UserId = userId!,
                IsFavorite = dto.IsFavorite, // ✅ EKLENDİ!
                CreatedDate = DateTime.Now, // (Opsiyonel) Ekleyebilirsin
                Photos = new List<DiaryPhoto>() // (Opsiyonel) boşsa hata olmasın
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

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }

                    diary.Photos.Add(new DiaryPhoto
                    {
                        PhotoUrl = $"/uploads/{fileName}"
                    });
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
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var diary = await _context.Diaries
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (diary == null)
                return NotFound("Günlük bulunamadı");

            _context.Diaries.Remove(diary);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Günlük silindi. ID: {id}"); // 📌 LOG SATIRI

            return Ok("Günlük silindi.");
        }


        // Günlük Güncelleme
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DiaryUpdateDto dto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var diary = await _context.Diaries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (diary == null)
                return NotFound("Günlük bulunamadı");

            diary.Title = dto.Title;
            diary.Content = dto.Content;
            diary.CreatedDate = DateTime.Now; // ister güncelle ister sabit kalsın
            diary.IsFavorite = dto.IsFavorite;
            await _context.SaveChangesAsync();

            return Ok("Günlük başarıyla güncellendi.");
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var diary = await _context.Diaries
                .Include(d => d.Photos)
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
            if (diary == null) return NotFound("Günlük bulunamadı");

            var result = new DiaryDetailDto
            {
                Id = diary.Id,
                Title = diary.Title,
                Content = diary.Content,
                CreatedDate = diary.CreatedDate,
                IsFavorite = diary.IsFavorite,

                // artİk PhotoUrls yerine:
                Photos = diary.Photos
                                 .Select(p => new DiaryPhotoDto
                                 {
                                     Id = p.Id,
                                     PhotoUrl = p.PhotoUrl
                                 })
                                 .ToList()
            };

            return Ok(result);
        }


        [HttpGet("search")]
        public async Task<IActionResult> Search(string keyword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

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
                Content = x.Content,
                CreatedDate = x.CreatedDate,
                PhotoUrls = x.Photos.Select(p => p.PhotoUrl).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpDelete("photo/{photoId}")]
        [Authorize]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            // Fotoğrafı veritabanından bul
            var photo = await _context.DiaryPhotos
                .Include(p => p.Diary)
                .FirstOrDefaultAsync(p => p.Id == photoId && p.Diary.UserId == userId);

            if (photo == null)
                return NotFound("Fotoğraf bulunamadı veya yetkiniz yok.");

            // Fiziksel dosyayı sil
            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", photo.PhotoUrl.TrimStart('/'));

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.DiaryPhotos.Remove(photo);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Fotoğraf silindi. Fotoğraf ID: {photoId}");

            return Ok("Fotoğraf başarıyla silindi.");
        }
        [HttpPut("photo/update-or-add-multiple/{diaryId}")]
        public async Task<IActionResult> AddMultiplePhotos(int diaryId, List<IFormFile> newPhotos)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }

            var userId = user.Id;

            var diary = await _context.Diaries
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.Id == diaryId && x.UserId == userId);

            if (diary == null)
                return NotFound("Günlük bulunamadı veya yetkiniz yok.");

            if (newPhotos == null || !newPhotos.Any())
                return BadRequest("Fotoğraf yüklenmedi.");

            foreach (var newPhoto in newPhotos)
            {
                if (!newPhoto.ContentType.StartsWith("image/"))
                    continue; // Resim değilse atla

                if (newPhoto.Length > 2 * 1024 * 1024)
                    continue; // Büyük dosya atla

                var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                Directory.CreateDirectory(uploadPath);

                var newFileName = Guid.NewGuid() + Path.GetExtension(newPhoto.FileName);
                var newFullPath = Path.Combine(uploadPath, newFileName);

                using (var stream = new FileStream(newFullPath, FileMode.Create))
                {
                    await newPhoto.CopyToAsync(stream);
                }

                diary.Photos.Add(new DiaryPhoto
                {
                    PhotoUrl = $"/uploads/{newFileName}"
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Çoklu fotoğraf eklendi. Günlük ID: {diaryId}");

            return Ok("Fotoğraflar başarıyla eklendi.");
        }



        [HttpPost("favorite/{id}")]
        public async Task<IActionResult> AddToFavorites(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var diary = await _context.Diaries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (diary == null)
                return NotFound("Günlük bulunamadı");

            diary.IsFavorite = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Günlük favoriye alındı. Id: {id}");

            return Ok("Günlük favorilere eklendi.");
        }
        [HttpPost("unfavorite/{id}")]
        public async Task<IActionResult> RemoveFromFavorites(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var diary = await _context.Diaries.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (diary == null)
                return NotFound("Günlük bulunamadı");

            diary.IsFavorite = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Günlük favoriden çıkarıldı. Id: {id}");

            return Ok("Günlük favorilerden çıkarıldı.");
        }
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Kullanıcı hesabı silinmiş veya oturum süresi dolmuş." });
            }
            var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var diaries = await _context.Diaries
                .Where(x => x.UserId == userId && x.IsFavorite)
                .Include(x => x.Photos)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            var result = diaries.Select(x => new DiaryDto
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                CreatedDate = x.CreatedDate,
                PhotoUrls = x.Photos.Select(p => p.PhotoUrl).ToList()
            }).ToList();

            return Ok(result);
        }



    }

}
