using System.Windows;
using System.Windows.Media;
using Text;

namespace Editor.ViewModel
{
    public interface IGlyphRunCharacter
    {
        double X { get; }
        double Y { get; }
        double Width { get; }
        double Height { get; }
        int UnicodeCodePoint { get; }

        public bool Contains(double x, double y)
        {
            return this.ContainsX(x) && this.ContainsY(y);
        }

        public bool ContainsX(double x)
        {
            return x >= this.X && x <= this.X + this.Width;
        }

        public bool ContainsY(double y)
        {
            return y >= this.Y && y <= this.Y + this.Height;
        }
    }

    public class GlyphRunCharacter : IGlyphRunCharacter
    {
        public GlyphRunCharacter(double x, double y, double width, double height, int unicodeCodePoint)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.UnicodeCodePoint = unicodeCodePoint;
        }

        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        public int UnicodeCodePoint { get; }
    }

    public class GlyphRunViewModel
    {
        public GlyphRunViewModel(GlyphRun? glyphRun, Brush brush, IEnumerable<IGlyphRunCharacter> chars, double? width = null)
        {
            this.GlyphRun = glyphRun;
            this.Chars = chars;
            this.Brush = brush;
            this.Width = width;
        }

        public GlyphRun? GlyphRun { get; }

        public Brush Brush { get; }

        public IEnumerable<IGlyphRunCharacter> Chars { get; }

        public double? Width { get; }

        public static IEnumerable<IGlyphRunCharacter> CalculatePrintableText(IEnumerable<LineViewModel> lines)
        {
            return lines.SelectMany(l => l.Where(ch => !ch.IsCustom));
        }

        // https://stackoverflow.com/a/33446396
        public static IEnumerable<IGlyphRunCharacter> CalculateLineNumbers(IEnumerable<LineViewModel> lines, Rope rope, double charWidth, double charHeight)
        {
            List<IGlyphRunCharacter> lineNumbers = new List<IGlyphRunCharacter>();

            CharacterViewModel? lastChar = lines.LastOrDefault(x => x is not EmptyLineViewModel)?.LastOrDefault();
            bool lastLineIsEmpty = lines.LastOrDefault() is EmptyLineViewModel;

            int lastLineNumber = rope.GetTotalLineBreaks() + 1;
            int maxLineNumberCharCount = lastLineNumber.ToString().Length;

            int prevLineNumber = -1;

            foreach (LineViewModel line in lines)
            {
                int lineNumber = rope.GetLineIndexForCharAtIndex(line.StartCharIdx) + 1;
                if (lineNumber != prevLineNumber)
                {
                    string lineNumberString = lineNumber.ToString();
                    int lineNumberCharCount = lineNumberString.Length;
                    int rightPaddingCount = maxLineNumberCharCount - lineNumberCharCount;
                    lineNumbers.AddRange(StringToGlyphRunChars(
                        lineNumberString,
                        rightPaddingCount * charWidth,
                        line.Y,
                        charWidth,
                        charHeight
                    ));
                }

                prevLineNumber = lineNumber;
            }

            return lineNumbers;
        }

        // https://stackoverflow.com/a/33446396
        public static GlyphRun? CharsToGlyphRun(IEnumerable<IGlyphRunCharacter> chars, Settings settings, float pixelsPerDip)
        {
            if (!chars.Any())
            {
                return null;
            }

            var glyphIndices = new List<ushort>();
            var advanceWidths = new List<double>();
            var glyphOffsets = new List<Point>();

            GlyphTypeface glyphTypeface = settings.GlyphTypeface;
            double renderingEmSize = settings.FontSize;

            foreach (IGlyphRunCharacter ch in chars)
            {
                glyphIndices.Add(glyphTypeface.CharacterToGlyphMap[ch.UnicodeCodePoint]);
                advanceWidths.Add(0);
                glyphOffsets.Add(new Point(ch.X, -ch.Y));
            }

            double ascent = double.Round(glyphTypeface.Baseline * renderingEmSize);
            var glyphRun = new GlyphRun(
                glyphTypeface,
                0,
                false,
                renderingEmSize,
                pixelsPerDip,
                glyphIndices,
                new Point(0, ascent),
                advanceWidths,
                glyphOffsets,
                null,
                null,
                null,
                null,
                null
            );

            return glyphRun;
        }

        private static IEnumerable<IGlyphRunCharacter> StringToGlyphRunChars(string text, double x, double y, double width, double height)
        {
            List<IGlyphRunCharacter> glyphChars = new List<IGlyphRunCharacter>();

            foreach (char c in text)
            {
                glyphChars.Add(new GlyphRunCharacter(x, y, width, height, c));
                x += width;
            }

            return glyphChars;
        }
    }
}
