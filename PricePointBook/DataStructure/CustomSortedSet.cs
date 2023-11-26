using System.Collections;
using System.Numerics;
using PricePointBook.OrderBook;

namespace PricePointBook.DataStructure;

/// <summary>
/// This a minimum viable copy of <see cref="SortedSet{T}"/>; with two small changes: when trying to add an element that
/// already exist in the set (in the sense of comparer.Compare(a, b) == 0), the element is replaced by the one passed in
/// argument. It is made specifically to work with the "packed doubles". See <see cref="OrderBookPacked"/>;
/// <br/>
/// See:
/// <code>
/// internal virtual bool AddIfNotPresent(T item) {
///     current.Item = item; // TODO $$$$$$$$$$$$$ HACK. Overwrite the current value. 
/// }
/// </code>
///
/// The other difference is that min and max values are tracked at each add/remove operations in order to provide in O(1)
/// time the response to “what are the best bid and offer?”
/// </summary>
/// <typeparam name="T"></typeparam>
public class CustomSortedSet<T> : IEnumerable<T>
{
    private IComparer<T> comparer;

    private Node? root;

    private int count;

    public T? MinValue;
    
    private bool _hasMinValueBeenInitialized = false;

    public T? MaxValue;
    
    private bool _hasMaxValueBeenInitialized = false;

    public CustomSortedSet(IComparer<T> comparer)
    {
        this.comparer = comparer;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Remove(T item) => DoRemove(item); // Hack so the implementation can be made virtual

    internal virtual bool DoRemove(T item)
    {
        if (root == null)
        {
            return false;
        }

        // Search for a node and then find its successor.
        // Then copy the item from the successor to the matching node, and delete the successor.
        // If a node doesn't have a successor, we can replace it with its left child (if not empty),
        // or delete the matching node.
        //
        // In top-down implementation, it is important to make sure the node to be deleted is not a 2-node.
        // Following code will make sure the node on the path is not a 2-node.

        Node? current = root;
        Node? parent = null;
        Node? grandParent = null;
        Node? match = null;
        Node? parentOfMatch = null;
        bool foundMatch = false;
        while (current != null)
        {
            if (current.Is2Node)
            {
                // Fix up 2-node
                if (parent == null)
                {
                    // `current` is the root. Mark it red.
                    current.ColorRed();
                }
                else
                {
                    Node sibling = parent.GetSibling(current);
                    if (sibling.IsRed)
                    {
                        // If parent is a 3-node, flip the orientation of the red link.
                        // We can achieve this by a single rotation.
                        // This case is converted to one of the other cases below.
                        if (parent.Right == sibling)
                        {
                            parent.RotateLeft();
                        }
                        else
                        {
                            parent.RotateRight();
                        }

                        parent.ColorRed();
                        sibling.ColorBlack(); // The red parent can't have black children.
                        // `sibling` becomes the child of `grandParent` or `root` after rotation. Update the link from that node.
                        ReplaceChildOrRoot(grandParent, parent, sibling);
                        // `sibling` will become the grandparent of `current`.
                        grandParent = sibling;
                        if (parent == match)
                        {
                            parentOfMatch = sibling;
                        }

                        sibling = parent.GetSibling(current);
                    }

                    if (sibling.Is2Node)
                    {
                        parent.Merge2Nodes();
                    }
                    else
                    {
                        // `current` is a 2-node and `sibling` is either a 3-node or a 4-node.
                        // We can change the color of `current` to red by some rotation.
                        Node newGrandParent = parent.Rotate(parent.GetRotation(current, sibling))!;

                        newGrandParent.Color = parent.Color;
                        parent.ColorBlack();
                        current.ColorRed();
 
                        ReplaceChildOrRoot(grandParent, parent, newGrandParent);
                        if (parent == match)
                        {
                            parentOfMatch = newGrandParent;
                        }
                    }
                }
            }

            // We don't need to compare after we find the match.
            int order = foundMatch ? -1 : comparer.Compare(item, current.Item);
            if (order == 0)
            {
                // Save the matching node.
                foundMatch = true;
                match = current;

                // HACK update MIN AND MAX 
                if (comparer.Compare(MinValue, current.Item) == 0)
                {
                    // The min is being removed.
                    MinValue = parent.Item;
                }

                if (comparer.Compare(MaxValue, current.Item) == 0)
                {
                    // The max is being removed.
                    MaxValue = parent.Item;
                }

                parentOfMatch = parent;
            }

            grandParent = parent;
            parent = current;
            // If we found a match, continue the search in the right sub-tree.
            current = order < 0 ? current.Left : current.Right;
        }

        // Move successor to the matching node position and replace links.
        if (match != null)
        {
            ReplaceNode(match, parentOfMatch!, parent!, grandParent!);
            --count;
        }

        root?.ColorBlack();
        return foundMatch;
    }

    /// <summary>
    /// Replaces the matching node with its successor.
    /// </summary>
    private void ReplaceNode(Node match, Node parentOfMatch, Node successor, Node parentOfSuccessor)
    {
        if (successor == match)
        {
            // This node has no successor. This can only happen if the right child of the match is null.
            successor = match.Left!;
        }
        else
        {
            successor.Right?.ColorBlack();

            if (parentOfSuccessor != match)
            {
                // Detach the successor from its parent and set its right child.
                parentOfSuccessor.Left = successor.Right;
                successor.Right = match.Right;
            }

            successor.Left = match.Left;
        }

        if (successor != null)
        {
            successor.Color = match.Color;
        }

        ReplaceChildOrRoot(parentOfMatch, match, successor!);
    }

    public bool Add(T item) => AddIfNotPresent(item); // Hack so the implementation can be made virtual

    internal enum NodeColor : byte
    {
        Black,
        Red,
    }

    internal enum TreeRotation : byte
    {
        Left,
        LeftRight,
        Right,
        RightLeft,
    }

    internal virtual bool AddIfNotPresent(T item)
    {
        if (!_hasMinValueBeenInitialized || comparer.Compare(item, MinValue) < 0)
        {
            MinValue = item;
            _hasMinValueBeenInitialized = true;
        }

        if (!_hasMaxValueBeenInitialized || comparer.Compare(item, MaxValue) > 0)
        {
            MaxValue = item;
            _hasMaxValueBeenInitialized = true;
        }

        if (root == null)
        {
            // The tree is empty and this is the first item.
            root = new Node(item, NodeColor.Black);
            count = 1;
            return true;
        }

        // Search for a node at bottom to insert the new node.
        // If we can guarantee the node we found is not a 4-node, it would be easy to do insertion.
        // We split 4-nodes along the search path.
        Node? current = root;
        Node? parent = null;
        Node? grandParent = null;
        Node? greatGrandParent = null;

        int order = 0;
        while (current != null)
        {
            order = comparer.Compare(item, current.Item);
            if (order == 0)
            {
                // We could have changed root node to red during the search process.
                // We need to set it to black before we return.
                root.ColorBlack();
                current.Item = item; // TODO $$$$$$$$$$$$$ HACK. Overwrite the current value. 
                return false;
            }

            // Split a 4-node into two 2-nodes.
            if (current.Is4Node)
            {
                current.Split4Node();
                // We could have introduced two consecutive red nodes after split. Fix that by rotation.
                if (Node.IsNonNullRed(parent))
                {
                    InsertionBalance(current, ref parent!, grandParent!, greatGrandParent!);
                }
            }

            greatGrandParent = grandParent;
            grandParent = parent;
            parent = current;
            current = (order < 0) ? current.Left : current.Right;
        }

        // We're ready to insert the new node.
        Node node = new Node(item, NodeColor.Red);
        if (order > 0)
        {
            parent.Right = node;
        }
        else
        {
            parent.Left = node;
        }

        // The new node will be red, so we will need to adjust colors if its parent is also red.
        if (parent.IsRed)
        {
            InsertionBalance(node, ref parent!, grandParent!, greatGrandParent!);
        }

        // The root node is always black.
        root.ColorBlack();
        ++count;
        return true;
    }

    // After calling InsertionBalance, we need to make sure `current` and `parent` are up-to-date.
    // It doesn't matter if we keep `grandParent` and `greatGrandParent` up-to-date, because we won't
    // need to split again in the next node.
    // By the time we need to split again, everything will be correctly set.
    private void InsertionBalance(Node current, ref Node parent, Node grandParent, Node greatGrandParent)
    {
        bool parentIsOnRight = grandParent.Right == parent;
        bool currentIsOnRight = parent.Right == current;

        Node newChildOfGreatGrandParent;
        if (parentIsOnRight == currentIsOnRight)
        {
            // Same orientation, single rotation
            newChildOfGreatGrandParent = currentIsOnRight ? grandParent.RotateLeft() : grandParent.RotateRight();
        }
        else
        {
            // Different orientation, double rotation
            newChildOfGreatGrandParent =
                currentIsOnRight ? grandParent.RotateLeftRight() : grandParent.RotateRightLeft();
            // Current node now becomes the child of `greatGrandParent`
            parent = greatGrandParent;
        }

        // `grandParent` will become a child of either `parent` of `current`.
        grandParent.ColorRed();
        newChildOfGreatGrandParent.ColorBlack();

        ReplaceChildOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
    }

    /// <summary>
    /// Replaces the child of a parent node, or replaces the root if the parent is <c>null</c>.
    /// </summary>
    /// <param name="parent">The (possibly <c>null</c>) parent.</param>
    /// <param name="child">The child node to replace.</param>
    /// <param name="newChild">The node to replace <paramref name="child"/> with.</param>
    private void ReplaceChildOrRoot(Node? parent, Node child, Node newChild)
    {
        if (parent != null)
        {
            parent.ReplaceChild(child, newChild);
        }
        else
        {
            root = newChild;
        }
    }

    internal virtual bool IsWithinRange(T item) => true;

    // Used for set checking operations (using enumerables) that rely on counting
    public static int Log2(int value) => BitOperations.Log2((uint)value);

    internal virtual int TotalCount()
    {
        return count;
    }

    internal sealed class Node
    {
        public Node(T item, NodeColor color)
        {
            Item = item;
            Color = color;
        }

        public static bool IsNonNullBlack(Node? node) => node != null && node.IsBlack;

        public static bool IsNonNullRed(Node? node) => node != null && node.IsRed;

        public static bool IsNullOrBlack(Node? node) => node == null || node.IsBlack;

        public T Item { get; set; }

        public Node? Left { get; set; }

        public Node? Right { get; set; }

        public NodeColor Color { get; set; }

        public bool IsBlack => Color == NodeColor.Black;

        public bool IsRed => Color == NodeColor.Red;

        public bool Is2Node => IsBlack && IsNullOrBlack(Left) && IsNullOrBlack(Right);

        public bool Is4Node => IsNonNullRed(Left) && IsNonNullRed(Right);

        public void ColorBlack() => Color = NodeColor.Black;

        public void ColorRed() => Color = NodeColor.Red;

        public Node DeepClone(int count)
        {
            Node newRoot = ShallowClone();

            var pendingNodes = new Stack<(Node source, Node target)>(2 * Log2(count) + 2);
            pendingNodes.Push((this, newRoot));

            while (pendingNodes.TryPop(out var next))
            {
                Node clonedNode;

                if (next.source.Left is Node left)
                {
                    clonedNode = left.ShallowClone();
                    next.target.Left = clonedNode;
                    pendingNodes.Push((left, clonedNode));
                }

                if (next.source.Right is Node right)
                {
                    clonedNode = right.ShallowClone();
                    next.target.Right = clonedNode;
                    pendingNodes.Push((right, clonedNode));
                }
            }

            return newRoot;
        }

        /// <summary>
        /// Gets the rotation this node should undergo during a removal.
        /// </summary>
        public TreeRotation GetRotation(Node current, Node sibling)
        {
            bool currentIsLeftChild = Left == current;
            return IsNonNullRed(sibling.Left)
                ? (currentIsLeftChild ? TreeRotation.RightLeft : TreeRotation.Right)
                : (currentIsLeftChild ? TreeRotation.Left : TreeRotation.LeftRight);
        }

        /// <summary>
        /// Gets the sibling of one of this node's children.
        /// </summary>
        public Node GetSibling(Node node)
        {
            return node == Left ? Right! : Left!;
        }

        public Node ShallowClone() => new Node(Item, Color);

        public void Split4Node()
        {
            ColorRed();
            Left.ColorBlack();
            Right.ColorBlack();
        }

        /// <summary>
        /// Does a rotation on this tree. May change the color of a grandchild from red to black.
        /// </summary>
        public Node? Rotate(TreeRotation rotation)
        {
            Node removeRed;
            switch (rotation)
            {
                case TreeRotation.Right:
                    removeRed = Left!.Left!;
                    removeRed.ColorBlack();
                    return RotateRight();
                case TreeRotation.Left:
                    removeRed = Right!.Right!;
                    removeRed.ColorBlack();
                    return RotateLeft();
                case TreeRotation.RightLeft:
                    return RotateRightLeft();
                case TreeRotation.LeftRight:
                    return RotateLeftRight();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Does a left rotation on this tree, making this node the new left child of the current right child.
        /// </summary>
        public Node RotateLeft()
        {
            Node child = Right!;
            Right = child.Left;
            child.Left = this;
            return child;
        }

        /// <summary>
        /// Does a left-right rotation on this tree. The left child is rotated left, then this node is rotated right.
        /// </summary>
        public Node RotateLeftRight()
        {
            Node child = Left!;
            Node grandChild = child.Right!;

            Left = grandChild.Right;
            grandChild.Right = this;
            child.Right = grandChild.Left;
            grandChild.Left = child;
            return grandChild;
        }

        /// <summary>
        /// Does a right rotation on this tree, making this node the new right child of the current left child.
        /// </summary>
        public Node RotateRight()
        {
            Node child = Left!;
            Left = child.Right;
            child.Right = this;
            return child;
        }

        /// <summary>
        /// Does a right-left rotation on this tree. The right child is rotated right, then this node is rotated left.
        /// </summary>
        public Node RotateRightLeft()
        {
            Node child = Right!;
            Node grandChild = child.Left!;

            Right = grandChild.Left;
            grandChild.Left = this;
            child.Left = grandChild.Right;
            grandChild.Right = child;
            return grandChild;
        }

        /// <summary>
        /// Combines two 2-nodes into a 4-node.
        /// </summary>
        public void Merge2Nodes()
        {
            // Combine two 2-nodes into a 4-node.
            ColorBlack();
            Left.ColorRed();
            Right.ColorRed();
        }

        /// <summary>
        /// Replaces a child of this node with a new node.
        /// </summary>
        /// <param name="child">The child to replace.</param>
        /// <param name="newChild">The node to replace <paramref name="child"/> with.</param>
        public void ReplaceChild(Node child, Node newChild)
        {
            if (Left == child)
            {
                Left = newChild;
            }
            else
            {
                Right = newChild;
            }
        }
    }

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly CustomSortedSet<T> _tree;
        private readonly int _version;

        private readonly Stack<Node> _stack;
        private Node? _current;

        private readonly bool _reverse;

        internal Enumerator(CustomSortedSet<T> set)
            : this(set, reverse: false)
        {
        }

        internal Enumerator(CustomSortedSet<T> set, bool reverse)
        {
            _tree = set;

            // 2 log(n + 1) is the maximum height.
            _stack = new Stack<Node>(2 * (int)Log2(set.TotalCount() + 1));
            _current = null;
            _reverse = reverse;

            Initialize();
        }

        private void Initialize()
        {
            _current = null;
            Node? node = _tree.root;
            Node? next, other;
            while (node != null)
            {
                next = (_reverse ? node.Right : node.Left);
                other = (_reverse ? node.Left : node.Right);
                if (_tree.IsWithinRange(node.Item))
                {
                    _stack.Push(node);
                    node = next;
                }
                else if (next == null || !_tree.IsWithinRange(next.Item))
                {
                    node = other;
                }
                else
                {
                    node = next;
                }
            }
        }

        public bool MoveNext()
        {
            if (_stack.Count == 0)
            {
                _current = null;
                return false;
            }

            _current = _stack.Pop();
            Node? node = (_reverse ? _current.Left : _current.Right);
            Node? next, other;
            while (node != null)
            {
                next = (_reverse ? node.Right : node.Left);
                other = (_reverse ? node.Left : node.Right);
                if (_tree.IsWithinRange(node.Item))
                {
                    _stack.Push(node);
                    node = next;
                }
                else if (other == null || !_tree.IsWithinRange(other.Item))
                {
                    node = next;
                }
                else
                {
                    node = other;
                }
            }

            return true;
        }

        public void Dispose()
        {
        }

        public T Current
        {
            get
            {
                if (_current != null)
                {
                    return _current.Item;
                }

                return default(T)!; // Should only happen when accessing Current is undefined behavior
            }
        }

        object? IEnumerator.Current
        {
            get { return _current.Item; }
        }

        internal bool NotStartedOrEnded => _current == null;

        internal void Reset()
        {
            _stack.Clear();
            Initialize();
        }

        void IEnumerator.Reset() => Reset();
    }
}