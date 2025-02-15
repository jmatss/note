using Editor.Range;
using Editor.ViewModel;
using System.Windows;
using System.Windows.Input;
using Text;

namespace Editor
{
    public class HandleInput
    {
        public struct Modifiers
        {
            public bool Ctrl { get; }
            public bool Shift { get; }
            public bool Alt { get; }

            public Modifiers(KeyboardDevice keyboard)
            {
                this.Ctrl = keyboard.IsKeyDown(Key.LeftCtrl) || keyboard.IsKeyDown(Key.RightCtrl);
                this.Shift = keyboard.IsKeyDown(Key.LeftShift) || keyboard.IsKeyDown(Key.RightShift);
                this.Alt = keyboard.IsKeyDown(Key.LeftAlt) || keyboard.IsKeyDown(Key.RightAlt);
            }

            public override string ToString()
            {
                return "{Ctrl: " + this.Ctrl + ", Shift: " + this.Shift + ", Alt: " + this.Alt + "}";
            }
        }

        public static int HandleTextLeftMouseClick(Rope rope, IEnumerable<LineViewModel> lines, Point position)
        {
            return GetInsertionCharIndexFromPosition(rope, lines, position.X, position.Y);
        }

        public static int HandleLineNumbersLeftMouseClick(Rope rope, IEnumerable<LineViewModel> lines, Point position)
        {
            return GetLineIndexFromPosition(rope, lines, position.Y);
        }

        public static SelectionRange? MoveSelection(
            Rope rope,
            Key key,
            Modifiers modifiers,
            SelectionRange selection,
            double viewWidth,
            double charDrawWidth,
            double charDrawHeight,
            int previousSelectionColumnIndex
        )
        {
            int newIdx;
            SelectionRange? newSelection = new SelectionRange(selection);

            switch (key)
            {
                case Key.A when modifiers.Ctrl:
                    {
                        newSelection.Start = 0;
                        newSelection.End = rope.GetTotalCharCount();
                        newSelection.InsertionPosition = InsertionPosition.Start;
                    }
                    break;

                case Key.Left:
                    {
                        if (newSelection.InsertionPositionIndex == 0)
                        {
                            return null;
                        }

                        if (modifiers.Ctrl)
                        {
                            bool skipEndWhitespaces = false;
                            newIdx = rope.GetCurrentWordStartIndex(newSelection.InsertionPositionIndex, skipEndWhitespaces);
                        }
                        else
                        {
                            newIdx = newSelection.InsertionPositionIndex - 1;
                        }

                        if (newIdx > 0 && rope.GetChar(newIdx - 1) == LineViewModel.CARRIAGE_RETURN)
                        {
                            newIdx--;
                        }

                        if (modifiers.Shift)
                        {
                            newSelection.InsertionPositionIndex = newIdx;
                        }
                        else if (selection.Length > 0 && !modifiers.Ctrl)
                        {
                            newSelection.Start = selection.Start;
                            newSelection.End = selection.Start;
                        }
                        else
                        {
                            newSelection.Start = newIdx;
                            newSelection.End = newIdx;
                        }
                    }
                    break;

                case Key.Right:
                    {
                        int lastIdx = rope.GetTotalCharCount();
                        if (newSelection.InsertionPositionIndex == lastIdx)
                        {
                            return null;
                        }

                        if (modifiers.Ctrl)
                        {
                            bool skipEndWhitespaces = true;
                            newIdx = rope.GetNextWordStartIndex(newSelection.InsertionPositionIndex, skipEndWhitespaces);
                        }
                        else
                        {
                            newIdx = newSelection.InsertionPositionIndex + 1;
                        }

                        if (newIdx > 0 && newIdx < lastIdx && rope.GetChar(newIdx - 1) == LineViewModel.CARRIAGE_RETURN)
                        {
                            newIdx++;
                        }

                        if (modifiers.Shift)
                        {
                            newSelection.InsertionPositionIndex = newIdx;
                        }
                        else if (selection.Length > 0 && !modifiers.Ctrl)
                        {
                            newSelection.Start = selection.End;
                            newSelection.End = selection.End;
                        }
                        else
                        {
                            newSelection.Start = newIdx;
                            newSelection.End = newIdx;
                        }
                    }
                    break;

                case Key.Up:
                    {
                        bool isDownwards = false;
                        int oldSelectionCharIdx = newSelection.InsertionPositionIndex;

                        SelectionRange? movedSelection = MoveSelectionVerticalWithWordWrap(
                            rope,
                            newSelection,
                            modifiers,
                            isDownwards,
                            oldSelectionCharIdx,
                            viewWidth,
                            charDrawWidth,
                            charDrawWidth,
                            previousSelectionColumnIndex
                        );

                        if (movedSelection != null)
                        {
                            newSelection = movedSelection;
                        }
                    }
                    break;

                case Key.Down:
                    {
                        bool isDownwards = true;
                        int oldSelectionCharIdx = newSelection.InsertionPositionIndex;

                        SelectionRange? movedSelection = MoveSelectionVerticalWithWordWrap(
                            rope,
                            newSelection,
                            modifiers,
                            isDownwards,
                            oldSelectionCharIdx,
                            viewWidth,
                            charDrawWidth,
                            charDrawWidth,
                            previousSelectionColumnIndex
                        );

                        if (movedSelection != null)
                        {
                            newSelection = movedSelection;
                        }
                    }
                    break;

                case Key.PageUp:
                    {
                        // TODO:
                        /*
                        int lineIdx = this.rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        if (lineIdx == 0)
                        {
                            return;
                        }

                        int targetLineIdx = lineIdx - this.viewActualDrawnLineCount;
                        if (targetLineIdx < 0)
                        {
                            targetLineIdx = 0;
                        }

                        newSelection = MoveSelectionVertical(lineIdx, targetLineIdx);
                        */
                    }
                    break;

                case Key.PageDown:
                    {
                        // TODO:
                        /*
                        int lineIdx = this.rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        if (lineIdx == 0)
                        {
                            return;
                        }

                        int targetLineIdx = lineIdx + this.viewActualDrawnLineCount;
                        int totalLineBreaks = this.rope.GetTotalLineBreaks();
                        if (targetLineIdx >= totalLineBreaks)
                        {
                            targetLineIdx = totalLineBreaks - 1;
                        }

                        newSelection = MoveSelectionVertical(lineIdx, targetLineIdx);
                        */
                    }
                    break;

                case Key.End:
                    {
                        int lastCharIdx = rope.GetTotalCharCount() - 1;
                        if (newSelection.InsertionPositionIndex == lastCharIdx)
                        {
                            return null;
                        }

                        int lineIdx = rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        int firstCharIdxAtLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
                        int charCountAtLine = rope.GetCharCountAtLineWithIndex(lineIdx);
                        int lastCharIdxAtLine = firstCharIdxAtLine + charCountAtLine - 1;

                        if (lastCharIdxAtLine > 0 && rope.GetChar(lastCharIdxAtLine - 1) == LineViewModel.CARRIAGE_RETURN)
                        {
                            lastCharIdxAtLine--;
                        }

                        if (modifiers.Shift)
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

                case Key.Home:
                    {
                        if (newSelection.InsertionPositionIndex == 0)
                        {
                            return null;
                        }

                        int lineIdx = rope.GetLineIndexForCharAtIndex(newSelection.InsertionPositionIndex);
                        int firstCharIdxAtLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
                        int firstWordIdxStartAtLine = rope.SkipWhitespaces(firstCharIdxAtLine);

                        if (modifiers.Shift)
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

            return newSelection;
        }

        private static SelectionRange? MoveSelectionVerticalWithWordWrap(
            Rope rope,
            SelectionRange newSelection,
            Modifiers modifiers,
            bool isDownwards,
            int charIdx,
            double viewWidth,
            double charDrawWidth,
            double charDrawHeight,
            int previousSelectionColumnIndex
        )
        {
            int lineIdx = rope.GetLineIndexForCharAtIndex(charIdx);
            double y = 0; // We don't care about the Y-position used during these calculations, so set arbitrarily to 0.

            int newCharIdx;
            LineViewModel? nextLine;

            if (isDownwards)
            {
                int lineCount = rope.GetTotalLineBreaks() + 1;
                var curLines = LineViewModel.CalculateVirtualLinesWithWordWrapMiddleToBottom(rope, viewWidth, charIdx, y, charDrawWidth, charDrawWidth);

                if (curLines.Count >= 2)
                {
                    // We are at a line that is wordwrapped and we know that there are a "virtual" line below
                    // in the view that is a part of the same actual line. Scroll to it.
                    nextLine = curLines[1];
                }
                else if (lineIdx + 1 >= lineCount)
                {
                    // We are at the last line, unable to scroll down further.
                    nextLine = null;
                }
                else
                {
                    // No wordwrapping below us on this line. Scroll to the next real line.
                    int firstCharIdxTargetLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx + 1);
                    var nextLines = LineViewModel.CalculateVirtualLinesWithWordWrapMiddleToBottom(rope, viewWidth, firstCharIdxTargetLine, y, charDrawWidth, charDrawWidth);
                    if (nextLines.Count > 0)
                    {
                        nextLine = nextLines[0];
                    }
                    else
                    {
                        // This is the last line and it doesn't contain any characters.
                        nextLine = new EmptyLineViewModel(firstCharIdxTargetLine, 0, 0, 0);
                    }
                }
            }
            else // isUpwards
            {
                var curLines = LineViewModel.CalculateVirtualLinesWithWordWrapTopToMiddle(rope, viewWidth, charIdx, charDrawWidth, charDrawWidth);

                if (curLines.Count >= 2)
                {
                    // We are at a line that is wordwrapped and we know that there are a "virtual" line above
                    // in the view that is a part of the same actual line. Scroll to it.
                    nextLine = curLines[curLines.Count - 2];
                }
                else if (lineIdx == 0)
                {
                    // We are at the first line, unable to scroll up further.
                    nextLine = null;
                }
                else
                {
                    // No wordwrapping above us on this line. Scroll to the next real line (which might potentially be wordwrapped).
                    int firstCharIdxTargetLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx - 1);
                    var nextLines = LineViewModel.CalculateVirtualLinesWithWordWrapMiddleToBottom(rope, viewWidth, firstCharIdxTargetLine, y, charDrawWidth, charDrawWidth);
                    nextLine = nextLines[nextLines.Count - 1];
                }
            }

            if (nextLine == null)
            {
                return null;
            }

            var curVirtualLine = LineViewModel.CalculateVirtualLinesWithWordWrapTopToMiddle(rope, viewWidth, charIdx, charDrawWidth, charDrawWidth)
                .LastOrDefault();
            int charCountAtNextLineWithoutLineBreaks = nextLine
                .Count(x => x.FirstChar != LineViewModel.CARRIAGE_RETURN && x.FirstChar != LineViewModel.LINE_BREAK);

            int charLengthToInsertionAtCurrentLine;
            if (previousSelectionColumnIndex != -1)
            {
                charLengthToInsertionAtCurrentLine = previousSelectionColumnIndex;
            }
            else
            {
                charLengthToInsertionAtCurrentLine = curVirtualLine?
                    .Select((x, i) => (x, i))
                    .FirstOrDefault(c => c.x.CharIdx == charIdx)
                    .i ?? 0;
            }

            int insertionIdxAtLine = Math.Min(charLengthToInsertionAtCurrentLine, charCountAtNextLineWithoutLineBreaks);
            if (insertionIdxAtLine >= nextLine.Count)
            {
                if (nextLine.LastOrDefault() is CharacterViewModel c &&
                    c.FirstChar != LineViewModel.CARRIAGE_RETURN &&
                    c.FirstChar != LineViewModel.LINE_BREAK)
                {
                    newCharIdx = nextLine.EndCharIdx + 1;
                }
                else
                {
                    newCharIdx = nextLine.EndCharIdx;
                }
            }
            else
            {
                newCharIdx = nextLine[insertionIdxAtLine].CharIdx;
            }

            if (modifiers.Ctrl)
            {
                // TODO: Should scroll up/down one row if this is a up/down.
                //       How to handle the other cases?
                return null;
            }
            else
            {
                if (modifiers.Shift)
                {
                    newSelection.InsertionPositionIndex = newCharIdx;
                }
                else
                {
                    newSelection.Start = newCharIdx;
                    newSelection.End = newCharIdx;
                }
            }

            return newSelection;
        }

        private static SelectionRange? MoveSelectionVertical(
            Rope rope,
            SelectionRange newSelection,
            Modifiers modifiers,
            int lineIdx,
            int targetLineIdx,
            bool lastCharOneTargetLineIsLineBreak
        )
        {
            int startCharIdxAtCurrentLine = rope.GetFirstCharIndexAtLineWithIndex(lineIdx);
            int startCharIdxAtTargetLine;
            if (targetLineIdx < 0)
            {
                startCharIdxAtTargetLine = 0;
            }
            else if (targetLineIdx > rope.GetTotalLineBreaks())
            {
                startCharIdxAtTargetLine = rope.GetTotalCharCount();
            }
            else
            {
                startCharIdxAtTargetLine = rope.GetFirstCharIndexAtLineWithIndex(targetLineIdx);
            }

            // -1 to ignore the line feed character itself.
            int totalCharCountAtTargetLine = rope.GetCharCountAtLineWithIndex(targetLineIdx) + (lastCharOneTargetLineIsLineBreak ? -1 : 0);
            int charLengthToInsertionAtCurrentLine = newSelection.InsertionPositionIndex - startCharIdxAtCurrentLine;
            int charLengthToNewSelectionAtTargetLine = Math.Min(totalCharCountAtTargetLine, charLengthToInsertionAtCurrentLine);

            if (modifiers.Ctrl)
            {
                // TODO: Should scroll up/down one row if this is a up/down.
                //       How to handle the other cases?
                return null;
            }
            else
            {
                int newIdx = startCharIdxAtTargetLine + charLengthToNewSelectionAtTargetLine;
                if (newIdx > 0 && rope.GetChar(newIdx - 1) == LineViewModel.CARRIAGE_RETURN)
                {
                    newIdx--;
                }

                if (modifiers.Shift)
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

        private static int GetInsertionCharIndexFromPosition(Rope rope, IEnumerable<LineViewModel> lines, double x, double y)
        {
            CharacterViewModel? ch = lines
                .SelectMany(l => l.Where(c => (c as IGlyphRunCharacter).Contains(x, y)))
                .FirstOrDefault();

            // 1. The mouse cursor is directly at a character.
            if (ch != null)
            {
                return HitCharacterToCharIdx(rope, x, ch);
            }

            int totalCharCount = rope.GetTotalCharCount();

            // 2. The cursors isn't directly at a character: See if we are left/right of a specific line.
            LineViewModel? line = lines.FirstOrDefault(l => y >= l.Y && y < l.Y + l.Height);
            if (line != null)
            {
                if (x <= line.X)
                {
                    return line.FirstOrDefault()?.CharIdx ?? totalCharCount;
                }

                CharacterViewModel? lastChar = line.LastOrDefault();
                if (lastChar == null)
                {
                    return totalCharCount;
                }

                int charIdx = lastChar.CharIdx;

                if (charIdx > 0 && rope.GetChar(charIdx - 1) == LineViewModel.CARRIAGE_RETURN)
                {
                    charIdx--;
                }
                else if (lastChar.FirstChar != LineViewModel.LINE_BREAK)
                {
                    charIdx++;
                }

                return charIdx;
            }

            // 3. The cursor is located above or below the text. Se if we are above/below a specific column.
            LineViewModel? firstLine = lines.FirstOrDefault();
            LineViewModel? lastLine = lines.LastOrDefault();
            if (firstLine != null && firstLine.Count > 0 && y < 0)
            {
                if (firstLine.StartCharIdx > 0)
                {
                    // TODO: Currently takes the first character of line above.
                    //       Implement so that it consideres the x-position of the click
                    int firstLineIdx = rope.GetLineIndexForCharAtIndex(firstLine.StartCharIdx);
                    return rope.GetFirstCharIndexAtLineWithIndex(firstLineIdx > 0 ? firstLineIdx - 1 : firstLineIdx);
                }
                else
                {
                    return HitLineToCharIdx(rope, x, firstLine);
                }
            }
            else if (lastLine != null && y > lastLine.Y + lastLine.Height)
            {
                if (lastLine.EndCharIdx + 1 < totalCharCount)
                {
                    // TODO: Currently takes the first character of line below.
                    //       Implement so that it considers the x-position of the click
                    int lastLineIdx = rope.GetLineIndexForCharAtIndex(lastLine.StartCharIdx);
                    int totalLineCount = rope.GetTotalLineBreaks() + 1;
                    return rope.GetFirstCharIndexAtLineWithIndex(lastLineIdx + 1 < totalLineCount ? lastLineIdx + 1 : lastLineIdx);
                }
                else
                {
                    return HitLineToCharIdx(rope, x, lastLine);
                }
            }
            else
            {
                // No lines exists. Select 0 as default
                return 0;
            }
        }

        private static int HitCharacterToCharIdx(Rope rope, double x, CharacterViewModel ch)
        {
            if (x > ch.X + ch.Width / 2.0 &&
                ch.FirstChar != LineViewModel.CARRIAGE_RETURN &&
                ch.FirstChar != LineViewModel.LINE_BREAK)
            {
                // If we pressed at the "end" (right) of the character, we probably want to
                // insert the cursor behind the character, not the default infront of it.
                return ch.CharIdx + (ch.IsSurrogate ? 2 : 1);
            }
            else
            {
                int charIdx = ch.CharIdx;

                if (charIdx > 0 && rope.GetChar(charIdx - 1) == LineViewModel.CARRIAGE_RETURN)
                {
                    charIdx--;
                }

                return charIdx;
            }
        }

        private static int HitLineToCharIdx(Rope rope, double x, LineViewModel line)
        {
            CharacterViewModel? c = line
                    .Where(c => (c as IGlyphRunCharacter).ContainsX(x))
                    .FirstOrDefault();

            if (c == null && x > line.X + line.Width)
            {
                var lastChar = line.LastOrDefault();
                if (lastChar != null)
                {
                    c = lastChar;
                }
                else
                {
                    int charCount = rope.GetTotalCharCount();
                    int charIdx = line.EndCharIdx;

                    if (charIdx > 0 && charIdx < charCount && rope.GetChar(charIdx) == LineViewModel.LINE_BREAK)
                    {
                        charIdx--;
                    }

                    if (charIdx > 0 && charIdx < charCount && rope.GetChar(charIdx) == LineViewModel.CARRIAGE_RETURN)
                    {
                        charIdx--;
                    }

                    return charIdx;
                }
            }

            if (c != null)
            {
                return HitCharacterToCharIdx(rope, x, c);
            }
            else // Most likely scenario: (x < 0)
            {
                return line.StartCharIdx;
            }
        }

        private static int GetLineIndexFromPosition(Rope rope, IEnumerable<LineViewModel> lines, double y)
        {
            LineViewModel? line = lines.FirstOrDefault(l => y >= l.Y && y < l.Y + l.Height);
            if (line != null)
            {
                int charIdx = line.StartCharIdx;
                return rope.GetLineIndexForCharAtIndex(charIdx);
            }
            else
            {
                return -1;
            }
        }
    }
}
