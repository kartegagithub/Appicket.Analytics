using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Appicket.Analytics.Loggers
{
    public class RequestLogHandler : DelegatingHandler
    {
        private string PageURL { get; set; } = "";
        public static RequestLogHandler CreateInstance(string pageURL = "")
        {
            return new RequestLogHandler(pageURL);
        }
        public static RequestLogHandler CreateInstance(HttpMessageHandler innerHandler, string pageURL = "")
        {
            return new RequestLogHandler(innerHandler, pageURL);
        }
        public RequestLogHandler(string pageURL = "") : this(null, pageURL)
        {

        }

        public RequestLogHandler(HttpMessageHandler innerHandler, string pageURL = "") : base(innerHandler)
        {
            this.PageURL = pageURL;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;

            var model = new Models.TrackingRequestLogModel()
            {
                Method = request.Method.ToString(),
                RequestedURL = request.RequestUri.ToString(),
                PagePath = this.PageURL
            };
            var watch = new Stopwatch();
            try
            {
                if (request.Content != null)
                {
                    model.Parameters = Encoding.UTF8.GetString(request.Content.ReadAsByteArrayAsync().Result);
                }
                if (request.Headers != null)
                {
                    model.Headers = Newtonsoft.Json.JsonConvert.SerializeObject(request.Headers);
                }
                watch.Start();
            }
            catch (System.Exception ex)
            {
                Analyzer.Current?.LogException(ex);
            }
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                try
                {
                    watch.Stop();
                    model.Duration = System.Convert.ToInt32(watch.ElapsedMilliseconds);
                    model.ResponseCode = (int)response.StatusCode;
                    byte[] responseMessage;

                    if (response.Content != null)
                    {
                        responseMessage = response.Content.ReadAsByteArrayAsync().Result;
                    }
                    else
                    {
                        responseMessage = Encoding.UTF8.GetBytes(response.ReasonPhrase);
                    }
                    model.ResponseBodyLength = responseMessage.Length;

                    Analyzer.Current?.LogRequest(model);
                }
                catch (System.Exception ex)
                {
                    Analyzer.Current?.LogRequest(model);
                    Analyzer.Current?.LogException(ex);
                }
            }
            catch (System.Exception ex)
            {
                model.ResponseCode = 501;
                Analyzer.Current?.LogRequest(model);
                Analyzer.Current?.LogException(ex);
            }
            finally
            {
                Analyzer.Current?.Commit();
                watch = null;
                model = null;
            }
            return response;
        }
    }
}
