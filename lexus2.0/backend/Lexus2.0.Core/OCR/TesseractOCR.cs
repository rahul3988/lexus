using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Lexus2_0.Core.Logging;

namespace Lexus2_0.Core.OCR
{
    /// <summary>
    /// Tesseract OCR wrapper for captcha solving
    /// Uses Tesseract DLLs (similar to NeXuS implementation)
    /// </summary>
    public class TesseractOCR : IDisposable
    {
        private readonly ILogger _logger;
        private IntPtr _tesseractHandle;
        private bool _disposed = false;

        // Tesseract DLL imports (P/Invoke)
        [DllImport("x64/tesseract50.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr TessBaseAPICreate();

        [DllImport("x64/tesseract50.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int TessBaseAPIInit3(IntPtr handle, string datapath, string language);

        [DllImport("x64/tesseract50.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void TessBaseAPIDelete(IntPtr handle);

        [DllImport("x64/tesseract50.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr handle);

        [DllImport("x64/tesseract50.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void TessDeleteText(IntPtr text);

        public TesseractOCR(ILogger logger, string tessDataPath = "./tessdata")
        {
            _logger = logger;
            Initialize(tessDataPath);
        }

        private void Initialize(string tessDataPath)
        {
            try
            {
                _tesseractHandle = TessBaseAPICreate();
                if (_tesseractHandle == IntPtr.Zero)
                {
                    throw new Exception("Failed to create Tesseract instance");
                }

                int result = TessBaseAPIInit3(_tesseractHandle, tessDataPath, "eng");
                if (result != 0)
                {
                    throw new Exception($"Failed to initialize Tesseract: {result}");
                }

                _logger.Info("Tesseract OCR initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize Tesseract OCR", ex);
                throw;
            }
        }

        /// <summary>
        /// Extract text from image (captcha solving)
        /// </summary>
        public string ExtractText(string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Image not found: {imagePath}");
            }

            try
            {
                // Preprocess image for better OCR
                var processedImage = PreprocessImage(imagePath);

                // Set image to Tesseract (simplified - actual implementation needs image buffer)
                // This is a placeholder - actual Tesseract API requires image buffer setup

                IntPtr textPtr = TessBaseAPIGetUTF8Text(_tesseractHandle);
                if (textPtr == IntPtr.Zero)
                {
                    return string.Empty;
                }

                string result = Marshal.PtrToStringAnsi(textPtr) ?? string.Empty;
                TessDeleteText(textPtr);

                // Clean up result (remove spaces, special chars for captcha)
                result = CleanCaptchaText(result);

                _logger.Debug($"OCR extracted text: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error extracting text from image: {imagePath}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Extract text from base64 image (for web captchas)
        /// </summary>
        public string ExtractTextFromBase64(string base64Image)
        {
            try
            {
                // Remove data URL prefix if present
                if (base64Image.Contains(","))
                {
                    base64Image = base64Image.Split(',')[1];
                }

                var imageBytes = Convert.FromBase64String(base64Image);
                var tempPath = Path.Combine(Path.GetTempPath(), $"captcha_{Guid.NewGuid()}.png");

                File.WriteAllBytes(tempPath, imageBytes);

                try
                {
                    return ExtractText(tempPath);
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error extracting text from base64 image", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Preprocess image for better OCR accuracy
        /// </summary>
        private string PreprocessImage(string imagePath)
        {
            // Image preprocessing (grayscale, contrast, noise reduction)
            // This is a placeholder - implement actual image processing
            return imagePath;
        }

        /// <summary>
        /// Clean captcha text (remove spaces, special characters)
        /// </summary>
        private string CleanCaptchaText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove spaces and special characters, keep only alphanumeric
            var cleaned = System.Text.RegularExpressions.Regex.Replace(text, @"[^A-Za-z0-9]", "");
            return cleaned.ToUpper();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_tesseractHandle != IntPtr.Zero)
                {
                    TessBaseAPIDelete(_tesseractHandle);
                    _tesseractHandle = IntPtr.Zero;
                }
                _disposed = true;
            }
        }
    }
}

