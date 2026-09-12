using CommunityToolkit.Maui.Storage;

namespace AllAroundEstimates.Services;

public static class PdfExportService
{
    public static async Task ExportPdfAsync(string fileName, MemoryStream pdfStream)
    {
        pdfStream.Position = 0;

#if WINDOWS
        var result = await FileSaver.Default.SaveAsync(fileName, pdfStream, CancellationToken.None);
        if (!result.IsSuccessful)
        {
            throw new IOException($"Failed to save PDF: {result.Exception?.Message}");
        }
#elif ANDROID
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        using (var fileStream = File.Create(filePath))
        {
            await pdfStream.CopyToAsync(fileStream);
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Share Estimate PDF",
            File = new ShareFile(filePath)
        });
#else
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("PDF export is only supported on Windows and Android.");
#endif
    }
}
