namespace Editor.ViewModel
{
    public class EmptyLineViewModel : LineViewModel
    {
        public EmptyLineViewModel(int charIdx, double x, double y, double height)
        {
            this.StartCharIdx = charIdx;
            this.EndCharIdx = charIdx;
            this.X = x;
            this.y = y;
            this.Height = height;
        }

        public override int StartCharIdx { get; }

        public override int EndCharIdx { get; }

        public override double X { get; }

        private double y;
        public override double Y => this.y;

        public override double Width => 0.0;

        public override double Height { get; }

        public void SetY(double y)
        {
            this.y = y;
        }
    }
}
