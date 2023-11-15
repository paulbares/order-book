namespace PricePointBook.DataStructure;

/// <summary>
/// A special implementation of Binary Heap that stores only unique elements by checking if an element exists when trying
/// to add it. It thus makes the time complexity of Add op. O(n)...
/// </summary>
public class BinaryHeapLong
{
    private const int GrowFactor = 2;

    private const int MinimumGrow = 4;

    private long[] _queue;

    /**
     * The number of elements in the priority queue.
     */
    private int _size;

    private readonly IComparer<long> _comparer;

    public BinaryHeapLong(int capacity, IComparer<long> comparer)
    {
        _comparer = comparer;
        _queue = new long[capacity];
    }

    public long Peek()
    {
        return _queue[0];
    }

    public void Add(long e)
    {
        var index = IndexOf(e); // O(n) !!
        if (index >= 0)
        {
            // Already exist, replace element with the current one.
            _queue[index] = e;
            return;
        }

        int i = _size;
        if (i >= _queue.Length)
        {
            Grow(i + 1);
        }

        SiftUp(i, e);
        _size = i + 1;
    }

    public bool Remove(long o)
    {
        int i = IndexOf(o);
        if (i == -1)
        {
            return false;
        }
        else
        {
            RemoveAt(i);
            return true;
        }
    }

    private int IndexOf(long o)
    {
        long[] es = _queue;
        for (int i = 0, n = _size; i < n; i++)
            if (_comparer.Compare(o, es[i]) == 0)
            {
                return i;
            }

        return -1;
    }

    private void RemoveAt(int i)
    {
        // assert i >= 0 && i < size;
        long[] es = _queue;
        int s = --_size;
        if (s == i)
        {
            es[i] = 0; // removed last element
        }
        else
        {
            long moved = es[s];
            es[s] = 0;
            SiftDown(i, moved);
            if (es[i] == moved)
            {
                SiftUp(i, moved);
            }
        }
    }

    private void SiftDown(int k, long x)
    {
        SiftDownUsingComparator(k, x, _queue, _size, _comparer);
    }


    private static void SiftDownUsingComparator(
        int k, long x, long[] es, int n, IComparer<long> cmp)
    {
        // assert n > 0;
        int half = n >>> 1;
        while (k < half)
        {
            int child = (k << 1) + 1;
            long c = es[child];
            int right = child + 1;
            if (right < n && cmp.Compare(c, es[right]) > 0)
            {
                c = es[child = right];
            }

            if (cmp.Compare(x, c) <= 0)
            {
                break;
            }

            es[k] = c;
            k = child;
        }

        es[k] = x;
    }

    private void SiftUp(int k, long x)
    {
        SiftUpUsingComparator(k, x, _queue, _comparer);
    }

    private static void SiftUpUsingComparator(
        int k, long x, long[] es, IComparer<long> comparer)
    {
        while (k > 0)
        {
            int parent = (k - 1) >>> 1;
            long e = es[parent];
            if (comparer.Compare(x, e) >= 0)
            {
                break;
            }

            es[k] = e;
            k = parent;
        }

        es[k] = x;
    }

    private void Grow(int minCapacity)
    {
        int newcapacity = GrowFactor * _queue.Length;

        // Allow the queue to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _nodes.Length overflowed thanks to the (uint) cast
        if ((uint)newcapacity > Array.MaxLength)
        {
            newcapacity = Array.MaxLength;
        }

        // Ensure minimum growth is respected.
        newcapacity = Math.Max(newcapacity, _queue.Length + MinimumGrow);

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < minCapacity)
        {
            newcapacity = minCapacity;
        }

        Array.Resize(ref _queue, newcapacity);
    }

    /// <summary>
    /// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    ///     / \
    ///    / ! \
    ///   /  !  \  NOT EFFICIENT, FOR TESTING PURPOSE !!!!!!!!!!!!!!!
    ///  /   !   \
    ///  ---------
    /// //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    /// Gets the elements in ascending order from the heap. It creates a copy of the heap before returning the result.
    /// </summary>
    public long[] GetOrderedElements()
    {
        var copy = new long[_size];
        Array.Copy(_queue, copy, _size);
        Array.Sort(copy, _comparer);
        return copy;
    }
}