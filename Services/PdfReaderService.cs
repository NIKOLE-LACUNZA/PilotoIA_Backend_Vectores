using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;
public class PdfReaderService
{
    public async Task<string> ExtractTextAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(ms);
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);

        return sb.ToString();
    }
}
