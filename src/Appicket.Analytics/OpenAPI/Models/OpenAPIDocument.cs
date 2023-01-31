using System;
using System.Collections.Generic;
using System.Text;

namespace Appicket.Analytics.OpenAPI.Models
{
    public class OpenAPIDocument
    {
        public OpenAPIDocumentInfo info { get; set; }
        public string host { get; set; }
        public string port { get; set; }
        public string[] schemes { get; set; }
        public Dictionary<string, OpenAPIEndpointMethod> paths { get; set; }
    }

    public class OpenAPIDocumentInfo
    {
        public string title { get; set; }
        public string description { get; set; }
        public string version { get; set; }
    }
    public class OpenAPIEndpointMethod
    {
        public OpenAPIEndpointMethodDetail post { get; set; }
    }

    public class OpenAPIEndpointMethodDetail
    {
        public string[] tags { get; set; }
        public string[] produces { get; set; }
        public string[] consumes { get; set; }
        public string operationId { get; set; }
    }
}
