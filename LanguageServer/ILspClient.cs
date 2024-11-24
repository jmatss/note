using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace LanguageServer
{
    public interface ILspClient
    {
        public string LanguageId { get; }

        public Uri? WorkspaceUri { get; }

        public void StartServer();

        public Task Initialize();

        public Task DidOpen(Uri textDocumentUri);

        public Task DidChange(Uri textDocumentUri, TextDocumentContentChangeEvent[] changes);
    }
}
