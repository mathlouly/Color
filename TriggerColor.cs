using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;

class TriggerColor(CancellationToken token, Arduino arduino, string enemyColor)
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    private const int VK_XBUTTON2 = 0x06; // Side btn front

    public void InitTriggerColor()
    {
        Mat screen = null;
        try
        {
            while (!token.IsCancellationRequested)
            {

                var isPressed = IsSideFrontMouseButtonPressed();

                if (isPressed)
                {
                    screen = CaptureRegionFov();
                    if (screen != null)
                    {
                        Point? target = FindTarget(screen);

                        if (target.HasValue)
                        {
                            SendFireToArduino();
                            Thread.Sleep(250);
                        }
                    }
                }
            }
        }
        finally
        {
            screen?.Dispose();
        }
    }

    private Mat CaptureRegionFov()
    {
        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        int fov = 4;
        int diameter = fov * 2;

        int x = (screenWidth - diameter) / 2;
        int y = (screenHeight - diameter) / 2;

        if (x < 0 || y < 0 || x + diameter > screenWidth || y + diameter > screenHeight || diameter <= 0)
        {
            Console.WriteLine("Região de captura fora dos limites da tela ou inválida.");
            return null;
        }

        Rectangle region = new Rectangle(x, y, diameter, diameter);
        Bitmap bitmap = new Bitmap(region.Width, region.Height);

        try
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                IntPtr desktopHandle = GetDesktopWindow();
                IntPtr desktopDC = GetWindowDC(desktopHandle);
                IntPtr graphicsDC = g.GetHdc();

                if (!BitBlt(graphicsDC, 0, 0, region.Width, region.Height, desktopDC, region.X, region.Y, 0x00CC0020))
                {
                    Console.WriteLine("Erro ao copiar a região usando BitBlt.");
                }

                g.ReleaseHdc(graphicsDC);
                ReleaseDC(desktopHandle, desktopDC);
            }

            Mat mat = new Mat();
            BitmapToMat(bitmap, mat);
            return mat;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao capturar a tela: {ex.Message}");
            return null;
        }
    }

    private void BitmapToMat(Bitmap bitmap, Mat mat)
    {
        if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb &&
            bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        {
            throw new NotSupportedException("Somente bitmaps em Format24bppRgb ou Format32bppArgb são suportados.");
        }

        System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            bitmap.PixelFormat
        );

        try
        {
            int channels = (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb) ? 3 : 4;

            mat.Create(bitmap.Height, bitmap.Width, Emgu.CV.CvEnum.DepthType.Cv8U, channels);

            using (Mat tempMat = new(bitmap.Height, bitmap.Width, Emgu.CV.CvEnum.DepthType.Cv8U, channels, bitmapData.Scan0, bitmapData.Stride))
            {
                tempMat.CopyTo(mat);
            }
        }
        finally
        {

            bitmap.UnlockBits(bitmapData);
        }
    }

    private Point? FindTarget(Mat screen)
    {
        var lowerBound = Colors2.GetColorBounds(enemyColor).Item1;
        var upperBound = Colors2.GetColorBounds(enemyColor).Item2;

        using (Mat hsvImage = new())
        using (Mat mask = new())
        {
            CvInvoke.CvtColor(screen, hsvImage, ColorConversion.Bgr2Hsv);

            CvInvoke.InRange(hsvImage, new ScalarArray(lowerBound), new ScalarArray(upperBound), mask);

            var moments = CvInvoke.Moments(mask, false);

            if (moments.M00 > 0)
            {
                int x = (int)(moments.M10 / moments.M00);
                int y = (int)(moments.M01 / moments.M00);
                return new Point(x, y);
            }
        }

        return null;
    }

    private bool IsSideFrontMouseButtonPressed()
    {
        return (GetAsyncKeyState(VK_XBUTTON2) & 0x8000) != 0;
    }

    private void SendFireToArduino()
    {
        string message = "FIRE\n";
        arduino.Write(message);
    }
}