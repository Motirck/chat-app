namespace ChatApp.Bot.Interfaces;

public interface IStockApiClient
{
    Task<string?> GetStockQuoteAsync(string stockCode);
}
