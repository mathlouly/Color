using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;

class AimbotColor
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


    private const int VK_XBUTTON2 = 0x06;

    private readonly CancellationToken token;
    private readonly Arduino arduino;
    private readonly AimbotOverlay aimbotOverlay;
    private readonly int fov;
    private readonly string enemyColor;

    public AimbotColor(CancellationToken token, Arduino arduino, AimbotOverlay aimbotOverlay, int fov, string enemyColor)
    {
        this.token = token;
        this.arduino = arduino;
        this.aimbotOverlay = aimbotOverlay;
        this.fov = fov;
        this.enemyColor = enemyColor;
    }

    public void InitAimbot()
    {
        Mat screen = null;
        try
        {
            while (!token.IsCancellationRequested)
            {
                bool isPressed = IsSideFrontMouseButtonPressed();

                UpdateOverlayState(isPressed);

                if (isPressed)
                {
                    screen = CaptureRegionFov();
                    if (screen != null)
                    {
                        Point? target = FindTargetHS(screen);

                        if (target.HasValue)
                        {
                            int relativeTargetX = (target.Value.X - fov) / 2;
                            int relativeTargetY = ((target.Value.Y - fov) / 2) + 3;

                            Point screenCoordinates = new(relativeTargetX, relativeTargetY);
                            SendCoordinatesToArduino(screenCoordinates);
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

    private void UpdateOverlayState(bool isPressed)
    {
        if (aimbotOverlay != null && aimbotOverlay.IsHandleCreated)
        {
            aimbotOverlay.Invoke(new Action(() => aimbotOverlay.UpdateButtonState(isPressed)));
        }
    }

    private Mat CaptureRegionFov()
    {
        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

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
        var lowerBound = Colors.GetColorBounds(enemyColor).Item1;
        var upperBound = Colors.GetColorBounds(enemyColor).Item2;

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

    private Point? FindTargetHS(Mat screen)
    {
        var lowerBound = Colors.GetColorBounds(enemyColor).Item1;
        var upperBound = Colors.GetColorBounds(enemyColor).Item2;

        using Mat hsvImage = new();
        using Mat mask = new();

        CvInvoke.CvtColor(screen, hsvImage, ColorConversion.Bgr2Hsv);

        CvInvoke.InRange(hsvImage, new ScalarArray(lowerBound), new ScalarArray(upperBound), mask);

        byte[] maskData = new byte[mask.Rows * mask.Cols];
        mask.CopyTo(maskData);

        for (int y = 0; y < mask.Rows; y++)
        {
            for (int x = 0; x < mask.Cols; x++)
            {
                int index = y * mask.Cols + x;

                if (maskData[index] == 255)
                {
                    return new Point(x, y);
                }
            }
        }

        return null;
    }

    private bool IsSideFrontMouseButtonPressed()
    {
        return (GetAsyncKeyState(VK_XBUTTON2) & 0x8000) != 0;
    }

    private void SendCoordinatesToArduino(Point coordinates)
    {
        string message = $"MOVE{coordinates.X},{coordinates.Y}\n";
        arduino.Write(message);
    }
}