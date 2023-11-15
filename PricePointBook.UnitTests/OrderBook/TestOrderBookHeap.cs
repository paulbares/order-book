using PricePointBook.OrderBook;

namespace PricePointBook.UnitTests.OrderBook;

[TestClass]
public class TestOrderBookHeap: ATestOrderBook
{
    public override IOrderBook CreateOrderBook()
    {
        return new OrderBookHeap();
    }
}