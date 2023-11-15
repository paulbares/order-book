using PricePointBook.OrderBook;

namespace PricePointBook.UnitTests.OrderBook;

[TestClass]
public class TestThreadSafeOrderBookPacked: ATestOrderBook
{
    public override IOrderBook CreateOrderBook()
    {
        return new ThreadSafeOrderBookPacked();
    }
}