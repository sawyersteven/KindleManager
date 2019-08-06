using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace KindleManager.Devices
{
    class DevManager
    {
        [Reactive]
        public DeviceBase[] DeviceList { get; set; }

        public DevManager()
        {
            FindDevices();
        }

        public DeviceBase OpenDevice(string Id)
        {
            DeviceBase SelectedDevice = DeviceList.FirstOrDefault(x => x.Id == Id);
            if (SelectedDevice == null)
            {
                throw new KeyNotFoundException($"Device with ID {Id} could not be opened.");
            }

            return SelectedDevice;
        }

        public void FindDevices()
        {
            List<DeviceBase> d = new List<DeviceBase>();
            d.AddRange(FindKindles());
            DeviceList = d.ToArray();
        }

        private DeviceBase[] FindKindles()
        {
            List<DeviceBase> devices = new List<DeviceBase>();

            foreach (ManagementObject drive in new ManagementObjectSearcher("select * from Win32_DiskDrive").Get())
            {
                if (((string)drive["PNPDeviceID"]).Contains("VEN_KINDLE"))
                {
                    foreach (ManagementObject o in drive.GetRelated("Win32_DiskPartition"))
                    {
                        foreach (ManagementObject i in o.GetRelated("Win32_LogicalDisk"))
                        {
                            devices.Add(new FSDevice(i.GetPropertyValue("Name") + "\\", (string)i.GetPropertyValue("VolumeName"), (string)drive.GetPropertyValue("Caption"), (string)i.GetPropertyValue("VolumeSerialNumber")));
                        }
                    }
                }
            }
            return devices.ToArray();
        }
    }
}
