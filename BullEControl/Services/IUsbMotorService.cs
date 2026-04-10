using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BullEControl.Services
{
    public interface IUsbMotorService
    {
        bool Connect();
        void Disconnect();
        bool SendCommand(string command);
        bool IsConnected { get; }
    }
}