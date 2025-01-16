using System.Runtime.InteropServices;

class NoRecoil(CancellationToken token, Arduino arduino, int baseRecoilAmount)
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_LBUTTON = 0x01;

    private int recoilStep = 0;
    private const int maxRecoilSteps = 50;
    private const int phaseChangeStep = 10;
    private int phase = 0;
    private Random random = new Random();

    public void InitNoRecoil()
    {
        while (!token.IsCancellationRequested)
        {
            if (IsLeftMouseButtonPressed())
            {
                Point recoilMovement = CalculateRecoilMovement();
                SendCoordinatesToArduino(recoilMovement);
                recoilStep++;
            }
            else
            {
                recoilStep = 0;
                phase = 0;
            }

            Thread.Sleep(10);
        }
    }

    private bool IsLeftMouseButtonPressed()
    {
        return (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
    }

    private Point CalculateRecoilMovement()
    {
        int xRecoil = 0;
        int yRecoil = recoilStep < maxRecoilSteps ? baseRecoilAmount + random.Next(0, 2) : 0;

        if (recoilStep >= maxRecoilSteps)
        {
            // Alterna entre fase 1 (esquerda) e fase 2 (direita) com base no recoilStep
            phase = (recoilStep / phaseChangeStep % 2) + 1;
        }

        if (phase == 0) // Fase de subida inicial
        {
            xRecoil = 0;
        }
        else if (phase == 1) // Movendo para a esquerda
        {
            xRecoil = -baseRecoilAmount + random.Next(-1, 2);
        }
        else if (phase == 2) // Movendo para a direita
        {
            xRecoil = baseRecoilAmount + random.Next(-1, 2);
        }

        return new Point(xRecoil, yRecoil);
    }


    private void SendCoordinatesToArduino(Point coordinates)
    {
        string message = $"MOVE{coordinates.X},{coordinates.Y}\n";
        arduino.Write(message);
    }
}
