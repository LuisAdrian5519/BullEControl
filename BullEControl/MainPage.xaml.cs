#if ANDROID
using Android.App;
using Android.Content;
using Android.Hardware.Usb;
#endif
using BullEControl.Services;
using Microsoft.Maui.Controls;

namespace BullEControl;

public partial class MainPage : ContentPage
{
    private readonly IUsbMotorService _usbService;

#if ANDROID
    private const int VENDOR_ID = 0x1A86;
    private const int PRODUCT_ID = 0x7523;
    private const string ACTION_USB_PERMISSION = "com.bullecontrol.USB_PERMISSION";

    private BroadcastReceiver? _usbPermissionReceiver;
#endif

    public MainPage(IUsbMotorService usbService)
    {
        InitializeComponent();
        _usbService = usbService;

#if ANDROID
        // Register receiver to handle the permission result
        _usbPermissionReceiver = new PermissionReceiver(_usbService, this);
        var filter = new IntentFilter(ACTION_USB_PERMISSION);
        Android.App.Application.Context.RegisterReceiver(_usbPermissionReceiver, filter);
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

#if ANDROID
        if (_usbPermissionReceiver != null)
        {
            try
            {
                Android.App.Application.Context.UnregisterReceiver(_usbPermissionReceiver);
                _usbPermissionReceiver = null;
            }
            catch
            {
                // ignore unregister errors
            }
        }
#endif
    }

    void OnConnectClicked(object s, EventArgs e)
    {
#if ANDROID
        var usbManager = (UsbManager)Android.App.Application.Context
                            .GetSystemService(Context.UsbService)!;

        UsbDevice? targetDevice = null;
        foreach (var device in usbManager.DeviceList!.Values)
        {
            if (device.VendorId == VENDOR_ID && device.ProductId == PRODUCT_ID)
            {
                targetDevice = device;
                break;
            }
        }

        if (targetDevice == null)
        {
            StatusLabel.Text = "❌ Dispositivo no encontrado";
            return;
        }

        if (!usbManager.HasPermission(targetDevice))
        {
            var permissionIntent = PendingIntent.GetBroadcast(
                Android.App.Application.Context, 0,
                new Intent(ACTION_USB_PERMISSION),
                PendingIntentFlags.Mutable);
            usbManager.RequestPermission(targetDevice, permissionIntent);
            StatusLabel.Text = "⏳ Esperando permiso USB...";
            return;
        }
#endif
        bool ok = _usbService.Connect();
        StatusLabel.Text = ok ? "✅ Conectado" : "❌ Dispositivo no encontrado";
        ForwardBtn.IsEnabled = ok;
        BackwardBtn.IsEnabled = ok;
    }

    void OnForwardClicked(object s, EventArgs e)
    {
        // Sends ASCII '1' (0x31)
        bool ok = _usbService.SendCommand(0x31);
        StatusLabel.Text = ok ? "▶ Avanzando..." : "❌ Error al enviar";
    }

    void OnBackwardClicked(object s, EventArgs e)
    {
        // Sends ASCII '0' (0x30)
        bool ok = _usbService.SendCommand(0x30);
        StatusLabel.Text = ok ? "◀ Retrocediendo..." : "❌ Error al enviar";
    }

    void OnDisconnectClicked(object s, EventArgs e)
    {
        _usbService.Disconnect();       
        StatusLabel.Text = "⚙️ Desconectado";
        ForwardBtn.IsEnabled = false;
        BackwardBtn.IsEnabled = false;
    }

#if ANDROID
    // Receiver that will be invoked when the permission dialog result returns.
    class PermissionReceiver : BroadcastReceiver
    {
        private readonly IUsbMotorService _service;
        private readonly MainPage _page;

        public PermissionReceiver(IUsbMotorService service, MainPage page)
        {
            _service = service;
            _page = page;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null || intent.Action != ACTION_USB_PERMISSION)
                return;

            // Try to connect now that permission response arrived.
            // Connect() will return false if permission was denied or device absent.
            bool ok = _service.Connect();

            // Update UI on main thread
            _page.Dispatcher.Dispatch(() =>
            {
                _page.StatusLabel.Text = ok ? "✅ Conectado" : "❌ Permiso denegado o dispositivo no encontrado";
                _page.ForwardBtn.IsEnabled = ok;
                _page.BackwardBtn.IsEnabled = ok;
            });
        }
    }
#endif
}