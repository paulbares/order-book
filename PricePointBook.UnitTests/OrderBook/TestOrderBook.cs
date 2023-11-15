using PricePointBook.OrderBook;

namespace PricePointBook.UnitTests.OrderBook;

[TestClass]
public class TestOrderBook: ATestOrderBook
{
    public override IOrderBook CreateOrderBook()
    {
        return new PricePointBook.OrderBook.OrderBook();
    }
}