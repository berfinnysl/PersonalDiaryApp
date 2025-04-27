namespace PersonalDiaryApp.UI.Models
{
    public class DiaryDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public bool IsFavorite { get; set; }
        public List<DiaryPhotoDto> Photos { get; set; } = new();
    }

    public class DiaryPhotoDto
    {
        public int Id { get; set; }
        public string PhotoUrl { get; set; } = null!;
    }
}
