using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalDiaryApp.Entities
{
    public class Diary
    {
        public int Id { get; set; }  // Primary Key

        public required string Title { get; set; }
        [Column(TypeName = "nvarchar(max)")]
        public required string Content { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public ICollection<DiaryPhoto> Photos { get; set; } = new List<DiaryPhoto>();
        public bool IsFavorite { get; set; } = false;

    }
}
