using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using System.Windows;
using Note.Rope;

namespace Editor.ViewModel
{
    public class LineViewModel : ObservableCollection<CharacterViewModel>
    {
        public const char LINE_BREAK = '\n';
        public const char CARRIAGE_RETURN = '\r';

        public virtual int StartCharIdx => this.FirstOrDefault()?.CharIdx ?? -1;

        public virtual int EndCharIdx => this.LastOrDefault()?.CharIdx ?? -1;

        public virtual double X => this.Min(c => c.X);

        public virtual double Y => this.Min(c => c.Y);

        public virtual double Width => this.Max(c => c.X + c.Width) - this.X;

        public virtual double Height => this.Max(c => c.Y + c.Height) - this.Y;

        public Thickness Position => new Thickness(0, this.Y, 0, 0);

        public static IEnumerable<LineViewModel> CalculateLines(
            Rope rope,
            double viewWidth,
            double viewHeight,
            int viewStartCharIdx,
            double charDrawWidth,
            double charDrawHeight,
            Settings settings,
            int? cursorIdxToBringIntoView
        )
        {
            rope.ValidateTree();

            if (settings.WordWrap)
            {
                List<LineViewModel> lines = CalculateLinesWithWordWrapTopToBottom(
                    rope,
                    viewWidth,
                    viewHeight,
                    viewStartCharIdx,
                    charDrawWidth,
                    charDrawHeight
                );

                int maxLineCountInView = (int)Math.Floor(viewHeight / charDrawHeight);
                int viewEndCharIdx = (lines.LastOrDefault()?.EndCharIdx) ?? int.MaxValue;

                if (cursorIdxToBringIntoView != null && cursorIdxToBringIntoView < viewStartCharIdx)
                {
                    lines = CalculateLinesWithWordWrapTopToBottom(
                        rope,
                        viewWidth,
                        viewHeight,
                        cursorIdxToBringIntoView.Value,
                        charDrawWidth,
                        charDrawHeight
                    );
                }
                else if (cursorIdxToBringIntoView != null &&
                         lines.Count == maxLineCountInView &&
                         ((cursorIdxToBringIntoView > viewEndCharIdx + 1) ||
                         (cursorIdxToBringIntoView == viewEndCharIdx + 1 &&
                         rope.IterateChars(viewEndCharIdx).First().Item1 == LINE_BREAK))) 
                {
                    lines = CalculateLinesWithWordWrapBottomToTop(
                        rope,
                        viewWidth,
                        viewHeight,
                        cursorIdxToBringIntoView.Value,
                        charDrawWidth,
                        charDrawHeight
                    );
                }

                if (!lines.Any())
                {
                    int charIdx = rope.GetTotalCharCount();
                    lines.Add(new EmptyLineViewModel(charIdx, 0, 0, charDrawHeight));
                }

                return lines;
            }
            else
            {
                throw new Exception("TODO: CalculateLines not word wrap");
            }
        }

        private static List<LineViewModel> CalculateLinesWithWordWrapTopToBottom(
            Rope rope,
            double viewWidth,
            double viewHeight,
            int viewStartCharIdx,
            double charDrawWidth,
            double charDrawHeight
        )
        {
            List<LineViewModel> lines = [];
            double curLocationY = 0.0;

            int totalCharCount = rope.GetTotalCharCount();
            int lineCountInView = (int)Math.Floor(viewHeight / charDrawHeight);
            int currentCharIdx = viewStartCharIdx;

            while (lines.Count < lineCountInView &&
                   currentCharIdx != -1)
            {
                IEnumerable<LineViewModel> lineViewModels = CalculateVirtualLinesWithWordWrapMiddleToBottom(
                    rope,
                    viewWidth,
                    currentCharIdx,
                    curLocationY,
                    charDrawWidth,
                    charDrawHeight
                );

                foreach (LineViewModel lineViewModel in lineViewModels)
                {
                    if (lines.Count < lineCountInView)
                    {
                        lines.Add(lineViewModel);
                        curLocationY += lineViewModels.Max(x => x.Height);
                    }
                }

                int nextCharIdx = (lineViewModels.LastOrDefault()?.EndCharIdx + 1) ?? -1;
                if (nextCharIdx == totalCharCount &&
                    lines.Count < lineCountInView &&
                    rope.IterateChars(totalCharCount - 1).First().Item1 == LINE_BREAK)
                {
                    lines.Add(new EmptyLineViewModel(totalCharCount, 0.0, curLocationY, charDrawHeight));
                    currentCharIdx = -1;
                }
                else if (nextCharIdx != -1 && nextCharIdx < totalCharCount)
                {
                    currentCharIdx = nextCharIdx;
                }
                else
                {
                    currentCharIdx = -1;
                }
            }

            return lines;
        }

        private static List<LineViewModel> CalculateLinesWithWordWrapBottomToTop(
            Rope rope,
            double viewWidth,
            double viewHeight,
            int viewEndCharIdx,
            double charDrawWidth,
            double charDrawHeight
        )
        {
            List<LineViewModel> lines = [];

            int totalCharCount = rope.GetTotalCharCount();
            int lineCountInView = (int)Math.Floor(viewHeight / charDrawHeight);
            int currentCharIdx = viewEndCharIdx;

            // Since we should only call this function if the selection is below the current view,
            // we know that we will find lines so that the whole view is covered in this function.
            double curLocationY = Math.Round(charDrawHeight * (lineCountInView - 1));

            while (lines.Count < lineCountInView && currentCharIdx >= 0)
            {
                IEnumerable<LineViewModel> lineViewModels = CalculateVirtualLinesWithWordWrapTopToMiddle(
                    rope,
                    viewWidth,
                    currentCharIdx,
                    charDrawWidth,
                    charDrawHeight
                );

                foreach (LineViewModel lineViewModel in lineViewModels.Reverse())
                {
                    if (lines.Count < lineCountInView)
                    {
                        lines.Add(lineViewModel);

                        foreach (CharacterViewModel ch in lineViewModel)
                        {
                            ch.Y = Math.Round(curLocationY); ;
                        }

                        if (lineViewModel is EmptyLineViewModel empty)
                        {
                            empty.SetY(Math.Round(curLocationY));
                        }

                        curLocationY -= charDrawHeight;
                    }
                }

                if (!lineViewModels.Any())
                {
                    curLocationY -= charDrawHeight;
                }

                int? nextCharIdx = (lineViewModels.FirstOrDefault()?.StartCharIdx - 1);
                if (nextCharIdx.HasValue)
                {
                    currentCharIdx = nextCharIdx.Value;
                }
                else if (currentCharIdx == 0)
                {
                    currentCharIdx = -1;
                }
                else
                {
                    int lineIdx = rope.GetLineIndexForCharAtIndex(currentCharIdx - 1);
                    currentCharIdx = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
                }
            }

            lines.Reverse();
            return lines;
        }

        /*
        /// <summary>
        /// Calculates the virtual lines (word wrapped lines) for the line
        /// with index `lineIdx`.
        /// </summary>
        /// <param name="rope"></param>
        /// <param name="viewWidth"></param>
        /// <param name="viewHeight"></param>
        /// <param name="viewEndCharIdx"></param>
        /// <param name="charDrawWidth"></param>
        /// <param name="charDrawHeight"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static IEnumerable<LineViewModel> CalculateVirtualLinesWordWrap(
            Rope rope,
            double viewWidth,
            int lineIdx,
            double charDrawWidth,
            double charDrawHeight,
            Settings settings
        )
        {
            rope.ValidateTree();

            int firstCharIdxAtLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
            int firstCharIdxAtNextLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx + 1);
            if (firstCharIdxAtNextLine == -1)
            {
                firstCharIdxAtNextLine = rope.GetTotalCharCount();
            }
            int currentCharIdx = firstCharIdxAtLine;

            double startLocationX = 0.0;
            double curLocationY = 0.0;

            List<LineViewModel> lines = new List<LineViewModel>();
            LineViewModel currentLineViewModel;

            while (currentCharIdx < firstCharIdxAtNextLine)
            {
                (currentLineViewModel, currentCharIdx) = CalculateLineViewModel(
                    rope,
                    currentCharIdx,
                    startLocationX,
                    curLocationY,
                    viewWidth,
                    charDrawWidth,
                    charDrawHeight,
                    settings
                );

                lines.Add(currentLineViewModel);
                curLocationY += currentLineViewModel.Max(x => x.Height);
            }

            return lines;
        }
        */

        private static List<LineViewModel> CalculateVirtualLinesWithWordWrapTopToMiddle(
            Rope rope,
            double viewWidth,
            int charEndIndex,
            double charDrawWidth,
            double charDrawHeight
        )
        {
            rope.ValidateTree();

            int lineIdx = rope.GetLineIndexForCharAtIndex(charEndIndex);
            int firstCharIdxAtLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
            int currentCharIdx = firstCharIdxAtLine;

            double startLocationX = 0.0;
            double curLocationY = -1.0;

            List<LineViewModel> lines = [];
            LineViewModel currentLineViewModel;

            while (currentCharIdx <= charEndIndex)
            {
                int prevCharIdx = currentCharIdx;
                (currentLineViewModel, currentCharIdx) = CalculateLineViewModel(
                    rope,
                    currentCharIdx,
                    startLocationX,
                    curLocationY,
                    viewWidth,
                    charDrawWidth,
                    charDrawHeight
                );

                // NOTE: The y-position is set to -1. This is because we don't know the position
                //       at this point. The caller of this function will set it later.
                lines.Add(currentLineViewModel);

                if (currentCharIdx == prevCharIdx)
                {
                    break;
                }
            }

            return lines;
        }

        public static int CharIdxAfterScrollDownwardsWithWordWrap(
            Rope rope,
            double viewWidth,
            int charIdx,
            double charDrawWidth,
            double charDrawHeight,
            int scrollDelta
        )
        {
            scrollDelta = int.Abs(scrollDelta);

            int newCharIdx = -1;
            int lastCharIdx = rope.GetTotalCharCount();
            lastCharIdx = lastCharIdx > 0 ? lastCharIdx - 1 : 0;
            bool lastCharIsLineBreak = rope.IterateChars(lastCharIdx).First().Item1 == LineViewModel.LINE_BREAK;
            lastCharIdx = lastCharIsLineBreak ? lastCharIdx + 1 : lastCharIdx;

            while (scrollDelta > 0 && charIdx <= lastCharIdx)
            {
                List<LineViewModel> lines = CalculateVirtualLinesWithWordWrapMiddleToBottom(
                    rope,
                    viewWidth,
                    charIdx,
                    0.0,
                    charDrawWidth,
                    charDrawHeight
                );

                if (lines.Count >= scrollDelta)
                {
                    newCharIdx = lines[scrollDelta - 1].StartCharIdx;
                    break;
                }
                else if (lines.Count == 0)
                {
                    break;
                }
                else
                {
                    scrollDelta -= lines.Count;
                    charIdx = lines.Last().EndCharIdx + 1;
                }
            }

            return newCharIdx != -1 ? newCharIdx : lastCharIdx;
        }

        public static int CharIdxAfterScrollUpwardsWithWordWrap(
            Rope rope,
            double viewWidth,
            int charIdx,
            double charDrawWidth,
            double charDrawHeight,
            int scrollDelta
        )
        {
            scrollDelta = int.Abs(scrollDelta);

            int newCharIdx = -1;

            while (scrollDelta > 0 && charIdx >= 0)
            {
                List<LineViewModel> lines = CalculateVirtualLinesWithWordWrapTopToMiddle(
                    rope,
                    viewWidth,
                    charIdx,
                    charDrawWidth,
                    charDrawHeight
                );

                if (lines.Count >= scrollDelta)
                {
                    newCharIdx = lines[lines.Count - scrollDelta].StartCharIdx;
                    break;
                }
                else if (lines.Count == 0)
                {
                    break;
                }
                else
                {
                    scrollDelta -= lines.Count;
                }

                int? nextCharIdx = (lines.FirstOrDefault()?.StartCharIdx - 1);
                if (nextCharIdx.HasValue)
                {
                    charIdx = nextCharIdx.Value;
                }
                else if (charIdx == 0)
                {
                    charIdx = -1;
                }
                else
                {
                    int lineIdx = rope.GetLineIndexForCharAtIndex(charIdx - 1);
                    charIdx = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
                }
            }

            return newCharIdx != -1 ? newCharIdx : 0;
        }

        private static List<LineViewModel> CalculateVirtualLinesWithWordWrapMiddleToBottom(
            Rope rope,
            double viewWidth,
            int charStartIndex,
            double locationY,
            double charDrawWidth,
            double charDrawHeight
        )
        {
            rope.ValidateTree();

            int lineIdx = rope.GetLineIndexForCharAtIndex(charStartIndex);
            int firstCharIdxAtLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
            int firstCharIdxAtNextLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx + 1);
            if (firstCharIdxAtNextLine == -1)
            {
                firstCharIdxAtNextLine = rope.GetTotalCharCount();
            }
            int currentCharIdx = firstCharIdxAtLine;

            double startLocationX = 0.0;
            double curLocationY = locationY;

            List<LineViewModel> lines = [];
            LineViewModel currentLineViewModel;

            while (currentCharIdx < firstCharIdxAtNextLine)
            {
                int prevCharIdx = currentCharIdx;
                (currentLineViewModel, currentCharIdx) = CalculateLineViewModel(
                    rope,
                    currentCharIdx,
                    startLocationX,
                    curLocationY,
                    viewWidth,
                    charDrawWidth,
                    charDrawHeight
                );

                if (currentCharIdx > charStartIndex)
                {
                    lines.Add(currentLineViewModel);
                    curLocationY += charDrawHeight;
                }
                else
                {
                    // Skip until we find the "virtual" line that contains the
                    // start char index
                }

                if (currentCharIdx == prevCharIdx)
                {
                    break;
                }
            }

            return lines;
        }

        /// <summary>
        /// Staring from the character found at `startCharIdx`, calculates what
        /// text should be written on a row.
        /// </summary>
        /// <param name="startCharIdx">Represents the first character of this new line</param>
        /// <param name="startLocationX">The X location we should start "drawing" the text (padding to the left)</param>
        /// <param name="locationY">The Y location where we should "draw" the text</param>
        /// <param name="viewMaxWidth">The maximum width (x location) of the line</param>
        /// <returns>A list of "drawn" text and the index of the character starting after the "drawn" text</returns>
        /// <exception cref="Exception"></exception>
        private static (LineViewModel, int) CalculateLineViewModel(
            Rope rope,
            int startCharIdx,
            double startLocationX,
            double locationY,
            double viewMaxWidth,
            double charDrawWidth,
            double charDrawHeight
        )
        {
            LineViewModel lineViewModel = new LineViewModel();

            double currentLocationX = startLocationX;
            int currentCharIdx = startCharIdx;

            foreach (var (cCur, cNext) in rope.IterateCharPairs(currentCharIdx))
            {
                bool isSurrogatePair = char.IsHighSurrogate(cCur);

                if (isSurrogatePair && cNext == null)
                {
                    // TODO:
                    throw new Exception("cCur high surrogate, cNext null");
                }
                else if (char.IsLowSurrogate(cCur))
                {
                    continue;
                }

                string text;
                if (isSurrogatePair)
                {
                    text = new string(new char[] { cCur, cNext.Value });
                }
                else
                {
                    text = cCur.ToString();
                }

                lineViewModel.Add(new CharacterViewModel(
                    currentLocationX,
                    locationY,
                    charDrawWidth,
                    charDrawHeight,
                    currentCharIdx,
                    text
                ));

                currentCharIdx += isSurrogatePair ? 2 : 1;
                currentLocationX += charDrawWidth;

                if (currentLocationX + charDrawWidth > viewMaxWidth || cCur == LINE_BREAK)
                {
                    break;
                }
            }

            if (!lineViewModel.Any())
            {
                lineViewModel = new EmptyLineViewModel(currentCharIdx, startLocationX, locationY, charDrawHeight);
            }

            return (lineViewModel, currentCharIdx);
        }

        private static (float, float) TextDrawSize(string text, Typeface typeFace, double fontSize, Visual visual)
        {
            if (text == null)
            {
                return (0, 0);
            }

            var formattedText = new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeFace,
                fontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(visual).PixelsPerDip
            )
            {
                Trimming = TextTrimming.None,
            };

            // If we are unable to get a width, e.g. if this is a special character (newline, ...),
            // get an arbitrary "default" width (whitespace for now).
            if (formattedText.WidthIncludingTrailingWhitespace == 0.0 && !string.Equals(text, " "))
            {
                return TextDrawSize(" ", typeFace, fontSize, visual);
            }

            return ((float)formattedText.WidthIncludingTrailingWhitespace, (float)formattedText.Height);
        }
    }
}
