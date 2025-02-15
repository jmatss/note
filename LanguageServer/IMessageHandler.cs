namespace LanguageServer
{
    internal interface IMessageHandler
    {
        public void HandleNotification(string content);

        public void HandleResponse(string content);
    }
}
