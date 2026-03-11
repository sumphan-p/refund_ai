using System.Data;
using Dapper;
using imp_api.Models;

namespace imp_api.Repositories;

public class ExportExcelRepository : IExportExcelRepository
{
    private readonly IDbConnection _db;

    public ExportExcelRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task<int> UpsertBatchAsync(IEnumerable<ExportExcel> records, string userName)
    {
        const string sql = @"
            MERGE imp.export_excel AS target
            USING (SELECT @DeclarNo AS DeclarNo, @ItemDeclarNo AS ItemDeclarNo) AS source
            ON target.DeclarNo = source.DeclarNo AND target.ItemDeclarNo = source.ItemDeclarNo
            WHEN MATCHED THEN
                UPDATE SET
                    ExporterName = @ExporterName, TaxId = @TaxId, BranchSeq = @BranchSeq,
                    DocumentType = @DocumentType, BuyerName = @BuyerName,
                    InvoiceNo = @InvoiceNo, InvDate = @InvDate, InvoiceItemNo = @InvoiceItemNo,
                    SubmissionDate = @SubmissionDate, ReleaseDate = @ReleaseDate, LoadingDate = @LoadingDate,
                    CurrentStatus = @CurrentStatus,
                    ProductCode = @ProductCode, Brand = @Brand, PurchaseOrder = @PurchaseOrder,
                    DescriptionEn1 = @DescriptionEn1, DescriptionEn2 = @DescriptionEn2,
                    DescriptionEn3 = @DescriptionEn3, DescriptionEn4 = @DescriptionEn4,
                    DescriptionTh1 = @DescriptionTh1, DescriptionTh2 = @DescriptionTh2,
                    DescriptionTh3 = @DescriptionTh3, DescriptionTh4 = @DescriptionTh4,
                    TermOfPayment = @TermOfPayment, TariffCode = @TariffCode, TariffType = @TariffType,
                    StatisticalCode = @StatisticalCode, StatisticalUnit = @StatisticalUnit,
                    PackageCount = @PackageCount, PackageUnit = @PackageUnit,
                    QtyInvoice = @QtyInvoice, QtyInvoiceUnit = @QtyInvoiceUnit,
                    QtyDeclar = @QtyDeclar, QtyDeclarUnit = @QtyDeclarUnit,
                    NetWeight = @NetWeight, NetWeightUnit = @NetWeightUnit,
                    UnitPrice = @UnitPrice, FOBForeign = @FOBForeign,
                    CurrencyCode = @CurrencyCode, FOBTHB = @FOBTHB,
                    VesselName = @VesselName, ExportPortCode = @ExportPortCode,
                    InspectionLocationCode = @InspectionLocationCode,
                    BuyerCountryCode = @BuyerCountryCode, DestinationCountryCode = @DestinationCountryCode,
                    Compensation = @Compensation, CompensationNo = @CompensationNo,
                    BOI = @BOI, BOINo = @BOINo, FormulaBOI = @FormulaBOI,
                    Section19Bis = @Section19Bis, Section19BisNo = @Section19BisNo,
                    RightsTransferNo = @RightsTransferNo, Bond = @Bond,
                    ModelNo = @ModelNo, ModelVer = @ModelVer, ModelCompTax = @ModelCompTax,
                    EPZ = @EPZ, FZ = @FZ,
                    ImportTaxIncentiveId = @ImportTaxIncentiveId, ExportTaxIncentiveId = @ExportTaxIncentiveId,
                    ReExport = @ReExport, NetReturn = @NetReturn,
                    SpecialPrivilegeCode = @SpecialPrivilegeCode, OriginCountryCode = @OriginCountryCode,
                    ImportDeclarNo = @ImportDeclarNo, ImportDeclarItemNo = @ImportDeclarItemNo,
                    ShortDeclar = @ShortDeclar, ShortPack = @ShortPack,
                    ShortQty = @ShortQty, ShortNetWeight = @ShortNetWeight,
                    ShortFOBForeign = @ShortFOBForeign, ShortFOBTHB = @ShortFOBTHB,
                    ShortPostDeclar = @ShortPostDeclar, ShortPostPack = @ShortPostPack,
                    ShortPostQty = @ShortPostQty, ShortPostNetWeight = @ShortPostNetWeight,
                    ShortPostFOBForeign = @ShortPostFOBForeign, ShortPostFOBTHB = @ShortPostFOBTHB,
                    PermitNo1 = @PermitNo1, PermitNo2 = @PermitNo2, PermitNo3 = @PermitNo3,
                    BookingNo = @BookingNo, HouseBLNo = @HouseBLNo, Remark = @Remark,
                    UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UserName
            WHEN NOT MATCHED THEN
                INSERT (
                    DeclarNo, ItemDeclarNo,
                    ExporterName, TaxId, BranchSeq, DocumentType, BuyerName,
                    InvoiceNo, InvDate, InvoiceItemNo,
                    SubmissionDate, ReleaseDate, LoadingDate, CurrentStatus,
                    ProductCode, Brand, PurchaseOrder,
                    DescriptionEn1, DescriptionEn2, DescriptionEn3, DescriptionEn4,
                    DescriptionTh1, DescriptionTh2, DescriptionTh3, DescriptionTh4,
                    TermOfPayment, TariffCode, TariffType, StatisticalCode, StatisticalUnit,
                    PackageCount, PackageUnit, QtyInvoice, QtyInvoiceUnit,
                    QtyDeclar, QtyDeclarUnit, NetWeight, NetWeightUnit,
                    UnitPrice, FOBForeign, CurrencyCode, FOBTHB,
                    VesselName, ExportPortCode, InspectionLocationCode,
                    BuyerCountryCode, DestinationCountryCode,
                    Compensation, CompensationNo, BOI, BOINo, FormulaBOI,
                    Section19Bis, Section19BisNo, RightsTransferNo, Bond,
                    ModelNo, ModelVer, ModelCompTax,
                    EPZ, FZ, ImportTaxIncentiveId, ExportTaxIncentiveId,
                    ReExport, NetReturn, SpecialPrivilegeCode, OriginCountryCode,
                    ImportDeclarNo, ImportDeclarItemNo,
                    ShortDeclar, ShortPack, ShortQty, ShortNetWeight, ShortFOBForeign, ShortFOBTHB,
                    ShortPostDeclar, ShortPostPack, ShortPostQty, ShortPostNetWeight, ShortPostFOBForeign, ShortPostFOBTHB,
                    PermitNo1, PermitNo2, PermitNo3, BookingNo, HouseBLNo, Remark,
                    CreatedBy
                )
                VALUES (
                    @DeclarNo, @ItemDeclarNo,
                    @ExporterName, @TaxId, @BranchSeq, @DocumentType, @BuyerName,
                    @InvoiceNo, @InvDate, @InvoiceItemNo,
                    @SubmissionDate, @ReleaseDate, @LoadingDate, @CurrentStatus,
                    @ProductCode, @Brand, @PurchaseOrder,
                    @DescriptionEn1, @DescriptionEn2, @DescriptionEn3, @DescriptionEn4,
                    @DescriptionTh1, @DescriptionTh2, @DescriptionTh3, @DescriptionTh4,
                    @TermOfPayment, @TariffCode, @TariffType, @StatisticalCode, @StatisticalUnit,
                    @PackageCount, @PackageUnit, @QtyInvoice, @QtyInvoiceUnit,
                    @QtyDeclar, @QtyDeclarUnit, @NetWeight, @NetWeightUnit,
                    @UnitPrice, @FOBForeign, @CurrencyCode, @FOBTHB,
                    @VesselName, @ExportPortCode, @InspectionLocationCode,
                    @BuyerCountryCode, @DestinationCountryCode,
                    @Compensation, @CompensationNo, @BOI, @BOINo, @FormulaBOI,
                    @Section19Bis, @Section19BisNo, @RightsTransferNo, @Bond,
                    @ModelNo, @ModelVer, @ModelCompTax,
                    @EPZ, @FZ, @ImportTaxIncentiveId, @ExportTaxIncentiveId,
                    @ReExport, @NetReturn, @SpecialPrivilegeCode, @OriginCountryCode,
                    @ImportDeclarNo, @ImportDeclarItemNo,
                    @ShortDeclar, @ShortPack, @ShortQty, @ShortNetWeight, @ShortFOBForeign, @ShortFOBTHB,
                    @ShortPostDeclar, @ShortPostPack, @ShortPostQty, @ShortPostNetWeight, @ShortPostFOBForeign, @ShortPostFOBTHB,
                    @PermitNo1, @PermitNo2, @PermitNo3, @BookingNo, @HouseBLNo, @Remark,
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

    public async Task<IEnumerable<ExportExcel>> GetAllAsync(int? limit = null)
    {
        var sql = limit.HasValue
            ? $"SELECT TOP ({limit.Value}) * FROM imp.export_excel ORDER BY CreatedAt DESC"
            : "SELECT * FROM imp.export_excel ORDER BY CreatedAt DESC";

        return await _db.QueryAsync<ExportExcel>(sql);
    }

    public async Task<ExportExcel?> GetByIdAsync(int id)
    {
        return await _db.QuerySingleOrDefaultAsync<ExportExcel>(
            "SELECT * FROM imp.export_excel WHERE Id = @Id", new { Id = id });
    }

    private static string BuildWhereClause(string? declarNo, string? invoiceNo, string? productCode, string? buyerName, DynamicParameters p)
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
        if (!string.IsNullOrWhiteSpace(buyerName))
        {
            conditions.Add("BuyerName LIKE @BuyerName");
            p.Add("BuyerName", $"%{buyerName.Trim()}%");
        }
        return conditions.Count > 0 ? " WHERE " + string.Join(" AND ", conditions) : "";
    }

    public async Task<IEnumerable<ExportExcel>> SearchAsync(string? declarNo, string? invoiceNo, string? productCode, string? buyerName, int page, int pageSize)
    {
        var p = new DynamicParameters();
        var where = BuildWhereClause(declarNo, invoiceNo, productCode, buyerName, p);
        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var sql = $"SELECT * FROM imp.export_excel{where} ORDER BY DeclarNo ASC, ItemDeclarNo ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        return await _db.QueryAsync<ExportExcel>(sql, p);
    }

    public async Task<int> CountAsync(string? declarNo, string? invoiceNo, string? productCode, string? buyerName)
    {
        var p = new DynamicParameters();
        var where = BuildWhereClause(declarNo, invoiceNo, productCode, buyerName, p);
        var sql = $"SELECT COUNT(*) FROM imp.export_excel{where}";
        return await _db.ExecuteScalarAsync<int>(sql, p);
    }

    public async Task UpdateAsync(int id, ExportExcel record, string userName)
    {
        const string sql = @"
            UPDATE imp.export_excel SET
                ExporterName = @ExporterName, TaxId = @TaxId, BuyerName = @BuyerName,
                InvoiceNo = @InvoiceNo, InvDate = @InvDate,
                ProductCode = @ProductCode, DescriptionEn1 = @DescriptionEn1, DescriptionEn2 = @DescriptionEn2,
                DescriptionTh1 = @DescriptionTh1, DescriptionTh2 = @DescriptionTh2,
                Brand = @Brand, QtyInvoice = @QtyInvoice, QtyInvoiceUnit = @QtyInvoiceUnit,
                UnitPrice = @UnitPrice, CurrencyCode = @CurrencyCode, FOBTHB = @FOBTHB,
                CurrentStatus = @CurrentStatus, Remark = @Remark,
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
            "DELETE FROM imp.export_excel WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
