using BullEControl.Services;

namespace BullEControl;

public partial class MainPage : ContentPage
{
    private readonly IUsbMotorService _usbService;

    public MainPage(IUsbMotorService usbService)
    {
        InitializeComponent();
        _usbService = usbService;
    }

    void OnConnectClicked(object s, EventArgs e)
    {
        bool ok = _usbService.Connect();
        StatusLabel.Text = ok ? "✅ Conectado" : "❌ Dispositivo no encontrado";
        ForwardBtn.IsEnabled = ok;
        BackwardBtn.IsEnabled = ok;
    }

    void OnForwardClicked(object s, EventArgs e)
    {
        bool ok = _usbService.SendCommand(0x31); // '1'
        StatusLabel.Text = ok ? "▶ Avanzando..." : "❌ Error al enviar";
    }

    void OnBackwardClicked(object s, EventArgs e)
    {
        bool ok = _usbService.SendCommand(0x30); // '0'
        StatusLabel.Text = ok ? "◀ Retrocediendo..." : "❌ Error al enviar";
    }

    void OnDisconnectClicked(object s, EventArgs e)
    {
        _usbService.Disconnect();
        StatusLabel.Text = "⚙️ Desconectado";
        ForwardBtn.IsEnabled = false;
        BackwardBtn.IsEnabled = false;
    }
}