using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO.Ports;
using CommunityToolkit.Mvvm.Input;
using System.Configuration;

namespace SerialReceiver
{
    public partial class MainViewModel : ObservableRecipient, IWindowModel
    {
        public SettingsReader? AppSettings { get; private set; }
        //private readonly SerialPort _serialPort = new SerialPort();
        [ObservableProperty]
        private string _selectedComPort = string.Empty;
        [ObservableProperty]
        private int _selectedComPortIndex = -1;
        [ObservableProperty]
        private bool _isSerialPortClosed = true;
        [ObservableProperty]
        private bool _showComPortList = false;
        partial void OnSelectedComPortChanged(string value)
        {
            //if (!_serialPort.IsOpen)
            //{
            //    _serialPort.PortName = value;
            //}
            if (AppSettings?.Default != null)
                AppSettings.Default.LastComPort = value;
        }
        [ObservableProperty]
        private double _baudRate = 9600;
        [RelayCommand]
        private void OpenClose()
        {
            // Implementation for opening/closing the serial port
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

        }

        public void OnWindowClosing()
        {
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
    }
}
