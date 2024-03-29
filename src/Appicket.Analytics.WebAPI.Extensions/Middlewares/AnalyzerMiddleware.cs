﻿using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Appicket.Analytics.Models;
using Microsoft.AspNetCore.Http.Extensions;
using System.IO;

namespace Appicket.Analytics.WebAPI.Extensions.Middlewares
{
    public class AnalyzerMiddleware
    {
        private readonly RequestDelegate _next;
        private AnalyzerOptions MiddlewareOptions { get; set; }
        public AnalyzerMiddleware(RequestDelegate next, AnalyzerOptions? options = null)
        {
            _next = next;
            this.MiddlewareOptions = options;
            if (this.MiddlewareOptions == null)
                this.MiddlewareOptions = new AnalyzerOptions();
        }

        public Task Invoke(HttpContext context)
        {
            if (context.Request.Method == "OPTIONS" || this.IsStaticFile(context.Request) || this.GetUserAgent(context.Request).IndexOf("Appicket") > -1 || this.IsAppicketPublicAPIRequest(context.Request))
                return BeginInvoke(context);

            var options = new ApplicationConfigModel()
            {
                ClientID = this.MiddlewareOptions.ClientID,
                ClientSecret = this.MiddlewareOptions.ClientSecret
            };
            if (this.MiddlewareOptions != null && this.MiddlewareOptions.ConfigureRequest != null)
                this.MiddlewareOptions.ConfigureRequest(context, options);

            var CurrentPageURL = this.GetKeyValue(context.Request, "X-APPICKET-REQURL", "");
            var DeviceID = this.GetKeyValue(context.Request, "X-APPICKET-DEVID", "");
            var AppicketSessionID = this.GetKeyValue(context.Request, "X-APPICKET-SID");
            var RefAppicketSessionID = this.GetKeyValue(context.Request, "X-VIEW-APPICKET-SID", "");
            if (string.IsNullOrEmpty(RefAppicketSessionID))
                RefAppicketSessionID = this.GetKeyValue(context.Request, "X-REF-APPICKET-SID");

            if (string.IsNullOrEmpty(options.DeviceID) && !string.IsNullOrEmpty(DeviceID))
                options.DeviceID = DeviceID;

            Analytics.Analyzer.AddHiddenRequestParams(this.MiddlewareOptions.HiddenRequestParams);
            Analytics.Analyzer.EnableRequestBodyLogging = this.MiddlewareOptions.EnableRequestBodyLogging;

            var Analyzer = new Analyzer(options, this.MiddlewareOptions.AppicketServerURL, Convert.ToInt64(AppicketSessionID), Convert.ToInt64(RefAppicketSessionID), this.MiddlewareOptions.APIDocumentationPath);
            if (!Analytics.Analyzer.IsActive || Analyzer.SessionID.GetValueOrDefault(0) <= 0)
                return BeginInvoke(context);

            context.Response.Cookies.Append("X-APPICKET-SID", Analyzer.SessionID.ToString(), new CookieOptions() { Path = "/", MaxAge = TimeSpan.FromMinutes(120), HttpOnly = false, Secure = context.Request.IsHttps });
            context.Response.Headers.Add("X-APPICKET-SID", new Microsoft.Extensions.Primitives.StringValues(Analyzer.SessionID.ToString()));

            var watch = new Stopwatch();
            watch.Start();
            try
            {
                return BeginInvoke(context);
            }
            catch (Exception ex)
            {
                Analytics.Analyzer.LogException(ex);
                throw;
            }
            finally
            {
                watch.Stop();
                watch = null;

                if (this.MiddlewareOptions.IsWebAPI)
                {
                    Analytics.Analyzer.LogRequest(new TrackingRequestLogModel()
                    {
                        ContentType = context.Request.ContentType,
                        Duration = Convert.ToInt32(watch.ElapsedMilliseconds),
                        Method = context.Request.Method,
                        PagePath = CurrentPageURL,
                        Headers = this.GetRequestHeaders(context.Request),
                        Parameters = this.GetRequestBody(context.Request),
                        RequestedURL = context.Request.GetEncodedUrl(),
                        ResponseCode = context.Response.StatusCode,
                        ResponseBodyLength = context.Response.ContentLength.GetValueOrDefault(0),
                        Direction = 1
                    });
                }
                else
                {
                    Analytics.Analyzer.ViewPage(new TrackingPageViewModel()
                    {
                        Duration = Convert.ToInt32(watch.ElapsedMilliseconds),
                        PagePath = context.Request.GetEncodedUrl(),
                        ResponseCode = context.Response.StatusCode,
                        Referer = context.Request.Headers.ContainsKey("Referer") ? context.Request.Headers["Referer"].ToString() : "",
                        ResponseBodyLength = context.Response.ContentLength.GetValueOrDefault(0)
                    });
                }
                Analytics.Analyzer.Commit();
            }
        }
        private string GetUserAgent(HttpRequest request)
        {
            var agent = "";
            if (request.Headers.ContainsKey("User-Agent"))
                agent = request.Headers["User-Agent"].ToString();
            if (string.IsNullOrEmpty(agent))
                agent = "";
            return agent;
        }
        private bool IsAppicketPublicAPIRequest(HttpRequest request)
        {
            if (request.Path.HasValue)
            {
                var path = request.Path.Value;
                if (path.Contains("public/", StringComparison.InvariantCultureIgnoreCase) && (request.Host.Host == "api.appicket.com" || request.Host.Host.IndexOf("localhost") > -1))
                {
                    return true;
                }
            }
            return false;
        }
        private bool IsStaticFile(HttpRequest request)
        {
            if (request.Path.HasValue)
            {
                var extension = System.IO.Path.GetExtension(request.Path.Value);
                if (!string.IsNullOrEmpty(extension))
                {
                    return true;
                }
            }
            return false;
        }
        private string GetKeyValue(HttpRequest request, string key, string defaultValue = "0")
        {
            var value = "";
            request.Cookies.TryGetValue(key, out value);
            if (string.IsNullOrEmpty(value))
            {
                if (request.Headers.ContainsKey(key))
                    value = request.Headers[key].ToString();
                if (string.IsNullOrEmpty(value))
                    value = defaultValue;
            }
            return value;
        }
        private string GetRequestBody(HttpRequest request)
        {
            try
            {
                using (StreamReader reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        private string GetRequestHeaders(HttpRequest request)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(request.Headers);
            }
            catch (Exception)
            {
                return "";
            }
        }
        private Task BeginInvoke(HttpContext context)
        {
            return _next.Invoke(context);
        }
    }
}
