using Appicket.Analytics.OpenAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Appicket.Analytics.OpenAPI
{
    public class Parser
    {
        public static async Task Parse(string path)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var content = client.GetStringAsync(path).Result;
                    var model = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenAPIDocument>(content);
                    if (model != null && model.paths != null && model.paths.Any())
                    {
                        model.RawContent = content;
                        await Analyzer.RegisterAPIDocumentation(model);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
