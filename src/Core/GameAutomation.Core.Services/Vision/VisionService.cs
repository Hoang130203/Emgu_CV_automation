using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GameAutomation.Core.Models.Vision;

namespace GameAutomation.Core.Services.Vision;

/// <summary>
/// Vision service implementation using EmguCV for template matching and image processing
/// </summary>
public class VisionService : IVisionService
{
    #region Win32 API for Screen Capture

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private static bool _dpiAwareSet = false;

    private static void EnsureDPIAware()
    {
        if (!_dpiAwareSet)
        {
            SetProcessDPIAware();
            _dpiAwareSet = true;
        }
    }

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
        IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private const int SRCCOPY = 0x00CC0020;

    #endregion

    #region Async Methods

    public async Task<byte[]> CaptureScreenAsync(string? windowTitle = null)
    {
        return await Task.Run(() =>
        {
            using var bitmap = CaptureScreen(windowTitle);
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        });
    }

    public async Task<DetectionResult> DetectAsync(byte[] image, byte[] template)
    {
        return await Task.Run(() =>
        {
            using var imageStream = new MemoryStream(image);
            using var templateStream = new MemoryStream(template);
            using var imageBitmap = new Bitmap(imageStream);
            using var templateBitmap = new Bitmap(templateStream);

            var results = FindTemplateInternal(imageBitmap, templateBitmap, 0.8);
            return results.FirstOrDefault() ?? new DetectionResult { Found = false };
        });
    }

    public async Task<string> ExtractTextAsync(byte[] image)
    {
        return await Task.Run(() =>
        {
            using var ms = new MemoryStream(image);
            using var bitmap = new Bitmap(ms);
            return ExtractText(bitmap);
        });
    }

    public async Task<bool> DetectColorAsync(byte[] image, int x, int y, int width, int height, byte r, byte g, byte b, int tolerance)
    {
        return await Task.Run(() =>
        {
            using var ms = new MemoryStream(image);
            using var bitmap = new Bitmap(ms);
            return DetectColor(bitmap, x, y, width, height, Color.FromArgb(r, g, b), tolerance);
        });
    }

    #endregion

    #region Sync Methods

    public Bitmap CaptureScreen(string? windowTitle = null)
    {
        EnsureDPIAware();

        if (string.IsNullOrEmpty(windowTitle))
        {
            // Capture full screen including taskbar (use GetSystemMetrics for actual physical pixels)
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            IntPtr hdcSrc = GetDC(IntPtr.Zero); // Get screen DC (includes taskbar)
            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
            IntPtr hOld = SelectObject(hdcDest, hBitmap);

            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);

            SelectObject(hdcDest, hOld);
            var bitmap = Image.FromHbitmap(hBitmap);

            DeleteObject(hBitmap);
            DeleteDC(hdcDest);
            ReleaseDC(IntPtr.Zero, hdcSrc);

            return bitmap;
        }
        else
        {
            // Capture specific window
            IntPtr hwnd = FindWindow(null, windowTitle);
            if (hwnd == IntPtr.Zero)
                throw new ArgumentException($"Window '{windowTitle}' not found");

            GetWindowRect(hwnd, out RECT rect);

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            IntPtr hdcSrc = GetWindowDC(hwnd);
            IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
            IntPtr hOld = SelectObject(hdcDest, hBitmap);

            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);

            SelectObject(hdcDest, hOld);
            var bitmap = Image.FromHbitmap(hBitmap);

            DeleteObject(hBitmap);
            DeleteDC(hdcDest);
            ReleaseDC(hwnd, hdcSrc);

            return bitmap;
        }
    }

    public List<DetectionResult> FindTemplate(Bitmap screenshot, string templatePath, double threshold = 0.8)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        using var templateBitmap = new Bitmap(templatePath);
        return FindTemplateInternal(screenshot, templateBitmap, threshold);
    }

    #endregion

    #region Helper Methods

    private static Bitmap ConvertTo24bpp(Bitmap source)
    {
        if (source.PixelFormat == PixelFormat.Format24bppRgb)
            return source;

        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(result))
        {
            g.DrawImage(source, 0, 0, source.Width, source.Height);
        }
        return result;
    }

    private static Image<Bgr, byte> BitmapToImage(Bitmap bitmap)
    {
        var image = new Image<Bgr, byte>(bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            using var mat = new Mat(bitmap.Height, bitmap.Width, DepthType.Cv8U, 3, bitmapData.Scan0, bitmapData.Stride);
            mat.CopyTo(image);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return image;
    }

    private List<DetectionResult> FindTemplateInternal(Bitmap screenshot, Bitmap template, double threshold)
    {
        var results = new List<DetectionResult>();

        // Convert to 24bpp RGB if needed
        bool screenshotConverted = screenshot.PixelFormat != PixelFormat.Format24bppRgb;
        bool templateConverted = template.PixelFormat != PixelFormat.Format24bppRgb;

        Bitmap screenshot24 = screenshotConverted ? ConvertTo24bpp(screenshot) : screenshot;
        Bitmap template24 = templateConverted ? ConvertTo24bpp(template) : template;

        try
        {
            using var sourceImage = BitmapToImage(screenshot24);
            using var templateImage = BitmapToImage(template24);
            using var result = sourceImage.MatchTemplate(templateImage, TemplateMatchingType.CcoeffNormed);

            // Find all matches above threshold
            double[] minValues, maxValues;
            Point[] minLocations, maxLocations;

            result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

            // Get the best match first
            if (maxValues[0] >= threshold)
            {
                results.Add(new DetectionResult
                {
                    Found = true,
                    Confidence = maxValues[0],
                    X = maxLocations[0].X,
                    Y = maxLocations[0].Y,
                    Width = template.Width,
                    Height = template.Height,
                    DetectedAt = DateTime.UtcNow
                });

                // Find additional matches by masking the found region
                using var resultCopy = result.Clone();
                var maskSize = Math.Max(template.Width, template.Height) / 2;

                for (int i = 0; i < 100; i++) // Limit to 100 matches max
                {
                    // Mask the found region
                    int maskX = Math.Max(0, maxLocations[0].X - maskSize);
                    int maskY = Math.Max(0, maxLocations[0].Y - maskSize);
                    int maskW = Math.Min(maskSize * 2, resultCopy.Width - maskX);
                    int maskH = Math.Min(maskSize * 2, resultCopy.Height - maskY);

                    if (maskW > 0 && maskH > 0)
                    {
                        resultCopy.ROI = new Rectangle(maskX, maskY, maskW, maskH);
                        resultCopy.SetZero();
                        resultCopy.ROI = Rectangle.Empty;
                    }

                    resultCopy.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    if (maxValues[0] < threshold)
                        break;

                    results.Add(new DetectionResult
                    {
                        Found = true,
                        Confidence = maxValues[0],
                        X = maxLocations[0].X,
                        Y = maxLocations[0].Y,
                        Width = template.Width,
                        Height = template.Height,
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
        }
        finally
        {
            if (screenshotConverted)
                screenshot24.Dispose();
            if (templateConverted)
                template24.Dispose();
        }

        return results.OrderByDescending(r => r.Confidence).ToList();
    }

    public string ExtractText(Bitmap image)
    {
        // Basic OCR placeholder - requires Tesseract setup
        // For full OCR, add Emgu.CV.OCR package and configure Tesseract
        throw new NotImplementedException("OCR requires Tesseract setup. Add Emgu.CV.OCR package and tessdata.");
    }

    public bool DetectColor(Bitmap image, int x, int y, int width, int height, Color targetColor, int tolerance = 10)
    {
        int endX = Math.Min(x + width, image.Width);
        int endY = Math.Min(y + height, image.Height);

        bool converted = image.PixelFormat != PixelFormat.Format24bppRgb;
        Bitmap image24 = converted ? ConvertTo24bpp(image) : image;

        try
        {
            using var img = BitmapToImage(image24);

            for (int py = y; py < endY; py++)
            {
                for (int px = x; px < endX; px++)
                {
                    var pixel = img[py, px];
                    if (Math.Abs(pixel.Red - targetColor.R) <= tolerance &&
                        Math.Abs(pixel.Green - targetColor.G) <= tolerance &&
                        Math.Abs(pixel.Blue - targetColor.B) <= tolerance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        finally
        {
            if (converted)
                image24.Dispose();
        }
    }

    public List<Point> FindColorPositions(Bitmap image, Color targetColor, int tolerance = 10)
    {
        var positions = new List<Point>();

        bool converted = image.PixelFormat != PixelFormat.Format24bppRgb;
        Bitmap image24 = converted ? ConvertTo24bpp(image) : image;

        try
        {
            using var img = BitmapToImage(image24);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = img[y, x];
                    if (Math.Abs(pixel.Red - targetColor.R) <= tolerance &&
                        Math.Abs(pixel.Green - targetColor.G) <= tolerance &&
                        Math.Abs(pixel.Blue - targetColor.B) <= tolerance)
                    {
                        positions.Add(new Point(x, y));
                    }
                }
            }

            return positions;
        }
        finally
        {
            if (converted)
                image24.Dispose();
        }
    }

    #endregion
}
