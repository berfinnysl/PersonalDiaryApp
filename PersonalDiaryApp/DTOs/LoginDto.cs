using System.ComponentModel.DataAnnotations;

namespace PersonalDiaryApp.API.Dtos
{
    public class LoginDto
    {
        [Required(ErrorMessage = "E-Posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-Posta")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Parola alanı zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; } = null!;
    }
}
