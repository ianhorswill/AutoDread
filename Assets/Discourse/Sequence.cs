using System.Linq;

namespace Assets.Discourse
{
    /// <summary>
    /// Selector that runs its children left to right until there are none left
    /// </summary>
    public class Sequence<T> : Selector<T>
    {
        public Sequence(string name, DiscourseTreeNode<T>[] children) : base(name, children) { }

        public override DiscourseTreeNode<T> SelectNewChild(T content, Discourse d)
        {
            return Children.First(c => c.IsUsable(content, d));
        }
    }
}
