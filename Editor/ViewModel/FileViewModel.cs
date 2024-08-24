using Editor.Range;
using Editor.View;
using Note.Rope;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Editor.HandleInput;

namespace Editor.ViewModel
{
    public class FileViewModel
    {
        // TODO: Get this value in cleaner way
        private readonly double pixelsPerDip = VisualTreeHelper.GetDpi(new Button()).PixelsPerDip;

        public FileViewModel(Settings settings)
        {
            this.Settings = settings;
        }

        public Rope Rope { get; private set; } = Rope.FromString(string.Empty, Encoding.UTF8);

        public Settings Settings { get; }

        public List<LineViewModel> Lines { get; } = new List<LineViewModel>();

        public GlyphRunViewModel? Text { get; set; }

        public GlyphRunViewModel? LineNumbers { get; set; }

        public GlyphRunViewModel? CustomCharWhitespace { get; set; }

        public CustomCharViewModel? CustomChars { get; set; }

        public List<SelectionViewModel> Selections { get; } = new List<SelectionViewModel>();

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

        public List<SelectionRange> SelectionRanges { get; } = new List<SelectionRange>();

        public Action<double, double>? OnDraw { get; set; }

        public Action? OnDrawSelections { get; set; }

        public void Load(Rope rope)
        {
            this.Rope = rope;
            this.ResetSelections();
            this.Recalculate(false, 0);
        }

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
                SelectionRange selection = this.SelectionRanges.First();
                SelectionRange newSelection = new SelectionRange(this.SelectionRanges.First())
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

        public SelectionRange ResetSelections()
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
                charIdxToBringIntoView = this.SelectionRanges.FirstOrDefault()?.InsertionPositionIndex ?? 0;
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

            if (redraw)
            {
                this.OnDrawSelections?.Invoke();
            }
        }

        public void UpdateSelection(SelectionRange selectionToUpdate, SelectionRange newSelection)
        {
            SelectionRange newNormalizedSelection = newSelection.Normalized();
            selectionToUpdate.Update(newNormalizedSelection);

            Trace.WriteLine("Updated selection: " + selectionToUpdate);
        }
    }
}
