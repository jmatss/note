using Editor.Range;
using Editor.View;
using Note.Rope;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Editor.HandleInput;

namespace Editor.ViewModel
{
    public class FileViewModel : INotifyPropertyChanged
    {
        // TODO: Get this value in cleaner way
        private readonly double pixelsPerDip = VisualTreeHelper.GetDpi(new Button()).PixelsPerDip;

        /// <summary>
        /// When navigating over multiple rows with sequential up/down arrow clicks, we want to
        /// keep track of the original column that we where on.
        /// 
        /// For example given the following example text.
        /// ```
        /// The first line
        /// Second
        /// The third line that contains more words
        /// ```
        /// 
        /// Assume that we start with the cursor at the end of the word `line` at the first line (column index 14).
        /// If we press the down arrow one time, the cursor will end up at the last line of the second line (column index 6).
        /// When we press the down arrow again, we want the cursor to end up at the same column as we started at if possible (14).
        /// This index is stored in this variable.
        /// </summary>
        private int _previousSelectionColumnIndex = -1;

        public FileViewModel(Settings settings)
        {
            this.Settings = settings;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Rope _rope = Rope.FromString(string.Empty, Encoding.UTF8);
        public Rope Rope
        {
            get => this._rope;
            private set
            {
                this._rope = value;
                this.NotifyPropertyChanged();
            }
        }

        public Settings Settings { get; }

        public List<LineViewModel> Lines { get; } = new List<LineViewModel>();

        public GlyphRunViewModel? Text { get; set; }

        public GlyphRunViewModel? LineNumbers { get; set; }

        public GlyphRunViewModel? CustomCharWhitespace { get; set; }

        public CustomCharViewModel? CustomChars { get; set; }

        public List<SelectionRange> Selections { get; } = new List<SelectionRange>();

        public List<SelectionViewModel> SelectionsInView { get; } = new List<SelectionViewModel>();

        public List<SelectionRange> Highlights { get; } = new List<SelectionRange>();

        public List<SelectionViewModel> HighlightsInView { get; } = new List<SelectionViewModel>();

        public List<CursorViewModel> Cursors { get; } = new List<CursorViewModel>();

        private double viewWidth;
        public double ViewWidth
        {
            get => this.viewWidth;
            set
            {
                if (value != this.viewWidth)
                {
                    this.viewWidth = value;
                    this.Recalculate(false);
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
                    this.Recalculate(false);
                }
            }
        }

        public GlyphTypeface? GlyphTypeFace { get; private set; }

        public Action<double, double>? OnDraw { get; set; }

        public Action<double, double>? OnDrawSelections { get; set; }

        public void Load(Rope rope)
        {
            this.ResetSelections();
            this.Rope = rope;
            this.Recalculate(false, 0);
        }

        public void HandlePrintableKeys(string text)
        {
            this.Write(text);
        }

        public void HandleSpecialKeys(Key key, Modifiers modifiers)
        {
            (double charDrawWidth, double charDrawHeight) = FileViewModel.CharacterDrawSize(this.Settings, this.pixelsPerDip);

            if (key is Key.Up or Key.Down)
            {
                if (this._previousSelectionColumnIndex == -1 &&
                    this.Selections.FirstOrDefault()?.InsertionPositionIndex is int selectionCharIdx)
                {
                    int selectionLineIdx = this.Rope.GetLineIndexForCharAtIndex(selectionCharIdx);
                    int lineStartCharIdx = this.Rope.GetFirstCharIndexAtLineWithIndex(selectionLineIdx);
                    int columnIdx = selectionCharIdx - lineStartCharIdx;
                    this._previousSelectionColumnIndex = columnIdx;
                }
            }
            else
            {
                this._previousSelectionColumnIndex = -1;
            }

            foreach (SelectionRange selection in this.Selections)
            {
               SelectionRange? newSelection = HandleInput.HandleSpecialKeys(
                    this.Rope,
                    key,
                    modifiers,
                    selection,
                    this.Settings,
                    this.ViewWidth,
                    charDrawWidth,
                    charDrawHeight,
                    this._previousSelectionColumnIndex
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
                foreach (SelectionRange selection in this.Selections)
                {
                    newSelections.Add(new SelectionRange(selection) {
                        InsertionPositionIndex = newCursorCharIndex
                    });
                }

                foreach ((var selectionToUpdate, var newSelection) in this.Selections.Zip(newSelections))
                {
                    this.UpdateSelection(selectionToUpdate, newSelection);
                }
            }
            else
            {
                SelectionRange selection = this.ResetSelections();
                this.UpdateSelection(selection, new SelectionRange(newCursorCharIndex));
            }

            this.RecalculateSelections(true);
        }

        public void HandleTextMouseLeftDoubleClick(Point position)
        {
            int newCursorCharIndex = HandleInput.HandleTextLeftMouseClick(this.Rope, this.Lines, position);

            bool skipEndWhitespaces = false;
            int startIdx = this.Rope.GetCurrentWordStartIndex(newCursorCharIndex, skipEndWhitespaces);
            int endIdx = this.Rope.GetNextWordStartIndex(newCursorCharIndex, skipEndWhitespaces);

            SelectionRange selection = this.ResetSelections();
            this.UpdateSelection(selection, new SelectionRange(startIdx, endIdx, InsertionPosition.End));

            this.RecalculateSelections(true);
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
                SelectionRange selection = this.Selections.First();
                SelectionRange newSelection = new SelectionRange(this.Selections.First())
                {
                    InsertionPositionIndex = newCursorCharIndex
                };

                if (!object.Equals(selection, newSelection))
                {
                    Trace.WriteLine("newSelection: " + newSelection);
                    this.UpdateSelection(selection, newSelection);
                    // TODO: Implement check to see if above/below to see if the whole view
                    //       (including text etc.) should be updated. Otherwise just make the
                    //       faster operation of updating selections only.
                    int oldStartIndex = this.Lines.FirstOrDefault()?.StartCharIdx ?? 0;
                    int oldEndIndex = this.Lines.LastOrDefault()?.EndCharIdx ?? this.Rope.GetTotalCharCount();
                    if (selection.Start < oldStartIndex || selection.End > oldEndIndex)
                    {
                        this.Recalculate(true);
                    }
                    else
                    {
                        this.RecalculateSelections(true);
                    }
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

                this.RecalculateSelections(true);
            }
        }

        public void HandleScroll(int scrollDelta)
        {
            this.Recalculate(false, scrollDelta);
        }

        public bool HandleScrollBarMouseLeftMove(Point position, Point prevPosition)
        {
            (_, double charDrawHeight) = FileViewModel.CharacterDrawSize(this.Settings, this.pixelsPerDip);

            int possibleAmountOfLinesInView = (int)((this.ViewHeight) / charDrawHeight);
            int allLines = this.Rope.GetTotalLineBreaks() + possibleAmountOfLinesInView;
            allLines = allLines == 0 ? 1 : allLines;

            double oneLineHeight = (this.ViewHeight - ScrollBarView.ArrowHeight * 2) / allLines;
            int curLine = (int)Math.Floor(prevPosition.Y / oneLineHeight);
            int newLine = (int)Math.Floor(position.Y / oneLineHeight);

            Trace.WriteLine("cur: " + curLine + ", new: " + newLine + ", h: " + newLine + ", prevY: " + prevPosition.Y + ", curY: " + position.Y);

            if (curLine != newLine)
            {
                int scrollDelta = newLine - curLine;
                this.HandleScroll(scrollDelta);
                return true;
            }
            else
            {
                // The previous pointer position and this new position refers to the same line.
                // I.e. moving the scrollbar to this new position would NOT scroll to a new
                // line, so no need to do anything here.
                return false;
            }
        }

        public string? Read()
        {
            if (this.Selections.All(x => x.Length == 0))
            {
                return null;
            }

            List<string> texts = new List<string>();

            foreach (SelectionRange selection in this.Selections)
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
            foreach (SelectionRange selection in this.Selections.Reverse<SelectionRange>())
            {
                SelectionRange newselection = WriteText(this.Rope, text, selection);
                this.UpdateSelection(selection, newselection);
            }

            this.Recalculate(true);
        }

        public bool FindAndNavigateToText(string textToFind)
        {
            SelectionRange? selection = this.Selections.FirstOrDefault();

            int startSearchCharIdx = selection != null ? selection.End : 0;
            int endSearchCharIdx = this.Rope.GetTotalCharCount();

            SelectionRange? foundTextLocation = this.FindText(textToFind, startSearchCharIdx, endSearchCharIdx);
            if (foundTextLocation == null && startSearchCharIdx != 0)
            {
                // Wrap around and search from beginning of file
                endSearchCharIdx = startSearchCharIdx;
                startSearchCharIdx = 0;
                foundTextLocation = this.FindText(textToFind, startSearchCharIdx, endSearchCharIdx);
            }

            if (foundTextLocation != null)
            {
                SelectionRange newSelection = this.ResetSelections();
                this.UpdateSelection(newSelection, foundTextLocation);
                this.Recalculate(true);
                return true;
            }
            else
            {
                return false;
            }
        }

        private SelectionRange? FindText(string textToFind, int startSearchCharIdx, int endSearchCharIdx)
        {
            Tuple<int, int>? foundText = this.Rope.FindAll(textToFind, startSearchCharIdx, endSearchCharIdx).FirstOrDefault();
            if (foundText != null)
            {
                return new SelectionRange(foundText.Item1, foundText.Item2, InsertionPosition.End);
            }
            else
            {
                return null;
            }
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

        public SelectionRange ResetSelections()
        {
            this.Selections.Clear();
            SelectionRange selection = new SelectionRange(0);
            this.Selections.Add(selection);
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

        public void Recalculate(bool bringCursorIntoView, int scrollDelta = 0)
        {
            if (this.ViewWidth <= 0.0 || this.ViewHeight <= 0.0)
            {
                return;
            }

            (double charDrawWidth, double charDrawHeight) = FileViewModel.CharacterDrawSize(this.Settings, this.pixelsPerDip);

            this.RecalculateLines(charDrawWidth, charDrawHeight, bringCursorIntoView, scrollDelta);

            this.RecalculateText();
            this.RecalculateLineNumbers(charDrawWidth, charDrawHeight);

            this.RecalculateSelections(false);

            this.OnDraw?.Invoke(charDrawWidth, charDrawHeight);
        }

        private void RecalculateLines(double charDrawWidth, double charDrawHeight, bool bringCursorIntoView, int scrollDelta)
        {
            int totalCharCount = this.Rope.GetTotalCharCount();
            int lastCharIdx = totalCharCount > 0 ? totalCharCount - 1 : 0;
            int viewStartCharIdx = this.Lines.FirstOrDefault()?.StartCharIdx ?? lastCharIdx;
            viewStartCharIdx = int.Clamp(viewStartCharIdx, 0, lastCharIdx);

            int? charIdxToBringIntoView = null;
            if (bringCursorIntoView)
            {
                charIdxToBringIntoView = this.Selections.FirstOrDefault()?.InsertionPositionIndex ?? 0;
            }
            else if (scrollDelta > 0)
            {
                viewStartCharIdx = LineViewModel.CharIdxAfterScrollDownwardsWithWordWrap(
                    this.Rope,
                    this.ViewWidth,
                    viewStartCharIdx,
                    charDrawWidth,
                    charDrawHeight,
                    scrollDelta
                );
            }
            else if (scrollDelta < 0)
            {
                viewStartCharIdx = LineViewModel.CharIdxAfterScrollUpwardsWithWordWrap(
                    this.Rope,
                    this.ViewWidth,
                    viewStartCharIdx,
                    charDrawWidth,
                    charDrawHeight,
                    scrollDelta
                );
            }

            var lines = LineViewModel.CalculateLines(
                this.Rope,
                this.ViewWidth,
                this.ViewHeight,
                viewStartCharIdx,
                charDrawWidth,
                charDrawHeight,
                this.Settings,
                charIdxToBringIntoView
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

            GlyphRun? whitespacesGlyphRun = GlyphRunViewModel.CharsToGlyphRun(
                whitespaces,
                this.Settings,
                (float)this.pixelsPerDip
            );
            this.CustomCharWhitespace = new GlyphRunViewModel(whitespacesGlyphRun, this.Settings.TextColorCustomChar, whitespaces);

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

        private void RecalculateSelections(bool redraw)
        {
            var selectionsInView = SelectionViewModel.CalculateSelections(
                this.Lines,
                this.Selections,
                this.Settings.SelectionBackgroundColor
            );
            this.SelectionsInView.Clear();
            foreach (SelectionViewModel selection in selectionsInView)
            {
                this.SelectionsInView.Add(selection);
            }

            this.Highlights.Clear();
            this.HighlightsInView.Clear();
            SelectionRange? firstSelection = this.Selections.FirstOrDefault();
            if (firstSelection != null && firstSelection.Length > 0)
            {
                string textToFind = string.Concat(
                    this.Rope.IterateChars(firstSelection.Start, firstSelection.End - firstSelection.Start).Select(x => x.Item1)
                );

                if (!string.IsNullOrWhiteSpace(textToFind))
                {
                    // TODO: Find all highlights in the background for performance reasons.
                    /*
                    var highLightRanges = SelectionViewModel.CalculateHighlightsInView(
                        this.Rope,
                        this.Lines,
                        highLightText
                    );
                    */
                    this.Highlights.AddRange(SelectionViewModel.CalculateHighlights(
                        this.Rope,
                        textToFind
                    ));
                    this.HighlightsInView.AddRange(SelectionViewModel.CalculateSelections(
                        this.Lines,
                        this.Highlights.Where(x => !x.Overlapse(firstSelection)),
                        new SolidColorBrush(Color.FromArgb(50, 150, 100, 0))
                    ));
                }
            }
            this.NotifyPropertyChanged(nameof(this.Highlights));

            var cursors = CursorViewModel.CalculateCursors(
                this.Rope,
                this.Lines,
                this.Selections,
                this.Settings
            );
            this.Cursors.Clear();
            foreach (CursorViewModel cursor in cursors)
            {
                this.Cursors.Add(cursor);
            }

            if (redraw)
            {
                (double charDrawWidth, double charDrawHeight) = FileViewModel.CharacterDrawSize(this.Settings, this.pixelsPerDip);
                this.OnDrawSelections?.Invoke(charDrawWidth, charDrawHeight);
            }
        }

        public void UpdateSelection(SelectionRange selectionToUpdate, SelectionRange newSelection)
        {
            SelectionRange newNormalizedSelection = newSelection.Normalized();
            selectionToUpdate.Update(newNormalizedSelection);

            this.NotifyPropertyChanged(nameof(this.Selections));

            Trace.WriteLine("Updated selection: " + selectionToUpdate);
        }
    }
}
