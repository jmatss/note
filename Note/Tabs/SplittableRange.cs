using System.Collections;

namespace Note.Tabs
{
    /// <summary>
    /// Represents a range that can be splitted.
    /// 
    /// Enumerating this range will return the parts of the original range
    /// that hasn't been removed/split.
    /// 
    /// This will be used e.g. when creating GridSplitters.
    /// </summary>
    public class SplittableRange : IEnumerable<Range>
    {
        private readonly Range _originalRange;
        private readonly List<Range> _subtractedRanges = [];

        /// <summary>
        /// </summary>
        /// <param name="start">Inclusive</param>
        /// <param name="end">Exclusive</param>
        public SplittableRange(int start, int end)
        {
            this._originalRange = new Range(start, end);
        }

        public void SplitOn(int start, int end)
        {
            this._subtractedRanges.Add(new Range(start, end));
        }

        public IEnumerator<Range> GetEnumerator()
        {
            int startI = this._originalRange.Start.Value;

            for (int i = this._originalRange.Start.Value; i < this._originalRange.End.Value; i++)
            {
                bool isSubtracted = this._subtractedRanges.Any(x => i >= x.Start.Value && i < x.End.Value);
                if (!isSubtracted)
                {
                    continue; // Keep incrementing the length of the range that we are collecting.
                }

                bool collectedLengthIsZero = startI == i;
                if (!collectedLengthIsZero)
                {
                    yield return new Range(startI, i);
                }

                startI = i + 1;
            }

            // Return range of remainder (if there is a remainder)
            if (startI < this._originalRange.End.Value)
            {
                yield return new Range(startI, this._originalRange.End.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
