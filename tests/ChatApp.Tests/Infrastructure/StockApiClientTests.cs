using System.Reflection;
using ChatApp.Bot.Clients;
using ChatApp.Core.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ChatApp.Tests.Infrastructure;

public class StockApiClientTests
{
    private static IOptions<StockApiOptions> MakeOptions(string baseUrl)
        => Options.Create(new StockApiOptions
        {
            BaseUrl = baseUrl,
            Format = "csv",
            Headers = true,
            Export = "file"
        });

    [Fact(DisplayName = "StockApiClient: parses valid CSV (ParseStockResponse)"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public void Parses_Valid_Csv_With_Private_Parser()
    {
        var csv = "Symbol,Date,Time,Open,High,Low,Close,Volume\nAAPL.US,2025-08-29,16:00,100,110,95,123.45,1000\n";
        var method = typeof(StockApiClient).GetMethod("ParseStockResponse", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        var result = method!.Invoke(null, new object?[] { csv, "aapl" }) as string;
        result.Should().NotBeNull();
        result!.Should().StartWith("AAPL quote is $");
        result.Should().EndWith(" per share");
        result.Should().MatchRegex(@"123[\.,]45");
    }

    [Fact(DisplayName = "StockApiClient: malformed CSV returns null (ParseStockResponse)"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public void Malformed_Csv_Returns_Null_With_Private_Parser()
    {
        var badCsv = "Symbol,Date\nHEADER_ONLY\n";
        var method = typeof(StockApiClient).GetMethod("ParseStockResponse", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        var result = method!.Invoke(null, new object?[] { badCsv, "aapl" });
        result.Should().BeNull();
    }

    [Fact(DisplayName = "StockApiClient: http error returns null (invalid BaseUrl)"), Trait("Category","Unit"), Trait("Area","Infrastructure")]
    public async Task Http_Error_Returns_Null()
    {
        var svc = new StockApiClient(MakeOptions("not-a-valid-url"));
        var res = await svc.GetStockQuoteAsync("aapl");
        res.Should().BeNull();
    }
}
