using Appicket.Analytics.Models;
using Appicket.Analytics.WebAPI.Extensions.Middlewares;

namespace Appicket.Analytics.WebAPI.Extensions.Test;

public class AnalyzerMiddlewareTests
{
    private AnalyzerMiddleware Middleware { get; set; }

    [SetUp]
    public void Setup()
    {
        this.Middleware = new AnalyzerMiddleware(null, new AnalyzerOptions()
        {
            ClientID = "ClientID",
            ClientSecret = "ClientSecret",
            APIDocumentationPath = "",
            IsWebAPI = false,
            ConfigureRequest = (context, model) =>
            {
                model.DeviceType = "Test";
                model.Environment = "Test";
            }
        });
    }

    [Test]
    public void InvokeTest()
    {
        this.Middleware.Invoke(null);
        Assert.Pass();
    }
}