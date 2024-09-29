using Editor.ViewModel;
using Editor;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.IO;
using Editor.Range;

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

        public (SearchWindow, SearchWindowViewModel)? SearchWindow { get; private set; }

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

        public void OpenSearchWindow()
        {
            if (this.SearchWindow == null)
            {
                var searchWindowViewModel = new SearchWindowViewModel(this.Settings, this.OnFind);
                var searchWindowView = new SearchWindow(searchWindowViewModel);
                this.SearchWindow = (searchWindowView, searchWindowViewModel);
                searchWindowView.Closed += (_1, _2) => this.SearchWindow = null;
                searchWindowView.Owner = Application.Current.MainWindow;
                searchWindowView.Show();
            }
            else
            {
                this.SearchWindow.Value.Item1.Activate();
            }

            SelectionRange? selectedText = this.FileViewModel?.Selections.FirstOrDefault();
            if (selectedText != null && selectedText.Length > 0)
            {
                string? text = this.FileViewModel?.Rope.GetText(selectedText.Start, selectedText.Length);
                this.SearchWindow.Value.Item2.Find = text;
            }
        }

        public void OnFind(string textToFind)
        {
            if (this.FileViewModel != null)
            {
                bool wasFound = this.FileViewModel.FindAndNavigateToText(textToFind);
                if (!wasFound)
                {
                    MessageBox.Show("Unable to find \"" + textToFind +  "\"");
                }
            }
        }
    }
}
