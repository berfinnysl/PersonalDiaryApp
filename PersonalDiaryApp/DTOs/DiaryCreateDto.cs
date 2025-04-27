namespace PersonalDiaryApp.DTOs
{
    public class DiaryCreateDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public List<IFormFile> Photos { get; set; } = new();
        public bool IsFavorite { get; set; }

    }
}
