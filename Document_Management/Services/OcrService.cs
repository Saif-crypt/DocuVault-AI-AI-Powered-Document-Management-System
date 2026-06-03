using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DocumentManagementApp.Services
{
    /// <summary>
    /// Service for integrating Python OCR module with ASP.NET application
    /// Executes external Python script and parses OCR output
    /// ✅ FIXED: Proper UTF-8 encoding for Unicode characters
    /// </summary>
    public class OcrService
    {
        // Path to Python executable
        private readonly string _pythonPath;

        // Path to OCR script
        private readonly string _ocrScriptPath;

        /// <summary>
        /// Constructor - Configure paths to Python and OCR script
        /// </summary>
        public OcrService()
        {
            // Python executable path
            _pythonPath = "python"; // Change to "python3" on Mac/Linux if needed
            
            // OCR script path - IMPORTANT: Update this to your actual path
            _ocrScriptPath = @"C:\Users\siddi\OneDrive\Desktop\New folder\New folder\New folder\New folder\AI Module (OCR)\ocr_engine.py";
        }

        /// <summary>
        /// Process a document file using Python OCR
        /// </summary>
        /// <param name="filePath">Absolute path to the file to process</param>
        /// <returns>Tuple with Success flag, Extracted text, and Error message</returns>
        public async Task<(bool Success, string? ExtractedText, string? ErrorMessage)> ProcessDocumentAsync(string filePath)
        {
            try
            {
                // Validate file exists
                if (!File.Exists(filePath))
                {
                    return (false, null, "File not found");
                }

                // ✅ FIX: Configure Python process with UTF-8 encoding
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{_ocrScriptPath}\" \"{filePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    
                    // ✅ CRITICAL: Force UTF-8 encoding
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    
                    WorkingDirectory = Path.GetDirectoryName(_ocrScriptPath)
                };

                // ✅ FIX: Set environment variables to force UTF-8
                processStartInfo.Environment["PYTHONIOENCODING"] = "utf-8";
                processStartInfo.Environment["PYTHONUTF8"] = "1"; // Python 3.7+

                // Execute Python script
                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    // Read output asynchronously with UTF-8
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // ✅ INCREASED: Wait for completion with longer timeout for large files
                    bool completed = await Task.Run(() => process.WaitForExit(180000)); // 180 seconds (3 minutes)

                    if (!completed)
                    {
                        try
                        {
                            process.Kill(true);
                        }
                        catch { /* Process already exited */ }
                        return (false, null, "OCR process timeout (>3 minutes)");
                    }

                    string output = await outputTask;
                    string error = await errorTask;

                    // Check for errors
                    if (process.ExitCode != 0)
                    {
                        return (false, null, $"Python error (Exit code: {process.ExitCode}): {error}");
                    }

                    // Parse extracted text
                    string? extractedText = ParseOcrOutput(output);

                    if (string.IsNullOrWhiteSpace(extractedText))
                    {
                        return (false, null, "No text extracted from document");
                    }

                    return (true, extractedText, null);
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"OCR Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse OCR output to extract text between delimiters
        /// ✅ Handles UTF-8 Unicode characters safely
        /// </summary>
        /// <param name="output">Raw Python script output</param>
        /// <returns>Extracted text or null if not found</returns>
        private string? ParseOcrOutput(string output)
        {
            const string startMarker = "-----START_TEXT-----";
            const string endMarker = "-----END_TEXT-----";

            int startIndex = output.IndexOf(startMarker, StringComparison.Ordinal);
            int endIndex = output.IndexOf(endMarker, StringComparison.Ordinal);

            if (startIndex == -1 || endIndex == -1 || startIndex >= endIndex)
            {
                return null;
            }

            // Extract text between markers
            startIndex += startMarker.Length;
            int length = endIndex - startIndex;

            // ✅ Extract and preserve Unicode characters
            string extractedText = output.Substring(startIndex, length).Trim();
            
            // ✅ Optional: Remove null characters that might cause issues
            extractedText = extractedText.Replace("\0", "");

            return extractedText;
        }
    }
}
