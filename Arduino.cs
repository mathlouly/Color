using System.IO.Ports;

class Arduino(string serialPortName)
{
    private SerialPort _serialPort;

    public void InitSerialPortConn()
    {
        _serialPort = new SerialPort(serialPortName, 9600);
        _serialPort.Open();
        Console.WriteLine($"Arduino open connection on {serialPortName}!");
        Thread.Sleep(1000);
    }

    public void Write(string message)
    {
        _serialPort.Write(message);
    }

    public void CloseSerialPortConn()
    {
        _serialPort.Close();
    }
}