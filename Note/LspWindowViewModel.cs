using Editor;
using LanguageServer;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Note
{
    // TODO: Remove the placeholder text
    public class LspWindowViewModel : INotifyPropertyChanged
    {
        private readonly Action<string, string, LspUri> _onLaunch;

        public LspWindowViewModel(Action<string, string, LspUri> onLaunch)
        {
            this._onLaunch = onLaunch;
        }

        private string? _languageId = "xml";
        public string? LanguageId
        {
            get => this._languageId;
            set
            {
                this._languageId = value;
                this.NotifyPropertyChanged();
            }
        }

        private string? _serverPath = "D:\\AAA_skit\\projekt\\lemminx\\org.eclipse.lemminx\\target\\lemminx-windows-x86_64-0.28.0.exe";
        public string? ServerPath
        {
            get => this._serverPath;
            set
            {
                this._serverPath = value;
                this.NotifyPropertyChanged();
            }
        }

        private string? _workspacePath;
        public string? WorkspacePath
        {
            get => this._workspacePath;
            set
            {
                this._workspacePath = value;
                this.NotifyPropertyChanged();
            }
        }

        private string? _textDocumentPath = "D:\\tmp\\jsoup\\target\\surefire-reports\\TEST-org.jsoup.select.XpathTest.xml";
        public string? TextDocumentPath
        {
            get => this._textDocumentPath;
            set
            {
                this._textDocumentPath = value;
                this.NotifyPropertyChanged();
            }
        }

        public bool Launch()
        {
            bool isLaunched = false;

            if (string.IsNullOrEmpty(this.LanguageId))
            {
                MessageBox.Show("Language ID is empty");
                return isLaunched;
            }
            else if (string.IsNullOrEmpty(this.ServerPath))
            {
                MessageBox.Show("Server path is empty");
                return isLaunched;
            }
            else if (!File.Exists(this.ServerPath))
            {
                MessageBox.Show("Server executable could not be found");
                return isLaunched;
            }
            else if (!string.IsNullOrEmpty(this.WorkspacePath) && !Directory.Exists(this.WorkspacePath))
            {
                MessageBox.Show("Workspace URI directory could not be found");
                return isLaunched;
            }
            else if (!string.IsNullOrEmpty(this.TextDocumentPath) && !File.Exists(this.TextDocumentPath))
            {
                MessageBox.Show("Text document could not be found");
                return isLaunched;
            }

            LspUri lspUri;

            if (!string.IsNullOrEmpty(this.WorkspacePath))
            {
                lspUri = LspUri.Workspace(this.WorkspacePath);
            }
            else if (!string.IsNullOrEmpty(this.TextDocumentPath))
            {
                lspUri = LspUri.TextDocument(this.TextDocumentPath);
            }
            else
            {
                MessageBox.Show("Either workspace URI or text document path must be set");
                return isLaunched;
            }

            this._onLaunch.Invoke(this.LanguageId, this.ServerPath, lspUri);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
