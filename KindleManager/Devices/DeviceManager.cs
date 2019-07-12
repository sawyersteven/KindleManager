using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace KindleManager.Devices
{
    class DevManager
    {
        public Device[] DeviceList { get; set; }

        public DevManager()
        {
            FindDevices();
        }

        public Device OpenDevice(string driveLetter)
        {
            Device SelectedDevice = DeviceList.FirstOrDefault(x => x.DriveLetter == driveLetter);
            if (SelectedDevice == null)
            {
                throw new KeyNotFoundException($"Drive {driveLetter} could not be opened.");
            }

            return SelectedDevice;
        }

        public void FindDevices()
        {
            List<Device> d = new List<Device>();
            d.AddRange(FindKindles());
            DeviceList = d.ToArray();
        }
        private Device[] FindKindles()
        {
            List<Device> devices = new List<Device>();

            foreach (ManagementObject drive in new ManagementObjectSearcher("select * from Win32_DiskDrive").Get())
            {
                if (((string)drive["PNPDeviceID"]).Contains("VEN_KINDLE"))
                {
                    foreach (ManagementObject o in drive.GetRelated("Win32_DiskPartition"))
                    {
                        foreach (ManagementObject i in o.GetRelated("Win32_LogicalDisk"))
                        {
                            devices.Add(new Devices.Kindle(i.GetPropertyValue("Name") + "\\", (string)i.GetPropertyValue("VolumeName"), (string)drive.GetPropertyValue("Caption")));
                        }
                    }
                }
            }
#if DEBUG
            string debugDir = System.Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Desktop\\KindleManagerDebug");
            System.IO.Directory.CreateDirectory(debugDir);
            devices.Add(new Kindle("G:\\", "Kindle", "Kindle Dir DEBUG"));
#endif
            return devices.ToArray();
        }
    }
}
