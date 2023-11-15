using System.Globalization;
using PricePointBook.BinanceDto;
using PricePointBook.DataStructure;
using PricePointBook.Utils;

namespace PricePointBook.OrderBook;

/// <summary>   
/// An implementation <see cref="IOrderBook"/>; using a BinaryHeapLong under the hood with packed integers.
/// </summary>
public class OrderBookHeap : IOrderBook
{
    
    /// <summary>
    /// The factor by which to scale the price and quantity.
    /// </summary>
    private const int ScaleFactor = 10_000; // TODO can be dynamically chosen/configured depending on the symbol

    private readonly IDictionary<string, BinaryHeapLong[]> _bidsAndAsksBySymbol =
        new Dictionary<string, BinaryHeapLong[]>();

    public void Initialize(string symbol, SnapshotEventDto snapshotEventDto)
    {
        if (_bidsAndAsksBySymbol.ContainsKey(symbol))
        {
            throw new InvalidOperationException(
                $"The book has already been initialized for symbol {symbol}. Call Clear before to reinitialize it.");
        }

        // Use a custom comparer. Keys are stored in the leftmost 32 bits
        var bids = new BinaryHeapLong(2048,
            Comparer<long>.Create((a, b) => -Bits.Unpack1(a).CompareTo(Bits.Unpack1(b))));
        var asks = new BinaryHeapLong(2048,
            Comparer<long>.Create((a, b) => Bits.Unpack1(a).CompareTo(Bits.Unpack1(b))));
        _bidsAndAsksBySymbol.Add(symbol, new[] { bids, asks });
        _update(bids, snapshotEventDto.Bids);
        _update(asks, snapshotEventDto.Asks);
    }

    public IList<Tuple<double, double>> GetOrderedBids(string symbol)
    {
        return GetOrderedBidsOrAsks(symbol, 0);
    }

    public IList<Tuple<double, double>> GetOrderedAsks(string symbol)
    {
        return GetOrderedBidsOrAsks(symbol, 1);
    }

    private IList<Tuple<double, double>> GetOrderedBidsOrAsks(string symbol, int index)
    {
        if (!_bidsAndAsksBySymbol.TryGetValue(symbol, out var bidsAndAsks))
        {
            return Array.Empty<Tuple<double, double>>().ToList();
        }

        IList<Tuple<double, double>> r = new List<Tuple<double, double>>();
        foreach (var item in bidsAndAsks[index].GetOrderedElements())
        {
            var price = (double)Bits.Unpack1(item) / ScaleFactor;
            var qty = (double)Bits.Unpack2(item) / ScaleFactor;
            r.Add(Tuple.Create(price, qty));
        }

        return r;
    }

    public bool Update(UpdateEventDto eventDto)
    {
        if (!_bidsAndAsksBySymbol.TryGetValue(eventDto.Symbol, out var bidsAndAsks))
        {
            return false;
        }

        _update(bidsAndAsks[0], eventDto.Bids);
        _update(bidsAndAsks[1], eventDto.Asks);
        return true;
    }

    private void _update(
        BinaryHeapLong set,
        IEnumerable<PriceLevelAndQuantityDto> pricesAndQuantities)
    {
        foreach (var priceLevelAndQuantityDto in pricesAndQuantities)
        {
            var invariantCulture = CultureInfo.InvariantCulture;
            var price = Convert.ToInt32(double.Parse(priceLevelAndQuantityDto.Price, invariantCulture) * ScaleFactor);
            var quantity =
                Convert.ToInt32(double.Parse(priceLevelAndQuantityDto.Quantity, invariantCulture) * ScaleFactor);
            var item = Bits.Pack(price, quantity);
            if (quantity == 0)
            {
                set.Remove(item);
            }
            else
            {
                set.Add(item);
            }
        }
    }

    public void Clear(string symbol)
    {
        _bidsAndAsksBySymbol.Remove(symbol);
    }

    public double GetBestBidPrice(string symbol)
    {
        if (!_bidsAndAsksBySymbol.TryGetValue(symbol, out var bidsAndAsks))
        {
            throw new InvalidOperationException($"There is no bid prices recorded in the book for the symbol {symbol}");
        }

        var bestBidPrice = bidsAndAsks[0].Peek();
        return (double)Bits.Unpack1(bestBidPrice) / ScaleFactor;
    }

    public double GetBestAskPrice(string symbol)
    {
        if (!_bidsAndAsksBySymbol.TryGetValue(symbol, out var bidsAndAsks))
        {
            throw new InvalidOperationException($"There is no ask prices recorded in the book for the symbol {symbol}");
        }

        var bestAskPrice = bidsAndAsks[1].Peek();
        return (double)Bits.Unpack1(bestAskPrice) / ScaleFactor;
    }
}