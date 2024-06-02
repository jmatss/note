using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Editor.ViewModel
{
    public class CustomCharWhitespaceViewModel : List<CharacterViewModel>
    {
        // TODO: Add custom char class for tab.
        public CustomCharWhitespaceViewModel(Brush brush, IEnumerable<CharacterViewModel> chars)
        {
            this.Brush = brush;
            this.AddRange(chars);
        }

        public Brush Brush { get; }
    }

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
            return lines.SelectMany(l => l.Where(ch => ch.IsCustom && ch.FirstChar == ' '));
        }

        public static IEnumerable<CharacterViewModel> CalculateCustom(IEnumerable<LineViewModel> lines)
        {
            return lines.SelectMany(l => l.Where(ch => ch.IsCustom && ch.FirstChar != ' '));
        }

        /*
        private void DrawCustomChar(Settings settings)
        {
            this.Elements.Clear();

            char c = this.FirstChar;

            switch (c)
            {
                case ' ':
                    double xCenter = this.X + this.Width / 2;
                    double yCenter = 0.0 + this.Height / 2;
                    double radius = this.Width / 3;

                    Grid customWhitespaceGrid = new Grid()
                    {
                        Width = this.Width,
                        Height = this.Height,
                    };

                    Ellipse circle = new Ellipse()
                    {
                        Width = radius,
                        Height = radius,
                        Fill = settings.TextColorCustomChar,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                    };

                    customWhitespaceGrid.Children.Add(circle);

                    this.Elements.Add(customWhitespaceGrid);
                    break;

                default:
                    char c1 = HEX_LOOKUP[(c & 0xF0) >> 4];
                    char c2 = HEX_LOOKUP[c & 0xF];

                    Grid customCharGrid = new Grid()
                    {
                        Width = this.Width,
                        Height = this.Height,
                        Background = settings.TextColorCustomChar,
                    };
                    customCharGrid.RowDefinitions.Add(new RowDefinition());
                    customCharGrid.RowDefinitions.Add(new RowDefinition());

                    TextBlock c1TextBlock = new TextBlock
                    {
                        Text = c1.ToString(),
                        FontFamily = new FontFamily(settings.FontFamily),
                        FontSize = settings.FontSize,
                        Foreground = settings.TextColor,
                        TextAlignment = TextAlignment.Center,
                    };

                    TextBlock c2TextBlock = new TextBlock
                    {
                        Text = c2.ToString(),
                        FontFamily = new FontFamily(settings.FontFamily),
                        FontSize = settings.FontSize,
                        Foreground = settings.TextColor,
                        TextAlignment = TextAlignment.Center,
                    };
                    Grid.SetRow(c2TextBlock, 1);

                    TransformGroup transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new ScaleTransform(0.9, 0.5));
                    c1TextBlock.LayoutTransform = transformGroup;
                    c2TextBlock.LayoutTransform = transformGroup;

                    customCharGrid.Children.Add(c1TextBlock);
                    customCharGrid.Children.Add(c2TextBlock);

                    this.Elements.Add(customCharGrid);
                    break;
            }
        }
        */

        private static bool IsCustomChar(char c)
        {
            return char.IsWhiteSpace(c);
        }
    }
}
