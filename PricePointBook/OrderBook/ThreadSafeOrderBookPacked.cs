using System.Collections.Concurrent;
using System.Globalization;
using PricePointBook.BinanceDto;
using PricePointBook.Concurrency;
using PricePointBook.DataStructure;
using PricePointBook.Utils;

namespace PricePointBook.OrderBook;

/// <summary>
/// Thread safe version of <see cref="ThreadSafeReadWriteObject{T}"/> using <see cref="OrderBookPacked"/>.
/// </summary>
public class ThreadSafeOrderBookPacked : IOrderBook
{
    /// <summary>
    /// The factor by which to scale the price and quantity.
    /// </summary>
    private const int ScaleFactor = 10_000; // TODO can be dynamically chosen/configured depending on the symbol

    private readonly ConcurrentDictionary<string, ThreadSafeReadWriteObject<CustomSortedSet<long>>[]>
        _bidsAndAsksBySymbol = new();

    public void Initialize(string symbol, SnapshotEventDto snapshotEventDto)
    {
        // Use a custom comparer. Keys are stored in the leftmost 32 bits
        CustomSortedSet<long> BidsFactory() =>
            new(Comparer<long>.Create((a, b) => -Bits.Unpack1(a).CompareTo(Bits.Unpack1(b))));

        CustomSortedSet<long> AsksFactory() =>
            new(Comparer<long>.Create((a, b) => Bits.Unpack1(a).CompareTo(Bits.Unpack1(b))));

        var bidsAndAsks = _bidsAndAsksBySymbol.AddOrUpdate(symbol, (_) =>
        {
            return new[]
            {
                new ThreadSafeReadWriteObject<CustomSortedSet<long>>(BidsFactory),
                new ThreadSafeReadWriteObject<CustomSortedSet<long>>(AsksFactory)
            };
        }, (s, objects) => throw new InvalidOperationException(
            $"The book has already been initialized for symbol {symbol}. Call Clear before to reinitialize it."));
        _update(bidsAndAsks[0], snapshotEventDto.Bids);
        _update(bidsAndAsks[1], snapshotEventDto.Asks);
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
        bidsAndAsks[index].Read(set =>
        {
            foreach (var item in set)
            {
                var price = (double)Bits.Unpack1(item) / ScaleFactor;
                var qty = (double)Bits.Unpack2(item) / ScaleFactor;
                r.Add(Tuple.Create(price, qty));
            }
        });

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
        ThreadSafeReadWriteObject<CustomSortedSet<long>> set,
        IEnumerable<PriceLevelAndQuantityDto> pricesAndQuantities)
    {
        set.Write(s =>
        {
            foreach (var priceLevelAndQuantityDto in pricesAndQuantities)
            {
                var invariantCulture = CultureInfo.InvariantCulture;
                var price = Convert.ToInt32(
                    double.Parse(priceLevelAndQuantityDto.Price, invariantCulture) * ScaleFactor);
                var quantity =
                    Convert.ToInt32(double.Parse(priceLevelAndQuantityDto.Quantity, invariantCulture) * ScaleFactor);
                var item = Bits.Pack(price, quantity);
                if (quantity == 0)
                {
                    s.Remove(item);
                }
                else
                {
                    s.Add(item);
                }
            }
        });
    }

    public void Clear(string symbol)
    {
        _bidsAndAsksBySymbol.Remove(symbol, out _);
    }

    public double GetBestBidPrice(string symbol)
    {
        if (!_bidsAndAsksBySymbol.TryGetValue(symbol, out var bidsAndAsks))
        {
            throw new InvalidOperationException($"There is no bid prices recorded in the book for the symbol {symbol}");
        }

        long bestBidPrice = 0;
        bidsAndAsks[0].Read(s =>
                bestBidPrice = s.MinValue // Min also, see the comparer.
        );
        return (double)Bits.Unpack1(bestBidPrice) / ScaleFactor;
    }

    public double GetBestAskPrice(string symbol)
    {
        if (!_bidsAndAsksBySymbol.TryGetValue(symbol, out var bidsAndAsks))
        {
            throw new InvalidOperationException($"There is no ask prices recorded in the book for the symbol {symbol}");
        }

        long bestAskPrice = 0;
        bidsAndAsks[1].Read(s =>
            bestAskPrice = s.MinValue
        );
        return (double)Bits.Unpack1(bestAskPrice) / ScaleFactor;
    }
}