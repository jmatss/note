namespace Text
{
    public class Node
    {
        public Counts Counts { get; set; } = Counts.Empty;

        public Node Parent { get; set; }

        // Only set if this is NOT a leaf.
        public Node Left { get; set; }
        public Node Right { get; set; }

        // Only set if this is a leaf.
        public BufferReference BufferRef;

        public bool IsLeaf() => this.Left == null && this.Right == null;

        public int Insert(int leafIdx, ReadOnlySpan<char> chars)
        {
            if (!this.IsLeaf())
            {
                // TODO:
                throw new Exception("Not leaf in Insert chars");
            }

            int amountOfCharsInserted;

            if (this.BufferRef.CanAppendData(leafIdx))
            {
                amountOfCharsInserted = this.BufferRef.Insert(leafIdx, chars);
                Counts insertedCounts = CalculateCountsForChars(chars.Slice(0, amountOfCharsInserted));
                this.PropagateCountsChangeToAncestors(insertedCounts);
            }
            else
            {
                amountOfCharsInserted = 0;
            }

            return amountOfCharsInserted;
        }

        public void Insert(int leafIdx, ICollection<BufferReference> bufferRefs)
        {
            if (!this.IsLeaf())
            {
                // TODO:
                throw new Exception("Not leaf in Insert bufferRefs");
            }
            else if (leafIdx != 0 && leafIdx != this.Counts.Chars)
            {
                // TODO:
                throw new Exception("leafIdx != 0 && leafIdx != this.Counts.Chars");
            }

            List<Node> newLeafs = new List<Node>();

            foreach (BufferReference bufferRef in bufferRefs)
            {
                newLeafs.Add(new Node() { BufferRef = bufferRef, Counts = Counts.Empty });
            }

            Queue<Node> inputs;
            Queue<Node> outputs = new Queue<Node>(newLeafs);

            // Create a new tree-structure that contains all the data.
            while (outputs.Count > 1)
            {
                inputs = new Queue<Node>(outputs);
                outputs = new Queue<Node>();

                while (inputs.Count > 0)
                {
                    Node leftNode = inputs.Dequeue();
                    Node rightNode = inputs.Count > 0 ? inputs.Dequeue() : null;

                    Node parent = new Node()
                    {
                        Counts = Counts.Empty,
                        Left = leftNode,
                        Right = rightNode,
                    };

                    leftNode.Parent = parent;
                    if (rightNode != null) rightNode.Parent = parent;

                    outputs.Enqueue(parent);
                }
            }

            // Convert the selected `this` node into a parent node.
            // One of the childs will be a "copy" of the `this` while
            // the other child will be the newly created tree.

            Node newSubTree = outputs.First();
            Node oldLeafNode = new Node { BufferRef = this.BufferRef, Counts = this.Counts };

            if (leafIdx == 0)
            {
                // Splits at start of buffer leaf. Wants to create a new leaf before
                // the current leaf.
                this.Left = newSubTree;
                this.Right = oldLeafNode;
                // `this` currently has the old count of `oldThisNode`. But since it is
                // made the right node, we don't want to include it in the new count.
                this.Counts = Counts.Empty;
            }
            else // (leafIdx == this.Counts.Chars)
            {
                // Splits at end of current leaf. Wants to create a new leaf after
                // the current leaf.
                this.Left = oldLeafNode;
                this.Right = newSubTree;
            }
            this.BufferRef = null;

            newSubTree.Parent = this;
            oldLeafNode.Parent = this;

            // Update counts for all the new leafs and propagate through the whole Rope.
            foreach (Node newLeaf in newLeafs)
            {
                // TODO: A lot of unnecessary calculations here.
                //       Should change to only changing the count
                //       for every node once.
                Counts counts = CalculateCountsForChars(newLeaf.BufferRef.Get());
                newLeaf.PropagateCountsChangeToAncestors(counts);
            }
        }

        public BufferReference Remove()
        {
            if (!this.IsLeaf())
            {
                throw new Exception("Not leaf in Remove");
            }

            this.PropagateCountsChangeToAncestors(this.Counts.AsNegative());
            BufferReference oldBufferRef = (BufferReference)this.BufferRef.Clone();

            if (this.Parent == null)
            {
                // The root.
                this.BufferRef = Buffer.WithDefault().CreateReference(0, 0);
            }
            else
            {
                this.Parent.RemoveChild(this);
            }

            this.BufferRef.Dispose();
            return oldBufferRef;
        }

        private void RemoveChild(Node childToRemove)
        {
            Node parent = this.Parent;

            if (ReferenceEquals(this.Left, childToRemove))
            {
                this.Left = null;
            }
            else if (ReferenceEquals(this.Right, childToRemove))
            {
                this.Right = null;
            }

            if (this.Left == null && this.Right == null)
            {
                if (this.Parent == null)
                {
                    // The root that is empty. Create an empty buffer
                    // instead of removing the root node.
                    this.BufferRef = Buffer.WithDefault().CreateReference(0, 0);
                    this.Counts = Counts.Empty;
                }
                else
                {
                    parent.RemoveChild(this);
                }
            }
            else if (this.Left == null || this.Right == null)
            {
                this.ReplaceBySingleChild();
            }
            else
            {
                // Unreachable, we should have set one of them to null in logic above.
            }
        }

        /// <summary>
        /// If the given node has a single child, the current node can be
        /// replaced by that child
        /// 
        /// This can be used when e.g. one of two children of a parent have
        /// been removed. In that case we can reduce the depth of the tree by
        /// replacing this node with its last child.
        /// </summary>
        public void ReplaceBySingleChild()
        {
            Node child;
            if (this.Left != null && this.Right == null)
            {
                child = this.Left;
            }
            else if (this.Left == null && this.Right != null)
            {
                child = this.Right;
            }
            else
            {
                return;
            }

            this.Counts = child.Counts;
            this.BufferRef = child.BufferRef;
            this.Left = child.Left;
            this.Right = child.Right;
            if (this.Left != null) this.Left.Parent = this;
            if (this.Right != null) this.Right.Parent = this;
        }

        public Node Concat(Node left, Node right)
        {
            Node root = new Node()
            {
                Left = left,
                Right = right,
            };
            root.Counts = root.GetCountsLeftOfNode();
            if (left != null) { left.Parent = root; }
            if (right != null) { right.Parent = root; }
            return root;
        }

        // TODO: Rebalance.
        public void SplitBeforeCharAtIndex(int idx)
        {
            var (leaf, leafIdx) = this.GetLeafContainingCharAtIndex(idx);
            if (leafIdx > 0 && leafIdx != leaf.BufferRef.Length)
            {
                leaf.SplitBeforeCharAtIndexInLeaf(leafIdx);
            }
            else
            {
                // Already split at "border" of leaf/buffer, so nothing to do.
            }
        }

        /// <summary>
        /// Returns the combined `Counts` for all nodes left of this node.
        /// If this is a leaf, this function returns an empty `Counts`.
        /// </summary>
        /// <returns>The `Counts` for all nodes to the left of this node</returns>
        public Counts GetCountsLeftOfNode()
        {
            if (this.Left != null)
            {
                return this.Left.GetCounts();
            }
            else
            {
                return Counts.Empty;
            }
        }

        /// <summary>
        /// Returns the combined `Counts` for all nodes "underneath" this node
        /// (including both left & right nodes).
        /// </summary>
        /// <returns>The combined `Counts` of all children</returns>
        public Counts GetCounts()
        {
            if (this.IsLeaf())
            {
                return this.Counts;
            }
            else
            {
                Counts curNodeCounts = this.Counts;
                if (this.Right != null)
                {
                    curNodeCounts = curNodeCounts.Plus(this.Right.GetCounts());
                }
                return curNodeCounts;
            }
        }

        private void PropagateCountsChangeToAncestors(Counts countsDiff, Node? sender = null)
        {
            // `sender` should never be null if we are at a non-leaf node.
            if (ReferenceEquals(sender, this.Left))
            {
                this.Counts = this.Counts.Plus(countsDiff);
            }
            this.Parent?.PropagateCountsChangeToAncestors(countsDiff, this);
        }

        private static Counts CalculateCountsForChars(ReadOnlySpan<char> chars)
        {
            int charCount = chars.Length;
            int lineBreakCount = 0;

            foreach (char c in chars)
            {
                if (c == Rope.LINE_FEED)
                {
                    lineBreakCount++;
                }
            }

            return new Counts(charCount, lineBreakCount);
        }

        /// <summary>
        /// Recursively finds the node that contains character with index
        /// `idx`. The `idx` does not represent the "global" char index of
        /// the whole text. It just represents the index for the sub-tree
        /// represented by the children of the current `this` node.
        /// </summary>
        /// <param name="idx">The index of the char to find in the current sub-tree</param>
        /// <returns>The node containing the character and the index for the char in the specific node buffer</returns>
        public (Node, int) GetLeafContainingCharAtIndex(int idx)
        {
            int amountOfCharsToTheLeft = this.Counts.Chars;

            if (idx < 0 || (this.IsLeaf() && idx >= this.BufferRef.Length))
            {
                return (null, -1);
            }
            else if (this.IsLeaf())
            {
                return (this, idx);
            }
            else if (idx >= amountOfCharsToTheLeft)
            {
                if (this.Right == null)
                {
                    return (null, -1);
                }
                int subTreeCharIdx = idx - amountOfCharsToTheLeft;
                return this.Right.GetLeafContainingCharAtIndex(subTreeCharIdx);
            }
            else if (idx < amountOfCharsToTheLeft)
            {
                return this.Left.GetLeafContainingCharAtIndex(idx);
            }
            else
            {
                // Should be unreachable.
                throw new Exception("Unreachable in GetLeafContainingCharAtIndex, idx: " + idx);
            }
        }

        public int GetFirstCharIndexAtLineWithIndex(int lineIdx, int globalCharIdx = 0)
        {
            int amountOfCharsToTheLeft = this.Counts.Chars;
            int amountOfLinesToTheLeft = this.Counts.LineBreaks;

            if (lineIdx < 0)
            {
                return -1;
            }
            else if (this.IsLeaf())
            {
                int leafIdx = 0;
                while (lineIdx > 0)
                {
                    // Unable to find line line index that we are looking for inside this leaf.
                    // Most likely because the specified line doesn't exist (points to one row below
                    // existing text).
                    if (leafIdx >= this.BufferRef.Length)
                    {
                        return -1;
                    }

                    char c = this.BufferRef.Buffer.GetChar(this.BufferRef.StartIdx + leafIdx).Item1;
                    if (c == Rope.LINE_FEED)
                    {
                        lineIdx--;
                    }
                    leafIdx++;
                }
                return globalCharIdx + leafIdx;
            }
            else if (lineIdx > amountOfLinesToTheLeft && this.Right == null)
            {
                // Cursor is currently pointing behind all characters.
                return -1;
            }
            else if (lineIdx > amountOfLinesToTheLeft)
            {
                int subTreeLineIdx = lineIdx - amountOfLinesToTheLeft;
                return this.Right.GetFirstCharIndexAtLineWithIndex(subTreeLineIdx, globalCharIdx + amountOfCharsToTheLeft);
            }
            else if (lineIdx <= amountOfLinesToTheLeft)
            {
                return this.Left.GetFirstCharIndexAtLineWithIndex(lineIdx, globalCharIdx);
            }
            else
            {
                // Should be unreachable.
                throw new Exception("Unreachable in GetFirstCharIndexAtLineWithIndex, lineIdx: " + lineIdx + ", globalCharIdx: " + globalCharIdx);
            }
        }

        public int GetLineIndexForCharAtIndex(int globalCharIdx, int charIdx, int lineSum = 0)
        {
            if (globalCharIdx == 0)
            {
                return 0;
            }

            int amountOfCharsToTheLeft = this.Counts.Chars;

            if (this.IsLeaf())
            {
                foreach (var c in this.BufferRef.Get().Slice(0, charIdx))
                {
                    if (c == Rope.LINE_FEED)
                    {
                        lineSum++;
                    }
                }
                return lineSum;
            }
            else if (charIdx == amountOfCharsToTheLeft && this.Right == null)
            {
                return lineSum + this.Counts.LineBreaks;
            }
            else if (charIdx >= amountOfCharsToTheLeft)
            {
                int subTreeCharIdx = charIdx - amountOfCharsToTheLeft;
                return this.Right.GetLineIndexForCharAtIndex(globalCharIdx, subTreeCharIdx, lineSum + this.Counts.LineBreaks);
            }
            else if (charIdx < amountOfCharsToTheLeft)
            {
                return this.Left.GetLineIndexForCharAtIndex(globalCharIdx, charIdx, lineSum);
            }
            else
            {
                // Should be unreachable.
                throw new Exception("Unreachable in GetLineIndexForCharAtIndex, idx: " + charIdx);
            }
        }

        public (Node, int) GetLastLeaf()
        {
            if (this.IsLeaf())
            {
                return (this, this.BufferRef.Length);
            }
            else if (this.Right != null)
            {
                return this.Right.GetLastLeaf();
            }
            else if (this.Left != null)
            {
                return this.Left.GetLastLeaf();
            }
            else
            {
                // Should be unreachable.
                throw new Exception("Unreachable in GetLastLeaf");
            }
        }

        private void SplitBeforeCharAtIndexInLeaf(int idx)
        {
            if (!this.IsLeaf())
            {
                throw new Exception("Not leaf in `SplitBeforeCharWithIndexInLeaf`");
            }

            int oldStartIdx = this.BufferRef.StartIdx;
            int oldLength = this.BufferRef.Length;

            int leftStartIdx = oldStartIdx;
            int leftLength = idx;
            int rightStartIdx = leftStartIdx + leftLength;
            int rightLength = oldLength - leftLength;

            var leftBufferRef = new BufferReference(leftStartIdx, leftLength, this.BufferRef.Buffer);
            var rightBufferRef = new BufferReference(rightStartIdx, rightLength, this.BufferRef.Buffer);

            Node leftNode = new Node()
            {
                Parent = this,
                BufferRef = leftBufferRef,
            };
            Node rightNode = new Node()
            {
                Parent = this,
                BufferRef = rightBufferRef,
            };

            this.Left = leftNode;
            this.Right = rightNode;

            leftNode.Counts = CalculateCountsForChars(leftNode.BufferRef.Get());
            rightNode.Counts = CalculateCountsForChars(rightNode.BufferRef.Get());
            this.Counts = leftNode.Counts;

            this.BufferRef.Buffer.RefCount--;
            this.BufferRef = null;
        }
    }
}
