namespace PersonalDiaryApp.DTOs
{
    public class DiaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedDate { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
    }
}
