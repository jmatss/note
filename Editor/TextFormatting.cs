using System.Drawing;
/*
namespace Note.Editor
{
    internal readonly struct TextFormatting
    {
        public static readonly TextFormatting DefaultText = TextFormatting.Text(TextEditor.TEXT_COLOR);

        public static readonly TextFormatting DefaultSelection = TextFormatting.Background(TextEditor.SELECTION_BACKGROUND_COLOR);

        public Color? TextColor { get; }
        public Color? BackgroundColor { get; }

        public TextFormatting(Color? textColor, Color? backgroundColor)
        {
            this.TextColor = textColor;
            this.BackgroundColor = backgroundColor;
        }

        public static TextFormatting Text(Color color)
        {
            return new TextFormatting(color, null);
        }

        public static TextFormatting Background(Color color)
        {
            return new TextFormatting(null, color);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is TextFormatting other)) return false;
            return object.Equals(this.TextColor, other.TextColor) &&
                   object.Equals(this.BackgroundColor, other.BackgroundColor);
        }

        public override int GetHashCode()
        {
            return (this.TextColor, this.BackgroundColor).GetHashCode();
        }
    }

    internal class TextFormattingRange : Range
    {
        public TextFormatting TextFormatting { get; set; }

        public TextFormattingRange(int start, int end, TextFormatting textFormatting) : base(start, end)
        {
            this.TextFormatting = textFormatting;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is TextFormattingRange other)) return false;
            return object.Equals(this.Start, other.Start) &&
                   object.Equals(this.End, other.End) &&
                   object.Equals(this.TextFormatting, other.TextFormatting);
        }

        public override int GetHashCode()
        {
            return (this.Start, this.End, this.TextFormatting).GetHashCode();
        }
    }

    internal class TextFormattingRules
    {
        private readonly List<TextFormattingRange> rangeRules = new List<TextFormattingRange>();
        private readonly Dictionary<char, TextFormatting> charRules = new Dictionary<char, TextFormatting>();

        public TextFormattingRules()
        {
        }

        public void Add(TextFormattingRange rangeRule)
        {
            this.rangeRules.Add(rangeRule);
        }

        public void Add(char c, TextFormatting textFormatting)
        {
            this.charRules[c] = textFormatting;
        }

        public void Remove(TextFormattingRange rangeRule)
        {
            this.rangeRules.Remove(rangeRule);
        }

        public void Remove(char c)
        {
            this.charRules.Remove(c);
        }

        public TextFormatting CalculateTextFormatting(char c, int idx)
        {
            IEnumerable<TextFormattingRange> currentRangeRules = this.rangeRules.Where(x => x.IsInRange(idx));

            bool currentHasRangeRule = currentRangeRules.Count() > 0;
            bool currentHasCharRule = this.charRules.ContainsKey(c);

            TextFormatting textFormatting;

            if (currentHasRangeRule && currentHasCharRule)
            {
                Color? textColor = currentRangeRules
                    .FirstOrDefault(x => x.TextFormatting.TextColor != null)?
                    .TextFormatting
                    .TextColor;
                Color? backgroundColor = currentRangeRules
                    .FirstOrDefault(x => x.TextFormatting.BackgroundColor != null)?
                    .TextFormatting
                    .BackgroundColor;

                if (textColor == null)
                {
                    textColor = this.charRules[c].TextColor ?? TextFormatting.DefaultText.TextColor;
                }
                if (backgroundColor == null)
                {
                    backgroundColor = this.charRules[c].BackgroundColor ?? TextFormatting.DefaultText.BackgroundColor;
                }

                textFormatting = new TextFormatting(textColor, backgroundColor);
            }
            else if (currentHasRangeRule)
            {
                // TODO: Combine colors in some way. Do if colors are transparante.
                //       Probably not if there are no transparency.
                Color? textColor = currentRangeRules
                    .FirstOrDefault(x => x.TextFormatting.TextColor != null)?
                    .TextFormatting
                    .TextColor ?? TextFormatting.DefaultText.TextColor;
                Color? backgroundColor = currentRangeRules
                    .FirstOrDefault(x => x.TextFormatting.BackgroundColor != null)?
                    .TextFormatting
                    .BackgroundColor ?? TextFormatting.DefaultText.BackgroundColor;

                textFormatting = new TextFormatting(textColor, backgroundColor);
            }
            else if (currentHasCharRule)
            {
                textFormatting = this.charRules[c];
            }
            else
            {
                // No rule applies, use default.
                textFormatting = TextFormatting.DefaultText;
            }

            return textFormatting;
        }
    }
}
*/