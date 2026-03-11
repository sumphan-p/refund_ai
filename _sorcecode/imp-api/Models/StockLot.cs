namespace imp_api.Models;

public class StockLot
{
    public int Id { get; set; }
    public string ImportDeclarNo { get; set; } = string.Empty;
    public int ImportItemNo { get; set; }
    public DateTime ImportDate { get; set; }
    public string PrivilegeType { get; set; } = string.Empty;

    // Raw material
    public string RawMaterialCode { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ProductDescription { get; set; }
    public string Unit { get; set; } = string.Empty;

    // Quantity
    public decimal QtyOriginal { get; set; }
    public decimal QtyUsed { get; set; }
    public decimal QtyBalance { get; set; }
    public decimal QtyTransferred { get; set; }

    // Value / Duty
    public decimal? UnitPrice { get; set; }
    public decimal? CIFValueTHB { get; set; }
    public decimal? DutyRate { get; set; }
    public decimal? DutyPerUnit { get; set; }
    public decimal? TotalDutyVAT { get; set; }

    // Privilege reference
    public string? ImportTaxIncId { get; set; }
    public string? BOICardNo { get; set; }
    public string? ProductionFormulaNo { get; set; }

    // Status
    public string Status { get; set; } = "ACTIVE";
    public DateTime? ExpiryDate { get; set; }

    // Audit
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}
