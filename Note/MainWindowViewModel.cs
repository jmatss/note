using Editor.ViewModel;
using Editor;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

// TODO: Find clean way to redraw the text on settings changes.
//       Should be generic for all possible settings changes.

namespace Note
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            Rope.Rope rope = Rope.Rope.FromString("Test text, abc123!\nA second line!\r\nThe third one ö\n", Encoding.UTF8);
            this.TextViewModel = new TextViewModel(rope, this.Settings);
            this.Settings.Todo_Freeze();
        }

        public Settings Settings { get; } = new Settings();

        private TextViewModel? _textViewModel;
        public TextViewModel? TextViewModel
        {
            get => this._textViewModel;
            set
            {
                this._textViewModel = value;
                this.NotifyPropertyChanged();
            }
        }

        public bool WordWrap
        {
            get => this.Settings.WordWrap;
            set
            {
                MessageBox.Show("TODO: Support for non word wrap not implemented");
                //this.Settings.WordWrap = value;
                //this.TextViewModel?.Recalculate(false);
                //this.NotifyPropertyChanged();
            }
        }

        public bool ShowAllCharacters
        {
            get => this.Settings.DrawCustomChars;
            set
            {
                this.Settings.DrawCustomChars = value;
                this.TextViewModel?.Recalculate(false);
                this.NotifyPropertyChanged();
            }
        }

        public bool WindowsLineBreaks
        {
            get => !this.Settings.UseUnixLineBreaks;
            set
            {
                this.Settings.UseUnixLineBreaks = !value;
                this.TextViewModel?.Recalculate(false);
                this.NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
