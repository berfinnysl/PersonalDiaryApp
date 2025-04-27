namespace PersonalDiaryApp.UI.Models
{
    // API'den dönen tekil hata kodu+mesajı
    public class ApiError
    {
        public string Code { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
