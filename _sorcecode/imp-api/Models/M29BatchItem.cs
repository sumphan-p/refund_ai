namespace imp_api.Models;

public class M29BatchItem
{
    public int Id { get; set; }
    public int BatchHeaderId { get; set; }
    public int ExportExcelId { get; set; }
    public string ExportDeclarNo { get; set; } = string.Empty;
    public int ExportItemNo { get; set; }
    public DateTime? ExportDate { get; set; }
    public string? LoadingDate { get; set; }
    public string? ProductCode { get; set; }
    public string? Section19BisNo { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? FOBTHB { get; set; }
    public int SortOrder { get; set; }

    // Audit
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }

    // From JOIN with export_excel (not persisted)
    public string? InvoiceNo { get; set; }
    public string? BuyerName { get; set; }
}
