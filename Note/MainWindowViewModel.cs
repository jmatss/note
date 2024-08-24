using Editor.ViewModel;
using Editor;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.IO;

// TODO: Find clean way to redraw the text on settings changes.
//       Should be generic for all possible settings changes.

namespace Note
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel(Settings settings)
        {
            this.Settings = settings;
            this.FileViewModel = new FileViewModel(settings);
            this.Settings.Todo_Freeze();
        }

        public Settings Settings { get; }

        public FileViewModel FileViewModel { get; }

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
                this.FileViewModel.Recalculate(false);
                this.NotifyPropertyChanged();
            }
        }

        public bool WindowsLineBreaks
        {
            get => !this.Settings.UseUnixLineBreaks;
            set
            {
                this.Settings.UseUnixLineBreaks = !value;
                this.FileViewModel.Recalculate(false);
                this.NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Load(string filepath)
        {
            if (!File.Exists(filepath))
            {
                MessageBox.Show("Unable to find file: " + filepath);
            }

            using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            using (var streamReader = new StreamReader(stream))
            {
                streamReader.Peek(); // Peek to set `CurrentEncoding`
                streamReader.BaseStream.Position = 0;
                var rope = Rope.Rope.FromStream(streamReader.BaseStream, streamReader.CurrentEncoding);
                this.FileViewModel?.Load(rope);
            }
        }
    }
}
