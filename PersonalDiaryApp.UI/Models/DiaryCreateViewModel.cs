using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PersonalDiaryApp.UI.Models
{
    public class DiaryCreateViewModel
    {
        [Display(Name = "Başlık")]
        [Required(ErrorMessage = "Lütfen günlük başlığını girin.")]
        public string Title { get; set; } = null!;

        [Display(Name = "İçerik")]
        [Required(ErrorMessage = "Lütfen günlük içeriğini girin.")]
        public string Content { get; set; } = null!;

        [Display(Name = "Favori")]
        public bool IsFavorite { get; set; }

        [Display(Name = "Fotoğraf Ekle")]
        public List<IFormFile>? Photos { get; set; }
    }
}
