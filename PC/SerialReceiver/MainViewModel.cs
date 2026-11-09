using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO.Ports;

namespace SerialReceiver
{
    public partial class MainViewModel : ObservableRecipient, IWindowModel
    {
        //private readonly SerialPort _serialPort = new SerialPort();
        [ObservableProperty]
        private string _selectedComPort = string.Empty;
        [ObservableProperty]
        private bool _isSerialPortClosed = true;
        partial void OnSelectedComPortChanged(string value)
        {
            //if (!_serialPort.IsOpen)
            //{
            //    _serialPort.PortName = value;
            //}
        }
        [ObservableProperty]
        private double _baudRate = 9600;
        public ObservableCollection<string> AvailableComPorts = new ObservableCollection<string>();

        public void OnWindowLoaded()
        {
            AvailableComPorts.Clear();
            //var portNames = SerialPort.GetPortNames();
            //foreach (var portName in portNames)
            //{
            //    AvailableComPorts.Add(portName);
            //}

        }
    }
}
