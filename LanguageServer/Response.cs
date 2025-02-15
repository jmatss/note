using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text;

namespace LanguageServer
{
    [DataContract]
    public class Response<T>
    {
        public Response()
        {
        }

        [DataMember(Name = "jsonrpc")]
        public string? JsonRpc { get; set; }

        [DataMember(Name = "id")]
        public int? Id { get; set; }

        [DataMember(Name = "result")]
        public T? Result { get; set; }

        public override string ToString()
        {
            var jsonContent = JsonConvert.SerializeObject(this);
            int contentLength = Encoding.UTF8.GetBytes(jsonContent).Length;
            return "Content-Length: " + contentLength + "\r\n\r\n" + jsonContent;
        }
    }
}
