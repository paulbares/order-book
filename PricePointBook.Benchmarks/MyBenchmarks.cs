using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using PricePointBook.BinanceDto;
using PricePointBook.OrderBook;

namespace PricePointBook.Benchmarks
{
    /// <summary>
    /// Use MemoryDiagnoser to see memory allocation and GC activity. As stated in the <a href="https://adamsitnik.com/the-new-Memory-Diagnoser/">doc</a>:
    /// 
    /// The Gen X column contains the number of Gen X collections per 1 000 Operations. If the value is equal 1, then it
    /// means that GC collects memory once per one thousand of benchmark invocations in generation X. BenchmarkDotNet is
    /// using some heuristic when running benchmarks, so the number of invocations can be different for different runs.
    /// Scaling makes the results comparable.
    /// - in the Gen column means that no garbage collection was performed.
    /// - in the Allocated column means that no managed memory was allocated. Depending on the version of BenchmarkDotNet
    /// If Gen X column is not present, then it means that no garbage collection was performed for generation X. If none
    /// of your benchmarks induces the GC, the Gen columns are not present.
    /// </summary>
    [MemoryDiagnoser(true)]
    public class OrderBookPerf
    {
        public const int NumberOfUpdates = 10_000;

        [Params(1024, 2048)] public int NumberOfPrices;

        [Params(0.25d, 0.5d)] public double RatioNumberOfPricesToUpdate;

        [Params(0.0625d, 0.125d)] public double RatioNumberOfPricesToRemove;

        private SnapshotEventDto snapshotEventDto;

        private IList<UpdateEventDto> updateEvents;

        // private readonly IOrderBook _orderBook = new OrderBookPacked();
        // private readonly IOrderBook _orderBook = new OrderBook();
        // private readonly IOrderBook _orderBook = new OrderBookHeap();
        private readonly IOrderBook _orderBook = new ThreadSafeOrderBookPacked();

        [GlobalSetup]
        public void GlobalSetup()
        {
            var dataGenerator = new DataGenerator(NumberOfPrices, 42);
            snapshotEventDto = dataGenerator.GenerateSnapshotEvent();
            updateEvents =
                dataGenerator.GenerateUpdateEvents("abc",
                    NumberOfUpdates,
                    Convert.ToInt32(NumberOfPrices * RatioNumberOfPricesToUpdate),
                    Convert.ToInt32(NumberOfPrices * RatioNumberOfPricesToRemove));
        }

        [Benchmark]
        public void Initialize()
        {
            _orderBook.Initialize("abc", snapshotEventDto); // TODO check for dead code elimination
            _orderBook.Clear("abc");
        }

        [Benchmark]
        public bool Updates()
        {
            var result = false;
            foreach (var u in updateEvents)
            {
                result ^= _orderBook.Update(u);
            }

            return result; // return sthg to prevent dead code elimination
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<OrderBookPerf>();
        }
    }
}