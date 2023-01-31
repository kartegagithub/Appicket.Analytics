namespace Appicket.Analytics.Models
{
    public class ResponseModel
    {
        public string Value { get; set; }
        public string AuthKey { get; set; }
        public long SessionID { get; set; }
        public string Message { get; set; }
        public ConfigModel Config { get; set; }
    }
}
