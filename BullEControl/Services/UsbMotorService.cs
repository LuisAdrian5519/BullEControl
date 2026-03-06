using Android.Content;
using Android.Hardware.Usb;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BullEControl.Services
{
    public class UsbMotorService : IUsbMotorService
    {
        private UsbManager? _usbManager;
        private UsbDevice? _device;
        private UsbDeviceConnection? _connection;
        private UsbEndpoint? _endpointOut;

        private const int VENDOR_ID = 0x2341; // Arduino Uno
        private const int PRODUCT_ID = 0x0043; // Arduino Uno

        public bool IsConnected => _connection != null;

        public bool Connect()
        {
            _usbManager = (UsbManager)Android.App.Application.Context
                            .GetSystemService(Context.UsbService)!;

            foreach (var device in _usbManager.DeviceList!.Values)
            {
                if (device.VendorId == VENDOR_ID && device.ProductId == PRODUCT_ID)
                {
                    _device = device;
                    break;
                }
            }

            if (_device == null) return false;

            var iface = _device.GetInterface(0);
            for (int i = 0; i < iface!.EndpointCount; i++)
            {
                var ep = iface.GetEndpoint(i);
                if (ep!.Direction == UsbAddressing.Out)
                    _endpointOut = ep;
            }

            _connection = _usbManager.OpenDevice(_device);
            _connection?.ClaimInterface(iface, true);

            return IsConnected;
        }

        public bool SendCommand(byte command)
        {
            if (!IsConnected || _endpointOut == null) return false;
            byte[] data = { command };
            int result = _connection!.BulkTransfer(_endpointOut, data, data.Length, 1000);
            return result >= 0;
        }

        public void Disconnect()
        {
            _connection?.Close();
            _connection = null;
        }
    }
}