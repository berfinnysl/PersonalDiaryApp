using System.ComponentModel.DataAnnotations;

namespace PersonalDiaryApp.UI.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta alanı zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-Posta")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Parola alanı zorunludur.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }
}
