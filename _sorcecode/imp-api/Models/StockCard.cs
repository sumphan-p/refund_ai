namespace imp_api.Models;

public class StockCard
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string PrivilegeType { get; set; } = string.Empty;

    // Import ref
    public string? ImportDeclarNo { get; set; }
    public int? ImportItemNo { get; set; }
    public DateTime? ImportDate { get; set; }

    // Export ref
    public string? ExportDeclarNo { get; set; }
    public int? ExportItemNo { get; set; }

    // Material
    public string RawMaterialCode { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ProductDescription { get; set; }
    public string Unit { get; set; } = string.Empty;

    // Quantity
    public decimal? QtyIn { get; set; }
    public decimal? QtyOut { get; set; }
    public decimal QtyBalance { get; set; }

    // Value / Duty
    public decimal? UnitPrice { get; set; }
    public decimal? CIFValueTHB { get; set; }
    public decimal? DutyRate { get; set; }
    public decimal? DutyAmount { get; set; }
    public decimal? VATAmount { get; set; }

    // Privilege reference
    public string? ImportTaxIncId { get; set; }
    public string? ProductionFormulaNo { get; set; }

    // Lot tracking
    public int? LotId { get; set; }
    public string? LotImportDeclarNo { get; set; }

    // Audit
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? Remark { get; set; }
}
