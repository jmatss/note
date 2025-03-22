using System.Collections;

namespace Text.Iterators
{
    /// <summary>
    /// An iterator over all leafs in a Rope, in reverse order.
    /// 
    /// This is equivalent to the `RopeLeafsIterator` but reverse, so read summary
    /// on `RopeLeafsIterator` for more information.
    /// </summary>
    public class RopeLeafsReverseIterator : IEnumerable<(Node, int, int)>
    {
        private readonly Rope rope;
        private readonly int startIdx;
        /// <summary>
        /// The amount of chars that will be iterated.
        /// -1 indicates no limit.
        /// </summary>
        private readonly int limit;

        public RopeLeafsReverseIterator(Rope rope, int startIdx, int limit)
        {
            this.rope = rope;
            this.startIdx = startIdx;
            this.limit = limit;
        }

        public IEnumerator<(Node, int, int)> GetEnumerator(int startIdx)
        {
            bool limitReached = false;
            int amountOfCharsIterated = 0;

            int curGlobalIdx = startIdx;
            var (curLeaf, curLeafIdx) = this.rope.root.GetLeafContainingCharAtIndex(curGlobalIdx);

            while (curLeaf != null && !limitReached)
            {
                int curLeafLength;
                if (this.limit > -1 && amountOfCharsIterated + curLeaf.BufferRef.Length >= this.limit)
                {
                    curLeafLength = this.limit - amountOfCharsIterated;
                    limitReached = true;
                }
                else
                {
                    curLeafLength = curLeafIdx + 1;
                }

                yield return (curLeaf, curLeafIdx, curLeafLength);

                amountOfCharsIterated += curLeafLength;
                curGlobalIdx -= curLeafLength;

                (curLeaf, curLeafIdx) = this.rope.root.GetLeafContainingCharAtIndex(curGlobalIdx);
            }
        }

        public IEnumerator<(Node, int, int)> GetEnumerator() => this.GetEnumerator(this.startIdx);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator(this.startIdx);
    }
}
