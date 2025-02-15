namespace Text
{
    public struct Counts
    {
        public int Chars { get; set; }
        public int LineBreaks { get; set; }

        public static Counts Empty = new Counts(0, 0);

        public Counts(int charCount, int lineBreakCount)
        {
            this.Chars = charCount;
            this.LineBreaks = lineBreakCount;
        }

        /// <summary>
        /// Returns the count for the given `type`.
        /// </summary>
        /// <param name="type">The type to fetch the count for</param>
        /// <returns>The count</returns>
        public int Get(CountsType type)
        {
            switch (type)
            {
                case CountsType.Chars: return this.Chars;
                case CountsType.LineBreaks: return this.LineBreaks;
                default: return -1; // unreachable.
            }
        }

        /// <summary>
        /// Returns the sum of the counts in `this` + `other`.
        /// </summary>
        /// <param name="other">The `Counts` to sum `this` together with</param>
        /// <returns>Returns the sum of the counts in `this` + `other`</returns>
        public Counts Plus(Counts other)
        {
            return new Counts(
                this.Chars + other.Chars,
                this.LineBreaks + other.LineBreaks
            );
        }

        /// <summary>
        /// Returns the current counts, but the sign have been flipped to minus
        /// instead of plus.
        /// 
        /// I.e. this returns all counts as if they have been multiplied
        /// by -1. This can be used when removing counts instead of adding them.
        /// </summary>
        /// <returns></returns>
        public Counts AsNegative()
        {
            return new Counts(this.Chars * -1, this.LineBreaks * -1);
        }
    }

    public enum CountsType
    {
        Chars,
        LineBreaks,
    }
}
