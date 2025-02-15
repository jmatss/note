using Newtonsoft.Json;
using System.Runtime.Serialization;
using System.Text;

namespace LanguageServer
{
    [DataContract]
    public class Request<T>
    {
        public Request(int id, string method, T? parameters)
        {
            this.Id = id;
            this.Method = method;
            this.Params = parameters;
        }

        [DataMember(Name = "jsonrpc")]
        public string JsonRpc => "2.0";

        [DataMember(Name = "id")]
        public int Id { get; }

        [DataMember(Name = "method")]
        public string Method { get; }

        [DataMember(Name = "params")]
        public T? Params { get; }

        public override string ToString()
        {
            var jsonContent = JsonConvert.SerializeObject(this);
            int contentLength = Encoding.UTF8.GetBytes(jsonContent).Length;
            return "Content-Length: " + contentLength + "\r\n\r\n" + jsonContent;
        }
    }
}
