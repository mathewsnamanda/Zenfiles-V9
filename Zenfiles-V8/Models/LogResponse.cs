using Zenfiles.Models;
using ZenFiles.Models;

namespace Testing_Logs.models
{
    
    public class LogResponse
    {
        public forlogs objectsearchresponse { get; set; }
        public string SourceContext { get; set; }
        public string ActionId { get; set; }
        public string ActionName { get; set; }
        public string RequestId { get; set; }
        public string RequestPath { get; set; }
        public string ConnectionId { get; set; }
        public string EnvironmentName { get; set; }
        public string MachineName { get; set; }
        public int ThreadId { get; set; }
       
    }
}
