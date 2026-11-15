using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO.Ports;
using CommunityToolkit.Mvvm.Input;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace SerialReceiver;
    public partial class MainViewModel : ObservableRecipient, IWindowModel
    {
        public SettingsReader? AppSettings { get; private set; }
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    [ObservableProperty]
    private ObservableCollection<Frame> _frames = new ObservableCollection<Frame>();
    //private readonly SerialPort _serialPort = new SerialPort();
    [ObservableProperty]
        private string _selectedComPort = string.Empty;
        [ObservableProperty]
        private int _selectedComPortIndex = -1;
        [ObservableProperty]
        private bool _isSerialPortClosed = true;
        [ObservableProperty]
        private bool _showComPortList = false;
        private readonly SerialPort _serialPort = new SerialPort();
    partial void OnSelectedComPortChanged(string value)
    {
        if (!_serialPort.IsOpen)
        {
            _serialPort.PortName = value;
        }
    }
    [ObservableProperty]
    private int _baudRate = 9600;

    [ObservableProperty]
    private string _statusText = "Ready";

    private void UpdateStatusText()
    {
        if (!_serialPort.IsOpen)
        {
            StatusText = $"Port {SelectedComPort} closed.";
        }
        else if (_cancellationTokenSource.Token.IsCancellationRequested)
        {
            StatusText = $"Port {SelectedComPort} opened but not listening.";
        }
        else
        {
            StatusText = $"Listening on {SelectedComPort}...";
        }
    }

    private async Task<int> ListenAsync(CancellationToken ct)
    {
        char[] buffer = new char[1024];
        StreamReader sr = new(_serialPort.BaseStream);
        int bytesRead = 0;
        try
        {
            bytesRead = await sr.ReadAsync(buffer, ct);// .ReadBlockAsync(buffer, 0, buffer.Length);
            AddFrame(buffer);
            
        }
        catch (IOException ex)
        {
            Debug.WriteLine("Error (Listen) - " + ex.Message);
            return Task.FromResult(-1).Result;
        }
        return Task.FromResult(bytesRead).Result;
    }

    private void AddFrame(char[] buffer)
    {
        var frame = new Frame(new TimeMs(TimeOnly.FromDateTime(DateTime.Now)), new string(buffer), true);
        Frames.Add(frame);
        if (_serialPort.IsOpen)
        {
            _ = ListenAsync(_cancellationTokenSource.Token);
        }
    }

    [RelayCommand]
    private void Abort()
    {
        _cancellationTokenSource.Cancel();
        UpdateStatusText();
    }

    [RelayCommand]
    private void OpenClose()
    {
        if (_serialPort.IsOpen)
        {
            // Cancel the listening task and close the serial port
            _cancellationTokenSource.Cancel();
            _serialPort.Close();
        }
        else
        {
            _serialPort.BaudRate = BaudRate;
            _serialPort.PortName = SelectedComPort;
            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open {_serialPort.PortName}: {ex.Message}");
            }
            // If the serial port is open, copy the current settings to the AppSettings
            if (_serialPort.IsOpen)
            {
                if (AppSettings?.Default != null)
                {
                    AppSettings.Default.LastComPort = _serialPort.PortName;
                    AppSettings.Default.LastBaudRate = BaudRate;
                }
            }
        }
        // Then update the IsSerialPortClosed property to reflect the current state of the serial port
        IsSerialPortClosed = !_serialPort.IsOpen;
        if (_serialPort.IsOpen)
        {
            _ = ListenAsync(_cancellationTokenSource.Token);
        }
        UpdateStatusText();
    }
        public ObservableCollection<string> AvailableComPorts { get; } = new ObservableCollection<string>();

    public void OnWindowLoaded()
    {
        GetComPortList();
        AppSettings = new SettingsReader();
        if (AvailableComPorts.Any() && AppSettings?.Default?.LastComPort == null)
        {
            SelectedComPortIndex = 0;
        }
        else if (AppSettings.Default?.LastComPort != null && AvailableComPorts.Contains(AppSettings.Default.LastComPort))
        {
            SelectedComPortIndex = AvailableComPorts.IndexOf(AppSettings.Default.LastComPort);
        }
        if (AppSettings?.Default?.LastBaudRate != null)
        {
            BaudRate = (int)AppSettings.Default.LastBaudRate;
        }
    }

        public void OnWindowClosing()
        {
            if (_serialPort.IsOpen)
        {
            // Cancel the listening task and close the serial port
            _cancellationTokenSource.Cancel();
            _serialPort.Close();
            }
            AppSettings!.Save();
        }

        private void GetComPortList()
        {
            AvailableComPorts.Clear();
            var portNames = SerialPort.GetPortNames();
            foreach (var portName in portNames)
            {
                AvailableComPorts.Add(portName);
            }
        }

        [RelayCommand]
        private void RefreshComPorts()
        {
            GetComPortList();
            if (AvailableComPorts.Any())
            {
                ShowComPortList = true;
            }
        }
    public record Frame(TimeMs Timestamp, string Data, bool IsValid);
}

public class TimeMs
{
    private readonly TimeOnly _timeOnly;
    public TimeMs(TimeOnly timeOnly)
    {
        _timeOnly = timeOnly;
    }
    public override string ToString()
    {
        return _timeOnly.ToString("HH:mm:ss:ffff");
    }
}
