using System;
using System.Collections.Generic;
using System.Text;

namespace Appicket.Analytics.Models
{
    public class ApplicationConfigModel
    {
        public long ID { get; set; }
        public long ReferencedID { get; set; }

        /// <summary>
        /// Device Type Name which you have created at appicket.com. Visit <see href="https://www.appicket.com/Tracking/DeviceTypeList"/>
        /// </summary>
        public string DeviceType { get; set; }

        /// <summary>
        /// Local System Account/User/Member ID to relate appicket data with your local data
        /// </summary>
        public string AccountID { get; set; }

        /// <summary>
        /// Local System Uername/Code/etc... to relate appicket data with your local data
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// ClientID which you have created at appicket.com. Visit <see href="https://www.appicket.com/Membership/ApplicationDomainList"/>
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// ClientSecret which you have created at appicket.com. Visit <see href="https://www.appicket.com/Membership/ApplicationDomainList"/>
        /// </summary>
        public string ClientSecret { get; set; }
        
        /// <summary>
        /// Device ID which app runs on
        /// </summary>
        public string DeviceID { get; set; }

        /// <summary>
        /// Device Model which app runs on
        /// </summary>
        public string DeviceModel { get; set; }

        /// <summary>
        /// Device Manufacturer
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Device Production Year
        /// </summary>
        public string Year { get; set; }

        /// <summary>
        /// Additional Data
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Environment app runs on PROD, TEST, DEV, etc...
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Device Used Memory
        /// </summary>
        public long UsedMemory { get; set; }

        /// <summary>
        /// Device Available Memory
        /// </summary>
        public long AvailableMemory { get; set; }

        /// <summary>
        /// Device Total Memory
        /// </summary>
        public long TotalMemory { get; set; }

        /// <summary>
        /// Device/App CPU Usage
        /// </summary>
        public long CPUUsage { get; set; }

        /// <summary>
        /// Device/App Operationg System Name
        /// </summary>
        public string OperatingSystem { get; set; }

        /// <summary>
        /// Device/App Operating System Version
        /// </summary>
        public string OSVersion { get; set; }

        /// <summary>
        /// Device/App Brand Name
        /// </summary>
        public string BrandName { get; set; }

        /// <summary>
        /// Device/App Firmware
        /// </summary>
        public string Firmware { get; set; }

        /// <summary>
        /// Screen Resolution, DPI
        /// </summary>
        public string ScreenSize { get; set; }

        /// <summary>
        /// User Screen Width And Height (1920x1080)
        /// </summary>
        public string ScreenWidthHeight { get; set; }

        /// <summary>
        /// A: Active, B: Background, C: Closed
        /// </summary>
        public string ApplicationStatus { get; set; }

        public bool CreateInitialSession { get; set; } = true;
    }
}
