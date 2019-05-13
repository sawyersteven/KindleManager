using Formats;

namespace Devices
{
    public interface IDevice
    {
        string DriveLetter { get; set; }
        bool FirstUse { get; }
        string ConfigFile { get; }
        DeviceConfig Config { get; set; }

        void SendBook(IBook localBook);

        void WriteConfig(DeviceConfig c);
    }
}
