using System.IO.Ports;

class Settings
{
    private readonly Arduino _arduino;

    private readonly string _configPath = "settings.config";
    private readonly Dictionary<string, dynamic> _settings = new() {
        { "serialPortName", "COM1" },
        { "fov", 40 },
        { "enemyColor", "purple" }
    };

    // Arduino
    public string[] _listSerialPortName;

    // CancellationToken
    private CancellationTokenSource _aimbotColorCts;
    private CancellationTokenSource _aimbotOverlayCts;
    private CancellationTokenSource _triggerCts;
    private CancellationTokenSource _noRecoilCts;
    private CancellationTokenSource _spinnerCts;

    // Features
    private bool _aimbotActive = false;
    private bool _triggerColorActive = false;
    private bool _noRecoilActive = false;
    private bool _spinnerActive = false;

    public Settings()
    {
        LoadConfig();
        InitArduinoConfig();
        InitAimbotColorConfig();
        InitAimbotOverlayConfig();

        // Arduino
        _arduino = new Arduino(_settings["serialPortName"]);
        _arduino.InitSerialPortConn();

        while (true)
        {
            SelectFeatures();

            // Check features active
            if (_aimbotActive) InitAimbotColor();
            if (!_aimbotActive && _aimbotColorCts != null && _aimbotOverlayCts != null)
            {
                _aimbotColorCts.Cancel();
                _aimbotOverlayCts.Cancel();
            }

            if (_triggerColorActive) InitTriggerColor();
            if (!_triggerColorActive && _triggerCts != null)
            {
                _triggerCts.Cancel();
            }

            if (_noRecoilActive) InitNoRecoil();
            if (!_noRecoilActive && _noRecoilCts != null)
            {
                _noRecoilCts.Cancel();
            }

            if (_spinnerActive) InitSpinner();
            if (!_spinnerActive && _spinnerCts != null)
            {
                _spinnerCts.Cancel();
            }
        }
    }

    void LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            using (var writer = new StreamWriter(_configPath))
            {
                foreach (var key in _settings.Keys)
                {
                    writer.WriteLine($"{key}={_settings[key]}");
                }
            }

            Console.WriteLine($"File '{_configPath}' success create!\n");
            Thread.Sleep(1500);
            Console.Clear();
        }

        foreach (var line in File.ReadAllLines(_configPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            {
                continue;
            }

            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                string value = parts[1].Trim();
                _settings[key] = value;
            }
        }
    }

    void WriterAllConfig()
    {
        using (var writer = new StreamWriter(_configPath))
        {
            foreach (var key in _settings.Keys)
            {
                writer.WriteLine($"{key}={_settings[key]}");
            }
        }
    }

    private void SelectFeatures()
    {
        Console.Clear();
        Console.Write($"""
        ### Menu ###

        1) Aimbot ({(_aimbotActive ? "ON" : "OFF")})
        2) Trigger ({(_triggerColorActive ? "ON" : "OFF")})
        3) NoRecoil ({(_noRecoilActive ? "ON" : "OFF")})
        4) Spinner ({(_spinnerActive ? "ON" : "OFF")})
        5) Exit

        Select an option: 
        """);

        string choice = Console.ReadLine();

        if (!string.IsNullOrEmpty(choice))
        {
            _ = int.TryParse(choice, out int choiceInt);

            if (choiceInt >= 1 && choiceInt <= 5)
            {
                switch (choiceInt)
                {
                    case 1:
                        _aimbotActive = !_aimbotActive;
                        break;
                    case 2:
                        _triggerColorActive = !_triggerColorActive;
                        break;
                    case 3:
                        _noRecoilActive = !_noRecoilActive;
                        break;
                    case 4:
                        _spinnerActive = !_spinnerActive;
                        break;
                    case 5:
                        Environment.Exit(0);
                        break;
                }
            }
        }
    }

    private void InitArduinoConfig()
    {
        _listSerialPortName = SerialPort.GetPortNames();

        if (_listSerialPortName.Length == 0)
        {
            Console.WriteLine("No serial port found.");
            return;
        }

        Console.WriteLine($"Serial ports found:");
        for (int i = 0; i < _listSerialPortName.Length; i++)
        {
            Console.WriteLine($"{i + 1}: {_listSerialPortName[i]}");
        }

        Console.Write($"Select a serial port (default={_settings["serialPortName"]}): ");
        string choice = Console.ReadLine();

        if (!string.IsNullOrEmpty(choice))
        {
            int choiceInt = int.Parse(choice);

            if (choiceInt > 0 && choiceInt <= _listSerialPortName.Length)
            {
                _settings["serialPortName"] = _listSerialPortName[choiceInt - 1];
                WriterAllConfig();
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter a number between 1 and {0}.", _listSerialPortName.Length);
            }
        }

        Console.Clear();
    }

    private void InitAimbotColorConfig()
    {
        Console.Write($"Enemy color (default={_settings["enemyColor"]}): ");
        string enemyColor = Console.ReadLine();

        if (!string.IsNullOrEmpty(enemyColor))
        {
            _settings["enemyColor"] = enemyColor;
            WriterAllConfig();
        }

        Console.Clear();
    }

    private void InitAimbotOverlayConfig()
    {
        Console.Write($"Define fov overlay aimbot (default={_settings["fov"]}): ");
        string fov = Console.ReadLine();
        int fovInt = 40;

        if (!string.IsNullOrEmpty(fov))
        {
            int.TryParse(_settings["fov"], out fovInt);

            if (fovInt > -1)
            {
                _settings["fov"] = fov;
                WriterAllConfig();
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter a number greater than 0.");

            }
        }

        int.TryParse(_settings["fov"], out fovInt);
        _settings["fov"] = fovInt;

        Console.Clear();
    }


    private void InitAimbotColor()
    {
        _aimbotOverlayCts = new();
        _aimbotColorCts = new();

        AimbotOverlay aimbotOverlay = null;

        Thread aimbotOverlayThread = new(() =>
        {
            aimbotOverlay = new AimbotOverlay(_aimbotOverlayCts.Token, _settings["fov"]);
            Application.Run(aimbotOverlay);
        });

        aimbotOverlayThread.SetApartmentState(ApartmentState.STA);
        aimbotOverlayThread.Start();

        Thread aimbotColorThread = new(() =>
        {
            while (aimbotOverlay == null)
            {
                Thread.Sleep(10);
            }

            var aimbotColor = new AimbotColor(_aimbotColorCts.Token, _arduino, aimbotOverlay, _settings["fov"], _settings["enemyColor"]);

            aimbotColor.InitAimbot();
        });

        aimbotColorThread.Start();
    }

    private void InitTriggerColor()
    {
        _triggerCts = new();

        Thread triggerColorThread = new(() =>
        {
            var triggerColor = new TriggerColor(_triggerCts.Token, _arduino, _settings["enemyColor"]);
            triggerColor.InitTriggerColor();
        });

        triggerColorThread.Start();
    }

    private void InitNoRecoil()
    {
        _noRecoilCts = new();

        Thread noRecoilThread = new(() =>
        {
            var noRecoil = new NoRecoil(_noRecoilCts.Token, _arduino, 2);
            noRecoil.InitNoRecoil();
        });

        noRecoilThread.Start();
    }

    private void InitSpinner()
    {
        _spinnerCts = new();

        Thread spinnerThread = new(() =>
        {
            var spinner = new Spinner(_spinnerCts.Token, _arduino);
            spinner.InitSpinner();
        });

        spinnerThread.Start();
    }
}