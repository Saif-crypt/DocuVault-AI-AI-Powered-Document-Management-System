using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DocumentManagementApp.Services
{
    /// <summary>
    /// Extractive Summarisation Service
    /// Calls Python script to generate frequency-based extractive summaries.
    ///
    /// Design decision: Generate ON DEMAND (not stored in DB)
    /// Reason: No schema migration needed, safer, and summaries can be
    /// regenerated anytime without re-running OCR or NLP.
    /// </summary>
    public class SummarisationService
    {
        private readonly string _pythonPath;
        private readonly string _summariseScriptPath;
        private readonly ILogger<SummarisationService> _logger;

        public SummarisationService(ILogger<SummarisationService> logger)
        {
            _logger = logger;
            _pythonPath = "python";

            // UPDATE THIS PATH to match your folder structure
            _summariseScriptPath = @"C:\Users\siddi\OneDrive\Desktop\New folder\New folder\New folder\New folder\AI Module (OCR)\summarise.py";
        }

        /// <summary>
        /// Generate an extractive summary from ProcessedText.
        /// Returns on-demand result - nothing is stored in DB.
        /// </summary>
        /// <param name="processedText">NLP-cleaned text from Document.ProcessedText</param>
        /// <param name="numSentences">Number of summary sentences (5-7)</param>
        public async Task<SummaryResult> SummariseAsync(string processedText, int numSentences = 5)
        {
            // Safety: never run if ProcessedText is null
            if (string.IsNullOrWhiteSpace(processedText))
            {
                _logger.LogWarning("Summarisation called with empty ProcessedText");
                return new SummaryResult
                {
                    Success = false,
                    ErrorMessage = "ProcessedText is empty. NLP preprocessing must complete first."
                };
            }

            // Validate script exists
            if (!File.Exists(_summariseScriptPath))
            {
                _logger.LogError($"Summarisation script not found at: {_summariseScriptPath}");
                return new SummaryResult
                {
                    Success = false,
                    ErrorMessage = "Summarisation script not found. Check script path."
                };
            }

            try
            {
                // Build JSON input for Python script
                var inputPayload = JsonSerializer.Serialize(new
                {
                    processed_text = processedText,
                    num_sentences = numSentences
                });

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{_summariseScriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardInputEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.GetDirectoryName(_summariseScriptPath)
                };

                processStartInfo.Environment["PYTHONIOENCODING"] = "utf-8";
                processStartInfo.Environment["PYTHONUTF8"] = "1";

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                // Send JSON to stdin
                await process.StandardInput.WriteAsync(inputPayload);
                await process.StandardInput.FlushAsync();
                process.StandardInput.Close();

                // Read output
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // 30 second timeout for summarisation
                bool completed = await Task.Run(() => process.WaitForExit(30000));

                if (!completed)
                {
                    try { process.Kill(true); } catch { }
                    return new SummaryResult
                    {
                        Success = false,
                        ErrorMessage = "Summarisation timed out after 30 seconds."
                    };
                }

                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogError($"Summarisation script failed. Code: {process.ExitCode}, Error: {error}");
                    return new SummaryResult
                    {
                        Success = false,
                        ErrorMessage = $"Script error (exit code {process.ExitCode})"
                    };
                }

                return ParseOutput(output);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Summarisation exception");
                return new SummaryResult
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Parse JSON output from Python summarisation script
        /// </summary>
        private SummaryResult ParseOutput(string jsonOutput)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<PythonSummaryOutput>(jsonOutput, options);

                if (data == null)
                {
                    return new SummaryResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse summarisation output"
                    };
                }

                return new SummaryResult
                {
                    Success = data.Success,
                    Summary = data.Summary ?? string.Empty,
                    Sentences = data.Sentences ?? Array.Empty<string>(),
                    TotalWords = data.TotalWords,
                    TotalChunks = data.TotalChunks,
                    SelectedChunks = data.SelectedChunks,
                    TopKeywords = data.TopKeywords ?? Array.Empty<KeywordScore>(),
                    Explanation = data.Explanation ?? string.Empty,
                    ErrorMessage = data.Error
                };
            }
            catch (Exception ex)
            {
                return new SummaryResult
                {
                    Success = false,
                    ErrorMessage = $"JSON parse error: {ex.Message}"
                };
            }
        }

        // Python script output structure
        private class PythonSummaryOutput
        {
            public bool Success { get; set; }
            public string? Summary { get; set; }
            public string[]? Sentences { get; set; }
            public int TotalWords { get; set; }
            public int TotalChunks { get; set; }
            public int SelectedChunks { get; set; }
            public KeywordScore[]? TopKeywords { get; set; }
            public string? Explanation { get; set; }
            public string? Error { get; set; }
        }
    }

    // ==========================================
    // Result models
    // ==========================================

    public class SummaryResult
    {
        public bool Success { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string[] Sentences { get; set; } = Array.Empty<string>();
        public int TotalWords { get; set; }
        public int TotalChunks { get; set; }
        public int SelectedChunks { get; set; }
        public KeywordScore[] TopKeywords { get; set; } = Array.Empty<KeywordScore>();
        public string Explanation { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class KeywordScore
    {
        public string Word { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
