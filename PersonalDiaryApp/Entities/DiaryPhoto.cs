namespace PersonalDiaryApp.Entities
{
    public class DiaryPhoto
    {
        public int Id { get; set; }  // Primary Key

        public required string PhotoUrl { get; set; }

        public int DiaryId { get; set; }
        public Diary Diary { get; set; } = null!;
    }
}
