namespace PersonalDiaryApp.DTOs
{
    public class DiaryDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public List<DiaryPhotoDto> Photos { get; set; } = new();
        public bool IsFavorite { get; set; }
        public List<string> PhotoUrls
      => Photos.Select(p => p.PhotoUrl).ToList();
    }
}
