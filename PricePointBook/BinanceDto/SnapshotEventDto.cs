namespace PricePointBook.BinanceDto;

/// <summary>
/// This DTO represents the payload received when calling https://api.binance.com/api/v3/depth?symbol=BNBBTC&amp;limit=1000
/// to get a depth snapshot. Note the API does not provide the symbol in the response. 
/// <br/>
/// Look at the <a href="https://binance-docs.github.io/apidocs/spot/en/#partial-book-depth-streams">Binance documentation</a>
/// for more info.
/// </summary>
public record SnapshotEventDto
{
    private readonly IList<PriceLevelAndQuantityDto> _bids;
    private readonly IList<PriceLevelAndQuantityDto> _asks;

    public SnapshotEventDto(IList<PriceLevelAndQuantityDto> bids, IList<PriceLevelAndQuantityDto> asks)
    {
        _bids = bids;
        _asks = asks;
    }

    public IEnumerable<PriceLevelAndQuantityDto> Bids => _bids;
    public IEnumerable<PriceLevelAndQuantityDto> Asks => _asks;

    public override string ToString()
    {
        var bidsStr = $"['{string.Join("', '", _bids)}']";
        var asksStr = $"['{string.Join("', '", _asks)}']";
        return $"{nameof(_bids)}: {bidsStr}, {nameof(_asks)}: {asksStr}";
    }
}