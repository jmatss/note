using System.Collections;

namespace Text.Iterators
{
    /// <summary>
    /// An iterator over all leafs in a Rope.
    /// 
    /// The returned `Node` represents the leaf.
    /// The first returned integer represents the local leaf index.
    /// The second returned integer represents the char length of the leaf.
    /// </summary>
    public class RopeLeafsIterator : IEnumerable<(Node, int, int)>
    {
        private readonly Rope rope;
        private readonly int startIdx;
        /// <summary>
        /// The amount of chars that will be iterated.
        /// -1 indicates no limit.
        /// </summary>
        private readonly int limit;

        public RopeLeafsIterator(Rope rope, int startIdx, int limit)
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

                if (this.limit > -1 && amountOfCharsIterated + curLeaf.BufferRef.Length - curLeafIdx >= this.limit)
                {
                    curLeafLength = this.limit - amountOfCharsIterated;
                    limitReached = true;
                }
                else
                {
                    curLeafLength = curLeaf.BufferRef.Length - curLeafIdx;
                }

                yield return (curLeaf, curLeafIdx, curLeafLength);

                amountOfCharsIterated += curLeafLength;
                curGlobalIdx += curLeafLength;

                (curLeaf, curLeafIdx) = this.rope.root.GetLeafContainingCharAtIndex(curGlobalIdx);
            }
        }

        public IEnumerator<(Node, int, int)> GetEnumerator() => this.GetEnumerator(this.startIdx);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator(this.startIdx);
    }
}
