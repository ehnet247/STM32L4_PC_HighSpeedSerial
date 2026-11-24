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
using System.Windows.Threading;
using System.Windows;

namespace SerialReceiver;
    public partial class MainViewModel : ObservableRecipient, IWindowModel
    {
    public enum EoFDetection
    {
        Size,
        Time,
        Sequence
    }
    public Array EoFDetectionItemsSource => Enum.GetValues(typeof(EoFDetection));
        public SettingsReader? AppSettings { get; private set; }
    private CancellationTokenSource? _cancellationTokenSource;
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
        [ObservableProperty]
        private bool _useStreamReader = true;
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
    private int? _selectedEoFDetectionMode = (int?)EoFDetection.Size;
    [ObservableProperty]
    private string? _eoFDetectionParam = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _textDataReceived = string.Empty;

    private void UpdateStatusText()
    {
        if (!_serialPort.IsOpen)
        {
            StatusText = $"Port {SelectedComPort} closed.";
        }
        else if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
        {
            StatusText = $"Port {SelectedComPort} open but not listening.";
        }
        else
        {
            StatusText = $"Listening on {SelectedComPort}...";
        }
    }
    private int Listen(CancellationToken ct)
    {
        char[] buffer = new char[1024];
        StreamReader sr = new(_serialPort.BaseStream);
        int bytesRead = 0;
        int bytesToRead = _serialPort.BytesToRead;
        bool endOfFrameFound = false;
        while (bytesToRead < 1023 && !IsSerialPortClosed && !ct.IsCancellationRequested)
        {
            Task.Delay(10, ct);
            bytesToRead = _serialPort.BytesToRead;
        }
        if (!IsSerialPortClosed && !ct.IsCancellationRequested)
        {
            try
            {
                //bytesRead = await sr.ReadAsync(buffer, cancellationTokenSource.Token);
                bytesRead = sr.ReadBlock(buffer, 0, bytesToRead);
                if (bytesRead > 0)
                    endOfFrameFound = AddFrame(buffer);

            }
            catch (IOException ex)
            {
                Debug.WriteLine("Error (Listen) - " + ex.Message);
                //return Task.FromResult(-1).Result;
                return -1;
            }
            if (bytesRead == -1)
            {
                endOfFrameFound = true;
            }
        }
        //return Task.FromResult(bytesRead).Result;
        return bytesRead;
    }

    private async Task<int> ListenAsync(CancellationToken ct)
    {
        char[] buffer = new char[1024];
        StreamReader sr = new(_serialPort.BaseStream);
        int bytesRead = 0;
        int bytesToRead = _serialPort.BytesToRead;
        bool endOfFrameFound = false;
        while (bytesToRead < 1023 && !IsSerialPortClosed && !ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(10, ct);
            }
            catch (OperationCanceledException)
            {
                // Nothing to do
            }
            if (!ct.IsCancellationRequested)
            bytesToRead = _serialPort.BytesToRead;
        }
        if (!IsSerialPortClosed && !ct.IsCancellationRequested && sr != null)
        {
            try
            {
                bytesRead = await sr.ReadAsync(buffer, 0, bytesToRead);
                if (bytesRead > 0)
                    endOfFrameFound = await AddFrameAsync(buffer);

            }
            catch (IOException ex)
            {
                Debug.WriteLine("Error (Listen) - " + ex.Message);
                //return Task.FromResult(-1).Result;
                return -1;
            }
            if (bytesRead == -1)
            {
                endOfFrameFound = true;
            }
        }
        //return Task.FromResult(bytesRead).Result;
        return bytesRead;
    }

    private bool IsFrameValid(Memory<char> buffer)
    {
        ReadOnlySpan<char> span = buffer.Span;
        if (span.Contains('\0'))
            return true;
        return false;
    }

    private bool IsFrameValid(byte[] buffer)
    {
        if (buffer.Length == 1024)
            return true;
        return false;
    }

    private bool AddFrame(Memory<char> buffer)
    {
        bool validFrame = IsFrameValid(buffer);
        // If the frame contains the end of the frame character '\0', we only take the data up to that point and discard the rest
        if (validFrame)
        {
            int length = MemoryExtensions.IndexOf(buffer.Span.ToArray(), '\0') + 1;
            char[] usefullData = new char[length];
            buffer.Slice(0, length).CopyTo(usefullData);
            var usefullString = new string(usefullData);
            TextDataReceived += usefullString; //+ Environment.NewLine;
            var frame = new Frame(new TimeMs(TimeOnly.FromDateTime(DateTime.Now)), usefullData.Length, usefullString, true);
            Frames.Add(frame);
        }

        //if (_serialPort.IsOpen && UseStreamReader)
        //    _ = ListenAsync(_cancellationTokenSource.Token);
        return validFrame;
    }

    private async Task<bool> AddFrameAsync(char[] buffer)
    {
        var usefullString = new string(buffer);
        var validFrame = IsFrameValid(buffer);
        if (validFrame)
        {
            TextDataReceived += usefullString + Environment.NewLine;
            var frame = new Frame(new TimeMs(TimeOnly.FromDateTime(DateTime.Now)), buffer.Length, usefullString, true);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Frames.Add(frame);
            });
        }
        else
        {
            var frame = new Frame(new TimeMs(TimeOnly.FromDateTime(DateTime.Now)), buffer.Length, usefullString, false);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Frames.Add(frame);
            });
        }
        if (UseStreamReader && _serialPort.IsOpen)
        {
            if (_cancellationTokenSource == null)
                _cancellationTokenSource = new CancellationTokenSource();
            await ListenAsync(_cancellationTokenSource.Token);
        }
        return validFrame;
    }

    private async Task AddFrameAsync(byte[] buffer)
    {
        var validFrame = IsFrameValid(buffer);
        if (validFrame)
        {
            var usefullString = Encoding.ASCII.GetString(buffer);
            TextDataReceived += usefullString + Environment.NewLine;
            var frame = new Frame(new TimeMs(TimeOnly.FromDateTime(DateTime.Now)), buffer.Length, usefullString, true);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Frames.Add(frame);
            });
        }
        else
        {
            var frame = new Frame(new TimeMs(TimeOnly.FromDateTime(DateTime.Now)), buffer.Length, Encoding.ASCII.GetString(buffer), false);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Frames.Add(frame);
            });
        }
        if (UseStreamReader && _serialPort.IsOpen)
        {
            // Start a new listening task with a timeout of 1000 milliseconds
            //_cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
            if (_cancellationTokenSource == null)
                _cancellationTokenSource = new CancellationTokenSource();
            await ListenAsync(_cancellationTokenSource.Token);
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        // Unsubscribe from the DataReceived event to prevent re-entry while processing
        _serialPort.DataReceived -= SerialPort_DataReceived;
        _ = SerialPort_DataReceivedAsync();
    }

    private async Task SerialPort_DataReceivedAsync()
    {
        if (SelectedEoFDetectionMode != null &&
            ((EoFDetection)SelectedEoFDetectionMode == EoFDetection.Size))
        {
            int eoFSize = -1;
            if (int.TryParse(EoFDetectionParam, out eoFSize) &&
                _serialPort.BytesToRead >= eoFSize)
            {
                byte[] buffer = new byte[1024];
                var bytesRead = _serialPort.Read(buffer, 0, 1024);
                if (bytesRead == 1024)
                {
                    // Clear the buffer to avoid reading the same data again
                    _serialPort.DiscardInBuffer();
                    await AddFrameAsync(buffer);
                }
                // Re-subscribe to the DataReceived event after processing
                _serialPort.DataReceived += SerialPort_DataReceived;

            }
            else
            {
                int bytesToRead = _serialPort.BytesToRead;
                byte[] buffer = new byte[bytesToRead];
                //_serialPort.Read(buffer, 0, bytesToRead);
                //AddFrame(buffer);
            }
        }
    }

    [RelayCommand]
    private void Abort()
    {
        _cancellationTokenSource?.Cancel();
        UpdateStatusText();
    }

    [RelayCommand(AllowConcurrentExecutions= true)]
    private async Task OpenCloseAsync()
    {
        if (_serialPort.IsOpen)
        {
            // Cancel the listening task and close the serial port
            _cancellationTokenSource?.Cancel();
            _serialPort.Close();
            if (!UseStreamReader)
                _serialPort.DataReceived -= SerialPort_DataReceived;
        }
        else
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _serialPort.BaudRate = BaudRate;
            _serialPort.PortName = SelectedComPort;
            if (!UseStreamReader)
                _serialPort.ReadBufferSize = 1024;
            try
            {
                _serialPort.Open();
                _serialPort.DiscardInBuffer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open {_serialPort.PortName}: {ex.Message}");
            }
            // If the serial port is successfully open, copy the current settings to the AppSettings
            if (_serialPort.IsOpen)
            {
                if (AppSettings?.Default != null)
                {
                    AppSettings.Default.LastComPort = _serialPort.PortName;
                    AppSettings.Default.LastBaudRate = BaudRate;
                    AppSettings.Default.LastEoFDetectionMode = SelectedEoFDetectionMode;
                    AppSettings.Default.LastEoFDetectionParam = EoFDetectionParam;
                }
            }
        }
        // Then update the IsSerialPortClosed property to reflect the current state of the serial port
        IsSerialPortClosed = !_serialPort.IsOpen;
        UpdateStatusText();
        if (_serialPort.IsOpen)
        {
            if (UseStreamReader)
            {
                if (_cancellationTokenSource == null)
                    _cancellationTokenSource = new CancellationTokenSource();
                await ListenAsync(_cancellationTokenSource.Token);
            }
            else
            {
                _serialPort.DataReceived += SerialPort_DataReceived;
            }
        }
        else
        {
            if (UseStreamReader)
                _cancellationTokenSource?.Cancel();
            else
                _serialPort.DataReceived -= SerialPort_DataReceived;
        }
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
        if (AppSettings?.Default?.LastEoFDetectionMode != null && AppSettings.Default.LastEoFDetectionMode < Enum.GetValues(typeof(EoFDetection)).Length)
        {
            SelectedEoFDetectionMode = AppSettings.Default.LastEoFDetectionMode;
        }
        if (AppSettings?.Default?.LastEoFDetectionParam != null)
        {
            EoFDetectionParam = AppSettings.Default.LastEoFDetectionParam;
        }
    }

        public void OnWindowClosing()
        {
            if (_serialPort.IsOpen)
        {
            // Cancel the listening task and close the serial port
            _cancellationTokenSource?.Cancel();
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

    [RelayCommand]
    private void ClearFrames()
    {
        TextDataReceived = string.Empty;
        Frames.Clear();
    }
    public record Frame(TimeMs Timestamp, int Size, string Data, bool IsValid);
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
