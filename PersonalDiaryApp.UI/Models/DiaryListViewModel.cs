using System;
using System.Collections.Generic;

namespace PersonalDiaryApp.UI.Models
{
    public class DiaryListViewModel
    {
        public List<DiaryViewModel> Items { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
