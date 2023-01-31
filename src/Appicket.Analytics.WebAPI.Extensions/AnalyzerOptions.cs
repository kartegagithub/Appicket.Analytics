using Appicket.Analytics.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Appicket.Analytics.WebAPI.Extensions
{
    public class AnalyzerOptions
    {
        public string AppicketServerURL { get; set; } = "";
        public string ClientID { get; set; } = "";
        public string ClientSecret { get; set; } = "";

        /// <summary>
        /// swagger.json full path
        /// </summary>
        public string APIDocumentationPath { get; set; } = "";
        public bool IsWebAPI { get; set; }
        public Action<HttpContext, ApplicationConfigModel> ConfigureRequest { get; set; }
        public AnalyzerOptions()
        {
            
        }
    }
}
