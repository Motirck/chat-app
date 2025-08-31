using System.Globalization;
using ChatApp.Bot.Interfaces;
using ChatApp.Core.Configuration;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;

namespace ChatApp.Bot.Clients;

public class StockApiClient : IStockApiClient
{
    private readonly StockApiOptions _options;

    public StockApiClient(IOptions<StockApiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string?> GetStockQuoteAsync(string stockCode)
    {
        try
        {
            var response = await _options.BaseUrl
                .SetQueryParams(new
                {
                    s = stockCode,
                    f = _options.Format,
                    h = _options.Headers ? "1" : "0",
                    e = _options.Export
                })
                .GetStringAsync();

            return ParseStockResponse(response, stockCode);
        }
        catch (FlurlHttpException ex)
        {
            // Log the error in production
            Console.WriteLine($"Stock API error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            // Log unexpected errors in production
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return null;
        }
    }

    private static string? ParseStockResponse(string response, string stockCode)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2) return null;
        
        var data = lines[1].Split(',');
        
        // CSV structure: Symbol,Date,Time,Open,High,Low,Close,Volume
        // Close price is at index 6
        if (data.Length < 7) return null;
        
        // Extract the close price (index 6 in the CSV)
        if (decimal.TryParse(data[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
        {
            return $"{stockCode.ToUpper()} quote is ${price:F2} per share";
        }
        
        return null;
    }
}