using System.Diagnostics;

namespace Note.Rope
{
    public class Buffer
    {
        /// <summary>
        /// The raw characters stored in this buffer.
        /// </summary>
        private char[] chars = new char[0];

        /// <summary>
        /// Amount of objects referencing/using this buffer.
        /// </summary>
        public int RefCount { get; set; } = 0;

        /// <summary>
        /// The amount of characters that are currently stored in this buffer.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// The maximum capacity for amount of characters in this buffer.
        /// </summary>
        public int Capacity => this.chars.Length;

        internal Buffer(char[] chars, int length)
        {
            if (chars == null)
            {
                throw new ArgumentNullException("chars");
            }
            else if (length > chars.Length)
            {
                throw new ArgumentException("length > chars.Length");
            }
            else if (chars.Length > Rope.BUFFER_MAX_SIZE)
            {
                throw new ArgumentException("chars.Length > Rope.BUFFER_MAX_SIZE");
            }

            this.chars = chars;
            this.Length = length;
        }

        public static Buffer WithDefault()
        {
            return Buffer.WithCapacity(Rope.BUFFER_START_SIZE);
        }

        public static Buffer WithCapacity(int capacity)
        {
            return new Buffer(new char[capacity], 0);
        }

        public static Buffer WithData(char[] chars, int length)
        {
            return new Buffer(chars, length);
        }

        /// <summary>
        /// Returns a reference to `length` characters starting at index `startIdx`
        /// into this buffer .
        /// </summary>
        /// <param name="startIdx">The start index of the data to reference</param>
        /// <param name="length">The amount of characters to reference</param>
        /// <returns>A reference to data in this buffer</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// If `startIdx` negative or tries to reference data outside of buffer bounds
        /// </exception>
        public BufferReference CreateReference(int startIdx, int length)
        {
            if (startIdx < 0)
            {
                string msg = "Negative startIdx not valid: " + startIdx;
                throw new IndexOutOfRangeException(msg);
            }
            else if (startIdx + length > this.Length)
            {
                string msg = "Tried to reference " + length + " characters starting from";
                msg += " index " + startIdx + " into buffer with length " + this.Length;
                throw new IndexOutOfRangeException(msg);
            }

            return new BufferReference(startIdx, length, this);
        }

        /// <summary>
        /// Returns the amount of unoccupied characters in this buffer.
        /// </summary>
        public int AmountOfFreeSpace() => this.Capacity - this.Length;

        /// <summary>
        /// Returns `length` amount of characters from the buffer starting at index `startIdx`.
        /// </summary>
        /// <param name="startIndex">The start index of the data to get</param>
        /// <param name="length">The amount of characters to get</param>
        /// <returns>A view of the data</returns>
        public ReadOnlySpan<char> Get(int startIndex, int length) => new ReadOnlySpan<char>(this.chars, startIndex, length);

        // TODO: Handle grapheme clusters?
        /// <summary>
        /// Returns the char starting at `startIdx`.
        /// 
        /// Also returns the "width" of the user perceived character if it consists of multiple
        /// characters in the code. This is done to handle surrogate pairs.
        /// 
        /// If the given char isn't a valid start of a character (e.g. if it is a low surrogate),
        /// -1 is returned.
        /// </summary>
        /// <param name="startIdx">The indx of the char to fetch</param>
        /// <returns>The char and its "underlying" char length</returns>
        public (char, int) GetChar(int startIdx)
        {
            char c = this.chars[startIdx];
            int cWidth = char.IsHighSurrogate(c) ? 2 : (char.IsLowSurrogate(c) ? -1 : 1);
            return (c, cWidth);
        }

        /// <summary>
        /// Inserts as much data as possible from the given `srcChars`
        /// to this buffer staring at index `startIdx`.
        /// 
        /// Returns the amount of chars written to the buffer.
        /// 
        /// This function ensures that only whole chars are written
        /// to the buffer. I.e. if a surrogate pair can't fit in this
        /// buffer, it will not be written to the buffer even if it might
        /// have space for more chars.
        /// </summary>
        /// <param name="startIdx">The index to start writing to</param>
        /// <param name="srcChars">The chars to write to the buffer</param>
        /// <returns>Amount of chars written to buffer</returns>
        public int Insert(int startIdx, ReadOnlySpan<char> srcChars)
        {
            if (startIdx != this.Length)
            {
                string msg = "Tried to write " + srcChars.Length + " characters starting from";
                msg += " index " + startIdx + " into buffer with length " + this.Length;
                throw new IndexOutOfRangeException(msg);
            }

            int amountOfCharsToInsert;

            if (srcChars.Length > this.AmountOfFreeSpace())
            {
                int oldCapacity = this.Capacity;
                int newCapacity = CalculateNewBufferSize(this.Length + srcChars.Length);

                if (newCapacity > oldCapacity)
                {
                    char[] newChars = new char[newCapacity];
                    Array.Copy(this.chars, newChars, this.Length);
                    this.chars = newChars;
                }

                int amountOfFreeSpace = this.AmountOfFreeSpace();

                if (amountOfFreeSpace == 0)
                {
                    throw new UnreachableException("amountOfFreeSpace == 0");
                }
                else if (srcChars.Length <= amountOfFreeSpace)
                {
                    amountOfCharsToInsert = srcChars.Length;
                }
                else
                {
                    // We don't want to split a character between two buffers
                    // (e.g. surrogate pairs). So make sure to only write whole
                    // chars to the buffer, even if we could fit more chars in it.
                    if (char.IsHighSurrogate(srcChars[amountOfFreeSpace - 1]))
                    {
                        // TODO: Can most likely end up in a infinite loop in this case
                        //       if we have found an high surrogate and we only have 1
                        //       space left in the buffer. Then we will not insert anything
                        //       but the callers to this function will most likely try to
                        //       insert again since there is 1 free space in the buffer.
                        amountOfCharsToInsert = amountOfFreeSpace - 2;
                    }
                    else
                    {
                        amountOfCharsToInsert = amountOfFreeSpace;
                    }
                }
            }
            else
            {
                amountOfCharsToInsert = srcChars.Length;
            }

            Span<char> dstData = new Span<char>(this.chars, startIdx, amountOfCharsToInsert);
            srcChars.Slice(0, amountOfCharsToInsert).CopyTo(dstData);

            this.Length += amountOfCharsToInsert;
            return amountOfCharsToInsert;
        }

        private static int CalculateNewBufferSize(int requiredSize)
        {
            int newBufferSize = Rope.BUFFER_START_SIZE;

            while (newBufferSize < requiredSize && newBufferSize < Rope.BUFFER_MAX_SIZE)
            {
                newBufferSize *= 2;
            }

            if (newBufferSize > Rope.BUFFER_MAX_SIZE)
            {
                newBufferSize = Rope.BUFFER_MAX_SIZE;
            }

            return newBufferSize;
        }
    }
}
