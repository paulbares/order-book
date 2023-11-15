using PricePointBook.BinanceDto;

namespace PricePointBook.OrderBook;

public interface IOrderBook
{
    /// <summary>
    /// Initializes the order book for the given symbol with the data contained in the dto passed as argument.
    /// </summary>
    /// <param name="symbol">the symbol</param>
    /// <param name="snapshotEventDto">the dto containing the initial data (prices and quantities)</param>
    public void Initialize(string symbol, SnapshotEventDto snapshotEventDto);

    /// <summary>
    /// Updates the order book with the data contained in the dto.
    /// </summary>
    /// <param name="eventDto">update event</param>
    public bool Update(UpdateEventDto eventDto);

    /// <summary>
    /// Deletes in the order book any price levels related to the given symbol.
    /// </summary>
    /// <param name="symbol">the symbol</param>
    public void Clear(string symbol);

    /// <summary>
    /// Returns the list of bids [price, quantity] ordered in <b>descending</b> order (for price).  
    /// </summary>
    /// <param name="symbol">The symbol for which bids info are returned</param>
    /// <returns>the ordered list of bids [price, quantity]</returns>
    public IList<Tuple<double, double>> GetOrderedBids(string symbol);
    
    /// <summary>
    /// Returns the list of asks [price, quantity] ordered in <b>ascending</b> order (for price).
    /// </summary>
    /// <param name="symbol">The symbol for which asks info are returned</param>
    /// <returns>the ordered list of asks [price, quantity]</returns>
    public IList<Tuple<double, double>> GetOrderedAsks(string symbol);
    
    /// <summary>
    /// Returns the best bid price for the given symbol i.e the <b>highest</b>.
    /// </summary>
    /// <param name="symbol">the symbol</param>
    /// <returns>the best bid price for the given symbol</returns>
    public double GetBestBidPrice(string symbol);
    
    /// <summary>
    /// Returns the best ask price for the given symbol i.e the <b>lowest</b>.
    /// </summary>
    /// <param name="symbol">the symbol</param>
    /// <returns>the best ask price for the given symbol</returns>
    public double GetBestAskPrice(string symbol);
}