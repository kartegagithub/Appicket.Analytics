using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Xml;

namespace Appicket.Analytics.Loggers
{
    public class LoggingMessageInspector : IClientMessageInspector
    {
        private Stopwatch Watch { get; set; } = new Stopwatch();
        private Models.TrackingRequestLogModel RequestModel { get; set; } = new Models.TrackingRequestLogModel();
        public LoggingMessageInspector()
        {

        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            this.Watch.Stop();
            this.RequestModel.Duration = System.Convert.ToInt32(this.Watch.ElapsedMilliseconds);
            using (var buffer = reply.CreateBufferedCopy(int.MaxValue))
            {
                var document = GetDocument(buffer.CreateMessage());
                this.RequestModel.ResponseBodyLength = document.OuterXml.Length;
                this.RequestModel.ResponseCode = 200;
                reply = buffer.CreateMessage();
            }

            Analyzer.Current?.LogRequest(this.RequestModel);
            Analyzer.Current?.Commit();
            this.RequestModel = null;
            this.Watch = null;
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            this.RequestModel.RequestedURL = channel.RemoteAddress.ToString();
            this.RequestModel.Method = "POST";
            this.RequestModel.Headers = Newtonsoft.Json.JsonConvert.SerializeObject(request.Headers);
            using (var buffer = request.CreateBufferedCopy(int.MaxValue))
            {
                var document = GetDocument(buffer.CreateMessage());
                this.RequestModel.Parameters = document.OuterXml;
                this.Watch.Start();

                request = buffer.CreateMessage();
                return null;
            }
        }

        private XmlDocument GetDocument(Message request)
        {
            XmlDocument document = new XmlDocument();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // write request to memory stream
                XmlWriter writer = XmlWriter.Create(memoryStream);
                request.WriteMessage(writer);
                writer.Flush();
                memoryStream.Position = 0;

                // load memory stream into a document
                document.Load(memoryStream);
            }

            return document;
        }
    }
}
