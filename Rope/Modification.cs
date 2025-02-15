namespace Text
{
    public interface IModification
    {
        
    }

    public readonly struct InsertModification : IModification
    {
        public int StartIdx { get; }
        public int Length { get; }

        public InsertModification(int startIdx, int length)
        {
            this.StartIdx = startIdx;
            this.Length = length;
        }
    }

    public readonly struct InsertModifications : IModification
    {
        public InsertModification Inserts { get; }

        public InsertModifications(InsertModification inserts)
        {
            this.Inserts = inserts;
        }
    }

    public readonly struct RemoveModification : IModification
    {
        public int StartIdx { get; }
        public int Length { get; }
        public IList<BufferReference> BufferRefs { get; }

        public RemoveModification(int startIdx, int length, IList<BufferReference> bufferRefs)
        {
            this.StartIdx = startIdx;
            this.Length = length;
            this.BufferRefs = bufferRefs;
        }
    }

    public readonly struct RemoveModifications : IModification
    {
        public RemoveModification Removes { get; }

        public RemoveModifications(RemoveModification removes)
        {
            this.Removes = removes;
        }
    }

    public readonly struct ReplaceModification : IModification
    {
        public RemoveModification Remove { get; }
        public InsertModification Insert { get; }

        public ReplaceModification(RemoveModification remove, InsertModification insert)
        {
            this.Remove = remove;
            this.Insert = insert;
        }
    }

    public readonly struct ReplaceModifications : IModification
    {
        public ReplaceModification Replaces { get; }

        public ReplaceModifications(ReplaceModification replaces)
        {
            this.Replaces = replaces;
        }
    }
}
