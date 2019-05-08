using Formats;

namespace Devices
{
    public interface IDevice
    {
        bool firstUse { get; }
        string configFile { get; }
        Config config { get; set; }

        void SendBook(IBook localBook);
    }
}
