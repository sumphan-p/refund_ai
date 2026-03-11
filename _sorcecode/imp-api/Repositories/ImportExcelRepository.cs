using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class ImportExcelRepository : IImportExcelRepository
{
    private readonly IDbConnection _db;

    public ImportExcelRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<int> UpsertBatchAsync(IEnumerable<ImportExcel> records, string userName)
    {
        const string sql = @"
            MERGE imp.import_excel AS target
            USING (SELECT @DeclarNo AS DeclarNo, @ItemDeclarNo AS ItemDeclarNo) AS source
            ON target.DeclarNo = source.DeclarNo AND target.ItemDeclarNo = source.ItemDeclarNo
            WHEN MATCHED THEN
                UPDATE SET
                    CustomerName = @CustomerName, CompanyTaxNo = @CompanyTaxNo, RefNo = @RefNo,
                    JobNo = @JobNo, VesselName = @VesselName, Voy = @Voy,
                    ExportIncentiveId = @ExportIncentiveId, TradPartner = @TradPartner,
                    TransportMode = @TransportMode, ReloadPort = @ReloadPort, Subgate = @Subgate,
                    InspectionCode = @InspectionCode, ApprovedPort = @ApprovedPort, ETA = @ETA,
                    MasterBL = @MasterBL, HouseBL = @HouseBL,
                    FactoryNo = @FactoryNo, EstablishNo = @EstablishNo, ApprovedNo = @ApprovedNo,
                    StatusBuyer = @StatusBuyer, LevelBuyer = @LevelBuyer, ExporterRef = @ExporterRef,
                    PurchaseNo = @PurchaseNo, OtherRefNo = @OtherRefNo,
                    ShipperName = @ShipperName, PurchaseCountry = @PurchaseCountry,
                    OriginCountry = @OriginCountry, CountryOfLoading = @CountryOfLoading,
                    PaymentTerm = @PaymentTerm, IncoTerm = @IncoTerm, Currency = @Currency, ExchRate = @ExchRate,
                    InLandCharge = @InLandCharge, InLandChargeCurrency = @InLandChargeCurrency, InLandChargeTHB = @InLandChargeTHB,
                    Freight = @Freight, FreightCurrency = @FreightCurrency, FreightTHB = @FreightTHB,
                    Insurance = @Insurance, InsuranceCurrency = @InsuranceCurrency, InsuranceTHB = @InsuranceTHB,
                    Packing = @Packing, PackingCurrency = @PackingCurrency, PackingTHB = @PackingTHB,
                    ForeInLand = @ForeInLand, ForeInLandCurrency = @ForeInLandCurrency, ForeInLandTHB = @ForeInLandTHB,
                    Landing = @Landing, LandingCurrency = @LandingCurrency, LandingTHB = @LandingTHB,
                    OtherCharge1 = @OtherCharge1, OtherCharge1Currency = @OtherCharge1Currency, OtherCharge1THB = @OtherCharge1THB,
                    OtherCharge2 = @OtherCharge2, OtherCharge2Currency = @OtherCharge2Currency, OtherCharge2THB = @OtherCharge2THB,
                    AEORefNo = @AEORefNo, InvoiceNo = @InvoiceNo, InvDate = @InvDate, ItemNo = @ItemNo,
                    ProductCode = @ProductCode, DescriptionEn1 = @DescriptionEn1, DescriptionEn2 = @DescriptionEn2,
                    DescriptionTh1 = @DescriptionTh1, DescriptionTh2 = @DescriptionTh2,
                    PermitNo = @PermitNo, ShippingMark = @ShippingMark, Remark = @Remark,
                    RtcProductCode = @RtcProductCode, Brand = @Brand, TarifClass = @TarifClass,
                    StateCode = @StateCode, StateUnit = @StateUnit, Sequence = @Sequence, Privilege = @Privilege,
                    ReasonReserveRight = @ReasonReserveRight, TariffDispute = @TariffDispute,
                    SQDispute = @SQDispute, RateDispute = @RateDispute, PrivilegeDispute = @PrivilegeDispute,
                    TypeOfTariff = @TypeOfTariff, Origin = @Origin,
                    TypeOfProduct = @TypeOfProduct, TypeOfProduct2 = @TypeOfProduct2,
                    CommNo = @CommNo, PaintCode = @PaintCode, UphCode = @UphCode, ProductYear = @ProductYear,
                    NetWeight = @NetWeight, QtyDegree = @QtyDegree, Quantity = @Quantity, QuantityUnit = @QuantityUnit,
                    QtyTariff = @QtyTariff, QtyUnit = @QtyUnit, PackNo = @PackNo, PackUnit = @PackUnit,
                    UnitPrice = @UnitPrice, InvoiceAmount = @InvoiceAmount, AmountCIF = @AmountCIF,
                    CIFCurrency = @CIFCurrency, CIFTHB = @CIFTHB,
                    AddPrice = @AddPrice, AddPriceTHB = @AddPriceTHB,
                    CIFReductionRate = @CIFReductionRate, AssessedPrice = @AssessedPrice, AssessedAmount = @AssessedAmount,
                    ExciseTariff = @ExciseTariff, UsedQty = @UsedQty, AssessedQty = @AssessedQty,
                    AssessedQtyUnit = @AssessedQtyUnit, SubQty = @SubQty, OriginCriteria = @OriginCriteria,
                    CerExporterTax = @CerExporterTax, ImportTaxInc = @ImportTaxInc, AHTNCode = @AHTNCode,
                    MaterialCode = @MaterialCode, DangerousCode = @DangerousCode, UsePrivilege = @UsePrivilege,
                    BOICardNo = @BOICardNo, ProductionFormula = @ProductionFormula, IsFreebie = @IsFreebie,
                    RefDeclarNo = @RefDeclarNo, RefItemNo = @RefItemNo,
                    DutyRate = @DutyRate, DutyRateS = @DutyRateS, DutyTextTHB = @DutyTextTHB,
                    ExciseTaxRate = @ExciseTaxRate, ExciseTax = @ExciseTax,
                    InteriorTax = @InteriorTax, OtherTax = @OtherTax, Fee = @Fee,
                    VATBase = @VATBase, Vat = @Vat, TotalDutyVAT = @TotalDutyVAT,
                    EDIDateTime = @EDIDateTime, StampDateTime = @StampDateTime, EDIStatus = @EDIStatus,
                    RemarkInternal = @RemarkInternal, DepositAmt = @DepositAmt,
                    UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UserName
            WHEN NOT MATCHED THEN
                INSERT (
                    DeclarNo, ItemDeclarNo, CustomerName, CompanyTaxNo, RefNo,
                    JobNo, VesselName, Voy, ExportIncentiveId, TradPartner,
                    TransportMode, ReloadPort, Subgate, InspectionCode, ApprovedPort, ETA,
                    MasterBL, HouseBL, FactoryNo, EstablishNo, ApprovedNo,
                    StatusBuyer, LevelBuyer, ExporterRef, PurchaseNo, OtherRefNo,
                    ShipperName, PurchaseCountry, OriginCountry, CountryOfLoading,
                    PaymentTerm, IncoTerm, Currency, ExchRate,
                    InLandCharge, InLandChargeCurrency, InLandChargeTHB,
                    Freight, FreightCurrency, FreightTHB,
                    Insurance, InsuranceCurrency, InsuranceTHB,
                    Packing, PackingCurrency, PackingTHB,
                    ForeInLand, ForeInLandCurrency, ForeInLandTHB,
                    Landing, LandingCurrency, LandingTHB,
                    OtherCharge1, OtherCharge1Currency, OtherCharge1THB,
                    OtherCharge2, OtherCharge2Currency, OtherCharge2THB,
                    AEORefNo, InvoiceNo, InvDate, ItemNo,
                    ProductCode, DescriptionEn1, DescriptionEn2, DescriptionTh1, DescriptionTh2,
                    PermitNo, ShippingMark, Remark, RtcProductCode, Brand, TarifClass,
                    StateCode, StateUnit, Sequence, Privilege,
                    ReasonReserveRight, TariffDispute, SQDispute, RateDispute, PrivilegeDispute,
                    TypeOfTariff, Origin, TypeOfProduct, TypeOfProduct2,
                    CommNo, PaintCode, UphCode, ProductYear,
                    NetWeight, QtyDegree, Quantity, QuantityUnit, QtyTariff, QtyUnit, PackNo, PackUnit,
                    UnitPrice, InvoiceAmount, AmountCIF, CIFCurrency, CIFTHB,
                    AddPrice, AddPriceTHB, CIFReductionRate, AssessedPrice, AssessedAmount,
                    ExciseTariff, UsedQty, AssessedQty, AssessedQtyUnit, SubQty, OriginCriteria,
                    CerExporterTax, ImportTaxInc, AHTNCode,
                    MaterialCode, DangerousCode, UsePrivilege, BOICardNo, ProductionFormula, IsFreebie,
                    RefDeclarNo, RefItemNo,
                    DutyRate, DutyRateS, DutyTextTHB, ExciseTaxRate, ExciseTax,
                    InteriorTax, OtherTax, Fee, VATBase, Vat, TotalDutyVAT,
                    EDIDateTime, StampDateTime, EDIStatus, RemarkInternal, DepositAmt,
                    CreatedBy
                )
                VALUES (
                    @DeclarNo, @ItemDeclarNo, @CustomerName, @CompanyTaxNo, @RefNo,
                    @JobNo, @VesselName, @Voy, @ExportIncentiveId, @TradPartner,
                    @TransportMode, @ReloadPort, @Subgate, @InspectionCode, @ApprovedPort, @ETA,
                    @MasterBL, @HouseBL, @FactoryNo, @EstablishNo, @ApprovedNo,
                    @StatusBuyer, @LevelBuyer, @ExporterRef, @PurchaseNo, @OtherRefNo,
                    @ShipperName, @PurchaseCountry, @OriginCountry, @CountryOfLoading,
                    @PaymentTerm, @IncoTerm, @Currency, @ExchRate,
                    @InLandCharge, @InLandChargeCurrency, @InLandChargeTHB,
                    @Freight, @FreightCurrency, @FreightTHB,
                    @Insurance, @InsuranceCurrency, @InsuranceTHB,
                    @Packing, @PackingCurrency, @PackingTHB,
                    @ForeInLand, @ForeInLandCurrency, @ForeInLandTHB,
                    @Landing, @LandingCurrency, @LandingTHB,
                    @OtherCharge1, @OtherCharge1Currency, @OtherCharge1THB,
                    @OtherCharge2, @OtherCharge2Currency, @OtherCharge2THB,
                    @AEORefNo, @InvoiceNo, @InvDate, @ItemNo,
                    @ProductCode, @DescriptionEn1, @DescriptionEn2, @DescriptionTh1, @DescriptionTh2,
                    @PermitNo, @ShippingMark, @Remark, @RtcProductCode, @Brand, @TarifClass,
                    @StateCode, @StateUnit, @Sequence, @Privilege,
                    @ReasonReserveRight, @TariffDispute, @SQDispute, @RateDispute, @PrivilegeDispute,
                    @TypeOfTariff, @Origin, @TypeOfProduct, @TypeOfProduct2,
                    @CommNo, @PaintCode, @UphCode, @ProductYear,
                    @NetWeight, @QtyDegree, @Quantity, @QuantityUnit, @QtyTariff, @QtyUnit, @PackNo, @PackUnit,
                    @UnitPrice, @InvoiceAmount, @AmountCIF, @CIFCurrency, @CIFTHB,
                    @AddPrice, @AddPriceTHB, @CIFReductionRate, @AssessedPrice, @AssessedAmount,
                    @ExciseTariff, @UsedQty, @AssessedQty, @AssessedQtyUnit, @SubQty, @OriginCriteria,
                    @CerExporterTax, @ImportTaxInc, @AHTNCode,
                    @MaterialCode, @DangerousCode, @UsePrivilege, @BOICardNo, @ProductionFormula, @IsFreebie,
                    @RefDeclarNo, @RefItemNo,
                    @DutyRate, @DutyRateS, @DutyTextTHB, @ExciseTaxRate, @ExciseTax,
                    @InteriorTax, @OtherTax, @Fee, @VATBase, @Vat, @TotalDutyVAT,
                    @EDIDateTime, @StampDateTime, @EDIStatus, @RemarkInternal, @DepositAmt,
                    @UserName
                );";

        if (_db.State != ConnectionState.Open)
            _db.Open();

        using var transaction = _db.BeginTransaction();
        var affected = 0;

        foreach (var record in records)
        {
            var parameters = new DynamicParameters(record);
            parameters.Add("UserName", userName);
            affected += await _db.ExecuteAsync(sql, parameters, transaction);
        }

        transaction.Commit();
        return affected;
    }

    public async Task<IEnumerable<ImportExcel>> GetAllAsync(int? limit = null)
    {
        var sql = limit.HasValue
            ? $"SELECT TOP ({limit.Value}) * FROM imp.import_excel ORDER BY CreatedAt DESC"
            : "SELECT * FROM imp.import_excel ORDER BY CreatedAt DESC";

        return await _db.QueryAsync<ImportExcel>(sql);
    }

    public async Task<ImportExcel?> GetByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<ImportExcel>(
            "SELECT * FROM imp.import_excel WHERE Id = @Id", new { Id = id });
    }

    private static string BuildWhereClause(string? declarNo, string? invoiceNo, string? productCode, string? brand, DynamicParameters p)
    {
        var conditions = new List<string>();
        if (!string.IsNullOrWhiteSpace(declarNo))
        {
            conditions.Add("DeclarNo LIKE @DeclarNo");
            p.Add("DeclarNo", $"%{declarNo.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(invoiceNo))
        {
            conditions.Add("InvoiceNo LIKE @InvoiceNo");
            p.Add("InvoiceNo", $"%{invoiceNo.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            conditions.Add("ProductCode LIKE @ProductCode");
            p.Add("ProductCode", $"%{productCode.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(brand))
        {
            conditions.Add("Brand LIKE @Brand");
            p.Add("Brand", $"%{brand.Trim()}%");
        }
        return conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
    }

    public async Task<IEnumerable<ImportExcel>> SearchAsync(string? declarNo, string? invoiceNo, string? productCode, string? brand, int page, int pageSize)
    {
        var p = new DynamicParameters();
        var where = BuildWhereClause(declarNo, invoiceNo, productCode, brand, p);
        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var sql = $"SELECT * FROM imp.import_excel{where} ORDER BY DeclarNo ASC, ItemDeclarNo ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        return await _db.QueryAsync<ImportExcel>(sql, p);
    }

    public async Task<int> CountAsync(string? declarNo, string? invoiceNo, string? productCode, string? brand)
    {
        var p = new DynamicParameters();
        var where = BuildWhereClause(declarNo, invoiceNo, productCode, brand, p);
        var sql = $"SELECT COUNT(*) FROM imp.import_excel{where}";
        return await _db.ExecuteScalarAsync<int>(sql, p);
    }

    public async Task UpdateAsync(int id, ImportExcel record, string userName)
    {
        const string sql = @"
            UPDATE imp.import_excel SET
                CustomerName = @CustomerName, CompanyTaxNo = @CompanyTaxNo, RefNo = @RefNo,
                InvoiceNo = @InvoiceNo, InvDate = @InvDate,
                ProductCode = @ProductCode, DescriptionEn1 = @DescriptionEn1, DescriptionEn2 = @DescriptionEn2,
                DescriptionTh1 = @DescriptionTh1, DescriptionTh2 = @DescriptionTh2,
                Brand = @Brand, Quantity = @Quantity, QuantityUnit = @QuantityUnit,
                UnitPrice = @UnitPrice, Currency = @Currency, CIFTHB = @CIFTHB,
                DutyRate = @DutyRate, TotalDutyVAT = @TotalDutyVAT, UsePrivilege = @UsePrivilege,
                Remark = @Remark, RemarkInternal = @RemarkInternal,
                UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UserName
            WHERE Id = @Id";

        var p = new DynamicParameters(record);
        p.Add("Id", id);
        p.Add("UserName", userName);
        await _db.ExecuteAsync(sql, p);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var affected = await _db.ExecuteAsync(
            "DELETE FROM imp.import_excel WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
