using Editor.ViewModel;
using Note.Editor;
using Note.Rope;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236
// https://github.com/teatime77/UI.Text.Core-Sample/blob/master/TextEditor/MyEditor.xaml.cs
// https://github.com/microsoft/Windows-universal-samples/blob/main/Samples/CustomEditControl/cs

// TODO: If the last line is empty, can't go down with the cursor by pressing down, only by pressing right.

// TODO: When jumping between lines, need to save the cursor column between consecutive line jumps.
//       Ex. going from line 1->2->3 would select char index 4->2->4
//       ```
//       abc|def
//       g|
//       xyz|åäö
//       ```
// TODO: Better/simpler logic to update selection in `this.textFormattingRules`.
// TODO: Pressing Home jump to start of first character instead of first empty line at top?
// TODO: When having some text selected. If start is at first charIdx, unable to press left to
//       deselect and navigate to start as expected. Same problem with end and going right.

/*
namespace Note
{
    public sealed partial class TextEditor : UserControl, INotifyPropertyChanged
    {
        public const char LINE_BREAK = '\n';
        public const float CANVAS_PADDING_X = 7;
        public const float CANVAS_PADDING_Y = 2;
        public const float LINE_NUMBER_PADDING = 10;
        public const float LINE_NUMBER_MIN_WIDTH = 20;

        public const string FONT_FAMILY = "Consolas";
        public const int FONT_SIZE = 14;

        public bool DRAW_CUSTOM_CHARS = true;
        public bool WORD_WRAP = true;
        public bool useUnixLineBreaks = true;
        private static int SCROLL_INCREMENT = 3;

        //public static readonly Color DEFAULT_BACKGROUND = Color.FromArgb(255, 48, 48, 48);
        public static readonly Color DEFAULT_BACKGROUND = Color.FromArgb(255, 41, 40, 45);
        //private static readonly Color DEFAULT_LIGHTER_BACKGROUND = Color.FromArgb(255, 70, 70, 70);
        private static readonly Color DEFAULT_LIGHTER_BACKGROUND = Color.FromArgb(255, 98, 98, 101);
        //private static readonly Color DEFAULT_DARKER_BACKGROUND = Color.FromArgb(255, 37, 37, 37);
        private static readonly Color DEFAULT_DARKER_BACKGROUND = Color.FromArgb(255, 35, 35, 40);
        private static readonly Color DEFAULT_EXTRA_DARK_BACKGROUND = Color.FromArgb(255, 30, 30, 35);

        public static readonly Color TEXT_COLOR = Color.FromArgb(255, 220, 220, 204);
        public static readonly Color TEXT_COLOR_CUSTOM_CHAR = Color.FromArgb(120, 220, 220, 204);
        public static readonly Color SELECTION_CURSOR_COLOR = TEXT_COLOR;
        public static readonly Color SELECTION_BACKGROUND_COLOR = Color.FromArgb(100, 173, 206, 250);

        public event PropertyChangedEventHandler PropertyChanged;

        //private CanvasTextFormat textFormat;
        private FontSettings? fontSettings = null;

        private readonly SelectionRange selection = new SelectionRange(0, 0, InsertionPosition.Start);
        //private Rect selectionRect = new Rect();

        //private TextFormattingRules textFormattingRules;

        private int viewStartLineIndex = 0;
        private int viewStartLineOffset = 0;

        private ObservableCollection<LineViewModel> Lines { get; } = new ObservableCollection<LineViewModel>();

        private bool isFocused = false;

        /// <summary>
        /// If the user is in the process of holding down and dragging e.g. a piece
        /// of text, we store that information in this variable. This will be used
        /// to e.g. draw the insertion cursor in the correct spot during drag operation.
        /// </summary>
        private int currentDragPosition = -1;

        // TODO: Initialize rope.
        public Rope.Rope rope { get; set; }

        public ulong PreviousLeftClickTimestamp { get; set; }

        private LeftClickHoldStatus leftClickHoldStatus = LeftClickHoldStatus.None;

        //private PointerPoint previousDragPosition;

        public ulong DoubleClickDelayMilliSeconds => 480; 

        /// <summary>
        /// Indicates the "hold" status of the left-click.
        /// 
        /// `None` represents the status where the left-click isn't being held down.
        /// The other variants indicates at what UI element the hold started at.
        /// </summary>
        private enum LeftClickHoldStatus
        {
            None,
            TextView,
            ScrollBar,
            UnknownOrigin,
        }

        public TextEditor()
        {
            this.InitializeComponent();
            this.DataContext = this;

            this.coreWindow = CoreWindow.GetForCurrentThread();
            this.coreWindow.KeyDown += CoreWindow_KeyDown;
            this.coreWindow.PointerPressed += CoreWindow_PointerPressed;
            this.coreWindow.PointerMoved += CoreWindow_PointerMoved;
            this.coreWindow.PointerReleased += CoreWindow_PointerReleased;
            this.coreWindow.PointerWheelChanged += CoreWindow_PointerWheelChanged;

            this.editContext = CoreTextServicesManager.GetForCurrentView().CreateEditContext();
            this.editContext.TextRequested += EditContext_TextRequested;
            this.editContext.SelectionRequested += EditContext_SelectionRequested;
            this.editContext.FocusRemoved += EditContext_FocusRemoved;
            this.editContext.TextUpdating += EditContext_TextUpdating;
            this.editContext.SelectionUpdating += EditContext_SelectionUpdating;
            this.editContext.FormatUpdating += EditContext_FormatUpdating;
            this.editContext.LayoutRequested += EditContext_LayoutRequested;

            string input = "This is a test Text.\nAnother line!!";
            //string input = "a";

            this.rope = Rope.Rope.FromString(input, Encoding.Unicode);
        }

        public double CanvasHeight => CANVAS_PADDING_Y + (this.rope.GetTotalLineBreaks() + 1) * this.fontSettings?.CharHeight ?? 0;

        public int MaxAmountOfLinesWithDefaultFont => (int)(this.TextView.ActualHeight / this.fontSettings.Value.CharHeight);

        private static int AmountOfDecimalDigits(int n)
        {
            int count = 0;
            do {
                n /= 10;
                count++;
            } while (n > 0);
            return count;
        }







        private void LineNumberView_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (DesignMode.DesignModeEnabled)
            {
                return;
            }

            CanvasTextFormat textFormat = this.TextFormat(-1);

            if (this.fontSettings == null)
            {
                // Always assumes that the given font is monospaced.
                Rect charRect = new CanvasTextLayout(args.DrawingSession, "X", textFormat, float.MaxValue, float.MaxValue).LayoutBounds;
                this.fontSettings = new FontSettings(textFormat, (float)charRect.Height, (float)charRect.Width);
            }

            DrawableCharacter lastChar = this.Lines.LastOrDefault()?.LastOrDefault();
            int? lastCharIdx = lastChar?.CharIdx;
            bool lastCharIsLineBreak = lastChar != null && lastChar.FirstChar == LINE_BREAK;

            int viewStartLineIndex = this.viewStartLineIndex;
            int viewEndLineIndex = lastCharIdx != null ? this.rope.GetLineIndexForCharAtIndex(lastCharIdx.Value) : viewStartLineIndex;
            if (lastCharIsLineBreak)
            {
                viewEndLineIndex++;
            }

            int maxAmountOfDigits = AmountOfDecimalDigits(viewEndLineIndex + 1);

            float newWidth = this.fontSettings.Value.CharWidth * (float)maxAmountOfDigits + LINE_NUMBER_PADDING * 2;
            if (newWidth < LINE_NUMBER_MIN_WIDTH)
            {
                newWidth = LINE_NUMBER_MIN_WIDTH;
            }

            this.LineNumberColumnDefinition.Width = new GridLength(newWidth);

            double viewWidth = this.LineNumberView.ActualWidth;
            double viewHeight = this.LineNumberView.ActualHeight;

            Rect rectBackground = new Rect(0, 0, viewWidth, viewHeight);
            args.DrawingSession.FillRectangle(rectBackground, DEFAULT_BACKGROUND);

            float locationX = LINE_NUMBER_PADDING;
            float locationY = CANVAS_PADDING_Y;

            foreach (DrawableLine drawableLine in this.Lines)
            {
                DrawableCharacter c = drawableLine.First();

                int lineIdx = this.rope.GetLineIndexForCharAtIndex(c.CharIdx);
                int firstCharIdx = this.rope.GetFirstCharIndexAtLineWithIndex(lineIdx);

                // If we are word-wrapping, only draw the line number for the first "actual"
                // line of the word-wrapped "virtual" lines.
                if (firstCharIdx == c.CharIdx)
                {
                    string text = (lineIdx + 1).ToString();
                    float paddedLocationX = locationX + (maxAmountOfDigits - text.Length) * this.fontSettings.Value.CharWidth;
                    args.DrawingSession.DrawText(text, paddedLocationX, locationY, TEXT_COLOR, textFormat);
                }

                locationY += drawableLine.Max(x => x.Height);
            }

            if (lastCharIsLineBreak)
            {
                string text = (this.rope.GetTotalLineBreaks() + 1).ToString();
                float paddedLocationX = locationX + (maxAmountOfDigits - text.Length) * this.fontSettings.Value.CharWidth;
                args.DrawingSession.DrawText(text, paddedLocationX, locationY, TEXT_COLOR, textFormat);
            }
        }

        private void ScrollBarView_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (DesignMode.DesignModeEnabled)
            {
                return;
            }

            Rect rectBackground = new Rect(0, 0, this.ScrollBarView.ActualWidth, this.ScrollBarView.ActualHeight);
            args.DrawingSession.FillRectangle(rectBackground, DEFAULT_DARKER_BACKGROUND);

            double arrowHeight = this.ScrollBarView.ActualWidth;
            double scrollHeight = this.ScrollBarView.ActualHeight - arrowHeight * 2;
            double scrollWidth = this.ScrollBarView.ActualWidth;

            // TODO:
            int addedToHandleScrollingToBelowText = this.MaxAmountOfLinesWithDefaultFont;

            int startIdx = 0;
            int startScrollBarIdx = this.viewStartLineIndex;
            // TODO: Outcommented to ToHandleScrollingToBelowText
            //int endScrollBarIdx = startScrollBarIdx + this.viewActualDrawnLineCount;
            int endScrollBarIdx = startScrollBarIdx + addedToHandleScrollingToBelowText;
            int totIdxs = this.rope.GetTotalLineBreaks() + addedToHandleScrollingToBelowText;

            double scrollBarHeight;
            double spaceAboveScrollBarHeight = arrowHeight;

            if (totIdxs > 0)
            {
                double scrollBarHeightRatio = (endScrollBarIdx - startScrollBarIdx) / (double)totIdxs;
                scrollBarHeight = Math.Max(scrollHeight * scrollBarHeightRatio, 0);
                double spaceAboveScrollBarHeightRatio = (startScrollBarIdx - startIdx) / (double)totIdxs;
                spaceAboveScrollBarHeight += scrollHeight * spaceAboveScrollBarHeightRatio;
            }
            else
            {
                scrollBarHeight = Math.Max(scrollHeight, 0);
                spaceAboveScrollBarHeight += 0;
            }

            Rect rectScroll = new Rect(0, spaceAboveScrollBarHeight, scrollWidth, scrollBarHeight);
            args.DrawingSession.FillRectangle(rectScroll, DEFAULT_LIGHTER_BACKGROUND);

            Rect rectArrowUp = new Rect(0, 0, scrollWidth, arrowHeight);
            args.DrawingSession.FillRectangle(rectArrowUp, DEFAULT_EXTRA_DARK_BACKGROUND);

            Rect rectArrowDown = new Rect(0, this.ScrollBarView.ActualHeight - arrowHeight, scrollWidth, arrowHeight);
            args.DrawingSession.FillRectangle(rectArrowDown, DEFAULT_EXTRA_DARK_BACKGROUND);
        }

        private void TextView_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (DesignMode.DesignModeEnabled)
            {
                return;
            }

            CanvasTextFormat textFormat = this.TextFormat(-1);

            if (this.fontSettings == null)
            {
                // Always assumes that the given font is monospaced.
                Rect charRect = new CanvasTextLayout(args.DrawingSession, "X", textFormat, float.MaxValue, float.MaxValue).LayoutBounds;
                this.fontSettings = new FontSettings(textFormat, (float)charRect.Height, (float)charRect.Width);
            }

            float viewWidth = (float)this.TextView.ActualWidth;
            float viewHeight = (float)this.TextView.ActualHeight;

            Rect rectBackground = new Rect(0, 0, viewWidth, viewHeight);
            args.DrawingSession.FillRectangle(rectBackground, DEFAULT_DARKER_BACKGROUND);

            this.Lines = this.CalculateLinesToDraw(
                args,
                viewWidth,
                viewHeight,
                this.viewStartLineIndex,
                this.viewStartLineOffset
            );

            int insertionCursorCharIdx = this.currentDragPosition != -1 ? this.currentDragPosition : this.selection.InsertionPositionIndex;

            DrawSelection(args.DrawingSession, this.Lines, this.selection);
            this.DrawCharacters(args.DrawingSession, this.Lines);
            DrawInsertionCursor(args.DrawingSession, this.Lines, insertionCursorCharIdx);
        }

        private static void DrawSelection(CanvasDrawingSession drawingSession, List<DrawableLine> drawableLines, SelectionRange selection)
        {
            if (selection.IsDummy() || selection.Start == selection.End)
            {
                return;
            }

            foreach (DrawableLine drawableLine in drawableLines)
            {
                if (drawableLine.StartCharIdx > selection.End)
                {
                    break;
                }
                else if (drawableLine.EndCharIdx < selection.Start)
                {
                    continue;
                }

                var drawableCharsInSelection = drawableLine.Where(x => selection.IsInRange(x.CharIdx));
                if (drawableCharsInSelection.Count() > 0)
                {
                    var firstDrawableCharInSelection = drawableCharsInSelection.First();
                    var lastDrawableCharInSelection = drawableCharsInSelection.Last();

                    float locationX = firstDrawableCharInSelection.X;
                    float locationY = firstDrawableCharInSelection.Y;
                    float width = lastDrawableCharInSelection.X + lastDrawableCharInSelection.Width - locationX;
                    float height = drawableLine.Max(x => x.Height);

                    Rect selectionRect = new Rect(locationX, locationY, width, height);
                    drawingSession.FillRectangle(selectionRect, SELECTION_BACKGROUND_COLOR);
                }
            }
        }

        // TODO: Make static. Need to send in posible `textFormat`.
        private void DrawCharacters(CanvasDrawingSession drawingSession, List<DrawableLine> drawableLines)
        {
            foreach (DrawableLine drawableLine in drawableLines)
            {
                foreach (DrawableCharacter drawableChar in drawableLine)
                {
                    CanvasTextFormat textFormat = this.TextFormat(drawableChar.CharIdx);
                    drawableChar.Draw(drawingSession, textFormat, TEXT_COLOR, DRAW_CUSTOM_CHARS);
                }
            }
        }

        private static void DrawInsertionCursor(CanvasDrawingSession drawingSession, List<DrawableLine> drawableLines, int insertionCursorCharIdx)
        {
            // TODO: Want to hide cursor when document isn't focused?
            DrawableCharacter charAtInsertionPosition = drawableLines
                .SelectMany(l => l.Where(c => c.CharIdx == insertionCursorCharIdx))
                .FirstOrDefault();

            if (charAtInsertionPosition != null)
            {
                DrawableCharacter c = charAtInsertionPosition;
                drawingSession.DrawLine(c.X, c.Y, c.X, c.Y + c.Height, SELECTION_CURSOR_COLOR, 1);
            }
            else // Assume positioned after the last character (can also be that no text exists).
            {
                DrawableCharacter c = drawableLines.LastOrDefault()?.LastOrDefault();
                if (c != null && c.FirstChar == LINE_BREAK)
                {
                    drawingSession.DrawLine(CANVAS_PADDING_X, c.Y + c.Height, CANVAS_PADDING_X, c.Y + 2 * c.Height, SELECTION_CURSOR_COLOR, 1);
                }
                else if (c != null)
                {
                    drawingSession.DrawLine(c.X + c.Width, c.Y, c.X + c.Width, c.Y + c.Height, SELECTION_CURSOR_COLOR, 1);
                }
            }
        }

        private void Invalidate()
        {
            this.TextView.Invalidate();
            this.LineNumberView.Invalidate();
            this.ScrollBarView.Invalidate();
        }

        private void MoveSelection(VirtualKey key)
        {
            int newIdx;
            SelectionRange newSelection = new SelectionRange(this.selection);

            switch (key)
            {
                case VirtualKey.Left:
                    {
                        if (newSelection.InsertionPositionIndex == 0)
                        {
                            return;
                        }

                        if (IsPressed(VirtualKey.Control))
                        {
                            bool skipEndWhitespaces = false;
                            newIdx = this.rope.GetCurrentWordStartIndex(newSelection.InsertionPositionIndex, skipEndWhitespaces);
                        }
                        else
                        {
                            newIdx = newSelection.InsertionPositionIndex - 1;
                        }

                        if (IsPressed(VirtualKey.Shift))
                        {
                            newSelection.InsertionPositionIndex = newIdx;
                        }
                        else if (this.selection.Length > 0 && !IsPressed(VirtualKey.Control))
                        {
                            newSelection.Start = this.selection.Start;
                            newSelection.End = this.selection.Start;
                        }
                        else
                        {
                            newSelection.Start = newIdx;
                            newSelection.End = newIdx;
                        }
                    }
                    break;

                case VirtualKey.Right:
                    {
                        int lastIdx = this.rope.GetTotalCharCount();
                        if (newSelection.InsertionPositionIndex == lastIdx)
                        {
                            return;
                        }

                        if (IsPressed(VirtualKey.Control))
                        {
                            bool skipEndWhitespaces = true;
                            newIdx = this.rope.GetNextWordStartIndex(newSelection.InsertionPositionIndex, skipEndWhitespaces);
                        }
                        else
                        {
                            newIdx = newSelection.InsertionPositionIndex + 1;
                        }

                        Trace.WriteLine("newIdx: " + newIdx);

                        if (IsPressed(VirtualKey.Shift))
                        {
                            newSelection.InsertionPositionIndex = newIdx;
                        }
                        else if (this.selection.Length > 0 && !IsPressed(VirtualKey.Control))
                        {
                            newSelection.Start = this.selection.End;
                            newSelection.End = this.selection.End;
                        }
                        else
                        {
                            newSelection.Start = newIdx;
                            newSelection.End = newIdx;
                        }

                        Trace.WriteLine("newSelection.Start: " + newSelection.Start + ", newSelection.End: " + newSelection.End);
                    }
                    break;

                case VirtualKey.Up:
                    {
                        int lineIdx = this.rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        if (lineIdx == 0)
                        {
                            return;
                        }

                        int targetLineIdx = lineIdx - 1;
                        int targetLineLastCharIdx = this.rope.GetFirstCharIndexAtLineWithIndex(lineIdx) - 1;
                        bool lastCharIsLineBreak = this.rope.IterateChars(targetLineLastCharIdx).First().Item1 == LINE_BREAK;
                        newSelection = MoveSelectionVertical(lineIdx, targetLineIdx, lastCharIsLineBreak);
                    }
                    break;

                case VirtualKey.Down:
                    {
                        int lineIdx = this.rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        int totalLineBreaks = this.rope.GetTotalLineBreaks();
                        if (lineIdx >= totalLineBreaks)
                        {
                            return;
                        }

                        int targetLineIdx = lineIdx + 1;
                        int belowTargetLineFirstCharIdx = this.rope.GetFirstCharIndexAtLineWithIndex(targetLineIdx + 1);
                        bool lastCharIsLineBreak;
                        if (belowTargetLineFirstCharIdx != -1)
                        {
                            int targetLineLastCharIdx = belowTargetLineFirstCharIdx - 1;
                            lastCharIsLineBreak = this.rope.IterateChars(targetLineLastCharIdx).First().Item1 == LINE_BREAK;
                        }
                        else
                        {
                            lastCharIsLineBreak = false;
                        }
                        newSelection = MoveSelectionVertical(lineIdx, targetLineIdx, lastCharIsLineBreak);
                    }
                    break;

                case VirtualKey.PageUp:
                    {
                        // TODO:
                        //int lineIdx = this.rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        //if (lineIdx == 0)
                        //{
                        //    return;
                        //}
                        //
                        //int targetLineIdx = lineIdx - this.viewActualDrawnLineCount;
                        //if (targetLineIdx < 0)
                        //{
                        //    targetLineIdx = 0;
                        //}
                        //
                        //newSelection = MoveSelectionVertical(lineIdx, targetLineIdx);
                    }
                    break;

                case VirtualKey.PageDown:
                    {
                        // TODO:
                        //int lineIdx = this.rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        //if (lineIdx == 0)
                        //{
                        //    return;
                        //}
                        //
                        //int targetLineIdx = lineIdx + this.viewActualDrawnLineCount;
                        //int totalLineBreaks = this.rope.GetTotalLineBreaks();
                        //if (targetLineIdx >= totalLineBreaks)
                        //{
                        //    targetLineIdx = totalLineBreaks - 1;
                        //}
                        //
                        //newSelection = MoveSelectionVertical(lineIdx, targetLineIdx);
                    }
                    break;

                case VirtualKey.End:
                    {
                        int lastCharIdx = this.rope.GetTotalCharCount() - 1;
                        if (newSelection.InsertionPositionIndex == lastCharIdx)
                        {
                            return;
                        }

                        int lineIdx = this.rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        int firstCharIdxAtLine = this.rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
                        int charCountAtLine = this.rope.GetCharCountAtLineWithIndex(lineIdx);
                        int lastCharIdxAtLine = firstCharIdxAtLine + charCountAtLine - 1;

                        if (IsPressed(VirtualKey.Shift))
                        {
                            newSelection.InsertionPositionIndex = lastCharIdxAtLine;
                        }
                        else
                        {
                            newSelection.Start = lastCharIdxAtLine;
                            newSelection.End = lastCharIdxAtLine;
                        }
                    }
                    break;

                case VirtualKey.Home:
                    {
                        if (newSelection.InsertionPositionIndex == 0)
                        {
                            return;
                        }

                        int lineIdx = this.rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        int firstCharIdxAtLine = this.rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
                        int firstWordIdxStartAtLine = this.rope.SkipWhitespaces(firstCharIdxAtLine);

                        if (IsPressed(VirtualKey.Shift))
                        {
                            newSelection.InsertionPositionIndex = firstWordIdxStartAtLine;
                        }
                        else
                        {
                            newSelection.Start = firstWordIdxStartAtLine;
                            newSelection.End = firstWordIdxStartAtLine;
                        }
                    }
                    break;

                default:
                    throw new Exception("Invalid key in MoveSelection: " + key);
            }

            if (!newSelection.IsDummy())
            {
                this.NotifySelectionChanged(newSelection);
            }
        }

        private SelectionRange MoveSelectionVertical(int lineIdx, int targetLineIdx, bool lastCharOneTargetLineIsLineBreak)
        {
            SelectionRange newSelection = new SelectionRange(this.selection);

            int startCharIdxAtCurrentLine = this.rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
            int startCharIdxAtTargetLine;
            if (targetLineIdx < 0)
            {
                startCharIdxAtTargetLine = 0;
            }
            else if (targetLineIdx > this.rope.GetTotalLineBreaks())
            {
                startCharIdxAtTargetLine = this.rope.GetTotalCharCount();
            }
            else
            {
                startCharIdxAtTargetLine = this.rope.GetFirstCharIndexAtLineWithIndex(targetLineIdx);
            }

            // -1 to ignore the line feed character itself.
            int totalCharCountAtTargetLine = this.rope.GetCharCountAtLineWithIndex(targetLineIdx) + (lastCharOneTargetLineIsLineBreak ? -1 : 0);
            int charLengthToInsertionAtCurrentLine = newSelection.InsertionPositionIndex - startCharIdxAtCurrentLine;
            int charLengthToNewSelectionAtTargetLine = Math.Min(totalCharCountAtTargetLine, charLengthToInsertionAtCurrentLine);

            if (IsPressed(VirtualKey.Control))
            {
                // TODO: Should scroll up/down one row if this is a up/down.
                //       How to handle the other cases?
                newSelection = SelectionRange.Dummy();
            }
            else
            {
                int newIdx = startCharIdxAtTargetLine + charLengthToNewSelectionAtTargetLine;
                if (IsPressed(VirtualKey.Shift))
                {
                    newSelection.InsertionPositionIndex = newIdx;
                }
                else
                {
                    newSelection.Start = newIdx;
                    newSelection.End = newIdx;
                }
            }

            return newSelection;
        }

        private (Typeface, double) CalculateFontSettings()
        {
            // TODO: Get font from somewhere.
            return (new Typeface(FONT_FAMILY), FONT_SIZE);
        }

        private readonly struct FontSettings
        {
            public CanvasTextFormat TextFormat { get; }
            public float CharHeight { get; }
            public float CharWidth { get; }

            public FontSettings(CanvasTextFormat textFormat, float charHeight, float charWidth)
            {
                this.TextFormat = textFormat;
                this.CharHeight = charHeight;
                this.CharWidth = charWidth;
            }
        }

        private async void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            Trace.WriteLine("CoreWindow_KeyDown - key: " + args.VirtualKey + ", isFocused: " + this.isFocused);

            if (!this.isFocused)
            {
                return;
            }

            switch (args.VirtualKey)
            {
                case VirtualKey.Left:
                case VirtualKey.Right:
                case VirtualKey.Up:
                case VirtualKey.Down:
                case VirtualKey.PageUp:
                case VirtualKey.PageDown:
                case VirtualKey.End:
                case VirtualKey.Home:
                    MoveSelection(args.VirtualKey);
                    break;

                case VirtualKey.Back:
                    {
                        if (this.selection.Start == 0 && this.selection.Length == 0)
                        {
                            return;
                        }

                        int startIdx;
                        int charAmountToRemove;
                        if (IsPressed(VirtualKey.Control))
                        {
                            bool skipEndWhitespaces = false;
                            startIdx = this.rope.GetCurrentWordStartIndex(this.selection.InsertionPositionIndex, skipEndWhitespaces);
                            charAmountToRemove = this.selection.End - startIdx;
                        }
                        else if (this.selection.Length > 0)
                        {
                            startIdx = this.selection.Start;
                            charAmountToRemove = this.selection.Length;
                        }
                        else
                        {
                            // Not a selection, just want to remove the character to
                            // the left of the current insertion cursor.
                            startIdx = this.selection.Start - 1;
                            charAmountToRemove = 1;
                        }

                        Range modifiedRange = new Range(startIdx, startIdx + charAmountToRemove);
                        SelectionRange newSelection = new SelectionRange(startIdx, startIdx, InsertionPosition.Start);

                        this.rope.Remove(startIdx, charAmountToRemove);
                        this.NotifyTextChanged(modifiedRange, charAmountToRemove, newSelection);
                    }
                    break;

                case VirtualKey.Delete:
                    {
                        int lastIdx = this.rope.GetTotalCharCount();
                        if (this.selection.Start == lastIdx)
                        {
                            return;
                        }

                        int charAmountToRemove;
                        if (IsPressed(VirtualKey.Control))
                        {
                            bool skipEndWhitespaces = true;
                            int endIdx = this.rope.GetNextWordStartIndex(this.selection.End, skipEndWhitespaces);
                            charAmountToRemove = endIdx - this.selection.Start;
                        }
                        else if (this.selection.Length > 0)
                        {
                            charAmountToRemove = this.selection.Length;
                        }
                        else
                        {
                            charAmountToRemove = 1;
                        }

                        int newIdx = this.selection.Start;
                        Range modifiedRange = new Range(newIdx, newIdx + charAmountToRemove);
                        SelectionRange newSelection = new SelectionRange(newIdx, newIdx, InsertionPosition.Start);

                        this.rope.Remove(newIdx, charAmountToRemove);
                        this.NotifyTextChanged(modifiedRange, charAmountToRemove, newSelection);
                    }
                    break;

                // TODO: Do we need to handle any of these characters here=
                //       Or will we get all "visual" characters through the
                //       `CoreTextEditContext`?
                case VirtualKey.Tab:
                    break;

                case VirtualKey.Enter:
                    {
                        string lineBreak = this.useUnixLineBreaks ? "\n" : "\r\n";
                        if (this.selection.Length > 0)
                        {
                            this.rope.Replace(this.selection.Start, this.selection.Length, lineBreak.AsSpan());
                        }
                        else
                        {
                            this.rope.Insert(this.selection.InsertionPositionIndex, lineBreak.AsSpan());
                        }

                        int newIdx = this.selection.Start + lineBreak.Length;
                        Range modifiedRange = new Range(this.selection.Start, this.selection.End);
                        SelectionRange newSelection = new SelectionRange(newIdx, newIdx, InsertionPosition.Start);

                        this.NotifyTextChanged(modifiedRange, lineBreak.Length, newSelection);
                    }
                    break;

                case VirtualKey.Shift:
                    break;
                case VirtualKey.Control:
                    break;
                case VirtualKey.Escape:
                    break;
                case VirtualKey.Space:
                    break;
                case VirtualKey.Insert:
                    break;

                case VirtualKey.Number0:
                    break;
                case VirtualKey.Number1:
                    break;
                case VirtualKey.Number2:
                    break;
                case VirtualKey.Number3:
                    break;
                case VirtualKey.Number4:
                    break;
                case VirtualKey.Number5:
                    break;
                case VirtualKey.Number6:
                    break;
                case VirtualKey.Number7:
                    break;
                case VirtualKey.Number8:
                    break;
                case VirtualKey.Number9:
                    break;
                case VirtualKey.A:
                    break;
                case VirtualKey.B:
                    break;
                case VirtualKey.C:
                    break;
                case VirtualKey.D:
                    break;
                case VirtualKey.E:
                    break;
                case VirtualKey.F:
                    break;
                case VirtualKey.G:
                    break;
                case VirtualKey.H:
                    break;
                case VirtualKey.I:
                    break;
                case VirtualKey.J:
                    break;
                case VirtualKey.K:
                    break;
                case VirtualKey.L:
                    break;
                case VirtualKey.M:
                    break;
                case VirtualKey.N:
                    break;
                case VirtualKey.O:
                    break;
                case VirtualKey.P:
                    break;
                case VirtualKey.Q:
                    break;
                case VirtualKey.R:
                    break;
                case VirtualKey.S:
                    break;
                case VirtualKey.T:
                    break;
                case VirtualKey.U:
                    break;
                case VirtualKey.V:
                    break;
                case VirtualKey.W:
                    break;
                case VirtualKey.X:
                    break;
                case VirtualKey.Y:
                    break;
                case VirtualKey.Z:
                    break;
                case VirtualKey.NumberPad0:
                    break;
                case VirtualKey.NumberPad1:
                    break;
                case VirtualKey.NumberPad2:
                    break;
                case VirtualKey.NumberPad3:
                    break;
                case VirtualKey.NumberPad4:
                    break;
                case VirtualKey.NumberPad5:
                    break;
                case VirtualKey.NumberPad6:
                    break;
                case VirtualKey.NumberPad7:
                    break;
                case VirtualKey.NumberPad8:
                    break;
                case VirtualKey.NumberPad9:
                    break;


                case VirtualKey.Multiply:
                    break;
                case VirtualKey.Add:
                    break;
                case VirtualKey.Separator:
                    break;
                case VirtualKey.Subtract:
                    break;
                case VirtualKey.Decimal:
                    break;
                case VirtualKey.Divide:
                    break;
                case VirtualKey.F1:
                    break;
                case VirtualKey.F2:
                    break;
                case VirtualKey.F3:
                    break;
                case VirtualKey.F4:
                    break;
                case VirtualKey.F5:
                    break;
                case VirtualKey.F6:
                    break;
                case VirtualKey.F7:
                    break;
                case VirtualKey.F8:
                    break;
                case VirtualKey.F9:
                    break;
                case VirtualKey.F10:
                    break;
                case VirtualKey.F11:
                    break;
                case VirtualKey.F12:
                    break;
                case VirtualKey.F13:
                    break;
                case VirtualKey.F14:
                    break;
                case VirtualKey.F15:
                    break;
                case VirtualKey.F16:
                    break;
                case VirtualKey.F17:
                    break;
                case VirtualKey.F18:
                    break;
                case VirtualKey.F19:
                    break;
                case VirtualKey.F20:
                    break;
                case VirtualKey.F21:
                    break;
                case VirtualKey.F22:
                    break;
                case VirtualKey.F23:
                    break;
                case VirtualKey.F24:
                    break;






                case VirtualKey.None:
                    break;
                case VirtualKey.LeftButton:
                    break;
                case VirtualKey.RightButton:
                    break;
                case VirtualKey.Cancel:
                    break;
                case VirtualKey.MiddleButton:
                    break;
                case VirtualKey.XButton1:
                    break;
                case VirtualKey.XButton2:
                    break;
                
                
                case VirtualKey.Clear:
                    break;
                
                
                case VirtualKey.Menu:
                    break;
                case VirtualKey.Pause:
                    break;
                case VirtualKey.CapitalLock:
                    break;
                case VirtualKey.Kana:
                    break;
                case VirtualKey.ImeOn:
                    break;
                case VirtualKey.Junja:
                    break;
                case VirtualKey.Final:
                    break;
                case VirtualKey.Hanja:
                    break;
                case VirtualKey.ImeOff:
                    break;
                
                case VirtualKey.Convert:
                    break;
                case VirtualKey.NonConvert:
                    break;
                case VirtualKey.Accept:
                    break;
                case VirtualKey.ModeChange:
                    break;
                
                
                case VirtualKey.Select:
                    break;
                case VirtualKey.Print:
                    break;
                case VirtualKey.Execute:
                    break;
                case VirtualKey.Snapshot:
                    break;
                
                case VirtualKey.Help:
                    break;
                


                case VirtualKey.LeftWindows:
                    break;
                case VirtualKey.RightWindows:
                    break;
                case VirtualKey.Application:
                    break;
                case VirtualKey.Sleep:
                    break;
                
                case VirtualKey.NavigationView:
                    break;
                case VirtualKey.NavigationMenu:
                    break;
                case VirtualKey.NavigationUp:
                    break;
                case VirtualKey.NavigationDown:
                    break;
                case VirtualKey.NavigationLeft:
                    break;
                case VirtualKey.NavigationRight:
                    break;
                case VirtualKey.NavigationAccept:
                    break;
                case VirtualKey.NavigationCancel:
                    break;
                case VirtualKey.NumberKeyLock:
                    break;
                case VirtualKey.Scroll:
                    break;
                case VirtualKey.LeftShift:
                    break;
                case VirtualKey.RightShift:
                    break;
                case VirtualKey.LeftControl:
                    break;
                case VirtualKey.RightControl:
                    break;
                case VirtualKey.LeftMenu:
                    break;
                case VirtualKey.RightMenu:
                    break;
                case VirtualKey.GoBack:
                    break;
                case VirtualKey.GoForward:
                    break;
                case VirtualKey.Refresh:
                    break;
                case VirtualKey.Stop:
                    break;
                case VirtualKey.Search:
                    break;
                case VirtualKey.Favorites:
                    break;
                case VirtualKey.GoHome:
                    break;
                case VirtualKey.GamepadA:
                    break;
                case VirtualKey.GamepadB:
                    break;
                case VirtualKey.GamepadX:
                    break;
                case VirtualKey.GamepadY:
                    break;
                case VirtualKey.GamepadRightShoulder:
                    break;
                case VirtualKey.GamepadLeftShoulder:
                    break;
                case VirtualKey.GamepadLeftTrigger:
                    break;
                case VirtualKey.GamepadRightTrigger:
                    break;
                case VirtualKey.GamepadDPadUp:
                    break;
                case VirtualKey.GamepadDPadDown:
                    break;
                case VirtualKey.GamepadDPadLeft:
                    break;
                case VirtualKey.GamepadDPadRight:
                    break;
                case VirtualKey.GamepadMenu:
                    break;
                case VirtualKey.GamepadView:
                    break;
                case VirtualKey.GamepadLeftThumbstickButton:
                    break;
                case VirtualKey.GamepadRightThumbstickButton:
                    break;
                case VirtualKey.GamepadLeftThumbstickUp:
                    break;
                case VirtualKey.GamepadLeftThumbstickDown:
                    break;
                case VirtualKey.GamepadLeftThumbstickRight:
                    break;
                case VirtualKey.GamepadLeftThumbstickLeft:
                    break;
                case VirtualKey.GamepadRightThumbstickUp:
                    break;
                case VirtualKey.GamepadRightThumbstickDown:
                    break;
                case VirtualKey.GamepadRightThumbstickRight:
                    break;
                case VirtualKey.GamepadRightThumbstickLeft:
                    break;
            }
        }

        private static ulong MicroToMilli(ulong micro) => micro / 1000;

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            ulong timestamp = args.CurrentPoint.Timestamp;
            Point pointerPosition = args.CurrentPoint.Position;
            PointerUpdateKind pointerUpdateKind = args.CurrentPoint.Properties.PointerUpdateKind;

            Rect lineNumberViewRect = GetRectRelativeToWindow(this.LineNumberView);
            Rect textViewRect = GetRectRelativeToWindow(this.TextView);
            Rect scrollBarViewRect = GetRectRelativeToWindow(this.ScrollBarView);

            bool clickIsInsideLineNumberView = lineNumberViewRect.Contains(pointerPosition);
            bool clickIsInsideTextView = textViewRect.Contains(pointerPosition);
            bool clickIsInsideScrollBarView = scrollBarViewRect.Contains(pointerPosition);
            bool clickIsInsideCanvas = clickIsInsideLineNumberView || clickIsInsideTextView || clickIsInsideScrollBarView;

            bool focusWasUpdated = this.HandleFocusClick(clickIsInsideCanvas);

            if (!clickIsInsideCanvas)
            {
                if (focusWasUpdated)
                {
                    this.Invalidate();
                }

                return;
            }

            if (clickIsInsideLineNumberView)
            {

            }
            else if (clickIsInsideTextView)
            {
                var (_, relativePointerPosition) = this.MoveOriginToZeroZero(textViewRect, pointerPosition);
                this.HandleTextViewClick(pointerUpdateKind, relativePointerPosition, timestamp);
            }
            else if (clickIsInsideScrollBarView)
            {
                var (_, relativePointerPosition) = this.MoveOriginToZeroZero(textViewRect, pointerPosition);
                this.HandleScrollBarViewClick(pointerUpdateKind, relativePointerPosition, timestamp);
            }

            if (pointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                this.PreviousLeftClickTimestamp = timestamp;
                this.previousDragPosition = args.CurrentPoint;
                this.leftClickHoldStatus = clickIsInsideTextView ? LeftClickHoldStatus.TextView
                                                                 : clickIsInsideScrollBarView ? LeftClickHoldStatus.ScrollBar
                                                                                              : LeftClickHoldStatus.UnknownOrigin;
            }

            this.Invalidate();
        }

        private void HandleTextViewClick(PointerUpdateKind pointerUpdateKind, Point pointerPosition, ulong timestamp)
        {
            switch (pointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                    ulong elapsed = MicroToMilli(timestamp - this.PreviousLeftClickTimestamp);
                    bool isDoubleClick = elapsed < this.DoubleClickDelayMilliSeconds;
                    if (isDoubleClick)
                    {
                        this.HandleTextViewDoubleLeftClick(pointerPosition);
                    }
                    else
                    {
                        this.HandleTextViewSingleLeftClick(pointerPosition);
                    }
                    break;

                case PointerUpdateKind.RightButtonPressed:
                    break;

                default:
                    break;
            }
        }

        private void HandleTextViewSingleLeftClick(Point pointerPosition)
        {
            int charIdx = this.GetInsertionCharIndexFromPosition((float)pointerPosition.X, (float)pointerPosition.Y);
            this.UpdateSelection(new SelectionRange(charIdx));
        }

        private void HandleTextViewDoubleLeftClick(Point pointerPosition)
        {
            int charIdx = this.GetInsertionCharIndexFromPosition((float)pointerPosition.X, (float)pointerPosition.Y);
            int startIdx = this.rope.GetCurrentWordStartIndex(charIdx, true);
            int endIdx = this.rope.GetNextWordStartIndex(charIdx, false);
            this.UpdateSelection(new SelectionRange(startIdx, endIdx, InsertionPosition.End));
        }

        private void HandleScrollBarViewClick(PointerUpdateKind pointerUpdateKind, Point pointerPosition, ulong timestamp)
        {
            if (pointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            {
                return;
            }

            double y = pointerPosition.Y;

            double arrowHeight = this.ScrollBarView.ActualWidth;
            double height = Math.Max(this.ScrollBarView.ActualHeight - arrowHeight * 2, 0);

            bool upArrowIsPressed = y <= arrowHeight;
            bool downArrowIsPressed = y >= arrowHeight + height;

            if (upArrowIsPressed)
            {
                this.ScrollAmountOfLines(-1);
            }
            else if (downArrowIsPressed)
            {
                this.ScrollAmountOfLines(1);
            }
            else
            {
                this.leftClickHoldStatus = LeftClickHoldStatus.ScrollBar;
            }
        }

        private bool HandleFocusClick(bool clickIsInsideCanvas)
        {
            bool focusWasUpdated = false;

            if (clickIsInsideCanvas && !this.isFocused)
            {
                this.isFocused = true;
                this.editContext.NotifyFocusEnter();
                this.Focus(FocusState.Programmatic);
                focusWasUpdated = true;
            }
            else if (!clickIsInsideCanvas && this.isFocused)
            {
                this.isFocused = false;
                this.editContext.NotifyFocusLeave();
                focusWasUpdated = true;
            }

            return focusWasUpdated;
        }

        /// <summary>
        /// Normalize so that the coordinates for `rect` & `point` starts at point (0,0).
        /// 
        /// We assume that the given `rect` & `point` are relative to the whole windows view.
        /// </summary>
        /// <param name="rect">The Rect to normalize</param>
        /// <param name="point">The Point to normalize</param>
        /// <returns>New Rect & Point with origin at point (0,0)</returns>
        private (Rect, Point) MoveOriginToZeroZero(Rect rect, Point point)
        {
            point.X -= rect.X;
            point.Y -= rect.Y;
            rect.X = 0;
            rect.Y = 0;
            return (rect, point);
        }

        private void CoreWindow_PointerMoved(CoreWindow sender, PointerEventArgs args)
        {
            Point pointerPosition = args.CurrentPoint.Position;

            Rect textViewRect = GetRectRelativeToWindow(this.TextView);
            Rect scrollBarViewRect = GetRectRelativeToWindow(this.ScrollBarView);

            if (this.leftClickHoldStatus == LeftClickHoldStatus.TextView && textViewRect.Contains(pointerPosition))
            {
                var (_, relativePointerPosition) = this.MoveOriginToZeroZero(textViewRect, pointerPosition);
                this.HandleTextViewDrag(relativePointerPosition);
            }
            else if (this.leftClickHoldStatus == LeftClickHoldStatus.ScrollBar && scrollBarViewRect.Contains(pointerPosition))
            {
                var (_, relativePointerPosition) = this.MoveOriginToZeroZero(scrollBarViewRect, pointerPosition);
                this.HandleScrollBarViewDrag(relativePointerPosition);
            }

            this.Invalidate();
        }

        private void HandleTextViewDrag(Point pointerPosition)
        {
            int newInsertionCharIdx = this.GetInsertionCharIndexFromPosition((float)pointerPosition.X, (float)pointerPosition.Y);
            int oldInsertionCharIdx = this.selection.InsertionPositionIndex;

            if (newInsertionCharIdx != oldInsertionCharIdx)
            {
                SelectionRange newSelection = new SelectionRange(this.selection)
                {
                    InsertionPositionIndex = newInsertionCharIdx
                };

                this.UpdateSelection(newSelection);
                this.Invalidate();
            }
        }

        private void HandleScrollBarViewDrag(Point pointerPosition)
        {
            PointerPoint previousDragPosition = this.previousDragPosition;


            double y = pointerPosition.Y;

            double arrowHeight = this.ScrollBarView.ActualWidth;
            double height = Math.Max(this.ScrollBarView.ActualHeight - arrowHeight * 2, 0);

            bool upArrowIsPressed = y <= arrowHeight;
            bool downArrowIsPressed = y >= arrowHeight + height;

            if (upArrowIsPressed)
            {
                this.ScrollAmountOfLines(-1);
            }
            else if (downArrowIsPressed)
            {
                this.ScrollAmountOfLines(1);
            }
            else
            {
                this.leftClickHoldStatus = LeftClickHoldStatus.ScrollBar;
            }
        }

        private void CoreWindow_PointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            if (args.CurrentPoint.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                this.leftClickHoldStatus = LeftClickHoldStatus.None;
            }
        }

        private void CoreWindow_PointerWheelChanged(CoreWindow sender, PointerEventArgs args)
        {
            // Positive value => scroll upwards
            // Negative       => scroll downwards
            int mouseWheelDelta =  args.CurrentPoint.Properties.MouseWheelDelta;

            if (mouseWheelDelta > 0)
            {
                this.ScrollAmountOfLines(-SCROLL_INCREMENT);
            }
            else if (mouseWheelDelta < 0)
            {
                this.ScrollAmountOfLines(SCROLL_INCREMENT);
            }

            this.Invalidate();
        }

        private void ScrollAmountOfLines(int amountOfLinesToScroll)
        {
            int minIdx = 0;
            int maxIdx = this.rope.GetTotalLineBreaks();

            int newValue = this.viewStartLineIndex + amountOfLinesToScroll;
            int clampedvalue = Math.Min(maxIdx, Math.Max(minIdx, newValue));

            this.viewStartLineIndex = clampedvalue;
        }

        private void EditContext_FocusRemoved(CoreTextEditContext sender, object args)
        {
            this.isFocused = false;
        }

        private void EditContext_TextRequested(CoreTextEditContext sender, CoreTextTextRequestedEventArgs args)
        {
            StringBuilder sb = new StringBuilder();
            CoreTextRange requestedRange = args.Request.Range;
            int limit = requestedRange.EndCaretPosition - requestedRange.StartCaretPosition;

            // TODO: Iterate chars in bulk instead of one-by-one.
            foreach (var (c, _) in this.rope.IterateChars(requestedRange.StartCaretPosition, limit))
            {
                sb.Append(c);
            }

            args.Request.Text = sb.ToString();
        }

        private void EditContext_SelectionRequested(CoreTextEditContext sender, CoreTextSelectionRequestedEventArgs args)
        {
            args.Request.Selection = this.selection.AsCoreTextRange();
        }

        private void EditContext_TextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
        {
            CoreTextRange rangeToReplace = args.Range;
            CoreTextRange newSelection = args.NewSelection;
            int charAmountToReplace = rangeToReplace.EndCaretPosition - rangeToReplace.StartCaretPosition;
            int startIdx = rangeToReplace.StartCaretPosition;
            string text = args.Text;

            Trace.WriteLine("Text: " + text + ", rangeToReplace.Start: " + rangeToReplace.StartCaretPosition + ", rangeToReplace.End: " + rangeToReplace.EndCaretPosition + ", charAmountToReplace: " + charAmountToReplace + ", new: " + (startIdx + text.Length));

            if (charAmountToReplace > 0)
            {
                this.rope.Replace(startIdx, charAmountToReplace, text.AsSpan());
            }
            else
            {
                this.rope.Insert(startIdx, text.AsSpan());
            }

            this.NotifySelectionChanged(new SelectionRange(newSelection), false);
        }

        private void EditContext_SelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs args)
        {
            this.NotifySelectionChanged(new SelectionRange(args.Selection), false);
        }

        private void EditContext_FormatUpdating(CoreTextEditContext sender, CoreTextFormatUpdatingEventArgs args)
        {
            // TODO: Do we care about this?
        }

        // TODO: How does this work if we have a selection that spans multiple lines?
        private void EditContext_LayoutRequested(CoreTextEditContext sender, CoreTextLayoutRequestedEventArgs args)
        {
            //Rect canvasRect = GetRect(this.TextView);
            //Rect selectionRect = this.selectionRect;
            //
            //Rect windowsBounds = Window.Current.CoreWindow.Bounds;
            //canvasRect.X += windowsBounds.X;
            //canvasRect.Y += windowsBounds.Y;
            //selectionRect.X += windowsBounds.X;
            //selectionRect.Y += windowsBounds.Y;
            //
            //double scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            //canvasRect = ScaleRect(canvasRect, scale);
            //selectionRect = ScaleRect(selectionRect, scale);
            //
            //args.Request.LayoutBounds.ControlBounds = canvasRect;
            //args.Request.LayoutBounds.TextBounds = selectionRect;
        }

        private bool IsPressed(VirtualKey key)
        {
            CoreVirtualKeyStates keyState = Window.Current.CoreWindow.GetKeyState(key);
            return keyState.HasFlag(CoreVirtualKeyStates.Down);
        }

        private void NotifyTextChanged(Range modifiedRange, int newTextLength, SelectionRange newSelection)
        {
            this.UpdateSelection(newSelection);

            this.editContext.NotifyTextChanged(modifiedRange.AsCoreTextRange(), newTextLength, newSelection.AsCoreTextRange());

            this.Invalidate();
        }

        private void NotifySelectionChanged(SelectionRange newSelection, bool notifyEditContext = true)
        {
            this.UpdateSelection(newSelection);

            if (notifyEditContext)
            {
                this.editContext.NotifySelectionChanged(this.selection.AsCoreTextRange());
            }

            Trace.WriteLine("newSelection: (" + newSelection.Start + ", " + newSelection.End + ")");

            // TODO: Scroll to selection in view?
            this.Invalidate();
        }

        private void UpdateSelection(SelectionRange newSelection)
        {
            SelectionRange newNormalizedSelection = newSelection.Normalized();
            this.selection.Update(newNormalizedSelection);

            int selectionStartLineIdx = this.rope.GetLineIndexForCharAtIndex(this.selection.Start);
            int viewStartLineIdx = this.viewStartLineIndex;

            if (selectionStartLineIdx < viewStartLineIdx)
            {
                this.viewStartLineIndex = selectionStartLineIdx;
                this.viewStartLineOffset = 0;
            }
            else if (selectionStartLineIdx > viewStartLineIndex + (this.MaxAmountOfLinesWithDefaultFont - 1))
            {
                // TODO: Probably want to lineup the last line with the end of the TextView,
                //       so want to do this in some other way to achieve it.
                this.viewStartLineIndex += selectionStartLineIdx - (viewStartLineIndex + (this.MaxAmountOfLinesWithDefaultFont - 1));
                this.viewStartLineOffset = 0; // TODO: This probably not needed (?).
            }
        }

        private int GetInsertionCharIndexFromPosition(float x, float y)
        {
            DrawableCharacter ch = this.Lines
                .SelectMany(l => l.Where(c => c.Contains(x, y)))
                .FirstOrDefault();

            // 1. We pressed directly at a character.
            if (ch != null)
            {
                if (x > ch.X + ch.Width / 2.0)
                {
                    // If we pressed at the "end" (right) of the character, we probably want to
                    // insert the cursor behind the character, not the default infront of it.
                    return ch.CharIdx + (ch.IsSurrogate ? 2 : 1);
                }
                else
                {
                    return ch.CharIdx;
                }
            }

            int totalCharCount = this.rope.GetTotalCharCount();

            // 2. We didn't press directly at a character. See if we pressed before/after a specific line.
            DrawableLine line = this.Lines.FirstOrDefault(l => y >= l.Y && y < l.Y + l.Height);
            if (line != null)
            {
                if (x <= line.X)
                {
                    return line.FirstOrDefault()?.CharIdx ?? totalCharCount;
                }

                DrawableCharacter lastChar = line.LastOrDefault();
                if (lastChar == null)
                {
                    return totalCharCount;
                }
                else if (lastChar.FirstChar == LINE_BREAK)
                {
                    return lastChar.CharIdx;
                }
                else
                {
                    return lastChar.CharIdx + (lastChar.IsSurrogate ? 2 : 1);
                }
            }

            // 3. Didn't press at a specific line. Return last index.
            return totalCharCount;
        }

        private static Rect GetRectRelativeToWindow(Control control)
        {
            Size size = new Size(control.ActualWidth, control.ActualHeight);
            Point location = control.TransformToVisual(null).TransformPoint(new Point());
            return new Rect(location, size);
        }

        private static Rect ScaleRect(Rect rect, double scale)
        {
            rect.X *= scale;
            rect.Y *= scale;
            rect.Width *= scale;
            rect.Height *= scale;
            return rect;
        }
    }
}
*/
