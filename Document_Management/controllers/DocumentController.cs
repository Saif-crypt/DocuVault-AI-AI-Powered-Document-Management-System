using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DocumentManagementApp.Data;
using DocumentManagementApp.Models;
using DocumentManagementApp.Services;

namespace DocumentManagementApp.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly OcrService _ocrService;
        private readonly ILogger<DocumentController> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory; // ✅ ADD THIS
        private readonly SummarisationService _summarisationService;

        public DocumentController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            OcrService ocrService,
            ILogger<DocumentController> logger,
            IServiceScopeFactory serviceScopeFactory,
            SummarisationService summarisationService)
        {
            _context = context;
            _environment = environment;
            _configuration = configuration;
            _ocrService = ocrService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory; // ✅ ADD THIS
            _summarisationService = summarisationService;
            
        }

        // GET: /Document/Index
public async Task<IActionResult> Index(string? search)  // ✅ Added parameter
{
    var userId = GetCurrentUserId();
    
    var query = _context.Documents
        .Where(d => d.UserId == userId);
    
    // ✅ Smart search logic
    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchTerm = search.Trim();
        query = query.Where(d => 
            (d.ProcessedText != null && EF.Functions.Like(d.ProcessedText, $"%{searchTerm}%")) ||
            (d.ProcessedText == null && d.ExtractedText != null && EF.Functions.Like(d.ExtractedText, $"%{searchTerm}%"))
        );
    }
    
    var documents = await query
        .OrderByDescending(d => d.UploadedAt)
        .ToListAsync();
    
    return View(documents);
}

        // GET: /Document/Upload
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        // POST: /Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string? description)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to upload");
                return View();
            }

            var maxFileSizeMB = _configuration.GetValue<int>("FileUpload:MaxFileSizeInMB");
            if (maxFileSizeMB == 0) maxFileSizeMB = 10;
            var maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;

            if (file.Length > maxFileSizeBytes)
            {
                ModelState.AddModelError("", $"File size cannot exceed {maxFileSizeMB}MB");
                return View();
            }

            var allowedExtensions = _configuration.GetValue<string>("FileUpload:AllowedExtensions")
                ?.Split(',') ?? new[] { ".pdf", ".docx", ".doc", ".txt", ".png", ".jpg", ".jpeg" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", $"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
                return View();
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var fullFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(fullFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var document = new Document
                {
                    FileName = file.FileName,
                    FilePath = fullFilePath,
                    FileExtension = fileExtension,
                    FileSizeInBytes = file.Length,
                    Description = description ?? string.Empty,
                    UserId = GetCurrentUserId(),
                    UploadedAt = DateTime.UtcNow,
                    OcrStatus = "Pending",
                    NlpStatus = "Pending",
                    IsProcessed = false,
                    IsNlpProcessed = false
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                // ✅ Start OCR processing with proper scoping
                _ = Task.Run(() => ProcessOcrAsync(document.Id, fullFilePath));

                TempData["SuccessMessage"] = "File uploaded successfully! OCR and NLP processing started.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                return View();
            }
        }

        /// <summary>
        /// ✅ FIXED: Process OCR with proper DbContext scoping
        /// </summary>
        private async Task ProcessOcrAsync(int documentId, string filePath)
        {
            // ✅ Create a new scope for background processing
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                try
                {
                    // ✅ Get a new DbContext instance from the scope
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    _logger.LogInformation($"Starting OCR processing for document {documentId}");

                    var (success, extractedText, errorMessage) = await _ocrService.ProcessDocumentAsync(filePath);

                    var document = await context.Documents.FindAsync(documentId);
                    if (document == null)
                    {
                        _logger.LogWarning($"Document {documentId} not found after OCR processing");
                        return;
                    }

                    if (success)
                    {
                        document.OcrStatus = "Completed";
                        document.ExtractedText = extractedText ?? string.Empty;
                        document.IsProcessed = true;
                        await context.SaveChangesAsync();
                        
                        _logger.LogInformation("OCR completed successfully for document {DocumentId}. Extracted {CharacterCount} characters.", documentId, extractedText?.Length ?? 0);

                        // ✅ Start NLP preprocessing if text exists
                        if (!string.IsNullOrWhiteSpace(extractedText))
                        {
                            _logger.LogInformation($"Starting NLP preprocessing for document {documentId}");
                            await ProcessNlpAsync(documentId, extractedText);
                        }
                        else
                        {
                            _logger.LogWarning($"No text extracted for document {documentId}, skipping NLP");
                            document.NlpStatus = "Skipped";
                            await context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        document.OcrStatus = "Failed";
                        document.IsProcessed = false;
                        document.NlpStatus = "Skipped";
                        await context.SaveChangesAsync();
                        _logger.LogWarning($"OCR failed for document {documentId}: {errorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"OCR processing error for document {documentId}");

                    // Try to mark as failed
                    try
                    {
                        using (var errorScope = _serviceScopeFactory.CreateScope())
                        {
                            var errorContext = errorScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            var document = await errorContext.Documents.FindAsync(documentId);
                            if (document != null)
                            {
                                document.OcrStatus = "Failed";
                                document.IsProcessed = false;
                                document.NlpStatus = "Skipped";
                                await errorContext.SaveChangesAsync();
                            }
                        }
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, $"Failed to update document {documentId} status to Failed");
                    }
                }
            }
        }

        /// <summary>
        /// ✅ FIXED: Process NLP with proper DbContext scoping
        /// </summary>
        private async Task ProcessNlpAsync(int documentId, string extractedText)
        {
            // ✅ Create a new scope for NLP processing
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var nlpService = scope.ServiceProvider.GetRequiredService<NlpService>();
                    
                    _logger.LogInformation($"Starting NLP preprocessing for document {documentId}");

                    var nlpResult = await nlpService.PreprocessTextAsync(extractedText);

                    var document = await context.Documents.FindAsync(documentId);
                    if (document == null)
                    {
                        _logger.LogWarning($"Document {documentId} not found after NLP processing");
                        return;
                    }

                    if (nlpResult.Success)
                    {
                        document.ProcessedText = nlpResult.ProcessedText;
                        document.NlpStatus = "Completed";
                        document.IsNlpProcessed = true;
                        await context.SaveChangesAsync();
                        
                        _logger.LogInformation("NLP completed for document {DocumentId}.", documentId);
                    }
                    else
                    {
                        document.NlpStatus = "Failed";
                        document.IsNlpProcessed = false;
                        await context.SaveChangesAsync();
                        _logger.LogWarning($"NLP failed for document {documentId}: {nlpResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"NLP preprocessing error for document {documentId}");

                    try
                    {
                        using (var errorScope = _serviceScopeFactory.CreateScope())
                        {
                            var errorContext = errorScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            var document = await errorContext.Documents.FindAsync(documentId);
                            if (document != null)
                            {
                                document.NlpStatus = "Failed";
                                document.IsNlpProcessed = false;
                                await errorContext.SaveChangesAsync();
                            }
                        }
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, $"Failed to update document {documentId} NLP status");
                    }
                }
            }
        }

        // GET: /Document/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }

        // GET: /Document/Download/5
        public async Task<IActionResult> Download(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (document == null)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(document.FilePath))
            {
                return NotFound("File not found on server");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(document.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, GetContentType(document.FileExtension ?? string.Empty), document.FileName);
        }

        // POST: /Document/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

            if (document == null)
            {
                return NotFound();
            }

            if (System.IO.File.Exists(document.FilePath))
            {
                try
                {
                    System.IO.File.Delete(document.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to delete file for document {id}");
                }
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Document deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Helper methods
        private string GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim ?? string.Empty;
        }

        private string GetContentType(string extension)
        {
            return (extension ?? string.Empty).ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                ".tiff" or ".tif" => "image/tiff",
                _ => "application/octet-stream"
            };
        }
          /// <summary>
/// View OCR extracted text (read-only)
/// </summary>
[HttpGet]
public async Task<IActionResult> ViewExtractedText(int id)
{
    var userId = GetCurrentUserId();
    var document = await _context.Documents
        .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

    if (document == null)
    {
        return NotFound();
    }

    if (string.IsNullOrWhiteSpace(document.ExtractedText))
    {
        TempData["ErrorMessage"] = "No extracted text available.";
        return RedirectToAction(nameof(Index));
    }

    return View(document);
}

/// <summary>
/// View NLP processed text (read-only)
/// </summary>
[HttpGet]
public async Task<IActionResult> ViewProcessedText(int id)
{
    var userId = GetCurrentUserId();
    var document = await _context.Documents
        .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

    if (document == null)
    {
        return NotFound();
    }

    if (string.IsNullOrWhiteSpace(document.ProcessedText))
    {
        TempData["ErrorMessage"] = "No processed text available.";
        return RedirectToAction(nameof(Index));
    }

    return View(document);
}

[HttpGet]
public async Task<IActionResult> Summarise(int id)
{
    // Verify document belongs to current user
    var userId = GetCurrentUserId();
    var document = await _context.Documents
        .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

    if (document == null)
    {
        return NotFound();
    }

    // Safety check: only run if NLP is complete and ProcessedText exists
    if (document.NlpStatus != "Completed" || string.IsNullOrWhiteSpace(document.ProcessedText))
    {
        TempData["ErrorMessage"] = "Summary is not available. NLP preprocessing must complete first.";
        return RedirectToAction(nameof(Index));
    }

    _logger.LogInformation($"Generating on-demand summary for document {id}");

    // Generate summary on demand
    var summaryResult = await _summarisationService.SummariseAsync(document.ProcessedText, numSentences: 5);

    // Pass both document and summary result to view via ViewBag
    ViewBag.SummaryResult = summaryResult;
    ViewBag.DocumentName = document.FileName;
    ViewBag.ProcessedWordCount = document.ProcessedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    return View(document);
}
    }
  
}
