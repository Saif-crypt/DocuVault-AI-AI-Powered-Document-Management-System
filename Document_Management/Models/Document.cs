using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DocumentManagementApp.Models
{
    /// <summary>
    /// Document model with OCR and NLP support
    /// </summary>
    public class Document
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string? FileExtension { get; set; }

        public long FileSizeInBytes { get; set; }

        public DateTime UploadedAt { get; set; }

        public string? Description { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // Navigation property
        public virtual IdentityUser? User { get; set; }

        // ===== OCR FIELDS =====

        /// <summary>
        /// Raw text extracted from document via OCR
        /// </summary>
        public string? ExtractedText { get; set; }

        /// <summary>
        /// OCR processing status: Pending, Completed, Failed
        /// </summary>
        [MaxLength(20)]
        [Required]
        public string OcrStatus { get; set; } = "Pending";

        /// <summary>
        /// Whether OCR processing has completed
        /// </summary>
        public bool IsProcessed { get; set; } = false;

        // ===== NLP FIELDS (NEW) =====

        /// <summary>
        /// Preprocessed text after NLP pipeline
        /// Cleaned, tokenized, stopwords removed, lemmatized
        /// </summary>
        public string? ProcessedText { get; set; }

        /// <summary>
        /// NLP preprocessing status: Pending, Completed, Failed
        /// </summary>
        [MaxLength(20)]
        public string NlpStatus { get; set; } = "Pending";

        /// <summary>
        /// Whether NLP preprocessing has completed
        /// </summary>
        public bool IsNlpProcessed { get; set; } = false;
    }
}
