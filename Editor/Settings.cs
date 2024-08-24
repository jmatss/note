using System.Windows.Media;

namespace Editor
{
    public class Settings
    {
        public Settings()
        {
        }

        public Brush TextColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 187, 187, 187));

        public Brush TextColorCustomChar { get; set; } = new SolidColorBrush(Color.FromArgb(90, 220, 220, 204));

        public Brush SelectionCursorColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 220, 220, 204));

        public Brush SelectionBackgroundColor {  get; set; } = new SolidColorBrush(Color.FromArgb(100, 173, 206, 250));

        public Brush BackgroundColor { get; set; } = new SolidColorBrush(Color.FromArgb(255, 41, 40, 45));

        private string fontFamily = "Consolas";
        public string FontFamily
        {
            get => this.fontFamily;
            set
            {
                this.fontFamily = value;
                this.typeFace = null;
                this.glyphTypeface = null;
            }
        }

        public int FontSize { get; set; } = 14;

        private Typeface? typeFace;
        public Typeface TypeFace
        {
            get
            {
                if (this.typeFace == null)
                {
                    this.typeFace = new Typeface(this.FontFamily);
                }
                return this.typeFace;
            }
        }

        private GlyphTypeface? glyphTypeface;
        public GlyphTypeface GlyphTypeface
        {
            get
            {
                if (this.glyphTypeface == null)
                {
                    // TODO: Error handling
                    this.TypeFace.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface);
                    this.glyphTypeface = glyphTypeface;
                }
                return this.glyphTypeface;
            }
        }

        public bool DrawCustomChars { get; set; } = true;

        public bool WordWrap { get; set; } = true;

        public bool UseUnixLineBreaks { get; set; } = false;

        public string TabString { get; set; } = "  ";

        public int ScrollIncrement { get; set; } = 4;

        public void Todo_Freeze()
        {
            this.TextColor.Freeze();
            this.TextColorCustomChar.Freeze();
            this.SelectionCursorColor.Freeze();
            this.SelectionBackgroundColor.Freeze();
            this.BackgroundColor.Freeze();
        }
    }
}
