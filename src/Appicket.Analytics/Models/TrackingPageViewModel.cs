namespace Appicket.Analytics.Models
{
    public class TrackingPageViewModel
    {
        public string PagePath { get; set; }
        public string Referer { get; set; }
        public int Duration { get; set; }
        public int ResponseCode { get; set; }
        public long ResponseBodyLength { get; set; }
    }
}
