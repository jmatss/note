using System.Collections;

namespace Text.Iterators
{
    /// <summary>
    /// An iterator over all chars in a Rope pair-wise.
    /// 
    /// E.g. iterating over the string "ABCDE" would yield the following result:
    /// AB -> BC -> CD -> DE -> E?
    /// </summary>
    public class RopeCharPairsIterator : IEnumerable<(char, char?)>
    {
        private readonly RopeCharsIterator charIterator;

        public RopeCharPairsIterator(Rope rope, int startIdx)
        {
            this.charIterator = new RopeCharsIterator(rope, startIdx, -1);
        }

        public IEnumerator<(char, char?)> GetEnumerator()
        {
            char cCurrent;
            char cNext;
            IEnumerator<(char, int)> enumerator = this.charIterator.GetEnumerator();

            if (enumerator.MoveNext())
            {
                cCurrent = enumerator.Current.Item1;
            }
            else
            {
                yield break;
            }

            while (enumerator.MoveNext())
            {
                cNext = enumerator.Current.Item1;
                yield return (cCurrent, cNext);
                cCurrent = cNext;
            }

            yield return (cCurrent, null);
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
