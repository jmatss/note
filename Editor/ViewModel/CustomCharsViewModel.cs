using System.Windows;
using System.Windows.Media;

namespace Editor.ViewModel
{
    public class CustomCharViewModel : List<CharacterViewModel>
    {
        private static readonly char[] HEX_LOOKUP = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        /// <summary>
        /// Used to downscale the size of the text used to display control characters.
        /// Since we want to fit two characters into one slot, we will stack the two
        /// characters on top of each other and scale them down by 2.
        /// </summary>
        public const double DownScaleY = 0.6;

        /// <summary>
        /// See comment for `DownScaleY` why it is used. This is used to reverse
        /// the effect when transforming the position of the characters.
        /// </summary>
        public const double UpScaleY = 1 / DownScaleY;

        public const double DownScaleX = 0.8;

        public const double UpScaleX = 1 / DownScaleX;

        public CustomCharViewModel(Brush foregroundBrush, Brush backgroundBrush, IEnumerable<CharacterViewModel> chars, GlyphRun glyphRun)
        {
            this.ForegroundBrush = foregroundBrush;
            this.BackgroundBrush = backgroundBrush;
            this.AddRange(chars);
            this.GlyphRun = glyphRun;
        }

        public Brush ForegroundBrush { get; }

        public Brush BackgroundBrush { get; }

        public GlyphRun? GlyphRun { get; }

        public static GlyphRun? CharsToCustomGlyphRun(IEnumerable<CharacterViewModel> chars, Settings settings, float pixelsPerDip)
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

            foreach (CharacterViewModel ch in chars)
            {
                char c = ch.FirstChar;
                char c1 = HEX_LOOKUP[(c & 0xF0) >> 4];
                char c2 = HEX_LOOKUP[c & 0xF];

                // Used to re-center the characters in the X-axis when scaling is applied.
                double paddingX = ((1 - DownScaleX) / 2) * ch.Width;

                glyphIndices.Add(glyphTypeface.CharacterToGlyphMap[c1]);
                glyphIndices.Add(glyphTypeface.CharacterToGlyphMap[c2]);
                advanceWidths.Add(0);
                advanceWidths.Add(0);
                glyphOffsets.Add(new Point((paddingX + ch.X) * UpScaleX, -ch.Y * UpScaleY));
                glyphOffsets.Add(new Point((paddingX + ch.X) * UpScaleX, -(ch.Y + (ch.Height / 2) - 2) * UpScaleY));
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
    }

    public class CustomCharsViewModel
    {
        // https://stackoverflow.com/a/33446396
        public static IEnumerable<CharacterViewModel> CalculateWhitespaces(IEnumerable<LineViewModel> lines)
        {
            return lines.SelectMany(l => l.Where(ch => ch.IsCustom && ch.FirstChar == ' '))
                .Select(ch => new CharacterViewModel(ch.X, ch.Y, ch.Width, ch.Height, ch.CharIdx, "·"));
        }

        public static IEnumerable<CharacterViewModel> CalculateCustom(IEnumerable<LineViewModel> lines)
        {
            return lines.SelectMany(l => l.Where(ch => ch.IsCustom && ch.FirstChar != ' '));
        }
    }
}
