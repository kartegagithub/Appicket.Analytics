using Appicket.Analytics.OpenAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Appicket.Analytics.OpenAPI
{
    public class Parser
    {
        private string Path { get; set; }
        private Analyzer Analyzer { get; set; }

        public static void Parse(string path, Analyzer analyzer)
        {
            var parser = new Parser(path);
            parser.Analyzer = analyzer;
            var thread = new System.Threading.Thread(parser.ParseInternal);
            thread.Start();
        }
        private void ParseInternal()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var content = client.GetStringAsync(this.Path).Result;
                    var model = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAPIDocument>(content);
                    if (model != null && model.paths != null && model.paths.Any())
                    {
                        this.Analyzer.RegisterAPIDocumentation(model);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        public Parser(string path)
        {
            this.Path = path;
        }
    }
}
