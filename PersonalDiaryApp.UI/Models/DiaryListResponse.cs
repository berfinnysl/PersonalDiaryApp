using System.Collections.Generic;

namespace PersonalDiaryApp.UI.Models
{
    public class DiaryListResponse
    {
        public List<DiaryViewModel> Data { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
