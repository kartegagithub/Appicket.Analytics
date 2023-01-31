namespace Appicket.Analytics.Models
{
    public class TrackingInteractionModel
    {
        public string PagePath { get; set; }
        public string Type { get; set; }
        public string ObjectID { get; set; }
        public string ObjectName { get; set; }
        public string Identifier1 { get; set; }
        public string Data { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }
        public string ParentObjectID { get; set; }
    }
}
