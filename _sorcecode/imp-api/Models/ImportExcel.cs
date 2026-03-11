namespace imp_api.Models;

public class ImportExcel
{
    public int Id { get; set; }

    // Key fields
    public string DeclarNo { get; set; } = string.Empty;
    public int ItemDeclarNo { get; set; }

    // Company info
    public string? CustomerName { get; set; }
    public string? CompanyTaxNo { get; set; }
    public string? RefNo { get; set; }

    // Shipment info
    public string? JobNo { get; set; }
    public string? VesselName { get; set; }
    public string? Voy { get; set; }
    public string? ExportIncentiveId { get; set; }
    public string? TradPartner { get; set; }
    public string? TransportMode { get; set; }
    public string? ReloadPort { get; set; }
    public string? Subgate { get; set; }
    public string? InspectionCode { get; set; }
    public string? ApprovedPort { get; set; }
    public string? ETA { get; set; }
    public string? MasterBL { get; set; }
    public string? HouseBL { get; set; }

    // Factory
    public string? FactoryNo { get; set; }
    public string? EstablishNo { get; set; }
    public string? ApprovedNo { get; set; }

    // Buyer
    public string? StatusBuyer { get; set; }
    public string? LevelBuyer { get; set; }
    public string? ExporterRef { get; set; }
    public string? PurchaseNo { get; set; }
    public string? OtherRefNo { get; set; }

    // Shipper / Country
    public string? ShipperName { get; set; }
    public string? PurchaseCountry { get; set; }
    public string? OriginCountry { get; set; }
    public string? CountryOfLoading { get; set; }

    // Payment
    public string? PaymentTerm { get; set; }
    public string? IncoTerm { get; set; }
    public string? Currency { get; set; }
    public decimal? ExchRate { get; set; }

    // Charges
    public decimal? InLandCharge { get; set; }
    public string? InLandChargeCurrency { get; set; }
    public decimal? InLandChargeTHB { get; set; }
    public decimal? Freight { get; set; }
    public string? FreightCurrency { get; set; }
    public decimal? FreightTHB { get; set; }
    public decimal? Insurance { get; set; }
    public string? InsuranceCurrency { get; set; }
    public decimal? InsuranceTHB { get; set; }
    public decimal? Packing { get; set; }
    public string? PackingCurrency { get; set; }
    public decimal? PackingTHB { get; set; }
    public decimal? ForeInLand { get; set; }
    public string? ForeInLandCurrency { get; set; }
    public decimal? ForeInLandTHB { get; set; }
    public decimal? Landing { get; set; }
    public string? LandingCurrency { get; set; }
    public decimal? LandingTHB { get; set; }
    public decimal? OtherCharge1 { get; set; }
    public string? OtherCharge1Currency { get; set; }
    public decimal? OtherCharge1THB { get; set; }
    public decimal? OtherCharge2 { get; set; }
    public string? OtherCharge2Currency { get; set; }
    public decimal? OtherCharge2THB { get; set; }

    // AEO / Invoice
    public string? AEORefNo { get; set; }
    public string? InvoiceNo { get; set; }
    public string? InvDate { get; set; }
    public decimal? ItemNo { get; set; }

    // Product
    public string? ProductCode { get; set; }
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionEn2 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? DescriptionTh2 { get; set; }
    public string? PermitNo { get; set; }
    public string? ShippingMark { get; set; }
    public string? Remark { get; set; }
    public string? RtcProductCode { get; set; }
    public string? Brand { get; set; }
    public string? TarifClass { get; set; }
    public string? StateCode { get; set; }
    public string? StateUnit { get; set; }
    public string? Sequence { get; set; }
    public string? Privilege { get; set; }

    // Privilege details
    public string? ReasonReserveRight { get; set; }
    public string? TariffDispute { get; set; }
    public string? SQDispute { get; set; }
    public string? RateDispute { get; set; }
    public string? PrivilegeDispute { get; set; }
    public string? TypeOfTariff { get; set; }
    public string? Origin { get; set; }
    public string? TypeOfProduct { get; set; }
    public string? TypeOfProduct2 { get; set; }
    public string? CommNo { get; set; }
    public string? PaintCode { get; set; }
    public string? UphCode { get; set; }
    public string? ProductYear { get; set; }

    // Quantity / Weight
    public decimal? NetWeight { get; set; }
    public decimal? QtyDegree { get; set; }
    public decimal? Quantity { get; set; }
    public string? QuantityUnit { get; set; }
    public decimal? QtyTariff { get; set; }
    public string? QtyUnit { get; set; }
    public decimal? PackNo { get; set; }
    public string? PackUnit { get; set; }

    // Price / Value
    public decimal? UnitPrice { get; set; }
    public decimal? InvoiceAmount { get; set; }
    public decimal? AmountCIF { get; set; }
    public string? CIFCurrency { get; set; }
    public decimal? CIFTHB { get; set; }
    public decimal? AddPrice { get; set; }
    public decimal? AddPriceTHB { get; set; }
    public decimal? CIFReductionRate { get; set; }
    public decimal? AssessedPrice { get; set; }
    public decimal? AssessedAmount { get; set; }

    // Excise / Tax
    public string? ExciseTariff { get; set; }
    public decimal? UsedQty { get; set; }
    public decimal? AssessedQty { get; set; }
    public string? AssessedQtyUnit { get; set; }
    public string? SubQty { get; set; }
    public string? OriginCriteria { get; set; }
    public string? CerExporterTax { get; set; }
    public string? ImportTaxInc { get; set; }
    public string? AHTNCode { get; set; }

    // Material / Rights
    public string? MaterialCode { get; set; }
    public string? DangerousCode { get; set; }
    public string? UsePrivilege { get; set; }
    public string? BOICardNo { get; set; }
    public string? ProductionFormula { get; set; }
    public string? IsFreebie { get; set; }
    public string? RefDeclarNo { get; set; }
    public decimal? RefItemNo { get; set; }

    // Duty / Tax rates
    public decimal? DutyRate { get; set; }
    public decimal? DutyRateS { get; set; }
    public decimal? DutyTextTHB { get; set; }
    public string? ExciseTaxRate { get; set; }
    public string? ExciseTax { get; set; }
    public string? InteriorTax { get; set; }
    public string? OtherTax { get; set; }
    public string? Fee { get; set; }
    public decimal? VATBase { get; set; }
    public decimal? Vat { get; set; }
    public decimal? TotalDutyVAT { get; set; }

    // Status / Timestamps
    public string? EDIDateTime { get; set; }
    public string? StampDateTime { get; set; }
    public string? EDIStatus { get; set; }
    public string? RemarkInternal { get; set; }
    public decimal? DepositAmt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
