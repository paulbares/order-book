using System.Globalization;
using PricePointBook.BinanceDto;

namespace PricePointBook.Benchmarks;

/// <summary>
/// Class that generates event dto object from a hard-coded starting price.  
/// </summary>
public class DataGenerator
{
    
    private const double Price = 0.0002;
    private const double Step = 0.00001;
    private readonly int _seed;
    private readonly int _numberOfPrices;

    public DataGenerator(int numberOfPrices, int seed)
    {
        _numberOfPrices = numberOfPrices;
        _seed = seed;
    }

    public SnapshotEventDto GenerateSnapshotEvent()
    {
        Random rand = new Random(_seed); // for reproducibility 
        var bids = new List<PriceLevelAndQuantityDto>();
        var asks = new List<PriceLevelAndQuantityDto>();

        var p = Price;
        for (var i = 0; i < _numberOfPrices; i++)
        {
            bids.Add(new(p.ToString(CultureInfo.InvariantCulture),
                rand.NextDouble().ToString(CultureInfo.InvariantCulture)));
            asks.Add(new(p.ToString(CultureInfo.InvariantCulture),
                rand.NextDouble().ToString(CultureInfo.InvariantCulture)));
            p += Step;
        }

        return new SnapshotEventDto(bids, asks);
    }

    public IList<UpdateEventDto> GenerateUpdateEvents(
        string symbol,
        int nbUpdates,
        int numberOfPricesToUpdate,
        int numberOfPricesToRemove)
    {
        var availablePrices = new HashSet<double>();
        var p = Price;
        for (var i = 0; i < _numberOfPrices; i++)
        {
            availablePrices.Add(p);
            p += Step;
        }

        IList<UpdateEventDto> updates = new List<UpdateEventDto>();
        Random rand = new Random(_seed); // for reproducibility 
        for (int i = 0; i < nbUpdates; i++)
        {
            var bids = new List<PriceLevelAndQuantityDto>();
            var asks = new List<PriceLevelAndQuantityDto>();
            // Choose randomly numberOfPricesToRemove prices
            var pricesToRemove = availablePrices.OrderBy(x => rand.Next()).Take(numberOfPricesToRemove).ToList();
            var invariantCulture = CultureInfo.InvariantCulture;
            foreach (var toRemove in pricesToRemove)
            {
                // qty = 0 => removal
                bids.Add(new(toRemove.ToString(invariantCulture), "0"));
                asks.Add(new(toRemove.ToString(invariantCulture), "0"));
                availablePrices.Remove(toRemove); // remove from the list of available prices for the next iteration
            }
            var pricesToUpdate = availablePrices.OrderBy(x => rand.Next()).Take(numberOfPricesToUpdate).ToList();
            foreach (var toUpdate in pricesToUpdate)
            {
                var nextDouble = rand.NextDouble();
                // We don't care about qty value. It should only be diff. than zero 
                bids.Add(new(toUpdate.ToString(invariantCulture),
                    nextDouble != 0 ? nextDouble.ToString(invariantCulture) : "0.1"));
                asks.Add(new(toUpdate.ToString(invariantCulture),
                    nextDouble != 0 ? nextDouble.ToString(invariantCulture) : "0.1"));    
            }

            updates.Add(new UpdateEventDto("e", symbol, bids, asks));
        }

        return updates;
    }
}