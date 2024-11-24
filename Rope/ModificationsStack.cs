namespace Text
{
    internal class ModificationsStack
    {
        private readonly Stack<IModification> modifications = new Stack<IModification>();

        /// <summary>
        /// When one reverst/undos a change, temporarily store it in this stack.
        /// This can then be used to "ctrl + shift + z" to re-revert the change.
        /// This will be cleared when a new modification is made that isn't an undo.
        /// </summary>
        private readonly Stack<IModification> modificationsUndo = new Stack<IModification>();

        private readonly Action<IModification>? onPush;

        public ModificationsStack(Action<IModification>? onPush)
        {
            this.onPush = onPush;
        }

        public void Push(IModification modification, bool isUndo)
        {
            if (isUndo)
            {
                this.modificationsUndo.Push(modification);
            }
            else
            {
                this.modifications.Push(modification);
                this.modificationsUndo.Clear();
            }

            this.onPush?.Invoke(modification);
        }

        public IModification? Pop(bool isUndo)
        {
            IModification? modification;

            if (isUndo)
            {
                this.modificationsUndo.TryPop(out modification);
            }
            else
            {
                this.modifications.TryPop(out modification);
            }

            return modification;
        }
    }
}
