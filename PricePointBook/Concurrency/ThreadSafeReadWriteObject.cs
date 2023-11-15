namespace PricePointBook.Concurrency;

/// <summary>
/// See http://concurrencyfreaks.blogspot.com/2013/12/left-right-classical-algorithm.html
///  </summary>
/// <typeparam name="T">The type of object to protect</typeparam>
public class ThreadSafeReadWriteObject<T>
{
    private readonly object _lockObj = new();

    private readonly T[] _instances;

    private int _readIndex; // 0 or 1

    private int _leftRight; // 0 or 1

    private readonly int[] _readIndicator = new int[2];

    public ThreadSafeReadWriteObject(Func<T> factory)
    {
        _instances = new T[] { factory.Invoke(), factory.Invoke() };
    }

    public void Read(Action<T> readAction)
    {
        int rIndex = Volatile.Read(ref _readIndex);
        Interlocked.Increment(ref _readIndicator[rIndex]); // arrive, being the read
        int index = Volatile.Read(ref _leftRight);
        T obj = _instances[index];
        // Wrap in try/finally to be sure to decrement the counter no matter what and end up in a good state.
        try
        {
            readAction.Invoke(obj); // Do some read stuff with obj
        }
        finally
        {
            Interlocked.Decrement(ref _readIndicator[rIndex]); // depart, read is over
        }
    }

    public void Write(Action<T> writeAction)
    {
        lock (_lockObj)
        {
            int index = Volatile.Read(ref _leftRight);
            int writeIndex = index == 0 ? 1 : 0;
            T obj = _instances[writeIndex];
            writeAction.Invoke(obj); // Do some write stuff with obj...
            Volatile.Write(ref _leftRight, writeIndex); // toggle. Future read with use the newly edited version

            int rIndex = Volatile.Read(ref _readIndex);
            int newRIndex = rIndex == 0 ? 1 : 0;
            var spinWait = new SpinWait();
            while (Volatile.Read(ref _readIndicator[newRIndex]) != 0)
            {
                spinWait.SpinOnce();
            }

            // Toggle readIndex
            Volatile.Write(ref _readIndex, newRIndex);

            while (Volatile.Read(ref _readIndicator[rIndex]) != 0)
            {
                spinWait.SpinOnce();
            }

            obj = _instances[index];
            writeAction.Invoke(obj); // Do some write stuff with obj...
        }
    }
}