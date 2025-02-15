using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text;

namespace LanguageServer
{
    [DataContract]
    public class Notification<T>
    {
        public Notification()
        {
        }

        [DataMember(Name = "jsonrpc")]
        public string? JsonRpc { get; set; }

        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "params")]
        public T? Params { get; set; }

        public override string ToString()
        {
            var jsonContent = JsonConvert.SerializeObject(this);
            int contentLength = Encoding.UTF8.GetBytes(jsonContent).Length;
            return "Content-Length: " + contentLength + "\r\n\r\n" + jsonContent;
        }
    }
}
