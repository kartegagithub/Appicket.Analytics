using Appicket.Analytics.Models;
using Appicket.Analytics.OpenAPI.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Threading;

namespace Appicket.Analytics
{
    public class Analyzer
    {
        public static Analyzer Current => _CurrentAnalyzerHolder?.Value;
        private static AsyncLocal<Analyzer> _CurrentAnalyzerHolder { get; set; } = new AsyncLocal<Analyzer>();
        public static int? InstanceID { get; set; }
        public static bool? IsEnabled { get; set; }
        private static object RequestLockObj = new object();
        private static object EnableLockObj = new object();
        private static ResponseModel RemoteConfig { get; set; }

        private string Version { get; set; } = "";
        public ApplicationConfigModel Config { get; set; } = new ApplicationConfigModel();
        private TrackingSessionWriteModel CurrentSession { get; set; }
        private string ServerURL { get; set; }
        public IWebProxy Proxy { get; set; }
        public int Timeout { get; set; }
        public long? SessionID
        {
            get
            {
                return this.CurrentSession?.SessionID;
            }
            set
            {
                if (this.CurrentSession == null)
                    this.CurrentSession = new TrackingSessionWriteModel();
                this.CurrentSession.SessionID = value.GetValueOrDefault(0);
            }
        }

        public void LogRequest(TrackingRequestLogModel model)
        {
            if (this.IsActive && RemoteConfig.Config.EnableRequest && this.CurrentSession != null)
                this.CurrentSession.Requests.Add(model);
        }

        public void Interact(TrackingInteractionModel model)
        {
            if (this.IsActive && RemoteConfig.Config.EnableInteractionTracking && this.CurrentSession != null)
                this.CurrentSession.Interactions.Add(model);
        }

        public void ViewProduct(ProductViewModel model)
        {
            if (this.IsActive && this.CurrentSession != null)
                this.CurrentSession.ProductViews.Add(model);
        }

        public void SaleProduct(ProductSalesModel model)
        {
            if (this.IsActive && this.CurrentSession != null)
            {
                this.CurrentSession.ProductSales.Add(model);
            }
        }
        public void ViewPage(TrackingPageViewModel model)
        {
            if (this.IsActive && RemoteConfig.Config.EnablePageView && this.CurrentSession != null)
            {
                this.CurrentSession.PageViews.Add(model);
            }
        }

        public void Log(LogType type, string description)
        {
            if (this.IsActive && RemoteConfig.Config.EnableLog && this.CurrentSession != null)
            {
                this.CurrentSession.Logs.Add(new TrackingLogModel()
                {
                    Description = description,
                    Type = (byte)type
                });
            }
        }

        public void LogException(Exception ex)
        {
            if (this.IsActive && RemoteConfig.Config.EnableExceptionLog && this.CurrentSession != null)
            {
                this.CurrentSession.Exceptions.Add(new TrackingExceptionLogModel()
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                });
                ex = ex.InnerException;
                while (ex != null)
                {
                    this.CurrentSession.Exceptions.Add(new TrackingExceptionLogModel()
                    {
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                    });
                    ex = ex.InnerException;
                }
            }
        }
        public void CheckIsEnabled(Action deferredFunc)
        {
            try
            {
                if (this.Config == null || string.IsNullOrEmpty(this.Config.ClientID) || string.IsNullOrEmpty(this.Config.DeviceType) || string.IsNullOrEmpty(this.Config.ClientSecret))
                {
                    IsEnabled = false;
                    return;
                }

                if (!IsEnabled.HasValue)
                {
                    lock (EnableLockObj)
                    {
                        if (!IsEnabled.HasValue)
                        {
                            var config = this.GetConfig();
                            var result = this.PostData("/public/IsEnabled", config);
                            if (!string.IsNullOrEmpty(result))
                            {
                                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseModel>(result);
                                if (model.Value == "1" && model.Config != null)
                                {
                                    RemoteConfig = model;
                                    IsEnabled = RemoteConfig.Config.EnableRequest || RemoteConfig.Config.EnableDeviceInfoUpdate || RemoteConfig.Config.EnableExceptionLog || RemoteConfig.Config.EnableLog || RemoteConfig.Config.EnablePageView || RemoteConfig.Config.EnableInteractionTracking;
                                    this.CurrentSession.SessionID = model.SessionID;
                                    if (deferredFunc != null)
                                        deferredFunc();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public void UpdateDeviceInfo()
        {
            if (this.IsActive && RemoteConfig.Config.EnableDeviceInfoUpdate)
            {
                this.RunInTread(() =>
                {
                    this.PostData("/public/UpdateDeviceInfo", this.GetConfig());
                });
            }
        }
        public void EndSession()
        {
            if (this.IsActive && this.CurrentSession != null && this.CurrentSession.SessionID > 0)
            {
                this.RunInTread(() =>
                {
                    this.PostData("/public/EndSession", new TrackingSessionModel() { ID = this.CurrentSession.SessionID });
                });
            }
        }
        public void Commit()
        {
            if (this.IsActive && this.CurrentSession != null && (this.CurrentSession.Exceptions.Any() || this.CurrentSession.Logs.Any() || this.CurrentSession.Interactions.Any() || this.CurrentSession.Requests.Any() || this.CurrentSession.PageViews.Any() || this.CurrentSession.ProductSales.Any() || this.CurrentSession.PageViews.Any()))
            {
                this.RunInTread(() =>
                {
                    this.StartSession();
                    this.PostData("/public/TrackSession", this.CurrentSession);
                });
            }
        }
        public void StartSession(bool force = false)
        {
            if (this.IsActive && (force || this.CurrentSession == null || this.CurrentSession.SessionID <= 0))
            {
                if (this.CurrentSession == null)
                    this.CurrentSession = new TrackingSessionWriteModel();

                var result = this.PostData("/public/StartSession", this.GetConfig());
                if (!string.IsNullOrEmpty(result))
                {
                    var model = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseModel>(result);
                    if (model.SessionID > 0)
                        this.CurrentSession.SessionID = model.SessionID;
                }
            }
        }
        private string PostData(string url, object data)
        {
            try
            {
                lock (RequestLockObj)
                {
                    using (var clientHandler = new HttpClientHandler())
                    {
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", $"Appicket.Analytics {this.Version}");
                            clientHandler.Proxy = this.Proxy;
                            clientHandler.UseProxy = this.Proxy != null;

                            var request = new HttpRequestMessage(HttpMethod.Post, $"{this.ServerURL}{url}");
                            if (data != null)
                                request.Content = JsonContent.Create(data, data.GetType(), new MediaTypeHeaderValue("application/json"));

                            if (RemoteConfig != null && !string.IsNullOrEmpty(RemoteConfig.AuthKey))
                                request.Content.Headers.Add("X-Auth-Key", RemoteConfig.AuthKey);

                            if (this.Timeout > 0)
                                client.Timeout = TimeSpan.FromMilliseconds(this.Timeout);
                            var response = client.SendAsync(request).Result;
                            if (response.IsSuccessStatusCode)
                                return response.Content.ReadAsStringAsync().Result;
                            else
                                return "";
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        public bool IsActive
        {
            get
            {
                return IsEnabled.HasValue && IsEnabled.Value;
            }
        }
        private ApplicationConfigModel GetConfig()
        {
            var process = Process.GetCurrentProcess();
            if (process != null)
            {
                this.Config.ID = this.CurrentSession.SessionID;
                this.Config.ReferencedID = this.CurrentSession.ReferencedSessionID;
                this.Config.AvailableMemory = process.WorkingSet64;
                this.Config.CPUUsage = Convert.ToInt64(process.TotalProcessorTime.TotalMilliseconds);
            }
            return this.Config;
        }
        public Analyzer(ApplicationConfigModel config, string serverURL = "", long sessionID = 0, long refSessionID = 0, string openAPIDocPath = "")
        {
            this.Config = config;
            if (string.IsNullOrEmpty(config.DeviceID))
                config.DeviceID = Environment.MachineName;
            if (string.IsNullOrEmpty(config.OperatingSystem))
                config.OperatingSystem = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
            if (string.IsNullOrEmpty(config.OSVersion))
                config.OSVersion = $"{Environment.OSVersion.VersionString}";
            if (string.IsNullOrEmpty(config.DeviceModel))
                config.DeviceModel = $"{Environment.OSVersion.Platform}";
            if (string.IsNullOrEmpty(config.Firmware))
                config.Firmware = $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}";
            if (string.IsNullOrEmpty(config.ApplicationStatus))
                config.ApplicationStatus = "A";
            this.Version = typeof(Analyzer).Assembly.GetName().Version.ToString();
            this.ServerURL = serverURL;
            if (string.IsNullOrEmpty(this.ServerURL))
                this.ServerURL = "https://api.appicket.com";

            this.CurrentSession = new TrackingSessionWriteModel() { SessionID = sessionID, ReferencedSessionID = refSessionID };
            if (!InstanceID.HasValue)
            {
                InstanceID = new Random().Next(int.MaxValue);
                this.CheckIsEnabled(() => {
                    if (!string.IsNullOrEmpty(openAPIDocPath))
                        this.RegisterAPIDocumentation(openAPIDocPath);
                });
            }
            else
            {
                this.StartSession(true);
            }

            _CurrentAnalyzerHolder.Value = this;
        }

        public void ResolveHeaders(HttpResponseMessage response)
        {
            if (this.CurrentSession == null || response == null)
                return;

            if (response.Headers.Contains("X-APPICKET-SID"))
                this.CurrentSession.SessionID = Convert.ToInt64(response.Headers.GetValues("X-APPICKET-SID").FirstOrDefault());
            if (response.Headers.Contains("X-REF-APPICKET-SID"))
                this.CurrentSession.ReferencedSessionID = Convert.ToInt64(response.Headers.GetValues("X-REF-APPICKET-SID").FirstOrDefault());
            if (response.Headers.Contains("X-VIEW-APPICKET-SID"))
                this.CurrentSession.ReferencedSessionID = Convert.ToInt64(response.Headers.GetValues("X-VIEW-APPICKET-SID").FirstOrDefault());
        }
        public void SetHeaders(WebHeaderCollection headers, string requestedURL = "")
        {
            if (this.CurrentSession == null || headers == null)
                return;
            headers.Add("X-APPICKET-REQURL", requestedURL);
            headers.Add("X-APPICKET-SID", this.CurrentSession.SessionID.ToString());
            headers.Add("X-REF-APPICKET-SID", this.CurrentSession.ReferencedSessionID.ToString());
        }
        private void RegisterAPIDocumentation(string path)
        {
            if (this.IsActive)
                OpenAPI.Parser.Parse(path, this);
        }
        internal void RegisterAPIDocumentation(OpenAPIDocument model)
        {
            if (model == null)
                return;

            if (this.IsActive)
            {
                this.RunInTread(() =>
                {
                    this.PostData("/public/RegisterOpenAPIDocument", new
                    {
                        Title = model.info.title,
                        Description = model.info.description,
                        Version = model.info.version,
                        TrackingHost = new
                        {
                            Name = model.host
                        },
                        URLs = from path in model.paths
                               select new
                               {
                                   Path = path.Key
                               }
                    });
                });
            }
        }
        private void RunInTread(ThreadStart ts)
        {
            var t = new Thread(ts);
            t.Start();
        }
    }
}