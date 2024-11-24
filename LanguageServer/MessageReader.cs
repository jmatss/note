using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LanguageServer
{
    public partial class MessageReader
    {
        private const string ContentLengthKey = "Content-Length";

        [GeneratedRegex("([0-9a-zA-Z-_]+): (\\d+)")]
        private static partial Regex HeaderRegex();

        [GeneratedRegex("\"id\"\\s?:\\s?(\\d+)")]
        private static partial Regex IdRegex();

        [GeneratedRegex("\"method\"\\s?:\\s?\"([^\"]+)\"")]
        private static partial Regex MethodRegex();

        /// <summary>
        /// A notification handler handles messages sent from the server that was never requested
        /// by the client. These handlers can handle multiple calls from the server.
        /// </summary>
        private readonly Dictionary<string, IMessageHandler> _notificationHandlers = [];

        /// <summary>
        /// A response handler handles a single server response to a request made by the client.
        /// After the response has been received and the handler has been invoked,
        /// it is removed from this dictionary and is never used again.
        /// 
        /// The key is the `id` of the client request message. The corresponding response from the
        /// server will contain the same `id` which allows us to match the response to a request.
        /// </summary>
        private readonly Dictionary<int, IMessageHandler> _responseHandlers = [];

        public MessageReader()
        {
        }

        public void AddNotificationHandler<T>(string method, Action<T> onSuccess, Action<string> onError)
        {
            this._notificationHandlers[method] = new MessageHandler<T>(onSuccess, onError);
        }

        public void AddResponseHandler<T>(int id, Action<T> onSuccess, Action<string> onError)
        {
            this._responseHandlers[id] = new MessageHandler<T>(onSuccess, onError);
        }

        public async Task Run(StreamReader reader, CancellationToken token)
        {
            while (true)
            {
                try
                {
                    await this.ReadMessage(reader, token);
                }
                catch (Exception e)
                {
                    // TODO: Write to log somewhere
                    Console.WriteLine(e);
                }

                token.ThrowIfCancellationRequested();
            }
        }

        private async Task ReadMessage(StreamReader reader, CancellationToken token)
        {
            var headers = await ReadHeaders(reader, token);
            Trace.WriteLine("HEADERS: " + string.Join(",", headers));
            var content = await ReadContent(reader, ContentLength(headers), token);
            Trace.WriteLine("CONTENT: " + content);

            bool hasId = int.TryParse(IdRegex().Match(content).Groups[1].Value, out int id);

            if (hasId)
            {
                if (!this._responseHandlers.TryGetValue(id, out IMessageHandler? handler))
                {
                    throw new InvalidOperationException("UNHANDLED - id: " + id + ", content: " + content);
                }

                this._responseHandlers.Remove(id);
                _ = Task.Run(() => handler.HandleResponse(content), CancellationToken.None);
            }
            else
            {
                // Assume this is a notification from server without a corresponding client request
                string method = MethodRegex().Match(content).Groups[1].Value;

                if (!this._notificationHandlers.TryGetValue(method, out IMessageHandler? handler))
                {
                    throw new InvalidOperationException("UNHANDLED - method: " + method + ", content: " + content);
                }

                _ = Task.Run(() => handler.HandleNotification(content), CancellationToken.None);
            }
        }

        private static async Task<Dictionary<string, string>> ReadHeaders(StreamReader reader, CancellationToken token)
        {
            var headers = new Dictionary<string, string>();

            var headerLine = await reader.ReadLineAsync(token);

            while (!string.IsNullOrEmpty(headerLine))
            {
                var match = HeaderRegex().Match(headerLine);
                if (!match.Success)
                {
                    throw new InvalidOperationException("Unable to parse header in ReadHeaders: " + headerLine);
                }

                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                headers[key] = value;

                headerLine = await reader.ReadLineAsync(token);
            }

            return headers;
        }

        private static async Task<string> ReadContent(StreamReader reader, int contentLength, CancellationToken token)
        {
            var contentChars = new char[contentLength];

            int readChars = await reader.ReadBlockAsync(contentChars, token);
            if (readChars != contentLength)
            {
                // TODO:
                throw new InvalidOperationException("Not all chars read when reading content. readChars: " + readChars + ", contentLength: " + contentLength);
            }

            return new string(contentChars);
        }

        private static int ContentLength(Dictionary<string, string> headers)
        {
            if (!headers.TryGetValue(ContentLengthKey, out string? contentLengthValue))
            {
                // TODO:
                throw new InvalidOperationException("Unable to find Content-Length header: " + string.Join(",", headers));
            }

            if (!int.TryParse(contentLengthValue, out int contentLength))
            {
                // TODO:
                throw new InvalidOperationException("Unable to parse Content-Length as int: " + string.Join(",", headers));
            }

            return contentLength;
        }
    }
}
