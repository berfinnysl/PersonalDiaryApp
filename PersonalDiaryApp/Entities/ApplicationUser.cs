using Microsoft.AspNetCore.Identity;

namespace PersonalDiaryApp.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Diary> Diaries { get; set; }
    }
}
