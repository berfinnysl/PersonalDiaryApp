namespace PersonalDiaryApp.API.Dtos
{
    public class RegisterDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        // ← ConfirmPassword yoktu
    }
}
