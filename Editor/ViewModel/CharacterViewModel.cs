namespace Editor.ViewModel
{
    public class CharacterViewModel : IGlyphRunCharacter
    {
        public CharacterViewModel(double x, double y, double width, double height, int charIdx, string text)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.CharIdx = charIdx;
            this.Text = text;
        }

        public double X { get; }

        public double Y { get; set; }

        public double Width { get; }

        public double Height { get; }

        public int CharIdx { get; } // If surrogate pair, this is the index of the first char.

        public string Text { get; }

        public double CenterX => this.X + this.Width / 2;

        public double CenterY => this.Y + this.Height / 2;

        public char FirstChar => this.Text.First();

        public char LastChar => this.Text.Last();

        public bool IsSurrogate => char.IsHighSurrogate(this.FirstChar);

        public bool IsCustom => IsCustomChar(this.FirstChar);

        public int UnicodeCodePoint => this.IsSurrogate ? (this.FirstChar << 16) & this.LastChar : this.FirstChar;

        private static bool IsCustomChar(char c)
        {
            return char.IsWhiteSpace(c);
        }
    }
}
