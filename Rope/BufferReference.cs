namespace Note.Rope
{
    public class BufferReference : ICloneable, IDisposable
    {
        public BufferReference(int startIdx, int length, Buffer buffer)
        {
            this.StartIdx = startIdx;
            this.Length = length;
            this.Buffer = buffer;
            this.Buffer.RefCount++;
        }

        public int StartIdx { get; }
        public int Length { get; private set; }
        public Buffer Buffer { get; }

        public int Insert(int startIdx, ReadOnlySpan<char> srcChars)
        {
            int amountOfCharsInserted = this.Buffer.Insert(startIdx, srcChars);
            this.Length += amountOfCharsInserted;
            return amountOfCharsInserted;
        }

        public ReadOnlySpan<char> Get() => this.Buffer.Get(this.StartIdx, this.Length);

        public bool CanAppendData(int startIdx) => startIdx == this.Buffer.Length && this.Buffer.Length < Rope.BUFFER_MAX_SIZE && this.Buffer.RefCount <= 1;

        public object Clone() => this.Buffer.CreateReference(this.StartIdx, this.Length);

        public void Dispose()
        {
            if (this.Buffer != null)
            {
                this.Buffer.RefCount--;
            }
        }
    }
}
