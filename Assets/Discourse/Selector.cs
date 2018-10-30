using System.Linq;

namespace Assets.Discourse
{
    /// <summary>
    /// A node that dispatches to one of its children
    /// </summary>
    public abstract class Selector<T> : DiscourseTreeNode<T>
    {
        /// <summary>
        /// The child nodes of this selector
        /// </summary>
        protected readonly DiscourseTreeNode<T>[] Children;
        /// <summary>
        /// The most recently active child
        /// </summary>
        private DiscourseTreeNode<T> activeChild;

        protected Selector(string name, DiscourseTreeNode<T>[] children) : base(name)
        {
            Children = children;
        }

        public override bool IsUsable(T content, Discourse d)
        {
            return Children.Any(c => c.IsUsable(content, d));
        }

        public override void NextIncrement(T content, Discourse d)
        {
            var selected = SelectChild(content, d);

            if (selected != activeChild)
            {
                activeChild.Deactivate(d);
                selected.Activate(content, d);
                activeChild = selected;
            }

            activeChild.NextIncrement(content, d);
        }

        public override void Deactivate(Discourse d)
        {
            base.Deactivate(d);

            // Deactivate our child, if any
            if (activeChild != null)
            {
                activeChild.Deactivate(d);
                activeChild = null;
            }
        }

        public virtual DiscourseTreeNode<T> SelectChild(T content, Discourse d)
        {
            if (activeChild != null && activeChild.IsUsable(content, d))
                return activeChild;
            return SelectNewChild(content, d);
        }

        public abstract DiscourseTreeNode<T> SelectNewChild(T content, Discourse d);
    }
}
