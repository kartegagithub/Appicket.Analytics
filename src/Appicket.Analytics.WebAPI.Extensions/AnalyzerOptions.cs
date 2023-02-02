using Appicket.Analytics.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace Appicket.Analytics.WebAPI.Extensions
{
    public class AnalyzerOptions
    {
        public string AppicketServerURL { get; set; } = "";
        public string ClientID { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public bool EnableRequestBodyLogging { get; set; } = false;

        /// <summary>
        /// swagger.json full path
        /// </summary>
        public string APIDocumentationPath { get; set; } = "";
        public bool IsWebAPI { get; set; }
        public Action<HttpContext, ApplicationConfigModel> ConfigureRequest { get; set; }
        public List<string> HiddenRequestParams { get; private set; } = new List<string>();
        public AnalyzerOptions()
        {

        }
    }
}
