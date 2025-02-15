using Microsoft.VisualStudio.LanguageServer.Protocol;
using System.Diagnostics;
using System.Text;

namespace LanguageServer
{
    public partial class ProcessLspClient : ILspClient
    {
        private Process? _process;
        private readonly string _serverPath;
        private readonly MessageReader _messageReader;
        private readonly CancellationToken _token;
        private int _requestId = 1;
        private readonly Dictionary<Uri, int> _textDocumentUriToVersion = [];

        public ProcessLspClient(string languageId, Uri? workspaceUri, string serverPath, MessageReader messageReader, CancellationToken token)
        {
            this.WorkspaceUri = workspaceUri;
            this.LanguageId = languageId;
            this._serverPath = serverPath;
            this._messageReader = messageReader;
            this._token = token;
        }

        public string LanguageId { get; }

        public Uri? WorkspaceUri { get; }

        public void StartServer()
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = this._serverPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false),
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
            };

            this._process = Process.Start(info);

            if (this._process == null)
            {
                throw new InvalidOperationException("process null when starting message reader");
            }

            _ = Task.Run(() => this._messageReader.Run(this._process.StandardOutput, this._token));
        }

        public async Task Initialize()
        {
            var parameters = new InitializeParams()
            {
                ProcessId = Environment.ProcessId,
                RootUri = this.WorkspaceUri,
                InitializationOptions = null, // TODO
                Capabilities = new ClientCapabilities()
                {
                    TextDocument = new TextDocumentClientCapabilities()
                    {
                        Completion = new CompletionSetting(),
                        Hover = new HoverSetting(),
                        DocumentHighlight = new DynamicRegistrationSetting(),
                        PublishDiagnostics = new PublishDiagnosticsSetting()
                        {
                            TagSupport = new TagSupport()
                            {
                                ValueSet = [DiagnosticTag.Deprecated, DiagnosticTag.Unnecessary],
                            }
                        }
                    },
                },
                Trace = TraceSetting.Verbose,
            };

            int id = this._requestId++;
            this._messageReader.AddResponseHandler<InitializeResult>(
                id,
                (x) => {}, // TODO: Check and store ServerCapabilities
                (error) => {}
            );
            await this.Write(new Request<InitializeParams>(id, "initialize", parameters));
        }

        public async Task DidOpen(Uri textDocumentUri)
        {
            if (!this._textDocumentUriToVersion.ContainsKey(textDocumentUri))
            {
                this._textDocumentUriToVersion[textDocumentUri] = 1;
            }
            else
            {
                throw new InvalidOperationException("Document already opened: " + textDocumentUri);
            }

            string textDocumentPath = textDocumentUri.LocalPath;
            if (!File.Exists(textDocumentPath))
            {
                throw new InvalidOperationException("Document does not exist: " + textDocumentUri);
            }

            var parameters = new DidOpenTextDocumentParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Uri = textDocumentUri,
                    LanguageId = this.LanguageId,
                    Version = this._textDocumentUriToVersion[textDocumentUri],
                    // TODO: Prevent duplicate read of file. Take as parameter to this function instead
                    Text = File.ReadAllText(textDocumentPath),
                }
            };

            int id = this._requestId++;
            await this.Write(new Request<DidOpenTextDocumentParams>(id, "textDocument/didOpen", parameters));
        }

        public async Task DidChange(Uri textDocumentUri, TextDocumentContentChangeEvent[] changes)
        {
            if (this._textDocumentUriToVersion.TryGetValue(textDocumentUri, out int version))
            {
                version++;
                this._textDocumentUriToVersion[textDocumentUri] = version;
            }
            else
            {
                throw new InvalidOperationException("Document not opened: " + textDocumentUri);
            }

            var parameters = new DidChangeTextDocumentParams()
            {
                TextDocument = new VersionedTextDocumentIdentifier()
                {
                    Uri = textDocumentUri,
                    Version = version,
                },
                ContentChanges = changes,
            };

            int id = this._requestId++;
            await this.Write(new Request<DidChangeTextDocumentParams>(id, "textDocument/didChange", parameters));
        }

        public async Task DocumentHighlight(Uri textDocumentUri, int lineIdx, int lineCharIdx)
        {
            var parameters = new DocumentHighlightParams()
            {
               TextDocument = new TextDocumentIdentifier()
               {
                   Uri = textDocumentUri,
               },
               Position = new Position()
               {
                   Line = lineIdx,
                   Character = lineCharIdx,
               }
            };

            int id = this._requestId++;
            this._messageReader.AddResponseHandler<List<DocumentHighlight>>(
                id,
                (x) => { },
                (error) => { }
            );
            await this.Write(new Request<DocumentHighlightParams>(id, "textDocument/documentHighlight", parameters));
        }

        private async Task Write<T>(Request<T> request)
        {
            var process = this._process ?? throw new InvalidOperationException("process null in Write");
            string msg = request.ToString();
            Trace.WriteLine("[WRITE] " + msg);
            await process.StandardInput.WriteAsync(msg);
        }
    }
}
