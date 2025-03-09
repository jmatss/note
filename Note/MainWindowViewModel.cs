using Editor.ViewModel;
using Editor;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.IO;
using Editor.Range;
using LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Diagnostic = Editor.Diagnostic;
using LspDiagnostic = Microsoft.VisualStudio.LanguageServer.Protocol.Diagnostic;
using DiagnosticSeverity = Editor.DiagnosticSeverity;
using LspDiagnosticTag = Microsoft.VisualStudio.LanguageServer.Protocol.DiagnosticTag;
using DiagnosticTag = Editor.DiagnosticTag;
using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using System.Windows.Input;
using static Editor.HandleInput;
using System.Diagnostics;
using Microsoft.Win32;
using Text;
using Note.Tabs;

// TODO: Find clean way to redraw the text on settings changes.
//       Should be generic for all possible settings changes.

namespace Note
{
    public class MainWindowViewModel : INotifyPropertyChanged, ITabFocus
    {
        public MainWindowViewModel(Settings settings)
        {
            this.Settings = settings;
            this.Settings.Todo_Freeze();
            this.TabGroupContainer = new TabGroupContainerViewModel(this);
        }

        public static int LspPositionToCharIdx(Rope rope, Position lspPosition)
        {
            int startCharIdx = rope.GetFirstCharIndexAtLineWithIndex(lspPosition.Line);
            return startCharIdx + lspPosition.Character;
        }

        public static Position CharIdxToLspPosition(Rope rope, int charIdx)
        {
            int lineIdx = rope.GetLineIndexForCharAtIndex(charIdx);
            int lineStartCharIdx = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
            int columnIdx = charIdx - lineStartCharIdx;
            return new Position(lineIdx, columnIdx);
        }

        public static IEnumerable<Diagnostic> LspDiagnosticsToDiagnostics(Rope rope, IEnumerable<LspDiagnostic> lspDiagnostics)
        {
            var diagnostics = new List<Diagnostic>();

            foreach (var lspDiagnostic in lspDiagnostics)
            {
                var startCharIdx = LspPositionToCharIdx(rope, lspDiagnostic.Range.Start);
                var endCharIdx = LspPositionToCharIdx(rope, lspDiagnostic.Range.End);
                var range = new RangeBase(startCharIdx, endCharIdx);

                var diagnosticTags = lspDiagnostic.Tags?
                    .Select(x => x == LspDiagnosticTag.Deprecated ? DiagnosticTag.Deprecated : DiagnosticTag.Unnecessary)
                    .ToArray();

                diagnostics.Add(new Diagnostic(range, lspDiagnostic.Message)
                {
                    Severity = (DiagnosticSeverity?)lspDiagnostic.Severity,
                    Code = lspDiagnostic.Code?.Value?.ToString(),
                    CodeDescription = lspDiagnostic.CodeDescription?.ToString(),
                    Source = lspDiagnostic.Source,
                    Tags = diagnosticTags,
                });
            }

            return diagnostics;
        }

        public static RangeBase LspRangeToRange(Rope rope, LspRange lspRange)
        {
            var startCharIdx = LspPositionToCharIdx(rope, lspRange.Start);
            var endCharIdx = LspPositionToCharIdx(rope, lspRange.End);
            return new RangeBase(startCharIdx, endCharIdx);
        }

        public static LspRange RangeToLspRange(Rope rope, RangeBase range)
        {
            return new LspRange()
            {
                Start = CharIdxToLspPosition(rope, range.Start),
                End = CharIdxToLspPosition(rope, range.End),
            };
        }

        public LspManager LspManager { get; } = new LspManager();











        public Settings Settings { get; }

        public TabGroupContainerViewModel TabGroupContainer { get; }

        private FileViewModel? _focusedFile;
        public FileViewModel? FocusedFile
        {
            get => this._focusedFile;
            set
            {
                if (this.FocusedFile == value)
                {
                    return;
                }

                this._focusedFile = value;
                value?.Recalculate(false);
                this.NotifyPropertyChanged();
            }
        }

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
                this.TabGroupContainer.RunOnAllVisibileFiles(x => x.Recalculate(false));
                this.NotifyPropertyChanged();
            }
        }

        public bool WindowsLineBreaks
        {
            get => !this.Settings.UseUnixLineBreaks;
            set
            {
                this.Settings.UseUnixLineBreaks = !value;
                this.TabGroupContainer.RunOnAllVisibileFiles(x => x.Recalculate(false));
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

                var fileViewModel = new FileViewModel(this.Settings);
                var rope = Rope.FromStream(streamReader.BaseStream, streamReader.CurrentEncoding);
                var textDocumentUri = new Uri(filepath);
                fileViewModel.Load(rope, textDocumentUri);

                this.TabGroupContainer.AddNewlyOpenedFile(this.FocusedFile, fileViewModel);
                this._focusedFile = fileViewModel;
            }
        }

        public void OpenFile()
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() ?? false)
            {
                this.Load(dialog.FileName);
            }
        }

        public void OpenSearchWindow()
        {
            if (this.SearchWindow == null)
            {
                var searchWindowViewModel = new SearchWindowViewModel(this.Settings, (textToFind) =>
                {
                    if (this.FocusedFile == null)
                    {
                        MessageBox.Show("No file selected in UI");
                    }
                    else if (!this.FocusedFile.FindAndNavigateToText(textToFind))
                    {
                        MessageBox.Show("Unable to find \"" + textToFind + "\"");
                    }
                });
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

            SelectionRange? selectedText = this.FocusedFile?.Selections.FirstOrDefault();
            if (selectedText != null && selectedText.Length > 0)
            {
                string? text = this.FocusedFile?.Rope.GetText(selectedText.Start, selectedText.Length);
                this.SearchWindow.Value.Item2.Find = text;
            }
        }

        public void OpenLspWindow()
        {
            var lspWindowViewModel = new LspWindowViewModel(async (languageId, serverPath, lspUri) =>
            {
                var lspClient = await this.LaunchLsp(languageId, serverPath, lspUri);
                this.LspManager.Add(lspUri, lspClient);

                // TODO: Go through open files and call `DidOpen` on the LSP client for all matching files.

                if (this.FocusedFile == null)
                {
                    MessageBox.Show("No file selected in UI");
                }
                else
                {
                    await lspClient.DidOpen(this.FocusedFile.TextDocumentUri);
                }                
            });
            var lspWindowView = new LspWindow(lspWindowViewModel);
            lspWindowView.Owner = Application.Current.MainWindow;
            _ = lspWindowView.ShowDialog();
        }

        private async Task<ILspClient> LaunchLsp(string languageId, string serverPath, LspUri lspUri)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            var messageReader = new MessageReader();

            var lspClient = new ProcessLspClient(
                languageId,
                null,
                serverPath,
                messageReader,
                tokenSource.Token
            );

            messageReader.AddNotificationHandler<PublishDiagnosticParams>(
                "textDocument/publishDiagnostics",
                (x) =>
                {
                    var uri = x.Uri;

                    if (this.FocusedFile == null)
                    {
                        MessageBox.Show("No file selected in UI");
                        return;
                    }

                    // TODO: Change this so that it works for multiple files. Currently we take the rope
                    //       of the textdocument that is open, but this will not work in the future.
                    var diagnostics = LspDiagnosticsToDiagnostics(this.FocusedFile.Rope, x.Diagnostics);
                    // TODO: Get STA thread from somewhere.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.FocusedFile.HandleDiagnostics(diagnostics);
                    });
                },
                (error) => { }
            );

            lspClient.StartServer();
            await lspClient.Initialize();

            if (this.FocusedFile == null)
            {
                throw new NotImplementedException("TODO: No file selected in UI");
            }

            // TODO: Change this so that it works for multiple files. Currently we take the rope
            //       of the textdocument that is open, but this will not work in the future.
            Rope rope = this.FocusedFile.Rope;
            rope.OnModification += async (modification) =>
            {
                RangeBase range;
                string text;

                if (modification is InsertModification insert)
                {
                    range = new RangeBase(insert.StartIdx, insert.StartIdx);
                    text = rope.GetText(insert.StartIdx, insert.Length);
                }
                else if (modification is RemoveModification remove)
                {
                    range = new RangeBase(remove.StartIdx, remove.StartIdx + remove.Length);
                    text = string.Empty;
                }
                else if (modification is ReplaceModification replace)
                {
                    range = new RangeBase(replace.Remove.StartIdx, replace.Remove.StartIdx + replace.Remove.Length);
                    text = rope.GetText(replace.Insert.StartIdx, replace.Insert.Length);
                }
                else
                {
                    throw new InvalidOperationException("Unknown modification type: " + modification.GetType());
                }

                if (this.FocusedFile == null)
                {
                    throw new NotImplementedException("TODO: No file selected in UI");
                }

                await lspClient.DidChange(
                    this.FocusedFile.TextDocumentUri,
                    [
                        new TextDocumentContentChangeEvent()
                        {
                            Range = RangeToLspRange(rope, range),
                            RangeLength = range.Length,
                            Text = text,
                        }
                    ]
                );
            };

            return lspClient;
        }

        // TODO: Move to somewhere else
        public bool TextInput(string text)
        {
            if (!string.IsNullOrEmpty(text) && !char.IsControl(text.First()))
            {
                if (this.FocusedFile == null)
                {
                    MessageBox.Show("No file selected in UI");
                    return true;
                }

                this.Write(this.FocusedFile, text);
                return true;
            }
            else
            {
                return false;
            }
        }

        // TODO: Move to somewhere else
        public bool KeyInput(Key key, Modifiers modifiers)
        {
            bool isHandled = true;

            if (this.FocusedFile == null)
            {
                MessageBox.Show("No file selected in UI");
                return true;
            }

            // TODO: Handle if no file selected/focused
            FileViewModel fileViewModel = this.FocusedFile;

            switch (key)
            {
                case Key.A when modifiers.Ctrl:
                case Key.Left:
                case Key.Up:
                case Key.Right:
                case Key.Down:
                case Key.PageUp:
                case Key.Next:
                case Key.End:
                case Key.Home:
                    fileViewModel.HandleNavigation(key, modifiers);
                    break;

                case Key.Back:
                case Key.Delete:
                    this.Delete(fileViewModel, key, modifiers);
                    break;

                case Key.Enter:
                case Key.LineFeed:
                    this.Write(fileViewModel, this.Settings.UseUnixLineBreaks ? "\n" : "\r\n");
                    break;

                case Key.Tab:
                    this.Write(fileViewModel, this.Settings.TabString);
                    break;

                case Key.C when modifiers.Ctrl:
                    string? copyText = fileViewModel.Read();
                    if (!string.IsNullOrEmpty(copyText))
                    {
                        Clipboard.SetText(copyText);
                    }
                    break;

                case Key.V when modifiers.Ctrl:
                    string pasteText = Clipboard.GetText();
                    this.Write(fileViewModel, pasteText);
                    break;

                case Key.X when modifiers.Ctrl:
                    string? cutText = fileViewModel.Read();
                    if (!string.IsNullOrEmpty(cutText))
                    {
                        Clipboard.SetText(cutText);
                        this.Write(fileViewModel, string.Empty);
                    }
                    break;

                case Key.Z when modifiers.Ctrl:
                    int newSelectionIndex = fileViewModel.Rope.Undo();
                    if (newSelectionIndex != -1)
                    {
                        SelectionRange selection = fileViewModel.ResetSelections();
                        selection.Update(new SelectionRange(newSelectionIndex));
                        fileViewModel.Recalculate(true);
                    }
                    break;

                case Key.O when modifiers.Ctrl:
                    this.OpenFile();
                    break;

                case Key.F when modifiers.Ctrl:
                    this.OpenSearchWindow();
                    break;

                default:
                    isHandled = false;
                    break;
            }

            return isHandled;
        }

        public void Write(FileViewModel fileViewModel, string text)
        {
            // Update selections in reverse order so that the changes doesn't affect each other.
            var selections = fileViewModel.Selections.Reverse<SelectionRange>();
            var rope = fileViewModel.Rope;

            foreach (SelectionRange selection in selections)
            {
                SelectionRange newselection = WriteTextToRope(text, fileViewModel.Rope, selection);
                // TODO: Can `UpdateSelection` be done in bulk instead of 1-by-1?
                fileViewModel.UpdateSelection(selection, newselection);
            }

            fileViewModel.Recalculate(true);
        }

        public void Delete(FileViewModel fileViewModel, Key key, Modifiers modifiers)
        {
            // Update selections in reverse order so that the changes doesn't affect each other.
            var selections = fileViewModel.Selections.Reverse<SelectionRange>();
            var rope = fileViewModel.Rope;

            foreach (SelectionRange selection in selections)
            {
                var result = DeleteTextFromRope(fileViewModel.Rope, key, modifiers, selection);
                if (result is (SelectionRange newselection, RangeBase removedRange))
                {
                    // TODO: Can `UpdateSelection` be done in bulk instead of 1-by-1?
                    fileViewModel.UpdateSelection(selection, newselection);
                }
            }

            fileViewModel.Recalculate(true);
        }

        private static SelectionRange WriteTextToRope(
            string text,
            Rope rope,
            RangeBase rangeToReplace
        )
        {
            int charAmountToReplace = rangeToReplace.Length;
            int startIdx = rangeToReplace.Start;

            Trace.WriteLine("Text: " + text + ", rangeToReplace: " + rangeToReplace + ", charAmountToReplace: " + charAmountToReplace + ", new: " + (startIdx + text.Length));

            if (charAmountToReplace > 0)
            {
                rope.Replace(startIdx, charAmountToReplace, text);
            }
            else
            {
                rope.Insert(startIdx, text);
            }

            return new SelectionRange(rangeToReplace.Start + text.Length);
        }

        private static (SelectionRange, RangeBase)? DeleteTextFromRope(
            Rope rope,
            Key key,
            Modifiers modifiers,
            SelectionRange rangeToReplace
        )
        {
            int startIdx;
            int charAmountToRemove;

            switch (key)
            {
                case Key.Back:
                    if (rangeToReplace.Start == 0 && rangeToReplace.Length == 0)
                    {
                        return null;
                    }

                    if (modifiers.Ctrl)
                    {
                        bool skipEndWhitespaces = false;
                        startIdx = rope.GetCurrentWordStartIndex(rangeToReplace.InsertionPositionIndex, skipEndWhitespaces);
                        charAmountToRemove = rangeToReplace.End - startIdx;
                    }
                    else if (rangeToReplace.Length > 0)
                    {
                        startIdx = rangeToReplace.Start;
                        charAmountToRemove = rangeToReplace.Length;
                    }
                    else
                    {
                        // Not a selection, just want to remove the character to
                        // the left of the current insertion cursor.
                        startIdx = rangeToReplace.Start - 1;
                        charAmountToRemove = 1;
                    }

                    if (startIdx > 0 && rope.GetChar(startIdx - 1) == LineViewModel.CARRIAGE_RETURN)
                    {
                        startIdx--;
                        charAmountToRemove++;
                    }
                    break;

                case Key.Delete:
                    int lastIdx = rope.GetTotalCharCount();
                    if (rangeToReplace.Start == lastIdx)
                    {
                        return null;
                    }

                    if (modifiers.Ctrl)
                    {
                        bool skipEndWhitespaces = true;
                        int endIdx = rope.GetNextWordStartIndex(rangeToReplace.End, skipEndWhitespaces);
                        charAmountToRemove = endIdx - rangeToReplace.Start;
                    }
                    else if (rangeToReplace.Length > 0)
                    {
                        charAmountToRemove = rangeToReplace.Length;
                    }
                    else
                    {
                        charAmountToRemove = 1;
                    }

                    startIdx = rangeToReplace.Start;
                    if (startIdx > 0 && startIdx < lastIdx && rope.GetChar(startIdx) == LineViewModel.CARRIAGE_RETURN)
                    {
                        charAmountToRemove++;
                    }
                    break;

                default:
                    throw new Exception("Invalid key in Delete: " + key);
            }

            rope.Remove(startIdx, charAmountToRemove);

            var newSelection = new SelectionRange(startIdx);
            var removedRange = new RangeBase(startIdx, startIdx + charAmountToRemove);

            return (newSelection, removedRange);
        }
    }
}
