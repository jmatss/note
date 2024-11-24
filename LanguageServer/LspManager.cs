namespace LanguageServer
{
    public class LspManager
    {
        private readonly Dictionary<LspUri, ILspClient> _activeLspClients = [];

        public LspManager()
        {
        }

        public void Add(LspUri lspUri, ILspClient lspClient)
        {
            this._activeLspClients.Add(lspUri, lspClient);
        }

        public void Remove(LspUri lspUri)
        {
            this._activeLspClients.Remove(lspUri);
        }

        public ILspClient? Get(Uri textDocumentUri)
        {
            if (this._activeLspClients.TryGetValue(LspUri.TextDocument(textDocumentUri), out ILspClient? client))
            {
                return client;
            }
            else
            {
                foreach (var entry in this._activeLspClients.Where(x => x.Key.Type == LspUriType.Workspace))
                {
                    Uri workspaceUri = entry.Key.Uri;
                    ILspClient lspClient = entry.Value;

                    if (workspaceUri.IsBaseOf(textDocumentUri))
                    {
                        return lspClient;
                    }
                }
            }

            return null;
        }
    }
}
