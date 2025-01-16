using System.Runtime.InteropServices;

class Spinner(CancellationToken token, Arduino arduino)
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private const int VK_XBUTTON2 = 0x05; // Side btn back

    ManualResetEventSlim _pauseEvent = new(true);

    public void InitSpinner()
    {
        while (!token.IsCancellationRequested)
        {
            _pauseEvent.Wait();

            var isPressed = IsSideFrontMouseButtonPressed();

            if (isPressed)
            {
                arduino.Write("MOVE100,0\n");
            }
        }
    }

    private bool IsSideFrontMouseButtonPressed()
    {
        return (GetAsyncKeyState(VK_XBUTTON2) & 0x8000) != 0;
    }

    public void Pause()
    {
        _pauseEvent.Reset();
    }

    public void Resume()
    {
        _pauseEvent.Set();
    }
}