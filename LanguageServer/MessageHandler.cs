using Newtonsoft.Json;
using System.Diagnostics;

namespace LanguageServer
{
    internal class MessageHandler<T> : IMessageHandler
    {
        public MessageHandler(Action<T> onSuccess, Action<string> onError)
        {
            this.OnSuccess = onSuccess;
            this.OnError = onError;
        }

        /// <summary>
        /// Called when message is received from the LSP server.
        /// </summary>
        public Action<T> OnSuccess { get; }

        /// <summary>
        /// Called when an error is received from the LSP server.
        /// </summary>
        public Action<string> OnError { get; }

        public void HandleResponse(string content)
        {
            var response = JsonConvert.DeserializeObject<Response<T>>(content);
            if (response == null)
            {
                string msg = "[ERROR] Unable to parse response as json: " + content;
                Trace.WriteLine(msg);
                this.OnError.Invoke(msg);
            }
            else if (response.Result == null)
            {
                string msg = "[ERROR] Unable to get result from response: " + content;
                Trace.WriteLine(msg);
                this.OnError.Invoke(msg);
            }
            else
            {
                string msg = "[SUCCESS]";
                Trace.WriteLine(msg);
                this.OnSuccess.Invoke(response.Result);
            }
        }

        public void HandleNotification(string content)
        {
            var notification = JsonConvert.DeserializeObject<Notification<T>>(content);
            if (notification == null)
            {
                string msg = "[ERROR] Unable to parse notification as json: " + content;
                Trace.WriteLine(msg);
                this.OnError.Invoke(msg);
            }
            else if (notification.Params == null)
            {
                string msg = "[ERROR] Unable to get params from notification: " + content;
                Trace.WriteLine(msg);
                this.OnError.Invoke(msg);
            }
            else
            {
                string msg = "[SUCCESS]";
                Trace.WriteLine(msg);
                this.OnSuccess.Invoke(notification.Params);
            }
        }
    }
}
