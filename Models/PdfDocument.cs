namespace HeatherDemoApp.Models;

public class PdfDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public DateTime IngestedDate { get; set; } = DateTime.UtcNow;
    public string BlobUrl { get; set; } = string.Empty;
}