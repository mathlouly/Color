using System.Drawing.Drawing2D;

class MenuOverlay : Form
{
    private const int _WS_EX_TRANSPARENT = 0x20;
    private const int _WS_EX_LAYERED = 0x80000;

    private bool _hiddenMenu;

    private bool _aimbotActive;
    private bool _triggetActive;
    private bool _noRecoilActive;
    private bool _spinnerActive;

    private string _serialPortName;
    private string _enemyColor;
    private int _fov;


    public MenuOverlay(string serialPortName, string enemyColor, int fov)
    {
        _serialPortName = serialPortName;
        _enemyColor = enemyColor;
        _fov = fov;

        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.Black;
        this.TransparencyKey = Color.Black;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.DoubleBuffered = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_hiddenMenu) return;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(10, 10, 140, 300);

        var paddingWidth = 16;
        var heightLine = 28;

        Brush colorEnemy = Colors.GetBrushFromColorName(_enemyColor);

        using (Brush brush = new SolidBrush(Color.FromArgb(128, Color.DimGray)))
        {
            g.FillRectangle(brush, rect);

            // Text
            g.DrawString("Menu", new Font("Arial", 14, FontStyle.Bold), Brushes.White, paddingWidth, paddingWidth);

            g.DrawString("F1 Aimbot", new Font("Arial", 12), _aimbotActive ? Brushes.GreenYellow : Brushes.Red, paddingWidth, heightLine * 2);
            g.DrawString("F2 Trigger", new Font("Arial", 12), _triggetActive ? Brushes.GreenYellow : Brushes.Red, paddingWidth, heightLine * 3);
            g.DrawString("F3 NoRecoil", new Font("Arial", 12), _noRecoilActive ? Brushes.GreenYellow : Brushes.Red, paddingWidth, heightLine * 4);
            g.DrawString("F4 Spinner", new Font("Arial", 12), _spinnerActive ? Brushes.GreenYellow : Brushes.Red, paddingWidth, heightLine * 5);
            g.DrawString("F5 Exit", new Font("Arial", 12), Brushes.White, paddingWidth, heightLine * 6);

            g.DrawString("Settings", new Font("Arial", 14, FontStyle.Bold), Brushes.White, paddingWidth, heightLine * 8);

            g.DrawString($"Port: {_serialPortName}", new Font("Arial", 11), colorEnemy, paddingWidth, heightLine * 9);
            g.DrawString($"Fov: {_fov}", new Font("Arial", 11), colorEnemy, paddingWidth, heightLine * 9 + 16);
            g.DrawString($"Color: {_enemyColor}", new Font("Arial", 11), colorEnemy, paddingWidth, heightLine * 9 + 32);
        }
    }

    public void UpdateMenuOptions(bool hiddenMenu, bool aimbotActive, bool triggetActive, bool noRecoilActive, bool spinnerActive)
    {
        _hiddenMenu = hiddenMenu;
        _aimbotActive = aimbotActive;
        _triggetActive = triggetActive;
        _noRecoilActive = noRecoilActive;
        _spinnerActive = spinnerActive;
        Invalidate();
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
}