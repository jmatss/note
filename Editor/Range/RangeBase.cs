namespace Editor.Range
{
    public class RangeBase
    {
        public int Start { get; set; }

        public int End { get; set; }

        public int Length => End - Start;

        public RangeBase(int start, int end)
        {
            Start = start;
            End = end;
        }

        public RangeBase(RangeBase other) : this(other.Start, other.End)
        {
        }

        public bool IsInRange(int idx) => idx >= Start && idx < End;

        public override string ToString()
        {
            return "{Range(" + this.Start + ", " + this.End + ")}";
        }
    }
}
