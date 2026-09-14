using System.Text.Json;
using AllAroundEstimates.Models;

namespace AllAroundEstimates.Services;

public static class InvoiceStorage
{
    private const string FileName = "invoices.json";

    private static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    public static void SaveInvoices(List<Invoice> invoices)
    {
        var json = JsonSerializer.Serialize(invoices, InvoiceJsonContext.Default.ListInvoice);
        File.WriteAllText(FilePath, json);
    }

    public static List<Invoice> LoadInvoices()
    {
        if (!File.Exists(FilePath))
            return new List<Invoice>();

        var json = File.ReadAllText(FilePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<Invoice>();

        return JsonSerializer.Deserialize(json, InvoiceJsonContext.Default.ListInvoice) ?? new List<Invoice>();
    }

    public static void AddInvoice(Invoice invoice)
    {
        var invoices = LoadInvoices();
        invoices.Add(invoice);
        SaveInvoices(invoices);
    }

    public static List<Invoice> GetInvoicesForEstimate(string estimateNumber)
    {
        return LoadInvoices()
            .Where(i => i.EstimateNumber == estimateNumber)
            .OrderByDescending(i => i.DateCreated)
            .ToList();
    }
}
