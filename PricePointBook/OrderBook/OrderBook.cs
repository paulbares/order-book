using System.Globalization;
using PricePointBook.BinanceDto;

namespace PricePointBook.OrderBook;

/// <summary>   
/// An implementation of <see cref="PricePointBook.OrderBook.IOrderBook"/>; using a Red-Black Tree under the hood (see the implementation of
/// <see cref="SortedDictionary{TKey,TValue}"/>). 
/// </summary>
public class OrderBook : IOrderBook
{
    
    private readonly IDictionary<string, IDictionary<double, double>[]> _bidsAndAsksBySymbol =
        new Dictionary<string, IDictionary<double, double>[]>();

    public void Initialize(string symbol, SnapshotEventDto snapshotEventDto)
    {
        if (_bidsAndAsksBySymbol.ContainsKey(symbol))
        {
            throw new InvalidOperationException(
                $"The book has already been initialized for symbol {symbol}. Call Clear before to reinitialize it.");
        }

        var bids = new SortedDictionary<double, double>(Comparer<double>.Create((a, b) => -a.CompareTo(b)));
        var asks = new SortedDictionary<double, double>(Comparer<double>.Create((a, b) => a.CompareTo(b)));
        _bidsAndAsksBySymbol.Add(symbol, new IDictionary<double, double>[] { bids, asks });
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
        return !_bidsAndAsksBySymbol.TryGetValue(symbol, out var bidsAndAsks)
            ? Array.Empty<Tuple<double, double>>().ToList()
            : bidsAndAsks[index].Select(entry => Tuple.Create(entry.Key, entry.Value)).ToList();
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

    private static void _update(
        IDictionary<double, double> dictionary,
        IEnumerable<PriceLevelAndQuantityDto> pricesAndQuantities)
    {
        foreach (var priceLevelAndQuantityDto in pricesAndQuantities)
        {
            var invariantCulture = CultureInfo.InvariantCulture;
            var price = double.Parse(priceLevelAndQuantityDto.Price, invariantCulture);
            var quantity = double.Parse(priceLevelAndQuantityDto.Quantity, invariantCulture);
            if (quantity == 0)
            {
                dictionary.Remove(price);
            }
            else
            {
                dictionary[price] = quantity;
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

        return bidsAndAsks[0].Keys.Max();
    }

    public double GetBestAskPrice(string symbol)
    {
        if (!_bidsAndAsksBySymbol.TryGetValue(symbol, out var bidsAndAsks))
        {
            throw new InvalidOperationException($"There is no ask prices recorded in the book for the symbol {symbol}");
        }

        return bidsAndAsks[1].Keys.Min();
    }
}