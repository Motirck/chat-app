namespace ChatApp.Core.Interfaces;

public interface IStockService
{
    Task<string?> GetStockQuoteAsync(string stockCode);
}
