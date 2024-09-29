using Editor;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// TODO: Find clean way to redraw the text on settings changes.
//       Should be generic for all possible settings changes.

namespace Note
{
    public class SearchWindowViewModel : INotifyPropertyChanged
    {
        private readonly Action<string> _onFind;

        public SearchWindowViewModel(Settings settings, Action<string> onFind)
        {
            this.Settings = settings;
            this._onFind = onFind;
        }

        public Settings Settings { get; }

        private string? _find;
        public string? Find
        {
            get => this._find;
            set
            {
                this._find = value;
                this.NotifyPropertyChanged();
            }
        }

        private string? _replace;
        public string? Replace
        {
            get => this._replace;
            set
            {
                this._replace = value;
                this.NotifyPropertyChanged();
            }
        }

        public void FindOperation()
        {
            if (!string.IsNullOrEmpty(this.Find))
            {
                this._onFind?.Invoke(this.Find);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
