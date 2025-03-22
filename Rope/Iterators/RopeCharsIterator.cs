using System.Collections;
using static Text.Rope;

namespace Text.Iterators
{
    /// <summary>
    /// An iterator over all chars in a Rope.
    /// 
    /// The returned integer is the ~length of the character.
    /// If the char isn't a part of a surrogate pair, the length 1 is returned.
    /// If the char is the start of a surrogate pair (i.e. high-surrogate), the length 2 is returned.
    /// If the char is the end of a surrogate pair (i.e. low-surrogate), the length -1 is returned.
    /// </summary>
    public class RopeCharsIterator : IEnumerable<(char, int)>
    {
        private readonly RopeLeafsIterator leafIterator;

        public RopeCharsIterator(Rope rope, int startIdx, int limit)
        {
            this.leafIterator = new RopeLeafsIterator(rope, startIdx, limit);
        }

        public IEnumerator<(char, int)> GetEnumerator()
        {
            foreach (var (leaf, bufRefIdx, length) in this.leafIterator)
            {
                int bufIdx = bufRefIdx + leaf.BufferRef.StartIdx;
                for (int i = bufIdx; i < bufIdx + length; i++)
                {
                    var (c, cWidth) = leaf.BufferRef.Buffer.GetChar(i);
                    yield return (c, cWidth);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
