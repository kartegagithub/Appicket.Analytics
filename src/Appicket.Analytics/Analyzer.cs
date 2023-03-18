using Appicket.Analytics.Models;
using Appicket.Analytics.OpenAPI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Appicket.Analytics
{
    public class Analyzer
    {
        [ThreadStatic]
        private static TrackingSessionWriteModel CurrentSession;

        private static List<TrackingSessionWriteModel> Sessions = new List<TrackingSessionWriteModel>();

        public static int? InstanceID { get; set; }
        public static bool? IsEnabled { get; set; }
        private static ResponseModel RemoteConfig { get; set; }
        public static List<string> HiddenRequestParams { get; private set; } = new List<string>();
        private static string Version { get; set; } = "";
        public static ApplicationConfigModel Config { get; set; } = new ApplicationConfigModel();
        private static string ServerURL { get; set; }
        public static IWebProxy Proxy { get; set; }
        public static int Timeout { get; set; }
        public static bool EnableRequestBodyLogging { get; set; }
        public static long? SessionID => CurrentSession?.SessionID;
        public static void LogRequest(TrackingRequestLogModel model)
        {
            if (Analyzer.IsActive && RemoteConfig.Config.EnableRequest && CurrentSession != null)
                CurrentSession.Requests.Add(model);
        }

        public static void Interact(TrackingInteractionModel model)
        {
            if (Analyzer.IsActive && RemoteConfig.Config.EnableInteractionTracking && CurrentSession != null)
                CurrentSession.Interactions.Add(model);
        }

        public static void ViewProduct(ProductViewModel model)
        {
            if (Analyzer.IsActive && CurrentSession != null)
                CurrentSession.ProductViews.Add(model);
        }

        public static void SaleProduct(ProductSalesModel model)
        {
            if (Analyzer.IsActive && CurrentSession != null)
                CurrentSession.ProductSales.Add(model);
        }
        public static void ViewPage(TrackingPageViewModel model)
        {
            if (Analyzer.IsActive && RemoteConfig.Config.EnablePageView && CurrentSession != null)
                CurrentSession.PageViews.Add(model);
        }

        public static void Log(LogType type, string description)
        {
            if (Analyzer.IsActive && RemoteConfig.Config.EnableLog && CurrentSession != null)
            {
                CurrentSession.Logs.Add(new TrackingLogModel()
                {
                    Description = description,
                    Type = (byte)type
                });
            }
        }

        public static void LogException(Exception ex)
        {
            if (Analyzer.IsActive && RemoteConfig.Config.EnableExceptionLog && CurrentSession != null)
            {
                CurrentSession.Exceptions.Add(new TrackingExceptionLogModel()
                {
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                });
                ex = ex.InnerException;
                while (ex != null)
                {
                    CurrentSession.Exceptions.Add(new TrackingExceptionLogModel()
                    {
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                    });
                    ex = ex.InnerException;
                }
            }
        }
        public static async void CheckIsEnabled(Action deferredFunc)
        {
            try
            {
                if (Config == null || string.IsNullOrEmpty(Config.ClientID) || string.IsNullOrEmpty(Config.DeviceType) || string.IsNullOrEmpty(Config.ClientSecret))
                {
                    IsEnabled = false;
                    return;
                }

                if (!IsEnabled.HasValue)
                {
                    if (!IsEnabled.HasValue)
                    {
                        var config = GetConfig(CurrentSession?.SessionID, CurrentSession?.ReferencedSessionID);
                        var result = await PostData("/public/IsEnabled", config);
                        if (!string.IsNullOrEmpty(result))
                        {
                            var model = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseModel>(result);
                            if (model.Value == "1" && model.Config != null)
                            {
                                RemoteConfig = model;
                                IsEnabled = RemoteConfig.Config.EnableRequest || RemoteConfig.Config.EnableDeviceInfoUpdate || RemoteConfig.Config.EnableExceptionLog || RemoteConfig.Config.EnableLog || RemoteConfig.Config.EnablePageView || RemoteConfig.Config.EnableInteractionTracking;
                                CurrentSession.SessionID = model.SessionID;
                                Sessions.Add(CurrentSession);
                                if (deferredFunc != null)
                                    deferredFunc();
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
        public static async void UpdateDeviceInfo()
        {
            if (Analyzer.IsActive && RemoteConfig.Config.EnableDeviceInfoUpdate)
                await PostData("/public/UpdateDeviceInfo", GetConfig(CurrentSession?.SessionID, CurrentSession?.ReferencedSessionID));
        }
        public static async void Commit()
        {
            await Commit(CurrentSession);
        }
        public static async Task Commit(TrackingSessionWriteModel session)
        {
            if (Analyzer.IsActive && session != null && (session.Exceptions.Any() || session.Logs.Any() || session.Interactions.Any() || session.Requests.Any() || session.PageViews.Any() || session.ProductSales.Any() || session.PageViews.Any()))
            {
                if (session.Requests != null && session.Requests.Any())
                {
                    if (!EnableRequestBodyLogging)
                        session.Requests.ForEach(op => op.Parameters = "");
                    else
                    {
                        foreach (var item in session.Requests)
                        {
                            if (ContainsHiddenParams(item.Parameters))
                                item.Parameters = "";
                        }
                    }
                }

                await PostData("/public/TrackSession", session);
            }
        }
        public static async Task<TrackingSessionWriteModel> StartSession()
        {
            if (Analyzer.IsActive)
            {
                var result = await PostData("/public/StartSession", GetConfig(CurrentSession?.SessionID, CurrentSession?.ReferencedSessionID));
                if (!string.IsNullOrEmpty(result))
                {
                    var model = Newtonsoft.Json.JsonConvert.DeserializeObject<ResponseModel>(result);
                    if (model.SessionID > 0)
                    {
                        var session = new TrackingSessionWriteModel() { SessionID = model.SessionID };
                        Sessions.Add(session);
                        return session;
                    }
                }
            }
            return null;
        }
        private static async Task<string> PostData(string url, object data)
        {
            try
            {
                using (var clientHandler = new HttpClientHandler())
                {
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", $"Appicket.Analytics {Version}");
                        clientHandler.Proxy = Proxy;
                        clientHandler.UseProxy = Proxy != null;

                        var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerURL}{url}");
                        if (data != null)
                            request.Content = JsonContent.Create(data, data.GetType(), new MediaTypeHeaderValue("application/json"));

                        if (RemoteConfig != null && !string.IsNullOrEmpty(RemoteConfig.AuthKey))
                            request.Content.Headers.Add("X-Auth-Key", RemoteConfig.AuthKey);

                        if (Timeout > 0)
                            client.Timeout = TimeSpan.FromMilliseconds(Timeout);
                        var response = client.SendAsync(request).Result;
                        if (response.IsSuccessStatusCode)
                            return await response.Content.ReadAsStringAsync();
                        else
                            return "";
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        public static bool IsActive => IsEnabled.HasValue && IsEnabled.Value;

        private static ApplicationConfigModel GetConfig(long? sessionID = 0, long? referencedSessionID = 0)
        {
            var process = Process.GetCurrentProcess();
            if (process != null)
            {
                Config.ID = sessionID.GetValueOrDefault(0);
                Config.ReferencedID = referencedSessionID.GetValueOrDefault(0);
                Config.AvailableMemory = process.WorkingSet64;
                Config.CPUUsage = Convert.ToInt64(process.TotalProcessorTime.TotalMilliseconds);
            }
            return Config;
        }
        public Analyzer(ApplicationConfigModel config, string serverURL = "", long sessionID = 0, long refSessionID = 0, string openAPIDocPath = "")
        {
            Config = config;
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
            Version = typeof(Analyzer).Assembly.GetName().Version.ToString();
            ServerURL = serverURL;
            if (string.IsNullOrEmpty(ServerURL))
                ServerURL = "https://api.appicket.com";

            CurrentSession = new TrackingSessionWriteModel() { SessionID = sessionID, ReferencedSessionID = refSessionID };
            if (!InstanceID.HasValue)
            {
                InstanceID = new Random().Next(int.MaxValue);
                CheckIsEnabled(() =>
                {
                    if (!string.IsNullOrEmpty(openAPIDocPath))
                        RegisterAPIDocumentation(openAPIDocPath).RunSynchronously();
                });

                HiddenRequestParams.AddRange(new List<string>() { "Password", "Email", "TCK", "Birthday", "BirthYear", "Birthdate", "Credit", "IdentityNumber" });
            }
            else
            {
                var session = StartSession();
                if (session != null)
                    CurrentSession.SessionID = session.Id;
            }
        }

        public static void ResolveHeaders(HttpResponseMessage response)
        {
            if (CurrentSession == null || response == null)
                return;

            if (response.Headers.Contains("X-APPICKET-SID"))
                CurrentSession.SessionID = Convert.ToInt64(response.Headers.GetValues("X-APPICKET-SID").FirstOrDefault(), System.Globalization.CultureInfo.CurrentCulture);
            if (response.Headers.Contains("X-REF-APPICKET-SID"))
                CurrentSession.ReferencedSessionID = Convert.ToInt64(response.Headers.GetValues("X-REF-APPICKET-SID").FirstOrDefault(), System.Globalization.CultureInfo.CurrentCulture);
            if (response.Headers.Contains("X-VIEW-APPICKET-SID"))
                CurrentSession.ReferencedSessionID = Convert.ToInt64(response.Headers.GetValues("X-VIEW-APPICKET-SID").FirstOrDefault(), System.Globalization.CultureInfo.CurrentCulture);
        }
        public static void SetHeaders(WebHeaderCollection headers, string requestedURL = "")
        {
            if (CurrentSession == null || headers == null)
                return;
            headers.Add("X-APPICKET-REQURL", requestedURL);
            headers.Add("X-APPICKET-SID", CurrentSession.SessionID.ToString(System.Globalization.CultureInfo.CurrentCulture));
            headers.Add("X-REF-APPICKET-SID", CurrentSession.ReferencedSessionID.ToString(System.Globalization.CultureInfo.CurrentCulture));
        }
        private async static Task RegisterAPIDocumentation(string path)
        {
            if (Analyzer.IsActive)
                await OpenAPI.Parser.Parse(path);
        }
        internal static async Task RegisterAPIDocumentation(OpenAPIDocument model)
        {
            if (model == null)
                return;

            if (Analyzer.IsActive)
            {
                await PostData("/public/RegisterOpenAPIDocument", new
                {
                    Title = model.info.title,
                    Description = model.info.description,
                    Version = model.info.version,
                    model.RawContent,
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
            }
        }

        public static bool ContainsHiddenParams(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            foreach (var item in HiddenRequestParams)
            {
                if (content.IndexOf(item, StringComparison.InvariantCultureIgnoreCase) > -1)
                    return true;
            }
            return false;
        }
        public static void AddHiddenRequestParams(List<string> list)
        {
            if (list == null || !list.Any())
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var val = list[i];
                if (!HiddenRequestParams.Contains(val))
                    HiddenRequestParams.Add(val);
                list.Remove(val);
            }
        }

        private static bool Processing;
        public static Task ProcessQueue()
        {
            if (!Processing)
            {
                Processing = true;
                while (true)
                {
                    if (!Analyzer.IsActive)
                        Thread.Sleep(60000);
                    else
                    {
                        for (int i = Sessions.Count - 1; i >= 0; i--)
                        {
                            _ = Commit(Sessions[i]);
                            Sessions.RemoveAt(i);
                        }
                    }
                }
            }
            return null;
        }
    }
}