using System;

namespace DocumentManagementApp.Models
{
    /// <summary>
    /// ViewModel for Profile page
    /// Contains user information and AI usage statistics
    /// </summary>
    public class ProfileViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? AccountCreatedDate { get; set; }
        
        public int TotalDocuments { get; set; }
        public int OcrProcessed { get; set; }
        public int NlpProcessed { get; set; }
        public int SummariesGenerated { get; set; }
    }
}
