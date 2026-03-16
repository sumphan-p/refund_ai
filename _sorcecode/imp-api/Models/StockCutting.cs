namespace imp_api.Models;

public class StockCutting
{
    public int Id { get; set; }
    public int StockLotId { get; set; }
    public string ExportDeclarNo { get; set; } = string.Empty;
    public int ExportItemNo { get; set; }
    public DateTime ExportDate { get; set; }
    public string PrivilegeType { get; set; } = string.Empty;

    // Formula
    public string? ProductionFormulaNo { get; set; }
    public int? BomDetailNo { get; set; }

    // Material
    public string RawMaterialCode { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    // Quantity
    public decimal ExportQty { get; set; }
    public decimal Ratio { get; set; }
    public decimal Scrap { get; set; }
    public decimal QtyRequired { get; set; }
    public decimal QtyCut { get; set; }

    // Duty refund
    public decimal? DutyPerUnit { get; set; }
    public decimal? DutyRefund { get; set; }

    // Document
    public string? BatchDocNo { get; set; }

    // Status
    public string Status { get; set; } = "PENDING";

    // Audit
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedDate { get; set; }

    // From JOIN with stock_m29_lot (not persisted in stock_m29_batch)
    public string? ImportDeclarNo { get; set; }
    public int ImportItemNo { get; set; }
    public DateTime? ImportDate { get; set; }
}
