using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocumentManagementApp.Services
{
    /// <summary>
    /// NLP Preprocessing Service
    /// Calls Python NLP script to preprocess OCR-extracted text
    /// </summary>
    public class NlpService
    {
        private readonly string _pythonPath;
        private readonly string _nlpScriptPath;
        private readonly ILogger<NlpService> _logger;

        public NlpService(ILogger<NlpService> logger)
        {
            _logger = logger;
            
            // Python executable path
            _pythonPath = "python"; // Change to full path if needed
            
            // NLP script path - UPDATE THIS TO YOUR ACTUAL PATH
            _nlpScriptPath = @"C:\Users\siddi\OneDrive\Desktop\New folder\New folder\New folder\New folder\AI Module (OCR)\nlp_preprocess.py";
        }

        /// <summary>
        /// Preprocess text using Python NLP pipeline
        /// </summary>
        /// <param name="rawText">Raw OCR-extracted text</param>
        /// <returns>NLP preprocessing result</returns>
        public async Task<NlpResult> PreprocessTextAsync(string rawText)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    _logger.LogWarning("NLP preprocessing called with empty text");
                    return new NlpResult
                    {
                        Success = false,
                        ProcessedText = string.Empty,
                        ErrorMessage = "Input text is empty"
                    };
                }

                // Validate script exists
                if (!File.Exists(_nlpScriptPath))
                {
                    _logger.LogError($"NLP script not found at: {_nlpScriptPath}");
                    return new NlpResult
                    {
                        Success = false,
                        ProcessedText = string.Empty,
                        ErrorMessage = "NLP script not found"
                    };
                }

                // Configure Python process
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _pythonPath,
                    Arguments = $"\"{_nlpScriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardInputEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.GetDirectoryName(_nlpScriptPath)
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();

                    // Write input text to stdin
                    await process.StandardInput.WriteAsync(rawText);
                    await process.StandardInput.FlushAsync();
                    process.StandardInput.Close();

                    // Read output asynchronously
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    // Wait for completion with timeout (60 seconds)
                    bool completed = await Task.Run(() => process.WaitForExit(60000));

                    if (!completed)
                    {
                        try
                        {
                            process.Kill(true);
                        }
                        catch { /* Process already exited */ }
                        
                        _logger.LogError("NLP preprocessing timeout");
                        return new NlpResult
                        {
                            Success = false,
                            ProcessedText = string.Empty,
                            ErrorMessage = "NLP processing timeout (>60 seconds)"
                        };
                    }

                    string output = await outputTask;
                    string error = await errorTask;

                    // Check for errors
                    if (process.ExitCode != 0)
                    {
                        _logger.LogError($"NLP preprocessing failed. Exit code: {process.ExitCode}, Error: {error}");
                        return new NlpResult
                        {
                            Success = false,
                            ProcessedText = string.Empty,
                            ErrorMessage = $"Python error (Exit code: {process.ExitCode})"
                        };
                    }

                    // Parse JSON output
                    var result = ParseNlpOutput(output);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation("NLP preprocessing completed successfully");
                    }
                    else
                    {
                        _logger.LogWarning($"NLP preprocessing failed: {result.ErrorMessage}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NLP preprocessing exception");
                return new NlpResult
                {
                    Success = false,
                    ProcessedText = string.Empty,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Parse JSON output from Python NLP script
        /// </summary>
        private NlpResult ParseNlpOutput(string jsonOutput)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var pythonResult = JsonSerializer.Deserialize<PythonNlpOutput>(jsonOutput, options);

                if (pythonResult == null)
                {
                    return new NlpResult
                    {
                        Success = false,
                        ProcessedText = string.Empty,
                        ErrorMessage = "Failed to parse NLP output"
                    };
                }

                return new NlpResult
                {
                    Success = pythonResult.Success,
                    ProcessedText = pythonResult.ProcessedText ?? string.Empty,
                    CleanedText = pythonResult.CleanedText,
                    Sentences = pythonResult.Sentences,
                    Tokens = pythonResult.Tokens,
                    FilteredTokens = pythonResult.FilteredTokens,
                    LemmatizedTokens = pythonResult.LemmatizedTokens,
                    Stats = pythonResult.Stats,
                    ErrorMessage = pythonResult.Error
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse NLP JSON output");
                return new NlpResult
                {
                    Success = false,
                    ProcessedText = string.Empty,
                    ErrorMessage = "Invalid JSON output from NLP script"
                };
            }
        }

        /// <summary>
        /// Python script output structure
        /// </summary>
        private class PythonNlpOutput
        {
            public bool Success { get; set; }
            public string? ProcessedText { get; set; }
            public string? CleanedText { get; set; }
            public string[]? Sentences { get; set; }
            public string[]? Tokens { get; set; }
            public string[]? FilteredTokens { get; set; }
            public string[]? LemmatizedTokens { get; set; }
            public NlpStats? Stats { get; set; }
            public string? Error { get; set; }
        }
    }

    /// <summary>
    /// NLP preprocessing result
    /// </summary>
    public class NlpResult
    {
        public bool Success { get; set; }
        public string ProcessedText { get; set; } = string.Empty;
        public string? CleanedText { get; set; }
        public string[]? Sentences { get; set; }
        public string[]? Tokens { get; set; }
        public string[]? FilteredTokens { get; set; }
        public string[]? LemmatizedTokens { get; set; }
        public NlpStats? Stats { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// NLP processing statistics
    /// </summary>
    public class NlpStats
    {
        public int OriginalLength { get; set; }
        public int CleanedLength { get; set; }
        public int NumSentences { get; set; }
        public int NumTokens { get; set; }
        public int NumFilteredTokens { get; set; }
        public int NumLemmatizedTokens { get; set; }
    }
}
