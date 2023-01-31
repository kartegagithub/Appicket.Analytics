namespace Appicket.Analytics.Models
{
    public class ConfigModel
    {
        public bool EnablePageView { get; set; }
        public bool EnableLog { get; set; }
        public bool EnableExceptionLog { get; set; }
        public bool EnableRequest { get; set; }
        public bool EnableDeviceInfoUpdate { get; set; }
        public bool EnableInteractionTracking { get; set; }
        public string DeveloperMode { get; set; }
        public string RemoteConfigData { get; set; }
    }
}
