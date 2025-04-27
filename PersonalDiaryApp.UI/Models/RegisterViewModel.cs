using System.ComponentModel.DataAnnotations;

namespace PersonalDiaryApp.UI.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad Soyad girmek zorunlu.")]
        // İngilizce ve Türkçe harfleri + boşluğu kabul ediyoruz
        [RegularExpression(@"^[A-Za-zÇĞİÖŞÜçğıöşü\s]+$",
            ErrorMessage = "Ad Soyad yalnızca harf ve boşluk içerebilir.")]
        [Display(Name = "Ad Soyad")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "E-Posta girmek zorunlu.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [Display(Name = "E-Posta")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Şifre girmek zorunlu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Şifreyi onaylamak zorunlu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
        [Display(Name = "Şifreyi Onayla")]
        public string ConfirmPassword { get; set; } = null!;

      
    }
}
