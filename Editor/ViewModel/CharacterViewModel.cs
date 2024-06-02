namespace Editor.ViewModel
{
    public class CharacterViewModel : IGlyphRunCharacter
    {
        private static readonly char[] HEX_LOOKUP = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public CharacterViewModel(double x, double y, double width, double height, int charIdx, string text, Settings settings)
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

        /*
        public ObservableCollection<UIElement> Elements { get; set; } = new ObservableCollection<UIElement>();

        public void Draw(Settings settings)
        {
            if (IsCustomChar(this.FirstChar) && settings.DrawCustomChars)
            {
                this.DrawCustomChar(settings);
            }
            else
            {
                TextBlock textBlock = new TextBlock
                {
                    Width = this.Width,
                    Height = this.Height,
                    Text = this.Text,
                    FontFamily = new FontFamily(settings.FontFamily),
                    FontSize = settings.FontSize,
                    Foreground = settings.TextColor,
                    TextAlignment = TextAlignment.Center,
                };

                this.Elements.Add(textBlock);
            }
        }

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
