namespace imp_api.Models;

public class ExportExcel
{
    public int Id { get; set; }

    // Key fields
    public string DeclarNo { get; set; } = string.Empty;
    public int ItemDeclarNo { get; set; }

    // Exporter info
    public string? ExporterName { get; set; }
    public string? TaxId { get; set; }
    public string? BranchSeq { get; set; }
    public string? DocumentType { get; set; }
    public string? BuyerName { get; set; }

    // Invoice
    public string? InvoiceNo { get; set; }
    public string? InvDate { get; set; }
    public int? InvoiceItemNo { get; set; }

    // Dates / Status
    public string? SubmissionDate { get; set; }
    public string? ReleaseDate { get; set; }
    public string? LoadingDate { get; set; }
    public string? CurrentStatus { get; set; }

    // Product
    public string? ProductCode { get; set; }
    public string? Brand { get; set; }
    public string? PurchaseOrder { get; set; }
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionEn2 { get; set; }
    public string? DescriptionEn3 { get; set; }
    public string? DescriptionEn4 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? DescriptionTh2 { get; set; }
    public string? DescriptionTh3 { get; set; }
    public string? DescriptionTh4 { get; set; }

    // Terms / Tariff
    public string? TermOfPayment { get; set; }
    public string? TariffCode { get; set; }
    public string? TariffType { get; set; }
    public string? StatisticalCode { get; set; }
    public string? StatisticalUnit { get; set; }

    // Quantity / Weight
    public decimal? PackageCount { get; set; }
    public string? PackageUnit { get; set; }
    public decimal? QtyInvoice { get; set; }
    public string? QtyInvoiceUnit { get; set; }
    public decimal? QtyDeclar { get; set; }
    public string? QtyDeclarUnit { get; set; }
    public decimal? NetWeight { get; set; }
    public string? NetWeightUnit { get; set; }

    // Price / Value
    public decimal? UnitPrice { get; set; }
    public decimal? FOBForeign { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? FOBTHB { get; set; }

    // Shipment
    public string? VesselName { get; set; }
    public string? ExportPortCode { get; set; }
    public string? InspectionLocationCode { get; set; }
    public string? BuyerCountryCode { get; set; }
    public string? DestinationCountryCode { get; set; }

    // Privileges
    public string? Compensation { get; set; }
    public string? CompensationNo { get; set; }
    public string? BOI { get; set; }
    public string? BOINo { get; set; }
    public string? FormulaBOI { get; set; }
    public string? Section19Bis { get; set; }
    public string? Section19BisNo { get; set; }
    public string? RightsTransferNo { get; set; }
    public string? Bond { get; set; }

    // Model
    public string? ModelNo { get; set; }
    public string? ModelVer { get; set; }
    public string? ModelCompTax { get; set; }

    // Zone / Incentive
    public string? EPZ { get; set; }
    public string? FZ { get; set; }
    public string? ImportTaxIncentiveId { get; set; }
    public string? ExportTaxIncentiveId { get; set; }
    public string? ReExport { get; set; }
    public string? NetReturn { get; set; }
    public string? SpecialPrivilegeCode { get; set; }
    public string? OriginCountryCode { get; set; }

    // Import reference
    public string? ImportDeclarNo { get; set; }
    public decimal? ImportDeclarItemNo { get; set; }

    // Short (ขาดจำนวน)
    public string? ShortDeclar { get; set; }
    public decimal? ShortPack { get; set; }
    public decimal? ShortQty { get; set; }
    public decimal? ShortNetWeight { get; set; }
    public decimal? ShortFOBForeign { get; set; }
    public decimal? ShortFOBTHB { get; set; }

    // Short Post (ขาดจำนวนหลังตรวจปล่อย)
    public string? ShortPostDeclar { get; set; }
    public decimal? ShortPostPack { get; set; }
    public decimal? ShortPostQty { get; set; }
    public decimal? ShortPostNetWeight { get; set; }
    public decimal? ShortPostFOBForeign { get; set; }
    public decimal? ShortPostFOBTHB { get; set; }

    // Permits / Reference
    public string? PermitNo1 { get; set; }
    public string? PermitNo2 { get; set; }
    public string? PermitNo3 { get; set; }
    public string? BookingNo { get; set; }
    public string? HouseBLNo { get; set; }
    public string? Remark { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
