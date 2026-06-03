using System;
using System.Collections.Generic;

namespace DocumentManagementApp.Models
{
    /// <summary>
    /// ViewModel for Dashboard page
    /// Contains statistics and recent documents for logged-in user
    /// </summary>
    public class DashboardViewModel
    {
        public int TotalDocuments { get; set; }
        public int OcrCompleted { get; set; }
        public int NlpProcessed { get; set; }
        public int SummariesGenerated { get; set; }
        public List<Document> RecentDocuments { get; set; } = new List<Document>();
    }
}
