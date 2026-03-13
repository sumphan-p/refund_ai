namespace imp_api.DTOs;

// =============================================
// Export items eligible for Section 19 bis cutting
// =============================================

public class ExportItemForCutting
{
    public int Id { get; set; }
    public string DeclarNo { get; set; } = string.Empty;
    public int ItemDeclarNo { get; set; }
    public string? ExportDate { get; set; }
    public string? ProductCode { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? DescriptionEn1 { get; set; }
    public decimal? QtyDeclar { get; set; }
    public string? QtyDeclarUnit { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? FOBTHB { get; set; }
    public string? Section19BisNo { get; set; }
    public string? ImportTaxIncentiveId { get; set; }
    public string? ImportDeclarNo { get; set; }
    public string? CuttingStatus { get; set; } // null=not cut, PENDING, CONFIRMED
}

// =============================================
// Stock Lot for display
// =============================================

public class StockLotListItem
{
    public int Id { get; set; }
    public string ImportDeclarNo { get; set; } = string.Empty;
    public int ImportItemNo { get; set; }
    public string? ImportDate { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal QtyOriginal { get; set; }
    public decimal QtyUsed { get; set; }
    public decimal QtyBalance { get; set; }
    public decimal? DutyPerUnit { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
}

// =============================================
// FIFO Cutting Request
// =============================================

public class CutStock19TvisRequest
{
    public string ExportDeclarNo { get; set; } = string.Empty;
    public int ExportItemNo { get; set; }
    public string ExportDate { get; set; } = string.Empty;
    public decimal ExportQty { get; set; }
    public string ExportUnit { get; set; } = string.Empty;
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? ImportTaxIncentiveId { get; set; }
}

// =============================================
// FIFO Cutting Response
// =============================================

public class CutStock19TvisResponse
{
    public List<CuttingResultItem> Cuttings { get; set; } = new();
    public decimal TotalQtyRequired { get; set; }
    public decimal TotalQtyCut { get; set; }
    public decimal TotalDutyRefund { get; set; }
    public bool IsFullyCut { get; set; }
}

public class CuttingResultItem
{
    public int? CuttingId { get; set; }
    public int LotId { get; set; }
    public string ImportDeclarNo { get; set; } = string.Empty;
    public int ImportItemNo { get; set; }
    public string? ImportDate { get; set; }
    public string RawMaterialCode { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal QtyCut { get; set; }
    public decimal? DutyPerUnit { get; set; }
    public decimal? DutyRefund { get; set; }
    public decimal LotBalanceAfter { get; set; }
}

// =============================================
// Cutting detail view (for already-cut exports)
// =============================================

public class ExportCuttingDetail
{
    public string ExportDeclarNo { get; set; } = string.Empty;
    public int ExportItemNo { get; set; }
    public string? ExportDate { get; set; }
    public string? ProductCode { get; set; }
    public decimal? ExportQty { get; set; }
    public string? ProductionFormulaNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalDutyRefund { get; set; }
    public List<CuttingResultItem> Cuttings { get; set; } = new();
}

// =============================================
// Sync import → stock_m29_lot
// =============================================

public class SyncStockLotRequest
{
    public string PrivilegeType { get; set; } = "19TVIS";
}

public class SyncStockLotResponse
{
    public int InsertedCount { get; set; }
    public int SkippedCount { get; set; }
}

// =============================================
// Export lines by exact DeclarNo (for detail panel)
// =============================================

public class ExportLineItem
{
    public int Id { get; set; }
    public string DeclarNo { get; set; } = string.Empty;
    public int ItemDeclarNo { get; set; }
    public string? ExportDate { get; set; }
    public string? InvoiceNo { get; set; }
    public string? ProductCode { get; set; }
    public string? DescriptionTh1 { get; set; }
    public decimal? QtyDeclar { get; set; }
    public string? QtyDeclarUnit { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? FOBTHB { get; set; }
    public string? ImportDeclarNo { get; set; }
    public string? Section19BisNo { get; set; }
    public string? ImportTaxIncentiveId { get; set; }
    public string? CuttingStatus { get; set; }
}

// =============================================
// BOM formula info (for display)
// =============================================

public class BomFormulaInfo
{
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? DescriptionTh1 { get; set; }
    public List<BomFormulaDetail> Details { get; set; } = new();
}

public class BomFormulaDetail
{
    public int No { get; set; }
    public string? RawMaterialCode { get; set; }
    public string? ProductType { get; set; }
    public string? Unit { get; set; }
    public decimal? Ratio { get; set; }
    public decimal? Scrap { get; set; }
    public decimal QtyRequired { get; set; }  // calculated: exportQty * (ratio + scrap)
    public decimal QtyFromFormula { get; set; } // exportQty * ratio
    public decimal? QtyOnHand { get; set; }    // from stock_m29_on_hand.QtyBalance
    public string? Remark { get; set; }
}

// =============================================
// Stock Card entries
// =============================================

public class StockCardEntry
{
    public int Id { get; set; }
    public string? TransactionType { get; set; }
    public string? TransactionDate { get; set; }
    public string? ImportDeclarNo { get; set; }
    public int? ImportItemNo { get; set; }
    public string? ImportDate { get; set; }
    public string? PrivilegeType { get; set; }
    public string? ExportDeclarNo { get; set; }
    public int? ExportItemNo { get; set; }
    public string? RawMaterialCode { get; set; }
    public string? ProductCode { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? QtyIn { get; set; }
    public decimal? QtyOut { get; set; }
    public decimal QtyBalance { get; set; }
    public string Status { get; set; } = "Active";
}

// =============================================
// Full cutting result (multi-line export)
// =============================================

public class CutStockBatchRequest
{
    public string ExportDeclarNo { get; set; } = string.Empty;
    public List<int> ExportItemNos { get; set; } = new();
    public string ProductionFormulaNo { get; set; } = string.Empty;
}
