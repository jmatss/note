namespace Editor.Range
{
    public enum InsertionPosition
    {
        Start,
        End,
        None,
    }

    public class SelectionRange : RangeBase
    {
        public InsertionPosition InsertionPosition { get; set; }

        public bool InsertionPositionIsAtStart => InsertionPosition != InsertionPosition.End;

        public int InsertionPositionIndex
        {
            get => InsertionPositionIsAtStart ? base.Start : base.End;
            set
            {
                if (InsertionPositionIsAtStart)
                {
                    base.Start = value;
                }
                else
                {
                    base.End = value;
                }
            }
        }

        public SelectionRange(int index) : this(index, index, InsertionPosition.Start)
        {
        }

        public SelectionRange(int start, int end, InsertionPosition insertionPoint) : base(start, end)
        {
            InsertionPosition = insertionPoint;
        }

        public SelectionRange(SelectionRange other) : this(other.Start, other.End, other.InsertionPosition)
        {
        }

        public void Update(SelectionRange newSelection)
        {
            this.Start = newSelection.Start;
            this.End = newSelection.End;
            InsertionPosition = newSelection.InsertionPosition;
        }

        public SelectionRange Normalized()
        {
            SelectionRange normalized = new SelectionRange(this);

            if (normalized.Start > normalized.End)
            {
                (normalized.End, normalized.Start) = (normalized.Start, normalized.End);
                switch (normalized.InsertionPosition)
                {
                    case InsertionPosition.Start:
                        normalized.InsertionPosition = InsertionPosition.End;
                        break;
                    case InsertionPosition.End:
                        normalized.InsertionPosition = InsertionPosition.Start;
                        break;
                    default:
                        break;
                }
            }

            return normalized;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (!(obj is SelectionRange other)) return false;
            return this.Start == other.Start &&
                   this.End == other.End &&
                   this.InsertionPosition == other.InsertionPosition;
        }

        public bool IsDummy() => this.Start == -1 || this.End == -1;

        public static SelectionRange Dummy() => new SelectionRange(-1, -1, InsertionPosition.None);
    }
}
