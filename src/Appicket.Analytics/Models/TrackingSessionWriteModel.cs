using System.Collections.Generic;

namespace Appicket.Analytics.Models
{
    public class TrackingSessionWriteModel
    {
        public long SessionID { get; set; }
        public long ReferencedSessionID { get; set; }
        public List<TrackingLogModel> Logs { get; set; } = new List<TrackingLogModel>();
        public List<TrackingRequestLogModel> Requests { get; set; } = new List<TrackingRequestLogModel>();
        public List<TrackingPageViewModel> PageViews { get; set; } = new List<TrackingPageViewModel>(); 
        public List<TrackingExceptionLogModel> Exceptions { get; set; } = new List<TrackingExceptionLogModel>();    
        public List<TrackingInteractionModel> Interactions { get; set; } = new List<TrackingInteractionModel>();    
        public List<ProductSalesModel> ProductSales { get; set; } = new List<ProductSalesModel>();  
        public List<ProductViewModel> ProductViews { get; set; } = new List<ProductViewModel>();    
    }
}
