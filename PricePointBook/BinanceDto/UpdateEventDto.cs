using PricePointBook.BinanceDto;

namespace PricePointBook.BinanceDto;

public class UpdateEventDto
{
    private readonly string _eventType;

    public IList<PriceLevelAndQuantityDto> Bids { get; }

    public IList<PriceLevelAndQuantityDto> Asks { get; }

    public string Symbol { get; }

    public UpdateEventDto(string eventType, string symbol, IList<PriceLevelAndQuantityDto> bids, IList<PriceLevelAndQuantityDto> asks)
    {
        _eventType = eventType;
        Symbol = symbol;
        Bids = bids;
        Asks = asks;
    }
}