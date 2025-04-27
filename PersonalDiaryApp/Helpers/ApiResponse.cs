namespace PersonalDiaryApp.Helpers
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public T Data { get; set; }

        public ApiResponse(T data, string message = "İşlem başarılı")
        {
            Success = true;
            Message = message;
            Data = data;
        }

        public ApiResponse(string message)
        {
            Success = false;
            Message = message;
        }
    }
}
