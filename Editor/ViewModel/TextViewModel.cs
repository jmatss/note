using Editor.Range;
using Note.Rope;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Editor.HandleInput;

namespace Editor.ViewModel
{
    public class TextViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // TODO: Get this value in cleaner way
        private readonly double pixelsPerDip = VisualTreeHelper.GetDpi(new Button()).PixelsPerDip;

        public TextViewModel(Rope rope, Settings settings)
        {
            this.Rope = rope;
            this.Settings = settings;
            this.ResetSelections();
        }

        public Rope Rope { get; }

        public Settings Settings { get; }

        public bool IsLeftClickHeld { get; set; } = false;

        public ObservableCollection<LineViewModel> Lines { get; } = new ObservableCollection<LineViewModel>();

        private GlyphRunViewModel text;
        public GlyphRunViewModel Text
        {
            get => this.text;
            set
            {
                this.text = value;
                this.OnPropertyChanged(nameof(this.Text));
            }
        }

        private GlyphRunViewModel lineNumbers;
        public GlyphRunViewModel LineNumbers
        {
            get => this.lineNumbers;
            set
            {
                this.lineNumbers = value;
                this.OnPropertyChanged(nameof(this.LineNumbers));
            }
        }

        private CustomCharWhitespaceViewModel customCharWhitespace;
        public CustomCharWhitespaceViewModel CustomCharWhitespace
        {
            get => this.customCharWhitespace;
            set
            {
                this.customCharWhitespace = value;
                this.OnPropertyChanged(nameof(this.CustomCharWhitespace));
            }
        }

        private CustomCharViewModel customChars;
        public CustomCharViewModel CustomChars
        {
            get => this.customChars;
            set
            {
                this.customChars = value;
                this.OnPropertyChanged(nameof(this.CustomChars));
            }
        }

        public ObservableCollection<SelectionViewModel> Selections { get; } = new ObservableCollection<SelectionViewModel>();

        public ObservableCollection<CursorViewModel> Cursors { get; } = new ObservableCollection<CursorViewModel>();

        private double viewWidth;
        public double ViewWidth
        {
            get => this.viewWidth;
            set
            {
                if (value != this.viewWidth)
                {
                    this.viewWidth = value;
                    this.Recalculate(true);
                }
            }
        }

        private double viewHeight;
        public double ViewHeight
        {
            get => this.viewHeight;
            set
            {
                if (value != this.viewHeight)
                {
                    this.viewHeight = value;
                    this.Recalculate(true);
                }
            }
        }

        public GlyphTypeface GlyphTypeFace { get; private set; }

        public List<SelectionRange> SelectionRanges { get; } = new List<SelectionRange>();       

        public void HandlePrintableKeys(string text)
        {
            this.Write(text);
        }

        public void HandleSpecialKeys(Key key, Modifiers modifiers)
        {
            foreach (SelectionRange selection in this.SelectionRanges)
            {
               SelectionRange? newSelection = HandleInput.HandleSpecialKeys(
                    this.Rope,
                    key,
                    modifiers,
                    selection,
                    this.Settings
                );

                if (newSelection != null)
                {
                    this.UpdateSelection(selection, newSelection);
                    this.Recalculate(true);
                }
            }
        }

        public void HandleTextMouseLeftClick(Point position)
        {
            int newCursorCharIndex = HandleInput.HandleTextLeftMouseClick(this.Rope, this.Lines, position);
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                // TODO: Handle correct. Should update the indices according to which rows
                //       the cursors are located
                List<SelectionRange> newSelections = new List<SelectionRange>();
                foreach (SelectionRange selection in this.SelectionRanges)
                {
                    newSelections.Add(new SelectionRange(selection) {
                        InsertionPositionIndex = newCursorCharIndex
                    });
                }

                foreach ((var selectionToUpdate, var newSelection) in this.SelectionRanges.Zip(newSelections))
                {
                    this.UpdateSelection(selectionToUpdate, newSelection);
                }
            }
            else
            {
                SelectionRange selection = this.ResetSelections();
                this.UpdateSelection(selection, new SelectionRange(newCursorCharIndex));
            }

            this.RecalculateSelections();
        }

        public void HandleTextMouseLeftMove(Point position)
        {
            int newCursorCharIndex = HandleInput.HandleTextLeftMouseClick(this.Rope, this.Lines, position);
            if (Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Alt))
            {
                throw new NotImplementedException("TODO: Shift+Alt move multiple cursors");
            }
            else
            {
                SelectionRange selection = this.SelectionRanges.First();
                SelectionRange newSelection = new SelectionRange(this.SelectionRanges.First())
                {
                    InsertionPositionIndex = newCursorCharIndex
                };

                if (!object.Equals(selection, newSelection))
                {
                    this.UpdateSelection(selection, newSelection);
                    this.Recalculate(true);
                }
            }
        }

        public void HandleLineNumbersMouseLeftClick(Point position)
        {
            int clickedLineIndex = HandleInput.HandleLineNumbersLeftMouseClick(this.Rope, this.Lines, position);
            if (clickedLineIndex != -1)
            {
                SelectionRange selection = this.ResetSelections();
                int firstCharIdx = this.Rope.GetFirstCharIndexAtLineWithIndex(clickedLineIndex);
                int lastCharIdx = this.Rope.GetFirstCharIndexAtLineWithIndex(clickedLineIndex + 1);
                lastCharIdx = lastCharIdx == -1 ? this.Rope.GetTotalCharCount() : lastCharIdx;
                this.UpdateSelection(selection, new SelectionRange(firstCharIdx, lastCharIdx, InsertionPosition.End));
                this.RecalculateSelections();
            }
        }

        public void HandleMouseWheel(int scrollDelta)
        {
            this.Recalculate(true, scrollDelta);
        }

        public string? Read()
        {
            if (this.SelectionRanges.All(x => x.Length == 0))
            {
                return null;
            }

            List<string> texts = new List<string>();

            foreach (SelectionRange selection in this.SelectionRanges)
            {
                IEnumerable<char> text = this.Rope
                    .IterateChars(selection.Start, selection.Length)
                    .Select(x => x.Item1);
                texts.Add(string.Concat(text));
            }

            return string.Join(this.Settings.UseUnixLineBreaks ? "\n" : "\r\n", texts);
        }

        public void Write(string text)
        {
            foreach (SelectionRange selection in this.SelectionRanges.Reverse<SelectionRange>())
            {
                SelectionRange newselection = WriteText(this.Rope, text, selection);
                this.UpdateSelection(selection, newselection);
            }

            this.Recalculate(true);
        }

        public static (double, double) CharacterDrawSize(Settings settings, double pixelsPerDip)
        {
            var formattedText = new FormattedText(
                "X",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                settings.TypeFace,
                settings.FontSize,
                Brushes.Black,
                pixelsPerDip
            );

            return (formattedText.Width, formattedText.Height);
        }

        private SelectionRange ResetSelections()
        {
            this.SelectionRanges.Clear();
            SelectionRange selection = new SelectionRange(0);
            this.SelectionRanges.Add(selection);
            return selection;
        }

        private static SelectionRange WriteText(
            Rope rope,
            string text,
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

        private void Recalculate(bool bringCursorIntoView, int scrollDelta = 0)
        {
            if (this.ViewWidth <= 0.0 || this.ViewHeight <= 0.0)
            {
                return;
            }

            (double charDrawWidth, double charDrawHeight) = TextViewModel.CharacterDrawSize(this.Settings, this.pixelsPerDip);

            this.RecalculateLines(charDrawWidth, charDrawHeight, bringCursorIntoView);

            this.RecalculateText();
            this.RecalculateLineNumbers(charDrawWidth, charDrawHeight);

            this.RecalculateSelections();
        }

        private void RecalculateLines(double charDrawWidth, double charDrawHeight, bool bringCursorIntoView)
        {
            int? cursorIdxToBringIntoView = null;
            if (bringCursorIntoView)
            {
                cursorIdxToBringIntoView = this.SelectionRanges.FirstOrDefault()?.InsertionPositionIndex ?? 0;
            }

            int totalCharCount = this.Rope.GetTotalCharCount();
            int lastCharIdx = totalCharCount > 0 ? totalCharCount - 1 : 0;
            int viewStartCharIdx = this.Lines.FirstOrDefault()?.StartCharIdx ?? lastCharIdx;
            viewStartCharIdx = int.Clamp(viewStartCharIdx, 0, lastCharIdx);

            var lines = LineViewModel.CalculateLines(
                this.Rope,
                this.ViewWidth,
                this.ViewHeight,
                viewStartCharIdx,
                charDrawWidth,
                charDrawHeight,
                this.Settings,
                cursorIdxToBringIntoView
            );
            this.Lines.Clear();
            foreach (LineViewModel line in lines)
            {
                this.Lines.Add(line);
            }
        }

        private void RecalculateText()
        {
            var textChars = GlyphRunViewModel.CalculatePrintableText(this.Lines);
            GlyphRun? textGlyphRun = GlyphRunViewModel.CharsToGlyphRun(
                textChars,
                this.Settings,
                (float)this.pixelsPerDip
            );
            this.Text = new GlyphRunViewModel(textGlyphRun, this.Settings.TextColor, textChars);

            this.RecalculateCustomChars();
        }

        private void RecalculateLineNumbers(double charDrawWidth, double charDrawHeight)
        {
            var lineNumbersChars = GlyphRunViewModel.CalculateLineNumbers(
                this.Lines,
                this.Rope,
                charDrawWidth,
                charDrawHeight
            );
            GlyphRun? lineNumbersGlyphRun = GlyphRunViewModel.CharsToGlyphRun(
                lineNumbersChars,
                this.Settings,
                (float)this.pixelsPerDip
            );
            double lineNumbersWidth = lineNumbersChars.Max(ch => ch.X + ch.Width as double?) ?? 0;
            this.LineNumbers = new GlyphRunViewModel(lineNumbersGlyphRun, this.Settings.TextColor, lineNumbersChars, lineNumbersWidth);
        }

        private void RecalculateCustomChars()
        {
            IEnumerable<CharacterViewModel> whitespaces;
            if (this.Settings.DrawCustomChars)
            {
                whitespaces = CustomCharsViewModel.CalculateWhitespaces(this.Lines);
            }
            else
            {
                whitespaces = Enumerable.Empty<CharacterViewModel>();
            }

            this.CustomCharWhitespace = new CustomCharWhitespaceViewModel(
                this.Settings.TextColorCustomChar,
                whitespaces
            );

            IEnumerable<CharacterViewModel> custom;
            if (this.Settings.DrawCustomChars)
            {
                custom = CustomCharsViewModel.CalculateCustom(this.Lines);
            }
            else
            {
                custom = Enumerable.Empty<CharacterViewModel>();
            }

            this.CustomChars = new CustomCharViewModel(
                this.Settings.TextColor,
                this.Settings.TextColorCustomChar,
                custom,
                CustomCharViewModel.CharsToCustomGlyphRun(custom, this.Settings, (float)this.pixelsPerDip)
            );
        }

        private void RecalculateSelections()
        {
            var selections = SelectionViewModel.CalculateSelections(
                this.Lines,
                this.SelectionRanges,
                this.Settings
            );
            this.Selections.Clear();
            foreach (SelectionViewModel selection in selections)
            {
                this.Selections.Add(selection);
            }

            var cursors = CursorViewModel.CalculateCursors(
                this.Rope,
                this.Lines,
                this.SelectionRanges,
                this.Settings
            );
            this.Cursors.Clear();
            foreach (CursorViewModel cursor in cursors)
            {
                this.Cursors.Add(cursor);
            }
        }

        private void UpdateSelection(SelectionRange selectionToUpdate, SelectionRange newSelection)
        {
            SelectionRange newNormalizedSelection = newSelection.Normalized();
            selectionToUpdate.Update(newNormalizedSelection);

            Trace.WriteLine("Updated selection: " + selectionToUpdate);

            /*

            int selectionStartLineIdx = this.Rope.GetLineIndexForCharAtIndex(selectionToUpdate.Start);

            int firstCharIdx = this.Lines.FirstOrDefault()?.StartCharIdx ?? 0;
            int lastCharIdx = this.Lines.LastOrDefault()?.EndCharIdx ?? 0;

            int firstLineIdxInView = this.Rope.GetLineIndexForCharAtIndex(firstCharIdx);
            int lastLineIdxInView = this.Rope.GetLineIndexForCharAtIndex(lastCharIdx);
            if (lastCharIdx > 0 && this.Rope.GetChar(lastCharIdx) == LineViewModel.LINE_BREAK)
            {
                lastLineIdxInView++;
            }

            int amountOfLinesInView = lastLineIdxInView - firstLineIdxInView;

            
            if (selectionStartLineIdx < this.StartLineIndex)
            {
                // TODO: Handle word wrapping offset correct
                this.StartLineIndex = selectionStartLineIdx;
                this.StartLineOffset = 0;
            }
            else if (selectionStartLineIdx > lastLineIdxInView)
            {
                int amountOfLinesToScroll = (selectionStartLineIdx - this.StartLineIndex) - amountOfLinesInView;
                // TODO: Handle word wrapping offset correct
                this.StartLineIndex += amountOfLinesToScroll;
                this.StartLineOffset = 0;
            }
            

            if (updateVisual)
            {
                this.RecalculateSelections();
            }
            */
        }
    }
}
