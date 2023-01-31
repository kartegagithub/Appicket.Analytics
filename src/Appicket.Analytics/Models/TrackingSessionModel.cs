using System;

namespace Appicket.Analytics.Models
{
    public class TrackingSessionModel: ApplicationConfigModel
    {

        /// <summary>
        /// Session Start Date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// Session End Date. Optional. When ApplicationStatus is 'C' then system closes all sessions on device
        public DateTime EndDate { get; set; }
    }
}
