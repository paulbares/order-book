using PricePointBook.OrderBook;

namespace PricePointBook.UnitTests.OrderBook;

[TestClass]
public class TestOrderBookPacked: ATestOrderBook
{
    public override IOrderBook CreateOrderBook()
    {
        return new OrderBookPacked();
    }
}