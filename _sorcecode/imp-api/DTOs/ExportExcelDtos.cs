namespace imp_api.DTOs;

public class ExportExcelUploadResponse
{
    public int TotalRows { get; set; }
    public int InsertedRows { get; set; }
    public int UpdatedRows { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ExportExcelPreviewItem
{
    public string DeclarNo { get; set; } = string.Empty;
    public int ItemDeclarNo { get; set; }
    public string? ExporterName { get; set; }
    public string? TaxId { get; set; }
    public string? BuyerName { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvDate { get; set; }
    public string? ProductCode { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? Brand { get; set; }
    public decimal? QtyInvoice { get; set; }
    public string? QtyInvoiceUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? FOBTHB { get; set; }
    public string? CurrentStatus { get; set; }
    public bool IsExisting { get; set; }
}

public class ExportManageListItem
{
    public int Id { get; set; }
    public string DeclarNo { get; set; } = string.Empty;
    public int ItemDeclarNo { get; set; }
    public string? ExporterName { get; set; }
    public string? BuyerName { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvDate { get; set; }
    public string? ProductCode { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? Brand { get; set; }
    public decimal? QtyInvoice { get; set; }
    public string? QtyInvoiceUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? FOBTHB { get; set; }
    public string? CurrentStatus { get; set; }
}

public class UpdateExportManageRequest
{
    public string? ExporterName { get; set; }
    public string? TaxId { get; set; }
    public string? BuyerName { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvDate { get; set; }
    public string? ProductCode { get; set; }
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionEn2 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? DescriptionTh2 { get; set; }
    public string? Brand { get; set; }
    public decimal? QtyInvoice { get; set; }
    public string? QtyInvoiceUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? FOBTHB { get; set; }
    public string? CurrentStatus { get; set; }
    public string? Remark { get; set; }
}
