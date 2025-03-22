using System.Collections;
using System.Text;
using Text.Iterators;

// TODO: Handle BOM. Should it maybe be filtered out before we
//       get to constructing a Rope? Probably best.
// TODO: Handle encoding. Currently only handles UTF-16.
// TODO: Implement so that RopeNode's can share the underlying data.
//       Should maybe have another variant that allows one to specify
//       a slice/indices.
// TODO: Should special characters be counted in the `charCount`? Ex.
//       does `\n` count as a char? Does `\r\n` count as one or two chars?

namespace Text
{
    public class Rope
    {
        public const char LINE_FEED = '\n';
        public const int BUFFER_START_SIZE = 4;
        public const int BUFFER_MAX_SIZE = 512;
        public const int FILE_READ_BUFFER_SIZE = 65536;

        public static byte[] BOM_BYTES = Encoding.UTF8.GetPreamble();

        internal readonly Node root;
        private readonly ModificationsStack modifications;

        private Rope(Node root)
        {
            this.root = root;
            this.modifications = new ModificationsStack((m) => this.OnModification?.Invoke(m));
        }

        public delegate void ModificationHandler(IModification modification);
        public event ModificationHandler? OnModification;

        public static Rope FromString(string str, Encoding encoding)
        {
            return FromBytes(encoding.GetBytes(str), encoding);
        }

        public static Rope FromBytes(byte[] bytes, Encoding encoding)
        {
            using (Stream stream = new MemoryStream(bytes))
            {
                stream.Position = 0;
                return FromStream(stream, encoding);
            }
        }

        public static Rope FromStream(Stream stream, Encoding encoding)
        {
            var rope = new Rope(new Node()
            {
                Counts = Counts.Empty,
                BufferRef = Buffer.WithDefault().CreateReference(0, 0),
            });

            byte[] byteBuffer = new byte[Rope.FILE_READ_BUFFER_SIZE];

            int insertCharIndex = 0;

            while (true)
            {
                int amountOfBytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length);
                if (amountOfBytesRead == 0)
                {
                    break; // EOF
                }

                char[] chars = encoding.GetChars(byteBuffer, 0, amountOfBytesRead);
                int amountOfCharsRead = chars.Length;

                if (char.IsHighSurrogate(chars[amountOfCharsRead - 1]))
                {
                    // Last character in the stream hasn't been read into the buffer fully.
                    // Don't add this "partial" character into the rope at this point. Instead
                    // "rewind" the stream and add the character in the next read when we know
                    // it will have been fully read.
                    stream.Position--;
                    amountOfCharsRead--;
                }

                rope.Insert(insertCharIndex, new ReadOnlySpan<char>(chars, 0, amountOfCharsRead));
                insertCharIndex += amountOfCharsRead;
            }

            return rope;
        }

        public void ValidateTree()
        {
            int totalActualCount = ValidateTree(this.root);
            if (this.GetTotalCharCount() != totalActualCount)
            {
                throw new Exception("total - this.GetTotalCharCount(): " + this.GetTotalCharCount() + ", totalActualCount: " + totalActualCount);
            }
        }

        public int ValidateTree(Node node)
        {
            if (node.IsLeaf())
            {
                if (node.BufferRef.Length != node.Counts.Chars)
                {
                    new Exception("leaf");
                }
                return node.BufferRef.Length;
            }
            else
            {
                int totalActualCount = 0;

                if (node.Left != null)
                {
                    totalActualCount += this.ValidateTree(node.Left);
                    if (totalActualCount != node.Counts.Chars)
                    {
                        throw new Exception("left");
                    }
                }

                if (node.Right != null)
                {
                    totalActualCount += this.ValidateTree(node.Right);
                }

                return totalActualCount;
            }
        }

        public void Insert(int idx, ReadOnlySpan<char> chars)
        {
            this.Insert(idx, chars, this.modifications);
        }

        private void Insert(int idx, ReadOnlySpan<char> chars, ModificationsStack modifications, bool isUndo = false)
        {
            var (leaf, leafIdx) = this.root.GetLeafContainingCharAtIndex(idx);
            if (leaf == null)
            {
                (leaf, leafIdx) = this.root.GetLastLeaf();
            }

            int amountOfCharsInserted = leaf.Insert(leafIdx, chars);
            int amountOfCharsLeftToInsert = chars.Length - amountOfCharsInserted;

            if (amountOfCharsLeftToInsert > 0)
            {
                List<BufferReference> bufferRefs = new List<BufferReference>();

                while (amountOfCharsLeftToInsert > 0)
                {
                    int startIdx = chars.Length - amountOfCharsLeftToInsert;
                    int bufferCapacity;

                    if (amountOfCharsLeftToInsert >= Rope.BUFFER_MAX_SIZE)
                    {
                        bufferCapacity = Rope.BUFFER_MAX_SIZE;
                    }
                    else
                    {
                        bufferCapacity = amountOfCharsLeftToInsert;
                    }

                    Buffer buffer = Buffer.WithData(chars.Slice(startIdx, bufferCapacity).ToArray(), bufferCapacity);
                    BufferReference bufferRef = buffer.CreateReference(0, bufferCapacity);

                    bufferRefs.Add(bufferRef);

                    amountOfCharsLeftToInsert -= bufferCapacity;
                }

                this.Insert(idx + amountOfCharsInserted, bufferRefs, null);
            }

            modifications.Push(new InsertModification(idx, chars.Length), isUndo);
        }

        private void Insert(int idx, ICollection<BufferReference> bufferRefs, ModificationsStack? modifications, bool isUndo = false)
        {
            this.root.SplitBeforeCharAtIndex(idx);

            var (leaf, leafIdx) = this.root.GetLeafContainingCharAtIndex(idx);
            if (leaf == null)
            {
                (leaf, leafIdx) = this.root.GetLastLeaf();
            }

            leaf.Insert(leafIdx, bufferRefs);
            int charLength = bufferRefs.Select(x => x.Length).Sum();
            modifications?.Push(new InsertModification(idx, charLength), isUndo);
        }

        public void Remove(int idx, int length)
        {
            this.Remove(idx, length, this.modifications);
        }

        private void Remove(int idx, int length, ModificationsStack modifications, bool isUndo = false)
        {
            this.root.SplitBeforeCharAtIndex(idx);
            this.root.SplitBeforeCharAtIndex(idx + length);

            var leafsToRemove = this.IterateLeafs(idx, length).Select(x => x.Item1).ToList();
            List<BufferReference> bufferRefs = new List<BufferReference>();

            foreach (Node leafToRemove in leafsToRemove)
            {
                bufferRefs.Add(leafToRemove.Remove());
            }

            modifications.Push(new RemoveModification(idx, length, bufferRefs), isUndo);
        }

        public void Replace(int idx, int lengthToRemove, ReadOnlySpan<char> charsToInsert)
        {
            this.Replace(idx, lengthToRemove, charsToInsert, this.modifications);
        }

        private void Replace(int idx, int lengthToRemove, ReadOnlySpan<char> charsToInsert, ModificationsStack modifications, bool isUndo = false)
        {
            ModificationsStack localModifications = new ModificationsStack(null);

            this.Remove(idx, lengthToRemove, localModifications);
            if (!(localModifications.Pop(false) is RemoveModification remove))
            {
                // TODO:
                throw new Exception("Not RemoveModification after Remove in Replace(chars)");
            }

            this.Insert(idx, charsToInsert, localModifications);
            if (!(localModifications.Pop(false) is InsertModification insert))
            {
                // TODO:
                throw new Exception("Not InsertModification after Insert in Replace(chars)");
            }

            modifications.Push(new ReplaceModification(remove, insert), isUndo);
        }

        private void Replace(int idx, int lengthToRemove, ICollection<BufferReference> bufferRefsToInsert, ModificationsStack modifications, bool isUndo = false)
        {
            ModificationsStack localModifications = new ModificationsStack(null);

            this.Remove(idx, lengthToRemove, localModifications);
            if (!(localModifications.Pop(false) is RemoveModification remove))
            {
                // TODO:
                throw new Exception("Not RemoveModification after in Replace(bufferRefs)");
            }

            this.Insert(idx, bufferRefsToInsert, localModifications);
            if (!(localModifications.Pop(false) is InsertModification insert))
            {
                // TODO:
                throw new Exception("Not InsertModification after Insert in Replace(bufferRefs)");
            }

            localModifications.Push(new ReplaceModification(remove, insert), isUndo);
        }

        public int Undo()
        {
            IModification? modification = this.modifications.Pop(false);
            if (modification == null)
            {
                return -1;
            }

            bool isUndo = true;

            if (modification is InsertModification insert)
            {
                this.Remove(insert.StartIdx, insert.Length, this.modifications, isUndo);
                return insert.StartIdx;
            }
            else if (modification is InsertModifications inserts)
            {
                throw new Exception("TODO: Undo.InsertModifications");
            }
            else if (modification is RemoveModification remove)
            {
                this.Insert(remove.StartIdx, remove.BufferRefs, this.modifications, isUndo);
                return remove.StartIdx + remove.Length;
            }
            else if (modification is RemoveModifications removes)
            {
                throw new Exception("TODO: Undo.RemoveModifications");
            }
            else if (modification is ReplaceModification replace)
            {
                InsertModification ins = replace.Insert;
                RemoveModification rem = replace.Remove;
                this.Replace(ins.StartIdx, ins.Length, rem.BufferRefs, this.modifications, isUndo);
                return ins.StartIdx + rem.Length;
            }
            else if (modification is ReplaceModifications replaces)
            {
                throw new Exception("TODO: Undo.ReplaceModifications");
            }

            return -1;
        }

        public int GetNextWordStartIndex(int idx, bool skipEndWhitespaces)
        {
            int lastIdx = this.GetTotalCharCount();
            if (idx >= lastIdx)
            {
                return lastIdx;
            }

            int curGlobalIdx = idx;
            char currentChar = this.IterateChars(curGlobalIdx).First().Item1;

            Func<char, bool> predicate;
            if (IsValidIdentifier(currentChar))
            {
                predicate = (x) => IsValidIdentifier(x);
            }
            else if (char.IsWhiteSpace(currentChar))
            {
                predicate = (x) => char.IsWhiteSpace(x);
            }
            else
            {
                predicate = (x) => !IsValidIdentifier(x);
            }

            foreach (var (c, _) in this.IterateChars(curGlobalIdx))
            {
                if (predicate(c))
                {
                    curGlobalIdx++;
                }
                else
                {
                    break;
                }
            }

            if (skipEndWhitespaces)
            {
                curGlobalIdx = this.SkipWhitespaces(curGlobalIdx);
            }

            return curGlobalIdx;
        }

        public int GetCurrentWordStartIndex(int idx, bool skipEndWhitespaces)
        {
            if (idx <= 0)
            {
                return 0;
            }

            int curGlobalIdx = idx;
            bool iterateInReverse = true;

            int idxBeforeWhitespacesSkipped = curGlobalIdx - 1;
            int idxAfterWhitespacesSkipped = this.SkipWhitespaces(idxBeforeWhitespacesSkipped, iterateInReverse);
            if (idxAfterWhitespacesSkipped <= 0)
            {
                return 0;
            }
            else if (idxBeforeWhitespacesSkipped != idxAfterWhitespacesSkipped)
            {
                curGlobalIdx = idxAfterWhitespacesSkipped;
            }
            else
            {
                curGlobalIdx--;
            }

            char currentChar = this.IterateChars(curGlobalIdx).First().Item1;

            Func<char, bool> predicate;
            if (IsValidIdentifier(currentChar))
            {
                predicate = (x) => IsValidIdentifier(x);
            }
            else
            {
                predicate = (x) => !IsValidIdentifier(x);
            }

            foreach (var (c, _) in this.IterateCharsReverse(curGlobalIdx))
            {
                if (predicate(c))
                {
                    curGlobalIdx--;
                }
                else
                {
                    curGlobalIdx++;
                    break;
                }
            }

            if (skipEndWhitespaces)
            {
                curGlobalIdx = this.SkipWhitespaces(curGlobalIdx, iterateInReverse);
            }

            return curGlobalIdx >= 0 ? curGlobalIdx : 0;
        }

        public int SkipWhitespaces(int globalIdx, bool iterateInReverse = false)
        {
            if (globalIdx == 0 || globalIdx == this.GetTotalCharCount())
            {
                return globalIdx;
            }
            
            IEnumerable<(char, int)> iterator;
            int incrementAmount;

            if (iterateInReverse)
            {
                iterator = this.IterateCharsReverse(globalIdx);
                incrementAmount = -1;
            }
            else
            {
                iterator = this.IterateChars(globalIdx);
                incrementAmount = +1;
            }

            foreach (var (c, _) in iterator)
            {
                if (char.IsWhiteSpace(c))
                {
                    globalIdx += incrementAmount;
                }
                else
                {
                    break;
                }
            }

            return globalIdx;
        }

        private static bool IsValidIdentifier(char c) => char.IsLetterOrDigit(c) || c == '_';

        private static bool IsWhitespace(char c) => char.IsWhiteSpace(c);

        public char GetChar(int charIdx)
        {
            return this.IterateChars(charIdx, 1).First().Item1;
        }

        public string GetText(int startCharIdx, int length)
        {
            return string.Join("", this.IterateChars(startCharIdx, length).Select(x => x.Item1));
        }

        public int GetTotalCharCount()
        {
            Counts rootTotalCounts = this.root.GetCounts();
            return rootTotalCounts.Chars;
        }

        public int GetTotalLineBreaks()
        {
            Counts rootTotalCounts = this.root.GetCounts();
            return rootTotalCounts.LineBreaks;
        }

        public int GetFirstCharIndexAtLineWithIndex(int lineIdx)
        {
            return this.root.GetFirstCharIndexAtLineWithIndex(lineIdx);
        }

        public int GetLineIndexForCharAtIndex(int charIdx)
        {
            return this.root.GetLineIndexForCharAtIndex(charIdx, charIdx);
        }

        public int GetCharCountAtLineWithIndex(int lineIdx)
        {
            int startCharIdx = this.GetFirstCharIndexAtLineWithIndex(lineIdx);
            int charCount = 0;

            foreach (var (c, _) in this.IterateChars(startCharIdx))
            {
                charCount++;
                if (c == LINE_FEED)
                {
                    break;
                }
            }

            return charCount;
        }

        public IEnumerable<Tuple<int, int>> FindAll(string textToFind, int startCharIdx, int endCharIdx)
        {
            int i = 0;
            int charIdx = startCharIdx;
            int stringToFindLength = textToFind.Length;

            foreach ((char curCh, _) in this.IterateChars(startCharIdx, endCharIdx - startCharIdx + 1))
            {
                if (curCh == textToFind[i])
                {
                    i++;

                    if (i >= stringToFindLength)
                    {
                        i = 0;
                        yield return new Tuple<int, int>(charIdx + 1 - stringToFindLength, charIdx + 1);
                    }
                }
                else
                {
                    i = 0;
                }

                charIdx++;
            }
        }

        public IEnumerable<(char, int)> IterateChars(int startIdx, int limit = -1) => new RopeCharsIterator(this, startIdx, limit);

        public IEnumerable<(char, char?)> IterateCharPairs(int startIdx) => new RopeCharPairsIterator(this, startIdx);

        public IEnumerable<(char, int)> IterateCharsReverse(int startIdx, int limit = -1) => new RopeCharsReverseIterator(this, startIdx, limit);

        public IEnumerable<(Node, int, int)> IterateLeafs(int startIdx, int limit = -1) => new RopeLeafsIterator(this, startIdx, limit);
    }
}
