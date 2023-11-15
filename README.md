# Order book

Note: I used DTO objects that represent the messages received from Binance. They are located in the 
namespace BinanceDto. IO and marshalling are important in real-world use case but out of scope here. Price and quantity values in 
my DTOs are "stringify" to mimic the messages sent by Binance. Parsing decimal numbers is a "real concern" that led to
the creation of dedicated library such as https://github.com/lemire/fast_double_parser (I used its Java version written by the same author to compare it 
with the most famous one: Javolution, and the former is way faster). I used a simple way to convert those strings into
double in my order book implementations but in reality this should be done beforehand with an efficient library.

To implement the order book, I tried four different implementations that all implements the interface `IOrderBook` with 
the basic operations. The first one is the most simple that helps me building the API, writing the tests and the benchmarks
from which I built three different implementations by iterating.

The first one is making use of a Red-Black Tree because of its properties that ensure efficient search, insertion, and 
deletion operations with a guaranteed logarithmic height (O(logN) average and worst case). 
I started to code my own implementation by discovered that there was already one implemented in the .NET core library so
I decided to give it a go (it also saved me a lot of time). The class in which it is used is OrderBook.

First impl: class `OrderBook`.

The second one is similar to the first one but uses a "trick" to not have to store two pair of double wrapped in 
KeyValuePair object (see SortedDictionary impl.). It packs the two doubles into a single long by making  the assumption
that each double multiplied by a certain scaling factor can fit into a 32-bits integer. See the class OrderBookPacked 
for more details. We could even refine this trick by knowing by advance the range of prices and quantities and store 
those doubles in smaller data types (<32 bits like byte, short, char....).
In addition, min and max values are tracked at each add/remove operations in order to provide in O(1) time the response
to “what are the best bid and offer?” instead of having to iterate over the tree to find them. 

Trick:

```csharp
public class Bits
{
    
    public static long Pack(int i1, int i2)
    {
        long packed1 = (long)i1 << 32;
        long packed2 = i2 & 0xFFFFFFFFL;
        return packed1 | packed2;
    }

    public static int Unpack1(long packed)
    {
        return (int)(packed >>> 32);
    }

    public static int Unpack2(long packed)
    {
        return (int)(packed & 0xFFFFFFFFL);
    }
}
```

Second impl: class `OrderBookPacked`.

For the third, I wanted to try an array based over a pointer-based data structure to see if it can leverage modern 
pipelining (SIMD), caching CPUs, branching... I created a custom Binary Heap that can store long only (it could be
generalized to any type though) and used the same trick presented above (packing of two integers into long). 
SPOILER ALERT: This implementation yields worst results compared to its predecessor due to the fact insertion time 
with this custom impl. of Binary Heap is O(N). See details in the class `BinaryHeapLong`.

Third impl: class `OrderBookHeap`.

Finally, I propose a thread safe version of the order book using the [Left-Right algorithm](http://concurrencyfreaks.blogspot.com/2013/12/left-right-classical-algorithm.html).
with the underlying structure being the one implemented for `OrderBookPacked`. This algorithm suits well for this use case
because with the Binance API, we would subscribe to a list of symbols and would receive updates via websocket channels. 
One per symbol => only one writer per symbol/multiple readers. Note for the reader: I started by implementing LR algorithm
in Java and ported it to C#. The Java impl. can be found at the end of this document.

Fourth impl: class `ThreadSafeOrderBookPacked`.

The benchmark results are presented at the end of this document. The fourth implementation has also be benchmarked to see 
the impact of the LR algo. usage and the impact of volatile operations.

## UnitTests

The unit tests can be found in PricePointBook.UnitTests directory. The main test classes are in `PricePointBook.UnitTests.OrderBook`
and inherit from the same base test class to test the different implementations.

Note: `ThreadSafeOrderBookPacked` should be stress tested to make sure it is really thread safe! 

## Benchmarks

Performed on my local machine with the following specs:

```
BenchmarkDotNet v0.13.10, macOS Sonoma 14.0 (23A344) [Darwin 23.0.0]
Apple M2 Max, 1 CPU, 12 logical and 12 physical cores
.NET SDK 7.0.403
  [Host]     : .NET 7.0.13 (7.0.1323.51816), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.13 (7.0.1323.51816), Arm64 RyuJIT AdvSIMD
```

The benchmarks use data generated in `PricePointBook.Benchmarks.DataGenerator`. I paid attention to generate a correct dataset 
by making sure that price updates and removals have actually an impact on the order book (they exist in the current 
state of the order book, it is not a simple Random.nextDouble()!).   

Different scenarios are tested in the benchmarks with different ratio of updates/removals. 

Benchmarks are using the following framework [benchmarkdotnet framework](https://benchmarkdotnet.org). Luckily for me,
it is quite similar to https://github.com/openjdk/jmh.

### Run

To run the benchmarks, go to PricePointBook.Benchmarks directory and run:
```
dotnet run -c Release
```

It runs the benchmarks for a single implementation. To change the implementation, (un)comment the relevant part of code in 
`MyBenchmarks.cs`

```csharp
private readonly IOrderBook _orderBook = new OrderBookPacked();
// private readonly IOrderBook _orderBook = new OrderBook();
// private readonly IOrderBook _orderBook = new OrderBookHeap();
// private readonly IOrderBook _orderBook = new ThreadSafeOrderBookPacked();
```

### Results - first implementation - class OrderBook

| Method     | NumberOfPrices | RatioNumberOfPricesToUpdate | RatioNumberOfPricesToRemove | Mean        | Error     | StdDev    | Gen0    | Gen1   | Allocated |
|----------- |--------------- |---------------------------- |---------------------------- |------------:|----------:|----------:|--------:|-------:|----------:|
| Initialize | 1024           | 0.25                        | 0.0625                      |   529.98 us |  1.370 us |  1.215 us | 13.6719 | 2.9297 |  115033 B |
| Updates    | 1024           | 0.25                        | 0.0625                      |    92.36 us |  0.733 us |  0.685 us |       - |      - |      40 B |
| Initialize | 1024           | 0.25                        | 0.125                       |   549.54 us |  1.513 us |  1.415 us | 13.6719 | 2.9297 |  115033 B |
| Updates    | 1024           | 0.25                        | 0.125                       |    90.19 us |  0.173 us |  0.162 us |       - |      - |      40 B |
| Initialize | 1024           | 0.5                         | 0.0625                      |   534.19 us |  0.996 us |  0.883 us | 13.6719 | 2.9297 |  115033 B |
| Updates    | 1024           | 0.5                         | 0.0625                      |    92.94 us |  0.162 us |  0.151 us |       - |      - |      40 B |
| Initialize | 1024           | 0.5                         | 0.125                       |   532.43 us |  1.095 us |  0.915 us | 13.6719 | 2.9297 |  115033 B |
| Updates    | 1024           | 0.5                         | 0.125                       |    90.07 us |  0.396 us |  0.370 us |       - |      - |      40 B |
| Initialize | 2048           | 0.25                        | 0.0625                      | 1,130.86 us | 13.151 us | 12.302 us | 27.3438 | 7.8125 |  229722 B |
| Updates    | 2048           | 0.25                        | 0.0625                      |    89.91 us |  0.270 us |  0.252 us |       - |      - |      40 B |
| Initialize | 2048           | 0.25                        | 0.125                       | 1,138.61 us | 13.984 us | 12.396 us | 27.3438 | 7.8125 |  229722 B |
| Updates    | 2048           | 0.25                        | 0.125                       |    91.73 us |  1.727 us |  1.615 us |       - |      - |      40 B |
| Initialize | 2048           | 0.5                         | 0.0625                      | 1,139.94 us |  2.854 us |  2.670 us | 27.3438 | 7.8125 |  229722 B |
| Updates    | 2048           | 0.5                         | 0.0625                      |    92.76 us |  0.270 us |  0.239 us |       - |      - |      40 B |
| Initialize | 2048           | 0.5                         | 0.125                       | 1,139.24 us |  1.443 us |  1.280 us | 27.3438 | 7.8125 |  229722 B |
| Updates    | 2048           | 0.5                         | 0.125                       |    90.17 us |  0.484 us |  0.429 us |       - |      - |      40 B |


### Results - second implementation - class OrderBookPacked

| Method     | NumberOfPrices | RatioNumberOfPricesToUpdate | RatioNumberOfPricesToRemove | Mean      | Error    | StdDev   | Gen0   | Allocated |
|----------- |--------------- |---------------------------- |---------------------------- |----------:|---------:|---------:|-------:|----------:|
| Initialize | 1024           | 0.25                        | 0.0625                      | 443.77 us | 3.617 us | 3.206 us | 0.9766 |   10144 B |
| Updates    | 1024           | 0.25                        | 0.0625                      |  94.47 us | 0.269 us | 0.252 us |      - |      40 B |
| Initialize | 1024           | 0.25                        | 0.125                       | 453.91 us | 1.615 us | 1.511 us | 0.9766 |   10144 B |
| Updates    | 1024           | 0.25                        | 0.125                       |  90.96 us | 0.496 us | 0.464 us |      - |      40 B |
| Initialize | 1024           | 0.5                         | 0.0625                      | 451.83 us | 1.030 us | 0.913 us | 0.9766 |   10144 B |
| Updates    | 1024           | 0.5                         | 0.0625                      |  90.98 us | 1.075 us | 0.953 us |      - |      40 B |
| Initialize | 1024           | 0.5                         | 0.125                       | 455.31 us | 1.861 us | 1.741 us | 0.9766 |   10144 B |
| Updates    | 1024           | 0.5                         | 0.125                       |  90.56 us | 0.618 us | 0.578 us |      - |      40 B |
| Initialize | 2048           | 0.25                        | 0.0625                      | 959.28 us | 7.654 us | 7.159 us | 1.9531 |   20033 B |
| Updates    | 2048           | 0.25                        | 0.0625                      |  90.44 us | 0.488 us | 0.457 us |      - |      40 B |
| Initialize | 2048           | 0.25                        | 0.125                       | 927.31 us | 5.665 us | 5.299 us | 1.9531 |   20033 B |
| Updates    | 2048           | 0.25                        | 0.125                       |  93.90 us | 0.627 us | 0.587 us |      - |      40 B |
| Initialize | 2048           | 0.5                         | 0.0625                      | 961.88 us | 8.152 us | 7.226 us | 1.9531 |   20033 B |
| Updates    | 2048           | 0.5                         | 0.0625                      |  90.45 us | 0.444 us | 0.415 us |      - |      40 B |
| Initialize | 2048           | 0.5                         | 0.125                       | 924.81 us | 7.633 us | 7.140 us | 1.9531 |   20033 B |
| Updates    | 2048           | 0.5                         | 0.125                       |  90.23 us | 1.296 us | 1.212 us |      - |      40 B |

### Results - third implementation- class OrderBookHeap

With initial capacity for the array = 2048

| Method     | NumberOfPrices | RatioNumberOfPricesToUpdate | RatioNumberOfPricesToRemove | Mean        | Error     | StdDev    | Gen0   | Allocated |
|----------- |--------------- |---------------------------- |---------------------------- |------------:|----------:|----------:|-------:|----------:|
| Initialize | 1024           | 0.25                        | 0.0625                      |   529.23 us |  5.437 us |  5.086 us | 3.9063 |   33065 B |
| Updates    | 1024           | 0.25                        | 0.0625                      |    94.18 us |  0.781 us |  0.731 us |      - |      40 B |
| Initialize | 1024           | 0.25                        | 0.125                       |   533.88 us |  6.759 us |  5.992 us | 3.9063 |   33065 B |
| Updates    | 1024           | 0.25                        | 0.125                       |    90.80 us |  0.533 us |  0.473 us |      - |      40 B |
| Initialize | 1024           | 0.5                         | 0.0625                      |   537.61 us |  3.713 us |  3.101 us | 3.9063 |   33065 B |
| Updates    | 1024           | 0.5                         | 0.0625                      |    93.62 us |  0.759 us |  0.710 us |      - |      40 B |
| Initialize | 1024           | 0.5                         | 0.125                       |   537.01 us |  2.030 us |  1.695 us | 3.9063 |   33065 B |
| Updates    | 1024           | 0.5                         | 0.125                       |    91.14 us |  0.574 us |  0.537 us |      - |      40 B |
| Initialize | 2048           | 0.25                        | 0.0625                      | 1,357.83 us | 12.899 us | 12.066 us | 3.9063 |   33066 B |
| Updates    | 2048           | 0.25                        | 0.0625                      |    90.31 us |  0.695 us |  0.650 us |      - |      40 B |
| Initialize | 2048           | 0.25                        | 0.125                       | 1,363.74 us |  8.213 us |  7.280 us | 3.9063 |   33066 B |
| Updates    | 2048           | 0.25                        | 0.125                       |    91.07 us |  0.781 us |  0.731 us |      - |      40 B |
| Initialize | 2048           | 0.5                         | 0.0625                      | 1,354.10 us | 11.892 us | 10.542 us | 3.9063 |   33066 B |
| Updates    | 2048           | 0.5                         | 0.0625                      |    93.93 us |  0.310 us |  0.275 us |      - |      40 B |
| Initialize | 2048           | 0.5                         | 0.125                       | 1,368.55 us | 10.830 us | 10.131 us | 3.9063 |   33066 B |
| Updates    | 2048           | 0.5                         | 0.125                       |    91.11 us |  0.789 us |  0.738 us |      - |      40 B |

### Results - fourth implementation - class ThreadSafeOrderBookPacked

| Method     | NumberOfPrices | RatioNumberOfPricesToUpdate | RatioNumberOfPricesToRemove | Mean       | Error    | StdDev   | Gen0   | Allocated |
|----------- |--------------- |---------------------------- |---------------------------- |-----------:|---------:|---------:|-------:|----------:|
| Initialize | 1024           | 0.25                        | 0.0625                      |   891.2 us |  3.06 us |  2.71 us | 1.9531 |   21025 B |
| Updates    | 1024           | 0.25                        | 0.0625                      |   128.4 us |  0.20 us |  0.17 us |      - |      40 B |
| Initialize | 1024           | 0.25                        | 0.125                       |   887.6 us |  1.90 us |  1.69 us | 1.9531 |   21025 B |
| Updates    | 1024           | 0.25                        | 0.125                       |   128.1 us |  0.21 us |  0.19 us |      - |      40 B |
| Initialize | 1024           | 0.5                         | 0.0625                      |   889.8 us |  2.13 us |  1.78 us | 1.9531 |   21025 B |
| Updates    | 1024           | 0.5                         | 0.0625                      |   128.0 us |  0.32 us |  0.30 us |      - |      40 B |
| Initialize | 1024           | 0.5                         | 0.125                       |   888.3 us |  1.92 us |  1.79 us | 1.9531 |   21025 B |
| Updates    | 1024           | 0.5                         | 0.125                       |   128.0 us |  0.17 us |  0.15 us |      - |      40 B |
| Initialize | 2048           | 0.25                        | 0.0625                      | 1,813.9 us |  7.00 us |  6.55 us | 3.9063 |   40802 B |
| Updates    | 2048           | 0.25                        | 0.0625                      |   128.1 us |  0.22 us |  0.20 us |      - |      40 B |
| Initialize | 2048           | 0.25                        | 0.125                       | 1,818.3 us | 16.67 us | 13.92 us | 3.9063 |   40802 B |
| Updates    | 2048           | 0.25                        | 0.125                       |   126.9 us |  0.32 us |  0.27 us |      - |      40 B |
| Initialize | 2048           | 0.5                         | 0.0625                      | 1,853.2 us | 22.80 us | 20.22 us | 3.9063 |   40802 B |
| Updates    | 2048           | 0.5                         | 0.0625                      |   127.8 us |  1.03 us |  0.91 us |      - |      40 B |
| Initialize | 2048           | 0.5                         | 0.125                       | 1,844.9 us |  8.56 us |  7.59 us | 3.9063 |   40802 B |
| Updates    | 2048           | 0.5                         | 0.125                       |   128.4 us |  1.29 us |  1.20 us |      - |      40 B |

### Interpretation

Comparison between the first three implementations (impl 1 = OrderBook, impl 2 = OrderBookPacked, impl 3 = OrderBookHeap):

We can see that the best implementation is 2. It is 15% faster than 1 for the Initialize phase and as fast as 1 for the 
Updates phase. 3 is out of the picture due to its run-time complexity. Array-based data structure and all the benefits that 
comes with does not change anything here in a positive way. What is noticeable also between 1 and 2 is the amount of memory 
used. 2 uses less memory (usage of long instead of KeyValuePair<double, double>) as a real advantage + less 
transient memory generated. No Gen1 for 2. And for Gen0, about 13 times less for the Initialize phase. I am not sure why 
I am not seeing for the Updates phase. 

Comparison of impl 2 (OrderBookPacked) and impl 4 (ThreadSafeOrderBookPacked): Without surprise, 4 is twice slower as 2 
and consumes the double of memory.

## Java implementation of LR algorithm

```java
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.locks.Lock;
import java.util.concurrent.locks.ReentrantLock;
import java.util.function.Consumer;
import java.util.function.Supplier;

/**
 * http://concurrencyfreaks.blogspot.com/2013/12/left-right-classical-algorithm.html
 */
public class Main {

  static class ThreadSafeObject<T> {

    final Supplier<T> factory;

    final Object[] instances;

    AtomicInteger readIndex = new AtomicInteger(0); // 0 or 1

    AtomicInteger[] readIndicator = new AtomicInteger[2];

    AtomicInteger leftRight = new AtomicInteger(0); // 0 or 1

    Lock lock = new ReentrantLock();

    ThreadSafeObject(Supplier<T> factory) {
      this.factory = factory;
      this.instances = new Object[]{factory.get(), factory.get()};
    }

    public void read(Consumer<Object> action) {

    }

    public void write(Consumer<Object> action) {

    }
  }

  Object[] objects = new Object[2];

  AtomicInteger readIndex = new AtomicInteger(0); // 0 or 1

  AtomicInteger[] readIndicator = new AtomicInteger[2];

  AtomicInteger leftRight = new AtomicInteger(0); // 0 or 1

  Lock lock = new ReentrantLock();

  public void read(Consumer<Object> action) {
    int rIndex = readIndex.get();
    readIndicator[rIndex].incrementAndGet(); // arrive, being the read
    int index = leftRight.get();
    Object obj = objects[index];
    try {
      action.accept(obj); // Do some read stuff with obj
    } finally {
      readIndicator[rIndex].decrementAndGet(); // depart, read is over
    }
  }

  public void write(Consumer<Object> action) {
    lock.lock();
    try {
      int index = leftRight.get();
      int writeIndex = index == 0 ? 1 : 0;
      Object obj = objects[writeIndex];
      action.accept(obj); // Do some write stuff with obj...
      leftRight.set(writeIndex); // toggle. Future read with use the newly edited version

      int rIndex = readIndex.get();
      int newRIndex = rIndex == 0 ? 1 : 0;
      while (readIndicator[newRIndex].get() != 0) {
        Thread.yield();
      }

      // Toggle readIndex
      readIndex.set(newRIndex);

      while (readIndicator[rIndex].get() != 0) {
        Thread.yield();
      }

      obj = objects[index];
      action.accept(obj); // Do some write stuff with obj...
    } finally {
      lock.unlock();
    }
  }
}
```