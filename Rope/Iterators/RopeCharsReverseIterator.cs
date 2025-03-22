using System.Collections;
using static Text.Rope;

namespace Text.Iterators
{
    /// <summary>
    /// An iterator over all chars in a Rope, in reverse order.
    /// 
    /// This is equivalent to the `RopeCharsIterator` but reverse, so read summary
    /// on `RopeCharsIterator` for more information.
    /// </summary>
    public class RopeCharsReverseIterator : IEnumerable<(char, int)>
    {
        private readonly RopeLeafsReverseIterator leafReverseIterator;

        public RopeCharsReverseIterator(Rope rope, int startIdx, int limit)
        {
            this.leafReverseIterator = new RopeLeafsReverseIterator(rope, startIdx, limit);
        }

        public IEnumerator<(char, int)> GetEnumerator()
        {
            foreach (var (leaf, bufRefIdx, length) in this.leafReverseIterator)
            {
                int maxIterationAmount = length;
                int i = bufRefIdx + leaf.BufferRef.StartIdx;
                while (maxIterationAmount > 0 && i >= 0)
                {
                    var (c, cWidth) = leaf.BufferRef.Buffer.GetChar(i);
                    yield return (c, cWidth);
                    i--;
                    maxIterationAmount--;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
