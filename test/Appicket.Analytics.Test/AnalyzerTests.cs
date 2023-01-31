using Appicket.Analytics.Models;

namespace Appicket.Analytics.Test
{
    public class AnalyzerTests
    {
        private Analyzer Analyzer { get; set; }

        [SetUp]
        public void Setup()
        {
            if (this.Analyzer == null)
            {
                var options = new ApplicationConfigModel()
                {
                    ClientID = "ClientID",
                    ClientSecret = "ClientSecret"
                };
                this.Analyzer = new Analyzer(options, "", 0, 0, "");
            }

        }

        [Test]
        public void SetHeadersTest()
        {
            var headers = new System.Net.WebHeaderCollection() { };
            this.Analyzer.SetHeaders(headers, "");
            Assert.Pass();
        }
    }
}