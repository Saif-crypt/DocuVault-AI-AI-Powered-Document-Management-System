# AI Module (OCR) — AI-Powered Document Management System

**A standalone Python OCR engine** for extracting text from scanned images, PDFs, and DOCX files using Tesseract OCR and python-docx.

This module is part of a **Final Year Major Project** that combines ASP.NET Core with AI-powered document processing (OCR + NLP + ML). It operates independently and can be called from the ASP.NET Core backend via command-line interface.

---

## 📋 Table of Contents

- [Features](#features)
- [System Requirements](#system-requirements)
- [Installation](#installation)
  - [Windows Installation](#windows-installation)
  - [Linux Installation](#linux-installation)
  - [Python Package Installation](#python-package-installation)
- [Usage](#usage)
  - [Command-Line Interface](#command-line-interface)
  - [Output Format](#output-format)
  - [Supported File Formats](#supported-file-formats)
- [Project Structure](#project-structure)
- [How It Works](#how-it-works)
  - [Processing Pipeline](#processing-pipeline)
  - [Image Preprocessing](#image-preprocessing)
  - [DOCX Text Extraction](#docx-text-extraction)
- [Testing](#testing)
- [Integration with ASP.NET Core](#integration-with-aspnet-core)
- [Troubleshooting](#troubleshooting)
- [Future Enhancements](#future-enhancements)

---

## ✨ Features

- ✅ **Multi-format support**: Images (PNG, JPG, JPEG, BMP, TIFF), scanned PDFs, and DOCX files
- ✅ **High-quality OCR**: Uses Tesseract 5.x with LSTM engine for maximum accuracy
- ✅ **Intelligent preprocessing**: Automatic grayscale conversion, upscaling, and adaptive thresholding for scanned documents
- ✅ **Native DOCX extraction**: Direct text extraction from DOCX files (no OCR needed for digital documents)
- ✅ **Multi-page handling**: Processes all pages in PDFs and extracts complete content from DOCX
- ✅ **Clean output**: Results wrapped in `START_TEXT` / `END_TEXT` markers for easy parsing
- ✅ **Error handling**: All errors reported to stderr; stdout stays clean for integration
- ✅ **Modular design**: Each processing stage isolated in its own function for easy extension

---

## 🖥️ System Requirements

### Operating System
- **Windows** 10/11 (tested and verified)
- **Linux** (Ubuntu 20.04+)
- **macOS** (10.15+)

### Core Dependencies

| Component | Version | Purpose |
|-----------|---------|---------|
| **Python** | 3.8+ | Runtime environment |
| **Tesseract OCR** | 5.0+ | OCR engine for scanned documents |
| **Poppler** | 0.68+ | PDF to image conversion |

### Python Packages (see `requirements.txt`)

```
pytesseract>=0.3.10   # Tesseract wrapper
Pillow>=10.0.0        # Image processing
opencv-python>=4.8.0  # Image preprocessing
pdf2image>=1.16.0     # PDF conversion
python-docx>=0.8.11   # DOCX text extraction
```

---

## 📦 Installation

### Windows Installation

#### Step 1: Install Tesseract OCR

```powershell
# Using winget (recommended)
winget install --id Tesseract-OCR.Tesseract

# Verify installation
tesseract --version
```

**Note:** Tesseract will be installed to `C:\Program Files\Tesseract-OCR\tesseract.exe` by default. The script is already configured to use this path.

#### Step 2: Install Poppler (for PDF support)

1. Download Poppler for Windows from: https://github.com/oschwartz10612/poppler-windows/releases
2. Extract the ZIP file to `C:\poppler` (or any preferred location)
3. Add the `Library\bin` folder to your system PATH:

```powershell
# Temporary (current session only)
$env:PATH += ";C:\poppler\Library\bin"

# Permanent (add via System Properties > Environment Variables)
# OR use this PowerShell command:
[System.Environment]::SetEnvironmentVariable("Path", $env:Path + ";C:\poppler\Library\bin", "User")
```

#### Step 3: Verify Installation

```powershell
# Check Tesseract
tesseract --version

# Check Poppler
pdftoppm -v
```

---

### Linux Installation

```bash
# Install Tesseract OCR
sudo apt update
sudo apt install tesseract-ocr

# Install Poppler utilities
sudo apt install poppler-utils

# Verify installation
tesseract --version
pdftoppm -v
```

---

### Python Package Installation

```bash
# Navigate to the AI_Module directory
cd AI_Module

# Create a virtual environment (recommended)
python -m venv .venv

# Activate the virtual environment
# Windows:
.venv\Scripts\activate

# Linux/macOS:
source .venv/bin/activate

# Install required packages
pip install -r requirements.txt
```

---

## 🚀 Usage

### Command-Line Interface

The OCR engine accepts a single file path as input and prints the extracted text to stdout.

**Basic Syntax:**

```bash
python ocr_engine.py <file_path>
```

### Examples

```bash
# Extract text from a scanned image
python ocr_engine.py test_input/sample_scan.png

# Extract text from a scanned PDF (processes all pages)
python ocr_engine.py test_input/sample_scan.pdf

# Extract text from a DOCX file (direct extraction, no OCR)
python ocr_engine.py test_input/test_document.docx

# Use with absolute paths
python ocr_engine.py "C:\Users\Documents\invoice.pdf"

# Use with relative paths from project root
python ocr_engine.py ../uploads/scan001.jpg
```

---

### Output Format

All extracted text is wrapped in markers for easy parsing by other systems (e.g., ASP.NET Core):

```
-----START_TEXT-----
SAMPLE SCANNED DOCUMENT
For OCR Testing - AI Module

Section 1: Introduction
This document was generated automatically to serve as a test input
for the OCR engine. It contains English text across multiple lines
so the extraction pipeline can be validated end-to-end.

Section 2: Key Points
1. The OCR module accepts both scanned images and scanned PDFs.
2. Images are preprocessed with grayscale conversion and thresholding.
3. Tesseract is used as the underlying OCR engine.
4. Output is wrapped in START_TEXT and END_TEXT markers.
-----END_TEXT-----
```

**Key Points:**
- ✅ Only the text content is printed to **stdout**
- ✅ All error messages go to **stderr** (keeping stdout clean)
- ✅ Multi-page PDFs have pages concatenated with double newlines (`\n\n`)
- ✅ DOCX files extract text from both paragraphs and tables
- ✅ No logs, emojis, or extra messages pollute the output

---

### Supported File Formats

| Format | Extensions | Processing Method | Notes |
|--------|-----------|-------------------|-------|
| **Images** | `.png`, `.jpg`, `.jpeg`, `.bmp`, `.tiff`, `.tif` | OCR after preprocessing | Best for scanned documents |
| **PDF** | `.pdf` | Convert to images (300 DPI) → OCR | Each page processed separately |
| **DOCX** | `.docx` | Direct text extraction | No OCR needed; extracts paragraphs & tables |

---

## 📁 Project Structure

```
AI_Module/
├── ocr_engine.py              # Main OCR script (entry point)
├── requirements.txt           # Python dependencies
├── test_input/                # Sample test files
│   ├── sample_scan.png        # Single-page scanned image
│   ├── sample_scan.pdf        # Multi-page scanned PDF (2 pages)
│   └── test_document.docx     # Sample DOCX file
├── test_output/               # Optional output directory
└── README.md                  # This file
```

---

## 🔧 How It Works

### Processing Pipeline

The OCR engine follows a **7-stage modular pipeline**:

```
┌─────────────────────────────────────────────────────────────────┐
│  1. INPUT VALIDATION                                            │
│     • Check file exists                                         │
│     • Validate file extension                                   │
│     • Return absolute path                                      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  2. FILE TYPE DETECTION                                         │
│     • Detect: image | pdf | docx                                │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  3. CONVERSION (if needed)                                      │
│     • PDF → Images (300 DPI using Poppler)                      │
│     • DOCX → Skip to text extraction                            │
│     • Images → Use directly                                     │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  4. PREPROCESSING (images only)                                 │
│     • Convert to grayscale                                      │
│     • Upscale if < 1500px (2x resize)                           │
│     • Adaptive Gaussian thresholding                            │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  5. TEXT EXTRACTION                                             │
│     • Images/PDFs: Tesseract OCR (--oem 3 --psm 3)              │
│     • DOCX: python-docx extraction (paragraphs + tables)        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  6. TEXT AGGREGATION                                            │
│     • Combine multi-page results with double newlines           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  7. OUTPUT FORMATTING                                           │
│     • Wrap in START_TEXT / END_TEXT markers                     │
│     • Print to stdout (errors to stderr)                        │
└─────────────────────────────────────────────────────────────────┘
```

---

### Image Preprocessing

For scanned images and PDF pages, the module applies **adaptive preprocessing** to maximize OCR accuracy:

```python
def preprocess_image(pil_image):
    1. Convert to grayscale (single channel)
    2. Upscale if either dimension < 1500px (2x resize using LANCZOS4)
    3. Apply adaptive Gaussian thresholding:
       • Block size: 15 (neighbourhood for threshold calculation)
       • C constant: 10 (fine-tuning parameter)
       • Result: Clean black-and-white text on white background
    4. Return processed PIL Image ready for Tesseract
```

**Why these steps?**
- **Grayscale**: Reduces complexity, speeds up OCR
- **Upscaling**: Small images lose detail during thresholding; 300+ DPI works best
- **Adaptive thresholding**: Handles uneven lighting, shadows, and page curvature better than global thresholding

---

### DOCX Text Extraction

For DOCX files, the module uses `python-docx` to extract text **without OCR**:

```python
def extract_text_from_docx(docx_path):
    1. Load document using python-docx
    2. Extract all paragraph text (skip empty paragraphs)
    3. Extract table data (cells separated by " | ")
    4. Combine all text with newlines
    5. Return clean text content
```

**Advantages:**
- ✅ **Fast**: No OCR processing needed for digital text
- ✅ **Accurate**: 100% accurate for native text (no character recognition errors)
- ✅ **Structured**: Preserves table structure with pipe delimiters

---

## 🧪 Testing

### Included Test Files

The module ships with three test files:

1. **`sample_scan.png`** — Single-page scanned document (1400×1800px, 300 DPI)
2. **`sample_scan.pdf`** — Two-page scanned PDF (invoice + terms)
3. **`test_document.docx`** — Digital DOCX with paragraphs and tables

### Running Tests

```bash
# Activate virtual environment
.venv\Scripts\activate  # Windows
source .venv/bin/activate  # Linux/macOS

# Test image OCR
python ocr_engine.py test_input/sample_scan.png

# Test PDF OCR (multi-page)
python ocr_engine.py test_input/sample_scan.pdf

# Test DOCX extraction
python ocr_engine.py test_input/test_document.docx
```

### Expected Output

All tests should print:
```
-----START_TEXT-----
<extracted text content>
-----END_TEXT-----
```

**No errors should appear.** If errors occur, see [Troubleshooting](#troubleshooting).

---

## 🔗 Integration with ASP.NET Core

The OCR module is designed to be called from the ASP.NET Core backend via **process execution**.

### C# Integration Example

```csharp
using System.Diagnostics;

public class OcrService
{
    private readonly string _pythonPath = @"C:\path\to\AI_Module\.venv\Scripts\python.exe";
    private readonly string _scriptPath = @"C:\path\to\AI_Module\ocr_engine.py";

    public async Task<string> ExtractTextAsync(string filePath)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = _pythonPath,
            Arguments = $"\"{_scriptPath}\" \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string errors = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"OCR failed: {errors}");
        }

        // Parse output between markers
        var startMarker = "-----START_TEXT-----";
        var endMarker = "-----END_TEXT-----";
        
        int startIndex = output.IndexOf(startMarker) + startMarker.Length;
        int endIndex = output.IndexOf(endMarker);
        
        if (startIndex < 0 || endIndex < 0)
        {
            throw new Exception("Invalid OCR output format");
        }

        return output.Substring(startIndex, endIndex - startIndex).Trim();
    }
}
```

### Integration Workflow

```
┌─────────────────┐      ┌──────────────────┐      ┌─────────────────┐
│  ASP.NET Core   │      │   OCR Module     │      │   Database      │
│   (Backend)     │      │   (Python)       │      │   (Storage)     │
└────────┬────────┘      └────────┬─────────┘      └────────┬────────┘
         │                        │                         │
         │  1. User uploads file  │                         │
         ├───────────────────────>│                         │
         │                        │                         │
         │  2. Call Python script │                         │
         │     with file path     │                         │
         ├───────────────────────>│                         │
         │                        │                         │
         │                   3. Run OCR                     │
         │                        │                         │
         │  4. Return extracted   │                         │
         │     text (stdout)      │                         │
         │<───────────────────────┤                         │
         │                        │                         │
         │  5. Save to database   │                         │
         ├────────────────────────┼────────────────────────>│
         │                        │                         │
         │  6. Return to user     │                         │
         │<───────────────────────┴─────────────────────────┘
```

---

## 🛠️ Troubleshooting

### Common Issues

#### 1. **"tesseract is not installed or it's not in your PATH"**

**Solution:**
```powershell
# Windows: Verify Tesseract installation
tesseract --version

# If not found, add to PATH
$env:PATH += ";C:\Program Files\Tesseract-OCR"

# Or reinstall
winget install --id Tesseract-OCR.Tesseract
```

#### 2. **"PDF conversion failed: Make sure Poppler is installed"**

**Solution:**
```powershell
# Download Poppler from: https://github.com/oschwartz10612/poppler-windows/releases
# Extract to C:\poppler
# Add to PATH
$env:PATH += ";C:\poppler\Library\bin"

# Verify
pdftoppm -v
```

#### 3. **OpenCV "adaptiveThreshold() missing required argument 'maxValue'"**

**Solution:** This was fixed in the current version. Make sure you're using the latest `ocr_engine.py` with **positional arguments** for `cv2.adaptiveThreshold()`:

```python
# ✅ Correct (positional arguments)
binary = cv2.adaptiveThreshold(gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY, 15, 10)

# ❌ Wrong (keyword arguments cause crash on Windows)
binary = cv2.adaptiveThreshold(gray, maxValue=255, adaptiveMethod=..., ...)
```

#### 4. **PowerShell script execution disabled**

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope CurrentUser
.venv\Scripts\activate
```

#### 5. **Poor OCR accuracy / garbled text**

**Possible causes and solutions:**
- **Low resolution input**: Use images scanned at 300 DPI or higher
- **Skewed/rotated pages**: Pre-rotate images before OCR
- **Non-English text**: Change Tesseract language: `pytesseract.image_to_string(img, lang='fra')` for French
- **Handwritten text**: Tesseract works best on printed text; consider specialized handwriting recognition models

---

## 🚀 Future Enhancements

This OCR module is designed to be **extended** with additional AI capabilities:

### Planned Features (Future Steps)

1. **NLP Preprocessing** (Step 3)
   - Text cleaning and normalization
   - Entity extraction (dates, amounts, names)
   - Keyword extraction

2. **ML Classification** (Step 4)
   - Document type classification (invoice, contract, report)
   - Confidence scoring
   - Multi-label tagging

3. **Semantic Search** (Step 5)
   - Vector embeddings for document similarity
   - Full-text search with ranking
   - Contextual query matching

4. **Enhanced Preprocessing**
   - Auto-rotation/deskewing
   - Multi-language support
   - Handwriting recognition (via specialized models)

5. **Performance Optimization**
   - Batch processing support
   - Parallel page processing
   - GPU acceleration for large PDFs

---

## 📝 License

This project is part of a Final Year Major Project for educational purposes.

---

## 👨‍💻 Author

**Student Name**: Mohammad Saif  
**Project**: AI-Powered Document Management System using NLP & OCR  
**Institution**: Panipat Institute of Engineering and Technology  
**Year**: 2026

---

## 🙏 Acknowledgments

- **Tesseract OCR** — Google's open-source OCR engine
- **Poppler** — PDF rendering library
- **OpenCV** — Computer vision and image processing
- **python-docx** — DOCX file manipulation

---

**For issues or questions, please contact: [siddiquisaif728@gmail.com]**
