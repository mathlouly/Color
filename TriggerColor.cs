using System.Drawing.Imaging;
using System.Runtime.InteropServices;

class TriggerColor(CancellationToken token, Arduino arduino, string enemyColor)
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_XBUTTON2 = 0x06; // Side btn front

    public void InitTriggerColor()
    {
        Bitmap screen = null;
        while (!token.IsCancellationRequested)
        {
            try
            {

                var isPressed = IsSideFrontMouseButtonPressed();

                if (isPressed)
                {
                    using (screen = CaptureRegion())
                    {
                        bool shouldBreak = false;

                        for (int x = 0; x < screen.Width; x++)
                        {
                            if (shouldBreak) break;
                            for (int y = 0; y < screen.Height; y++)
                            {
                                Color pixelColor = screen.GetPixel(x, y);

                                if (IsColorInRange(pixelColor, Colors.GetColorByName(enemyColor), 100))
                                {
                                    Thread.Sleep(50);
                                    SendFireToArduino();
                                    Thread.Sleep(300);
                                    shouldBreak = true;
                                    break;
                                }
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
    }

    private Bitmap CaptureRegion()
    {
        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        int fov = 6;
        int diameter = fov * 2;

        if (diameter <= 0)
            throw new ArgumentException("FOV deve ser maior que zero.");

        int x = (screenWidth - diameter) / 2;
        int y = (screenHeight - diameter) / 2;

        if (x < 0 || y < 0 || x + diameter > screenWidth || y + diameter > screenHeight)
            throw new ArgumentException("A região calculada está fora dos limites da tela.");

        Rectangle region = new Rectangle(x, y, diameter, diameter);
        Bitmap bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    private bool IsColorInRange(Color pixel, Color target, int tolerance)
    {
        int rDiff = pixel.R - target.R;
        int gDiff = pixel.G - target.G;
        int bDiff = pixel.B - target.B;

        double distance = Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);

        return distance <= tolerance;
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