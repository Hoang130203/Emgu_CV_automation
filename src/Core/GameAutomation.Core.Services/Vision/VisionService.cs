using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using GameAutomation.Core.Models.Configuration;
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

        if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
            return false;

        bool converted = false;
        Bitmap processImage = image;

        // Ensure 24bpp or 32bpp for consistent processing
        if (image.PixelFormat != PixelFormat.Format24bppRgb && image.PixelFormat != PixelFormat.Format32bppArgb)
        {
            processImage = ConvertTo24bpp(image);
            converted = true;
        }

        BitmapData? data = null;
        try
        {
            data = processImage.LockBits(
                new Rectangle(0, 0, processImage.Width, processImage.Height),
                ImageLockMode.ReadOnly,
                processImage.PixelFormat);

            int bytesPerPixel = processImage.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;
            int stride = data.Stride;
            IntPtr scan0 = data.Scan0;

            unsafe
            {
                byte* ptr = (byte*)scan0;

                for (int py = y; py < endY; py++)
                {
                    byte* row = ptr + (py * stride);
                    for (int px = x; px < endX; px++)
                    {
                        int idx = px * bytesPerPixel;
                        byte b = row[idx];
                        byte g = row[idx + 1];
                        byte r = row[idx + 2];

                        if (Math.Abs(r - targetColor.R) <= tolerance &&
                            Math.Abs(g - targetColor.G) <= tolerance &&
                            Math.Abs(b - targetColor.B) <= tolerance)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        finally
        {
            if (data != null)
                processImage.UnlockBits(data);
            
            if (converted)
                processImage.Dispose();
        }
    }

    public List<Point> FindColorPositions(Bitmap image, Color targetColor, int tolerance = 10)
    {
        var positions = new List<Point>();

        bool converted = false;
        Bitmap processImage = image;

        if (image.PixelFormat != PixelFormat.Format24bppRgb && image.PixelFormat != PixelFormat.Format32bppArgb)
        {
            processImage = ConvertTo24bpp(image);
            converted = true;
        }

        BitmapData? data = null;
        try
        {
            data = processImage.LockBits(
                new Rectangle(0, 0, processImage.Width, processImage.Height),
                ImageLockMode.ReadOnly,
                processImage.PixelFormat);

            int bytesPerPixel = processImage.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4;
            int stride = data.Stride;
            IntPtr scan0 = data.Scan0;

            unsafe
            {
                byte* ptr = (byte*)scan0;

                for (int y = 0; y < processImage.Height; y++)
                {
                    byte* row = ptr + (y * stride);
                    for (int x = 0; x < processImage.Width; x++)
                    {
                        int idx = x * bytesPerPixel;
                        byte b = row[idx];
                        byte g = row[idx + 1];
                        byte r = row[idx + 2];

                        if (Math.Abs(r - targetColor.R) <= tolerance &&
                            Math.Abs(g - targetColor.G) <= tolerance &&
                            Math.Abs(b - targetColor.B) <= tolerance)
                        {
                            positions.Add(new Point(x, y));
                        }
                    }
                }
            }

            return positions;
        }
        finally
        {
            if (data != null)
                processImage.UnlockBits(data);

            if (converted)
                processImage.Dispose();
        }
    }

    #endregion

    #region Multi-Scale Template Matching

    public List<DetectionResult> FindTemplateMultiScale(
        Bitmap screenshot,
        string templatePath,
        double threshold = 0.7,
        double minScale = 0.5,
        double maxScale = 2.0,
        int scaleSteps = 10)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        using var templateBitmap = new Bitmap(templatePath);
        return FindTemplateMultiScaleInternal(screenshot, templateBitmap, threshold, minScale, maxScale, scaleSteps);
    }

    private List<DetectionResult> FindTemplateMultiScaleInternal(
        Bitmap screenshot,
        Bitmap template,
        double threshold,
        double minScale,
        double maxScale,
        int scaleSteps)
    {
        var allResults = new List<DetectionResult>();

        // Convert screenshot to 24bpp once
        bool screenshotConverted = screenshot.PixelFormat != PixelFormat.Format24bppRgb;
        Bitmap screenshot24 = screenshotConverted ? ConvertTo24bpp(screenshot) : screenshot;

        try
        {
            using var sourceImage = BitmapToImage(screenshot24);

            // Calculate scale factors to try
            double scaleStep = (maxScale - minScale) / Math.Max(1, scaleSteps - 1);

            for (int i = 0; i < scaleSteps; i++)
            {
                double scale = minScale + (i * scaleStep);

                // Calculate new template size
                int newWidth = (int)(template.Width * scale);
                int newHeight = (int)(template.Height * scale);

                // Skip if template would be too small or larger than screenshot
                if (newWidth < 10 || newHeight < 10 ||
                    newWidth >= screenshot.Width || newHeight >= screenshot.Height)
                    continue;

                // Resize template
                using var resizedTemplate = new Bitmap(template, newWidth, newHeight);
                bool templateConverted = resizedTemplate.PixelFormat != PixelFormat.Format24bppRgb;
                Bitmap template24 = templateConverted ? ConvertTo24bpp(resizedTemplate) : resizedTemplate;

                try
                {
                    using var templateImage = BitmapToImage(template24);
                    using var result = sourceImage.MatchTemplate(templateImage, TemplateMatchingType.CcoeffNormed);

                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    if (maxValues[0] >= threshold)
                    {
                        allResults.Add(new DetectionResult
                        {
                            Found = true,
                            Confidence = maxValues[0],
                            X = maxLocations[0].X,
                            Y = maxLocations[0].Y,
                            Width = newWidth,
                            Height = newHeight,
                            DetectedAt = DateTime.UtcNow
                        });
                    }
                }
                finally
                {
                    if (templateConverted)
                        template24.Dispose();
                }
            }
        }
        finally
        {
            if (screenshotConverted)
                screenshot24.Dispose();
        }

        // Return best matches sorted by confidence
        return allResults.OrderByDescending(r => r.Confidence).ToList();
    }

    #endregion

    #region Feature Matching Methods

    public List<DetectionResult> FindTemplateWithFeatures(
        Bitmap screenshot,
        string templatePath,
        FeatureMatchingAlgorithm algorithm = FeatureMatchingAlgorithm.ORB,
        int minMatchCount = 10,
        double ratioThreshold = 0.75)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        using var templateBitmap = new Bitmap(templatePath);
        return FindTemplateWithFeaturesInternal(screenshot, templateBitmap, algorithm, minMatchCount, ratioThreshold);
    }

    public List<DetectionResult> FindTemplateAuto(
        Bitmap screenshot,
        string templatePath,
        bool useFeatureMatching = false,
        double threshold = 0.8,
        FeatureMatchingAlgorithm algorithm = FeatureMatchingAlgorithm.ORB,
        int minMatchCount = 10,
        double ratioThreshold = 0.75)
    {
        if (useFeatureMatching)
        {
            return FindTemplateWithFeatures(screenshot, templatePath, algorithm, minMatchCount, ratioThreshold);
        }
        else
        {
            return FindTemplate(screenshot, templatePath, threshold);
        }
    }

    private List<DetectionResult> FindTemplateWithFeaturesInternal(
        Bitmap screenshot,
        Bitmap template,
        FeatureMatchingAlgorithm algorithm,
        int minMatchCount,
        double ratioThreshold)
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

            // Convert to grayscale for feature detection
            using var sourceGray = sourceImage.Convert<Gray, byte>();
            using var templateGray = templateImage.Convert<Gray, byte>();

            // Create feature detector based on algorithm
            Feature2D detector = algorithm switch
            {
                FeatureMatchingAlgorithm.SIFT => new SIFT(),
                FeatureMatchingAlgorithm.ORB => new ORB(),
                _ => new ORB()
            };

            using (detector)
            {
                // Detect keypoints and compute descriptors
                using var templateKeypoints = new VectorOfKeyPoint();
                using var sourceKeypoints = new VectorOfKeyPoint();
                using var templateDescriptors = new Mat();
                using var sourceDescriptors = new Mat();

                detector.DetectAndCompute(templateGray, null, templateKeypoints, templateDescriptors, false);
                detector.DetectAndCompute(sourceGray, null, sourceKeypoints, sourceDescriptors, false);

                // Check if we have enough keypoints
                if (templateKeypoints.Size < 4 || sourceKeypoints.Size < 4)
                {
                    return results;
                }

                // Match descriptors using BFMatcher with KNN
                using var matcher = new BFMatcher(
                    algorithm == FeatureMatchingAlgorithm.SIFT ? DistanceType.L2 : DistanceType.Hamming);

                using var matches = new VectorOfVectorOfDMatch();
                matcher.KnnMatch(templateDescriptors, sourceDescriptors, matches, 2);

                // Apply Lowe's ratio test
                var goodMatches = new List<MDMatch>();
                for (int i = 0; i < matches.Size; i++)
                {
                    if (matches[i].Size >= 2)
                    {
                        var m = matches[i][0];
                        var n = matches[i][1];
                        if (m.Distance < ratioThreshold * n.Distance)
                        {
                            goodMatches.Add(m);
                        }
                    }
                }

                // Check if we have enough good matches
                if (goodMatches.Count >= minMatchCount)
                {
                    // Get matched keypoint coordinates
                    var srcPoints = new PointF[goodMatches.Count];
                    var dstPoints = new PointF[goodMatches.Count];

                    for (int i = 0; i < goodMatches.Count; i++)
                    {
                        srcPoints[i] = templateKeypoints[goodMatches[i].QueryIdx].Point;
                        dstPoints[i] = sourceKeypoints[goodMatches[i].TrainIdx].Point;
                    }

                    // Find homography
                    using var srcMat = new Mat(goodMatches.Count, 1, DepthType.Cv32F, 2);
                    using var dstMat = new Mat(goodMatches.Count, 1, DepthType.Cv32F, 2);

                    srcMat.SetTo(srcPoints);
                    dstMat.SetTo(dstPoints);

                    using var homography = CvInvoke.FindHomography(srcMat, dstMat, RobustEstimationAlgorithm.Ransac, 5.0);

                    if (!homography.IsEmpty)
                    {
                        // Transform template corners to find bounding box in source
                        var templateCorners = new PointF[]
                        {
                            new PointF(0, 0),
                            new PointF(template.Width, 0),
                            new PointF(template.Width, template.Height),
                            new PointF(0, template.Height)
                        };

                        // Use VectorOfPointF for perspective transform
                        using var srcCorners = new VectorOfPointF(templateCorners);
                        using var dstCorners = new VectorOfPointF();
                        CvInvoke.PerspectiveTransform(srcCorners, dstCorners, homography);

                        var corners = dstCorners.ToArray();

                        // Calculate bounding box
                        float minX = float.MaxValue, minY = float.MaxValue;
                        float maxX = float.MinValue, maxY = float.MinValue;

                        foreach (var corner in corners)
                        {
                            minX = Math.Min(minX, corner.X);
                            minY = Math.Min(minY, corner.Y);
                            maxX = Math.Max(maxX, corner.X);
                            maxY = Math.Max(maxY, corner.Y);
                        }

                        // Validate bounding box
                        int x = (int)Math.Max(0, minX);
                        int y = (int)Math.Max(0, minY);
                        int width = (int)(maxX - minX);
                        int height = (int)(maxY - minY);

                        if (width > 0 && height > 0 &&
                            x + width <= screenshot.Width &&
                            y + height <= screenshot.Height)
                        {
                            // Calculate confidence based on match quality
                            double confidence = (double)goodMatches.Count / Math.Max(templateKeypoints.Size, 1);
                            confidence = Math.Min(1.0, confidence);

                            results.Add(new DetectionResult
                            {
                                Found = true,
                                Confidence = confidence,
                                X = x,
                                Y = y,
                                Width = width,
                                Height = height,
                                DetectedAt = DateTime.UtcNow
                            });
                        }
                    }
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

    #endregion

    #region ROI-Based Search Methods

    public List<DetectionResult> FindTemplateInRegion(
        Bitmap screenshot,
        string templatePath,
        SearchRegion region,
        double threshold = 0.8)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        // If full screen, use regular method
        if (region.IsFullScreen)
        {
            return FindTemplate(screenshot, templatePath, threshold);
        }

        // Calculate pixel bounds from region ratios
        var (roiX, roiY, roiWidth, roiHeight) = region.ToPixelBounds(screenshot.Width, screenshot.Height);

        // Validate ROI bounds
        if (roiWidth <= 0 || roiHeight <= 0)
            return new List<DetectionResult>();

        using var templateBitmap = new Bitmap(templatePath);

        // Skip if template is larger than ROI
        if (templateBitmap.Width > roiWidth || templateBitmap.Height > roiHeight)
            return new List<DetectionResult>();

        // Extract ROI from screenshot
        using var roiBitmap = ExtractRegion(screenshot, roiX, roiY, roiWidth, roiHeight);

        // Find template in ROI
        var roiResults = FindTemplateInternal(roiBitmap, templateBitmap, threshold);

        // Transform coordinates back to full screenshot space
        return roiResults.Select(r => new DetectionResult
        {
            Found = r.Found,
            Confidence = r.Confidence,
            X = r.X + roiX,  // Add ROI offset
            Y = r.Y + roiY,
            Width = r.Width,
            Height = r.Height,
            DetectedAt = r.DetectedAt
        }).ToList();
    }

    public List<DetectionResult> FindTemplateMultiScaleInRegion(
        Bitmap screenshot,
        string templatePath,
        SearchRegion? region,
        double threshold = 0.7,
        double minScale = 0.5,
        double maxScale = 2.0,
        int scaleSteps = 10)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        // If no region or full screen, use regular method
        if (region == null || region.IsFullScreen)
        {
            return FindTemplateMultiScale(screenshot, templatePath, threshold, minScale, maxScale, scaleSteps);
        }

        // Calculate pixel bounds from region ratios
        var (roiX, roiY, roiWidth, roiHeight) = region.ToPixelBounds(screenshot.Width, screenshot.Height);

        // Validate ROI bounds
        if (roiWidth <= 0 || roiHeight <= 0)
            return new List<DetectionResult>();

        using var templateBitmap = new Bitmap(templatePath);

        // Extract ROI from screenshot
        using var roiBitmap = ExtractRegion(screenshot, roiX, roiY, roiWidth, roiHeight);

        // Find template in ROI using multi-scale
        var roiResults = FindTemplateMultiScaleInternal(roiBitmap, templateBitmap, threshold, minScale, maxScale, scaleSteps);

        // Transform coordinates back to full screenshot space
        return roiResults.Select(r => new DetectionResult
        {
            Found = r.Found,
            Confidence = r.Confidence,
            X = r.X + roiX,  // Add ROI offset
            Y = r.Y + roiY,
            Width = r.Width,
            Height = r.Height,
            DetectedAt = r.DetectedAt
        }).ToList();
    }

    public (int x, int y, int width, int height)? GetWindowBounds(string windowTitle)
    {
        IntPtr hwnd = FindWindow(null, windowTitle);
        if (hwnd == IntPtr.Zero)
            return null;

        if (!GetWindowRect(hwnd, out RECT rect))
            return null;

        return (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    /// <summary>
    /// Extract a rectangular region from a bitmap
    /// </summary>
    private static Bitmap ExtractRegion(Bitmap source, int x, int y, int width, int height)
    {
        // Clamp to valid bounds
        x = Math.Max(0, Math.Min(x, source.Width - 1));
        y = Math.Max(0, Math.Min(y, source.Height - 1));
        width = Math.Min(width, source.Width - x);
        height = Math.Min(height, source.Height - y);

        if (width <= 0 || height <= 0)
            return new Bitmap(1, 1);

        var destRect = new Rectangle(0, 0, width, height);
        var srcRect = new Rectangle(x, y, width, height);

        var result = new Bitmap(width, height, source.PixelFormat);
        using (var g = Graphics.FromImage(result))
        {
            g.DrawImage(source, destRect, srcRect, GraphicsUnit.Pixel);
        }

        return result;
    }

    #endregion
}
