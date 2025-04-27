namespace PersonalDiaryApp.DTOs
{
    public class DiaryUpdateDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsFavorite { get; set; } 

    }
}
