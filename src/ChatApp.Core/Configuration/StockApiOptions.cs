namespace ChatApp.Core.Configuration;

public class StockApiOptions
{
    public const string SectionName = "StockApi";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public bool Headers { get; set; }
    public string Export { get; set; } = string.Empty;
}
