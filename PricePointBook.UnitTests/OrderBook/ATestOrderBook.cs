using PricePointBook.OrderBook;

namespace PricePointBook.UnitTests.OrderBook
{
    [TestClass]
    public abstract class ATestOrderBook
    {
        public abstract IOrderBook CreateOrderBook();
        
        [TestMethod]
        public void TestUpdates()
        {
            var snapshotEventDto = TestUtils.SnapshotEvent();
            IOrderBook orderBook = CreateOrderBook();
            orderBook.Initialize(TestUtils.Symbol, snapshotEventDto);

            AssertBidsInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0030d, 99d),
                Tuple.Create(0.0028d, 9.3d),
                Tuple.Create(0.0026d, 2.3d),
                Tuple.Create(0.0024d, 14.7d),
                Tuple.Create(0.0022d, 6.4d),
                Tuple.Create(0.0020d, 9.7d),
            });
            AssertAsksInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0024d, 14.9d),
                Tuple.Create(0.0026d, 3.6d),
                Tuple.Create(0.0028d, 1.0d),
            });
            Assert.AreEqual(0.0030d, orderBook.GetBestBidPrice(TestUtils.Symbol));
            Assert.AreEqual(0.0024d, orderBook.GetBestAskPrice(TestUtils.Symbol));

            orderBook.Update(TestUtils.Update1());
            AssertBidsInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0030d, 99d),
                Tuple.Create(0.0028d, 9.3d),
                Tuple.Create(0.0026d, 2.3d),
                Tuple.Create(0.0024d, 10d),
                Tuple.Create(0.0022d, 6.4d),
                Tuple.Create(0.0020d, 9.7d),
            });
            AssertAsksInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0024d, 14.9d),
                Tuple.Create(0.0026d, 100d),
                Tuple.Create(0.0028d, 1.0d),
            });
            Assert.AreEqual(0.0030d, orderBook.GetBestBidPrice(TestUtils.Symbol));
            Assert.AreEqual(0.0024d, orderBook.GetBestAskPrice(TestUtils.Symbol));
            
            // Should remove one element because qty = 0
            orderBook.Update(TestUtils.Update2());
            AssertBidsInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0030d, 99d),
                Tuple.Create(0.0028d, 9.3d),
                Tuple.Create(0.0026d, 2.3d),
                Tuple.Create(0.0024d, 8d),
                Tuple.Create(0.0022d, 6.4d),
                Tuple.Create(0.0020d, 9.7d),
            });
            AssertAsksInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0024d, 14.9d),
                Tuple.Create(0.0026d, 100d),
            });  
            Assert.AreEqual(0.0030d, orderBook.GetBestBidPrice(TestUtils.Symbol));
            Assert.AreEqual(0.0024d, orderBook.GetBestAskPrice(TestUtils.Symbol));
            
            // Should remove one element because qty = 0
            orderBook.Update(TestUtils.Update3());
            AssertBidsInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0028d, 9.3d),
                Tuple.Create(0.0026d, 2.3d),
                Tuple.Create(0.0024d, 8d),
                Tuple.Create(0.0022d, 6.4d),
                Tuple.Create(0.0020d, 9.7d),
            });
            AssertAsksInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0024d, 14.9d),
                Tuple.Create(0.0026d, 15d),
                Tuple.Create(0.0027d, 5d),
            });  
            Assert.AreEqual(0.0028d, orderBook.GetBestBidPrice(TestUtils.Symbol));
            Assert.AreEqual(0.0024d, orderBook.GetBestAskPrice(TestUtils.Symbol));
            
            // Should remove one element because qty = 0
            orderBook.Update(TestUtils.Update4());
            AssertBidsInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0028d, 9.3d),
                Tuple.Create(0.0026d, 2.3d),
                Tuple.Create(0.0025d, 100d),
                Tuple.Create(0.0024d, 8d),
                Tuple.Create(0.0022d, 6.4d),
                Tuple.Create(0.0020d, 9.7d),
            });
            AssertAsksInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0024d, 14.9d),
                Tuple.Create(0.0027d, 5d),
            });
            Assert.AreEqual(0.0028d, orderBook.GetBestBidPrice(TestUtils.Symbol));
            Assert.AreEqual(0.0024d, orderBook.GetBestAskPrice(TestUtils.Symbol));
            
            // Should remove one element because qty = 0
            orderBook.Update(TestUtils.Update5());
            AssertBidsInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0028d, 9.3d),
                Tuple.Create(0.0026d, 2.3d),
                Tuple.Create(0.0024d, 8d),
                Tuple.Create(0.0022d, 6.4d),
                Tuple.Create(0.0020d, 9.7d),
            });
            AssertAsksInOrderBook(orderBook, new List<Tuple<double, double>>
            {
                Tuple.Create(0.0026d, 15d),
                Tuple.Create(0.0027d, 5d),
            });
            Assert.AreEqual(0.0028d, orderBook.GetBestBidPrice(TestUtils.Symbol));
            Assert.AreEqual(0.0026d, orderBook.GetBestAskPrice(TestUtils.Symbol));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "Initialize multiple times the order book with the same symbol is not possible")]
        public void TestInitializeMultipleTimesSameSymbol()
        {
            var snapshotEventDto = TestUtils.SnapshotEvent();
            var orderBook = new PricePointBook.OrderBook.OrderBook();
            orderBook.Initialize(TestUtils.Symbol, snapshotEventDto);
            orderBook.Initialize(TestUtils.Symbol, snapshotEventDto);
        }

        /// <summary>
        /// Same as <see cref="TestInitializeMultipleTimesSameSymbol"/> but with the order book is cleared for the given
        /// symbol before the second call to <see cref="PricePointBook.OrderBook.OrderBook
        /// </summary>
        [TestMethod]
        public void TestInitializeMultipleTimesSameSymbolWithClearInBetween()
        {
            var snapshotEventDto = TestUtils.SnapshotEvent();
            IOrderBook orderBook = new PricePointBook.OrderBook.OrderBook();
            orderBook.Initialize(TestUtils.Symbol, snapshotEventDto);
            orderBook.Clear(TestUtils.Symbol);

            Assert.AreEqual(orderBook.GetOrderedBids(TestUtils.Symbol).Count, 0);
            Assert.AreEqual(orderBook.GetOrderedAsks(TestUtils.Symbol).Count, 0);
            
            orderBook.Initialize(TestUtils.Symbol, snapshotEventDto);
            // It should not throw, the test should end gracefully.
        }

        private static void AssertBidsInOrderBook(IOrderBook orderBook, List<Tuple<double, double>> expectedBids)
        {
            var snapshotEntries = orderBook.GetOrderedBids(TestUtils.Symbol);
            CollectionAssert.AreEqual(expectedBids, snapshotEntries.ToArray());
        }

        private static void AssertAsksInOrderBook(IOrderBook orderBook, List<Tuple<double, double>> expectedAsks)
        {
            var snapshotEntries = orderBook.GetOrderedAsks(TestUtils.Symbol);
            CollectionAssert.AreEqual(expectedAsks, snapshotEntries.ToArray());
        }
    }
}