#if ANDROID
using Android.Content;
using Android.Hardware.Usb;

namespace BullEControl.Services
{
    public class UsbMotorService : IUsbMotorService
    {
        private UsbManager? _usbManager;
        private UsbDevice? _device;
        private UsbDeviceConnection? _connection;
        private UsbEndpoint? _endpointOut;

        private const int VENDOR_ID = 0x1A86;
        private const int PRODUCT_ID = 0x7523;

        public bool IsConnected => _connection != null;

        public bool Connect()
        {
            _usbManager = (UsbManager)Android.App.Application.Context
                            .GetSystemService(Context.UsbService)!;

            if (_usbManager.DeviceList == null || _usbManager.DeviceList.Count == 0)
            {
                Android.Util.Log.Debug("BULLE", "DeviceList vacío");
                return false;
            }

            foreach (var device in _usbManager.DeviceList.Values)
            {
                Android.Util.Log.Debug("BULLE", $"Device encontrado: VID={device.VendorId:X4} PID={device.ProductId:X4}");
                if (device.VendorId == VENDOR_ID && device.ProductId == PRODUCT_ID)
                {
                    _device = device;
                    break;
                }
            }

            if (_device == null)
            {
                Android.Util.Log.Debug("BULLE", "Device target no encontrado");
                return false;
            }

            if (!_usbManager.HasPermission(_device))
            {
                Android.Util.Log.Debug("BULLE", "Sin permiso");
                return false;
            }

            for (int i = 0; i < _device.InterfaceCount; i++)
            {
                var iface = _device.GetInterface(i);
                for (int j = 0; j < iface!.EndpointCount; j++)
                {
                    var ep = iface.GetEndpoint(j);
                    Android.Util.Log.Debug("BULLE", $"Endpoint {j}: dir={ep!.Direction} type={ep.Type}");
                    if (ep.Direction == UsbAddressing.Out)
                    {
                        _endpointOut = ep;
                        _connection = _usbManager.OpenDevice(_device);
                        _connection?.ClaimInterface(iface, true);

                        // Reset CH340
                        _connection?.ControlTransfer((UsbAddressing)0x40, 0xA1, 0, 0, null, 0, 1000);

                        // Configurar 115200 baud CH340
                        int r1 = _connection?.ControlTransfer(
                            (UsbAddressing)0x40, 0x9A, 0x1312, 0xC3, null, 0, 1000) ?? -99;
                        int r2 = _connection?.ControlTransfer(
                            (UsbAddressing)0x40, 0x9A, 0x0F2C, 0x0008, null, 0, 1000) ?? -99;

                        Android.Util.Log.Debug("BULLE", $"ControlTransfer r1={r1} r2={r2}"); 
                        return IsConnected;
                    }
                }
            }

            Android.Util.Log.Debug("BULLE", "No se encontró endpoint OUT");
            return false;
        }

        public bool SendCommand(string command)
        {
            Android.Util.Log.Debug("BULLE", $"SendCommand called: {command}, IsConnected={IsConnected}, endpoint={_endpointOut != null}");
            if (!IsConnected || _endpointOut == null) return false;
            byte[] data = System.Text.Encoding.ASCII.GetBytes(command + "\n");
            int result = _connection!.BulkTransfer(_endpointOut, data, data.Length, 2000);
            Android.Util.Log.Debug("BULLE", $"BulkTransfer result: {result}");
            return result >= 0;
        }

        public void Disconnect()
        {
            _connection?.Close();
            _connection = null;
            _device = null;
            _endpointOut = null;
        }
    }
}
#endif