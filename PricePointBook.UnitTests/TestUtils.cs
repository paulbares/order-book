using PricePointBook.BinanceDto;

namespace PricePointBook.UnitTests;

public class TestUtils
{
    
    public const string Symbol = "BNBBTC";

    public static SnapshotEventDto SnapshotEvent()
    {
        var bids = new List<PriceLevelAndQuantityDto>
        {
            new("0.0030", "99"),
            new("0.0028", "9.3"),
            new("0.0026", "2.3"),
            new("0.0024", "14.70000000"),
            new("0.0022", "6.40000000"),
            new("0.0020", "9.70000000")
        };
        var asks = new List<PriceLevelAndQuantityDto>
        {
            new("0.0024", "14.90000000"),
            new("0.0026", "3.60000000"),
            new("0.0028", "1.00000000")
        };
        return new SnapshotEventDto(bids, asks);
    }

    public static UpdateEventDto Update1()
    {
        var bids = new List<PriceLevelAndQuantityDto> { new("0.0024", "10") };
        var asks = new List<PriceLevelAndQuantityDto> { new("0.0026", "100") };
        return new UpdateEventDto("e", Symbol, bids, asks);
    }

    public static UpdateEventDto Update2()
    {
        var bids = new List<PriceLevelAndQuantityDto> { new("0.0024", "8") };
        var asks = new List<PriceLevelAndQuantityDto> { new("0.0028", "0") };
        return new UpdateEventDto("e", Symbol, bids, asks);
    }

    public static UpdateEventDto Update3()
    {
        var bids = new List<PriceLevelAndQuantityDto> { new("0.0030", "0") };
        var asks = new List<PriceLevelAndQuantityDto>
        {
            new("0.0026", "15"),
            new("0.0027", "5")
        };
        return new UpdateEventDto("e", Symbol, bids, asks);
    }

    public static UpdateEventDto Update4()
    {
        var bids = new List<PriceLevelAndQuantityDto> { new("0.0025", "100") };
        var asks = new List<PriceLevelAndQuantityDto>
        {
            new("0.0026", "0"),
            new("0.0027", "5")
        };
        return new UpdateEventDto("e", Symbol, bids, asks);
    }

    public static UpdateEventDto Update5()
    {
        var bids = new List<PriceLevelAndQuantityDto> { new("0.0025", "0") };
        var asks = new List<PriceLevelAndQuantityDto>
        {
            new("0.0026", "15"),
            new("0.0024", "0")
        };
        return new UpdateEventDto("e", Symbol, bids, asks);
    }
}