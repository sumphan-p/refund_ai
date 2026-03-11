namespace imp_api.DTOs;

public class ImportExcelUploadResponse
{
    public int TotalRows { get; set; }
    public int InsertedRows { get; set; }
    public int UpdatedRows { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ImportExcelPreviewItem
{
    public string DeclarNo { get; set; } = string.Empty;
    public int ItemDeclarNo { get; set; }
    public string? CustomerName { get; set; }
    public string? CompanyTaxNo { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvDate { get; set; }
    public string? ProductCode { get; set; }
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? Brand { get; set; }
    public decimal? Quantity { get; set; }
    public string? QuantityUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? CIFTHB { get; set; }
    public decimal? DutyRate { get; set; }
    public decimal? TotalDutyVAT { get; set; }
    public string? UsePrivilege { get; set; }
    public string? Currency { get; set; }
    public bool IsExisting { get; set; }
}

public class ImportManageListItem
{
    public int Id { get; set; }
    public string DeclarNo { get; set; } = string.Empty;
    public int ItemDeclarNo { get; set; }
    public string? CustomerName { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvDate { get; set; }
    public string? ProductCode { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? Brand { get; set; }
    public decimal? Quantity { get; set; }
    public string? QuantityUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Currency { get; set; }
    public decimal? CIFTHB { get; set; }
    public decimal? DutyRate { get; set; }
    public decimal? TotalDutyVAT { get; set; }
    public string? UsePrivilege { get; set; }
}

public class UpdateImportManageRequest
{
    public string? CustomerName { get; set; }
    public string? CompanyTaxNo { get; set; }
    public string? RefNo { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvDate { get; set; }
    public string? ProductCode { get; set; }
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionEn2 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? DescriptionTh2 { get; set; }
    public string? Brand { get; set; }
    public decimal? Quantity { get; set; }
    public string? QuantityUnit { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Currency { get; set; }
    public decimal? CIFTHB { get; set; }
    public decimal? DutyRate { get; set; }
    public decimal? TotalDutyVAT { get; set; }
    public string? UsePrivilege { get; set; }
    public string? Remark { get; set; }
    public string? RemarkInternal { get; set; }
}

public class PagedResponse<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
