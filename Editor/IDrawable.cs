namespace Editor
{
    public interface IDrawable
    {
        double X { get; }

        double Y { get; }

        double Width { get; }

        double Height { get; }

        void Draw(Settings settings);

        bool Contains(double x, double y)
        {
            return x >= this.X && x <= this.X + this.Width &&
                   y >= this.Y && y <= this.Y + this.Height;
        }
    }
}
