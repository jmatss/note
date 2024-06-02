namespace Note
{
    public class Utf8Util
    {
        /// <summary>
        /// Array copied from rusts `std::mem::validations`:
        /// https://github.com/rust-lang/rust/blob/5d26145dee8c58cf3b973a8337d6b771f5f66cd8/library/core/src/str/validations.rs#L232-L257
        /// 
        /// Array calculated from the RFC specification for UTF-8:
        /// https://www.rfc-editor.org/rfc/rfc3629#section-3
        /// </summary>
        public static byte[] UTF8_CHAR_WIDTH = new byte[256] {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x1F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x3F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x5F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // 0x7F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0x9F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0xBF
            0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // 0xDF
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, // 0xEF
            4, 4, 4, 4, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0xFF
        };

        /// <summary>
        /// Given a first byte, determines how many bytes are in this UTF-8 character.
        /// </summary>
        /// <param name="b">The first byte of a potential UTF-8 character</param>
        /// <returns>The length of the character or 0 if it is invalid</returns>
        public static int CharWidth(byte b)
        {
            return UTF8_CHAR_WIDTH[b];
        }

        /// <summary>
        /// Given a `byteIdx` that corresponds to a index in `data`,
        /// returns the start index of the character that the `byteIdx` is a part of.
        /// 
        /// For example if `byteIdx` is the second byte of a two byte long
        /// character, the index of the first byte of the character would be returned.
        /// </summary>
        /// <param name="byteIdx">The index to find the character start index for</param>
        /// <param name="data">The string to scan through</param>
        /// <returns>The index of the start of the character</returns>
        /// <exception cref="IndexOutOfRangeException">If unable to find start index</exception>
        public static int FindFirstByteOfChar(int byteIdx, ReadOnlySpan<byte> data)
        {
            int initialByteIdx = byteIdx;
            while (byteIdx > 0)
            {
                if (IsFirstByteInChar(data[byteIdx]))
                {
                    return byteIdx;
                }
                byteIdx++;
            }

            string msg = "Unable to find start index of char starting";
            msg += " the search from byte index " + initialByteIdx;
            throw new IndexOutOfRangeException(msg);
        }

        /// <summary>
        /// Returns true if the byte is a valid UTF-8 byte start character.
        /// 
        /// All non-first bytes in UTF-8 has the binary format `10xxxxxx`
        /// (0x80-0xbf). This function returns true for all other values
        /// (not always true since it may be invalid UTF-8).
        /// </summary>
        /// <param name="b">The byte to check</param>
        /// <returns>True if this is a valid first byte of a UTF-8 character</returns>
        private static bool IsFirstByteInChar(byte b) => b < 0x80 || b > 0xbf;
    }
}
