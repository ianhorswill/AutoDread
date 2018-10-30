using System.Diagnostics;

namespace Assets.Discourse
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public abstract class DiscourseTreeNode<T>
    {
        public readonly string Name;

        protected DiscourseTreeNode(string name)
        {
            Name = name;
        }

        public string Generate(T content, Discourse d)
        {
            while (IsUsable(content, d))
                NextIncrement(content, d);
            return d.Text;
        }
        
        /// <summary>
        /// True if the node would be able to generate an increment if activated
        /// Called only when node is inactive
        /// </summary>
        public abstract bool IsUsable(T content, Discourse d);

        /// <summary>
        /// Called before activating a node
        /// </summary>
        /// <param name="d">The discourse to which to add</param>
        public virtual void Activate(T content, Discourse d)
        {
        }

        /// <summary>
        /// Called before deactivating a node
        /// </summary>
        /// <param name="d">The discourse to which to add</param>
        public virtual void Deactivate(Discourse d)
        {
        }

        /// <summary>
        /// Ask the node to generate its next increment
        /// </summary>
        /// <param name="d">The discourse to which to add</param>
        public abstract void NextIncrement(T content, Discourse d);
    }
}
