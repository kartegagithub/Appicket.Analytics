namespace Appicket.Analytics.Models
{
    public class TrackingRequestLogModel
    {
        /// <summary>
        /// Invoker Page path
        /// </summary>
        public string PagePath { get; set; }

        public string RequestType { get; set; }

        /// <summary>
        /// Requested url/api address
        /// </summary>
        public string RequestedURL { get; set; }

        /// <summary>
        /// Request Headers. [{Key, Value}]
        /// </summary>
        public string Headers { get; set; }

        /// <summary>
        /// POST parameters
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// POST, GET, etc...
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// application/json, etc...
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Optional: Respone body
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Response body length
        /// </summary>
        public long ResponseBodyLength { get; set; }

        public int Duration { get; set; }

        /// <summary>
        /// 200: OK, etc...
        /// </summary>
        public int ResponseCode { get; set; }

        /// <summary>
        /// 0: External, 1: Internal
        /// </summary>
        public int Direction { get; set; }
    }
}
