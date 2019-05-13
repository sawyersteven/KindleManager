using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace Devices
{
    class DevManager
    {
        public IDevice[] _DeviceList;
        public IDevice[] DeviceList { get => _DeviceList; }

        public IDevice SelectedDevice { get; set; }

        public DevManager()
        {
            _DeviceList = FindKindles();
        }

        public bool OpenDevice(string driveLetter)
        {
            SelectedDevice = DeviceList.FirstOrDefault(x => x.DriveLetter == driveLetter);
            if (SelectedDevice == null)
            {
                throw new KeyNotFoundException($"Drive {driveLetter} could not be opened.");
            }

            return SelectedDevice.FirstUse;
        }


        public static IDevice[] FindKindles()
        {
            List<IDevice> devices = new List<IDevice>();

            foreach (ManagementObject drive in new ManagementObjectSearcher("select * from Win32_DiskDrive").Get())
            {
                if (((string)drive["PNPDeviceID"]).Contains("VEN_KINDLE"))
                {
                    foreach (ManagementObject o in drive.GetRelated("Win32_DiskPartition"))
                    {
                        foreach (ManagementObject i in o.GetRelated("Win32_LogicalDisk"))
                        {
                            devices.Add(new Kindle(i.GetPropertyValue("Name") + "\\", (string)i.GetPropertyValue("VolumeName"), (string)drive.GetPropertyValue("Caption")));
                        }
                    }
                }
            }

#if DEBUG
            devices.Add(new Kindle("G:\\", "Emtec", "Kindle Device Caption"));
            devices.Add(new Kindle("Y:\\", "Kindle", "Kindle Device Caption"));
            devices.Add(new Kindle("Z:\\", "Kindle", "Kindle Device Caption"));
#endif
            return devices.ToArray();
        }
    }
}
