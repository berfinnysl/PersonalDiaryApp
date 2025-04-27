using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace PersonalDiaryApp.UI.Models
{
    public class DiaryViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public bool IsFavorite { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
        public List<int> PhotoIds { get; set; } = new();
        public List<PhotoInfo> Photos { get; set; } = new();

        // API'den dönen foto dosya adları
        public IFormFileCollection? NewPhotos { get; set; }
        public List<string> ExistingPhotoUrls { get; set; } = new();

    }


}
