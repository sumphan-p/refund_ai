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
    public string? InvoiceNo { get; set; }
    public string? BuyerName { get; set; }
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
    public int AgeDays { get; set; }
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
    public int? StartDocRunning { get; set; } // optional: custom starting running number
}

// =============================================
// FIFO Cutting Response
// =============================================

public class CutStock19TvisResponse
{
    public string? BatchDocNo { get; set; }
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
    public int UpdatedCount { get; set; }
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
    public decimal? QtyOnHand { get; set; }    // from SUM(stock_m29_lot.QtyBalance) WHERE ACTIVE
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

public class NextDocNoResponse
{
    public int NextRunning { get; set; }
    public string YearSuffix { get; set; } = string.Empty;
    public string NextDocNo { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

// =============================================
// Batch Management DTOs
// =============================================

public class BatchListItem
{
    public string BatchDocNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ExportItemCount { get; set; }
    public decimal TotalNetWeight { get; set; }
    public decimal TotalFOBTHB { get; set; }
    public string? Remark { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class BatchDetailResponse
{
    public string BatchDocNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public decimal TotalQtyCut { get; set; }
    public decimal TotalDutyRefund { get; set; }
    public List<BatchExportItemDetail> ExportItems { get; set; } = new();
}

public class CreateBatchRequest
{
    public List<int> ExportExcelIds { get; set; } = new();
}

public class CreateBatchResponse
{
    public int BatchHeaderId { get; set; }
    public string BatchDocNo { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal TotalNetWeight { get; set; }
    public decimal TotalFOBTHB { get; set; }
    public string? Remark { get; set; }
}

public class M29BatchDetailResponse
{
    public string BatchDocNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal TotalNetWeight { get; set; }
    public decimal TotalFOBTHB { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledDate { get; set; }
    public List<M29BatchItemDetail> Items { get; set; } = new();
}

public class M29BatchItemDetail
{
    public int Id { get; set; }
    public int ExportExcelId { get; set; }
    public string ExportDeclarNo { get; set; } = string.Empty;
    public int ExportItemNo { get; set; }
    public string? ExportDate { get; set; }
    public string? LoadingDate { get; set; }
    public string? ProductCode { get; set; }
    public string? Section19BisNo { get; set; }
    public decimal? NetWeight { get; set; }
    public decimal? FOBTHB { get; set; }
    public int SortOrder { get; set; }

    // From JOIN with export_excel
    public string? InvoiceNo { get; set; }
    public string? BuyerName { get; set; }
}

public class BatchExportItemDetail
{
    public string ExportDeclarNo { get; set; } = string.Empty;
    public int ExportItemNo { get; set; }
    public string? ExportDate { get; set; }
    public string? ProductionFormulaNo { get; set; }
    public decimal ExportQty { get; set; }
    public decimal TotalQtyCut { get; set; }
    public decimal TotalDutyRefund { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<CuttingResultItem> Cuttings { get; set; } = new();
}
