
class AimbotOverlay : Form
{
    private readonly CancellationToken _token;

    private const int _WS_EX_TRANSPARENT = 0x20;
    private const int _WS_EX_LAYERED = 0x80000;
    private readonly int _fov = 0;
    private bool _isButtonPressed = false;

    public AimbotOverlay(CancellationToken token, int fov)
    {
        _fov = fov;
        _token = token;

        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.Black;
        this.TransparencyKey = Color.Black;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.ShowInTaskbar = false;

        StartTokenMonitor();
    }

    public void UpdateButtonState(bool isPressed)
    {
        if (_isButtonPressed == isPressed) return;
        _isButtonPressed = isPressed;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;

        int diameter = _fov * 5 / 2;

        int x = (screenWidth - diameter) / 2;
        int y = (screenHeight - diameter) / 2;

        Color color = _isButtonPressed ? Color.LawnGreen : Color.Red;

        using (Pen pen = new(color, 1))
        {
            g.DrawEllipse(pen, x, y, diameter, diameter);
        }
    }
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= _WS_EX_TRANSPARENT | _WS_EX_LAYERED;
            cp.ExStyle |= 0x80;
            return cp;
        }
    }

    private async void StartTokenMonitor()
    {
        try
        {
            await Task.Run(() =>
            {
                while (!_token.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                }
            });

            if (_token.IsCancellationRequested)
            {
                Invoke(new Action(() =>
                {
                    Close();
                }));
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
