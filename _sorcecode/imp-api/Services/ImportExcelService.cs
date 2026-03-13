using System.Data;
using Dapper;
using ExcelDataReader;
using imp_api.DTOs;
using imp_api.Models;
using imp_api.Repositories;

namespace imp_api.Services;

public class ImportExcelService : IImportExcelService
{
    private readonly IImportExcelRepository _repository;
    private readonly IDbConnection _db;
    private readonly string _connectionString;

    public ImportExcelService(IImportExcelRepository repository, IDbConnection db, IConfiguration configuration)
    {
        _repository = repository;
        _db = db;
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    // Expected column count for Import Excel structure (Import_YYYYMMDD.xls)
    private const int ExpectedColumnCount = 141;

    // Key header names at specific positions to validate Import structure
    private static readonly (int Index, string ExpectedHeader)[] HeaderChecks =
    [
        (3, "Declar no."),         // DeclarNo
        (4, "item declar no."),    // ItemDeclarNo
        (59, "Invoice No"),        // InvoiceNo
        (62, "ProductCode"),       // ProductCode
        (92, "Quantity"),          // Quantity
    ];

    public Task<List<ImportExcel>> ParseExcelAsync(Stream fileStream)
    {
        using var reader = ExcelReaderFactory.CreateReader(fileStream, new ExcelReaderConfiguration
        {
            FallbackEncoding = System.Text.Encoding.GetEncoding(874) // Thai Windows (TIS-620)
        });
        var records = new List<ImportExcel>();

        // Read header row
        if (!reader.Read()) return Task.FromResult(records);

        // Validate structure: column count
        if (reader.FieldCount < ExpectedColumnCount)
        {
            throw new AppException("INVALID_STRUCTURE",
                $"โครงสร้างไฟล์ไม่ถูกต้อง: ไฟล์มี {reader.FieldCount} คอลัมน์ แต่ต้องมีอย่างน้อย {ExpectedColumnCount} คอลัมน์ กรุณาใช้ไฟล์โครงสร้าง Import (Import_YYYYMMDD.xls) เท่านั้น");
        }

        // Validate structure: check key header names
        var mismatchedHeaders = new List<string>();
        foreach (var (index, expected) in HeaderChecks)
        {
            var actual = reader.GetValue(index)?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(actual) && !actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                mismatchedHeaders.Add($"คอลัมน์ที่ {index + 1}: พบ '{actual}' แต่คาดว่า '{expected}'");
            }
        }

        if (mismatchedHeaders.Count > 0)
        {
            throw new AppException("INVALID_STRUCTURE",
                $"โครงสร้างไฟล์ไม่ตรงกับรูปแบบ Import กรุณาใช้ไฟล์โครงสร้าง Import (Import_YYYYMMDD.xls) เท่านั้น ({string.Join(", ", mismatchedHeaders)})");
        }

        while (reader.Read())
        {
            var declarNo = GetString(reader, 3);
            var itemDeclarNo = GetInt(reader, 4);

            if (string.IsNullOrWhiteSpace(declarNo)) continue;

            records.Add(new ImportExcel
            {
                DeclarNo = declarNo,
                ItemDeclarNo = itemDeclarNo,
                CustomerName = GetString(reader, 0),
                CompanyTaxNo = GetString(reader, 1),
                RefNo = GetString(reader, 2),
                JobNo = GetString(reader, 5),
                VesselName = GetString(reader, 6),
                Voy = GetString(reader, 7),
                ExportIncentiveId = GetString(reader, 8),
                TradPartner = GetString(reader, 9),
                TransportMode = GetString(reader, 10),
                ReloadPort = GetString(reader, 11),
                Subgate = GetString(reader, 12),
                InspectionCode = GetString(reader, 13),
                ApprovedPort = GetString(reader, 14),
                ETA = GetString(reader, 15),
                MasterBL = GetString(reader, 16),
                HouseBL = GetString(reader, 17),
                FactoryNo = GetString(reader, 18),
                EstablishNo = GetString(reader, 19),
                ApprovedNo = GetString(reader, 20),
                StatusBuyer = GetString(reader, 21),
                LevelBuyer = GetString(reader, 22),
                ExporterRef = GetString(reader, 23),
                PurchaseNo = GetString(reader, 24),
                OtherRefNo = GetString(reader, 25),
                ShipperName = GetString(reader, 26),
                PurchaseCountry = GetString(reader, 27),
                OriginCountry = GetString(reader, 28),
                CountryOfLoading = GetString(reader, 29),
                PaymentTerm = GetString(reader, 30),
                IncoTerm = GetString(reader, 31),
                Currency = GetString(reader, 32),
                ExchRate = GetDecimal(reader, 33),
                InLandCharge = GetDecimal(reader, 34),
                InLandChargeCurrency = GetString(reader, 35),
                InLandChargeTHB = GetDecimal(reader, 36),
                Freight = GetDecimal(reader, 37),
                FreightCurrency = GetString(reader, 38),
                FreightTHB = GetDecimal(reader, 39),
                Insurance = GetDecimal(reader, 40),
                InsuranceCurrency = GetString(reader, 41),
                InsuranceTHB = GetDecimal(reader, 42),
                Packing = GetDecimal(reader, 43),
                PackingCurrency = GetString(reader, 44),
                PackingTHB = GetDecimal(reader, 45),
                ForeInLand = GetDecimal(reader, 46),
                ForeInLandCurrency = GetString(reader, 47),
                ForeInLandTHB = GetDecimal(reader, 48),
                Landing = GetDecimal(reader, 49),
                LandingCurrency = GetString(reader, 50),
                LandingTHB = GetDecimal(reader, 51),
                OtherCharge1 = GetDecimal(reader, 52),
                OtherCharge1Currency = GetString(reader, 53),
                OtherCharge1THB = GetDecimal(reader, 54),
                OtherCharge2 = GetDecimal(reader, 55),
                OtherCharge2Currency = GetString(reader, 56),
                OtherCharge2THB = GetDecimal(reader, 57),
                AEORefNo = GetString(reader, 58),
                InvoiceNo = GetString(reader, 59),
                InvDate = GetString(reader, 60),
                ItemNo = GetDecimal(reader, 61),
                ProductCode = GetString(reader, 62),
                DescriptionEn1 = GetString(reader, 63),
                DescriptionEn2 = GetString(reader, 64),
                DescriptionTh1 = GetString(reader, 65),
                DescriptionTh2 = GetString(reader, 66),
                PermitNo = GetString(reader, 67),
                ShippingMark = GetString(reader, 68),
                Remark = GetString(reader, 69),
                RtcProductCode = GetString(reader, 70),
                Brand = GetString(reader, 71),
                TarifClass = GetString(reader, 72),
                StateCode = GetString(reader, 73),
                StateUnit = GetString(reader, 74),
                Sequence = GetString(reader, 75),
                Privilege = GetString(reader, 76),
                ReasonReserveRight = GetString(reader, 77),
                TariffDispute = GetString(reader, 78),
                SQDispute = GetString(reader, 79),
                RateDispute = GetString(reader, 80),
                PrivilegeDispute = GetString(reader, 81),
                TypeOfTariff = GetString(reader, 82),
                Origin = GetString(reader, 83),
                TypeOfProduct = GetString(reader, 84),
                TypeOfProduct2 = GetString(reader, 85),
                CommNo = GetString(reader, 86),
                PaintCode = GetString(reader, 87),
                UphCode = GetString(reader, 88),
                ProductYear = GetString(reader, 89),
                NetWeight = GetDecimal(reader, 90),
                QtyDegree = GetDecimal(reader, 91),
                Quantity = GetDecimal(reader, 92),
                QuantityUnit = GetString(reader, 93),
                QtyTariff = GetDecimal(reader, 94),
                QtyUnit = GetString(reader, 95),
                PackNo = GetDecimal(reader, 96),
                PackUnit = GetString(reader, 97),
                UnitPrice = GetDecimal(reader, 98),
                InvoiceAmount = GetDecimal(reader, 99),
                AmountCIF = GetDecimal(reader, 100),
                CIFCurrency = GetString(reader, 101),
                CIFTHB = GetDecimal(reader, 102),
                AddPrice = GetDecimal(reader, 103),
                AddPriceTHB = GetDecimal(reader, 104),
                CIFReductionRate = GetDecimal(reader, 105),
                AssessedPrice = GetDecimal(reader, 106),
                AssessedAmount = GetDecimal(reader, 107),
                ExciseTariff = GetString(reader, 108),
                UsedQty = GetDecimal(reader, 109),
                AssessedQty = GetDecimal(reader, 110),
                AssessedQtyUnit = GetString(reader, 111),
                SubQty = GetString(reader, 112),
                OriginCriteria = GetString(reader, 113),
                CerExporterTax = GetString(reader, 114),
                ImportTaxInc = GetString(reader, 115),
                AHTNCode = GetString(reader, 116),
                MaterialCode = GetString(reader, 117),
                DangerousCode = GetString(reader, 118),
                UsePrivilege = GetString(reader, 119),
                BOICardNo = GetString(reader, 120),
                ProductionFormula = GetString(reader, 121),
                IsFreebie = GetString(reader, 122),
                RefDeclarNo = GetString(reader, 123),
                RefItemNo = GetDecimal(reader, 124),
                DutyRate = GetDecimal(reader, 125),
                DutyRateS = GetDecimal(reader, 126),
                DutyTextTHB = GetDecimal(reader, 127),
                ExciseTaxRate = GetString(reader, 128),
                ExciseTax = GetString(reader, 129),
                InteriorTax = GetString(reader, 130),
                OtherTax = GetString(reader, 131),
                Fee = GetString(reader, 132),
                VATBase = GetDecimal(reader, 133),
                Vat = GetDecimal(reader, 134),
                TotalDutyVAT = GetDecimal(reader, 135),
                EDIDateTime = GetString(reader, 136),
                StampDateTime = GetString(reader, 137),
                EDIStatus = GetString(reader, 138),
                RemarkInternal = GetString(reader, 139),
                DepositAmt = GetDecimal(reader, 140),
            });
        }

        return Task.FromResult(records);
    }

    public async Task<List<ImportExcelPreviewItem>> PreviewAsync(List<ImportExcel> records)
    {
        var existingKeys = new HashSet<string>();

        if (records.Count > 0)
        {
            var existing = await _db.QueryAsync<(string DeclarNo, int ItemDeclarNo)>(
                "SELECT DeclarNo, ItemDeclarNo FROM imp.import_excel");
            foreach (var e in existing)
                existingKeys.Add($"{e.DeclarNo}|{e.ItemDeclarNo}");
        }

        return records.Select(r => new ImportExcelPreviewItem
        {
            DeclarNo = r.DeclarNo,
            ItemDeclarNo = r.ItemDeclarNo,
            CustomerName = r.CustomerName,
            CompanyTaxNo = r.CompanyTaxNo,
            InvoiceNo = r.InvoiceNo,
            InvDate = r.InvDate,
            ProductCode = r.ProductCode,
            DescriptionEn1 = r.DescriptionEn1,
            DescriptionTh1 = r.DescriptionTh1,
            Brand = r.Brand,
            Quantity = r.Quantity,
            QuantityUnit = r.QuantityUnit,
            UnitPrice = r.UnitPrice,
            CIFTHB = r.CIFTHB,
            DutyRate = r.DutyRate,
            TotalDutyVAT = r.TotalDutyVAT,
            UsePrivilege = r.UsePrivilege,
            Currency = r.Currency,
            IsExisting = existingKeys.Contains($"{r.DeclarNo}|{r.ItemDeclarNo}")
        }).ToList();
    }

    public async Task<ImportExcelUploadResponse> SaveAsync(List<ImportExcel> records, string userName)
    {
        if (records.Count == 0)
            throw new AppException("NO_DATA", "ไม่มีข้อมูลสำหรับบันทึก");

        var existingKeys = new HashSet<string>();
        var existing = await _db.QueryAsync<(string DeclarNo, int ItemDeclarNo)>(
            "SELECT DeclarNo, ItemDeclarNo FROM imp.import_excel");
        foreach (var e in existing)
            existingKeys.Add($"{e.DeclarNo}|{e.ItemDeclarNo}");

        var insertCount = records.Count(r => !existingKeys.Contains($"{r.DeclarNo}|{r.ItemDeclarNo}"));
        var updateCount = records.Count(r => existingKeys.Contains($"{r.DeclarNo}|{r.ItemDeclarNo}"));

        await _repository.UpsertBatchAsync(records, userName);

        // Insert stock_m29_card (IN) + upsert stock_m29_on_hand + stock_m29_lot for new records
        var stockMsg = "";
        try
        {
            await UpsertStockOnImportAsync(records, existingKeys, userName);
            using var countConn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            await countConn.OpenAsync();
            var cardCount = await countConn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM imp.stock_m29_card");
            var onHandCount = await countConn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM imp.stock_m29_on_hand");
            var lotCount = await countConn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM imp.stock_m29_lot");
            stockMsg = $" | Stock: card={cardCount}, on_hand={onHandCount}, lot={lotCount}";
        }
        catch (Exception ex)
        {
            stockMsg = $" | Stock ERROR: {ex.Message}";
        }

        return new ImportExcelUploadResponse
        {
            TotalRows = records.Count,
            InsertedRows = insertCount,
            UpdatedRows = updateCount,
            Message = $"บันทึกสำเร็จ {records.Count} รายการ (เพิ่มใหม่ {insertCount}, อัพเดท {updateCount}){stockMsg}"
        };
    }

    /// <summary>Insert stock_m29_card (IN) and upsert stock_m29_on_hand for imported raw materials</summary>
    private async Task UpsertStockOnImportAsync(List<ImportExcel> records, HashSet<string> existingKeys, string userName)
    {
        // Filter: UsePrivilege contains '19ทวิ' or '19tvis' + must have MaterialCode + NetWeight > 0
        var validRecords = records
            .Where(r =>
            {
                var priv = r.UsePrivilege?.Trim();
                if (string.IsNullOrEmpty(priv)) return false;
                return priv == "19ทวิ"
                    || priv.Contains("ทวิ", StringComparison.OrdinalIgnoreCase)
                    || priv.Contains("19tvis", StringComparison.OrdinalIgnoreCase)
                    || priv.StartsWith("19", StringComparison.OrdinalIgnoreCase);
            })
            .Where(r => !string.IsNullOrWhiteSpace(r.MaterialCode) && r.NetWeight.HasValue && r.NetWeight > 0)
            .OrderBy(r => r.ETA)  // FIFO by ETA
            .ToList();

        if (validRecords.Count == 0) return;

        // Use fresh connection to avoid state issues after repository transaction
        using var conn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
        await conn.OpenAsync();

        // Get existing stock_m29_card entries to avoid duplicates
        var existingStockCards = new HashSet<string>();
        var scEntries = await conn.QueryAsync<(string ImportDeclarNo, int ImportItemNo)>(
            "SELECT ImportDeclarNo, ImportItemNo FROM imp.stock_m29_card WHERE TransactionType = 'IN' AND ImportDeclarNo IS NOT NULL");
        foreach (var sc in scEntries)
            existingStockCards.Add($"{sc.ImportDeclarNo}|{sc.ImportItemNo}");

        // Only process records that don't already have a stock_m29_card IN entry
        var newForStock = validRecords
            .Where(r => !existingStockCards.Contains($"{r.DeclarNo}|{r.ItemDeclarNo}"))
            .ToList();

        if (newForStock.Count == 0) return;

        using var tx = conn.BeginTransaction();
        try
        {
            foreach (var r in newForStock)
            {
                var rawMaterialCode = r.MaterialCode!.Trim();
                var unit = r.QuantityUnit?.Trim() ?? "KG";
                var qtyIn = r.NetWeight!.Value;
                var importDateStr = r.ETA ?? r.StampDateTime ?? r.EDIDateTime;  // FIFO by ETA
                var importDate = ParseFlexibleDate(importDateStr);
                var privilegeType = "19TVIS";
                var dutyPerUnit = (qtyIn != 0 && r.TotalDutyVAT.HasValue) ? r.TotalDutyVAT.Value / qtyIn : (decimal?)null;

                // Get current balance for this material from stock_m29_on_hand
                var currentBalance = await conn.ExecuteScalarAsync<decimal?>(
                    "SELECT QtyBalance FROM imp.stock_m29_on_hand WHERE RawMaterialCode = @RawMaterialCode",
                    new { RawMaterialCode = rawMaterialCode }, tx) ?? 0m;

                var newBalance = currentBalance + qtyIn;

                // 1. Insert stock_m29_card (IN)
                await conn.ExecuteAsync(@"
                    INSERT INTO imp.stock_m29_card
                        (TransactionDate, TransactionType, PrivilegeType,
                         ImportDeclarNo, ImportItemNo, ImportDate,
                         RawMaterialCode, ProductCode, ProductDescription, Unit,
                         QtyIn, QtyBalance,
                         UnitPrice, CIFValueTHB, DutyRate, DutyAmount, VATAmount,
                         ImportTaxIncId, BOICardNo, ProductionFormulaNo,
                         CreatedBy, Remark)
                    VALUES
                        (@TransactionDate, 'IN', @PrivilegeType,
                         @ImportDeclarNo, @ImportItemNo, @ImportDate,
                         @RawMaterialCode, @ProductCode, @ProductDescription, @Unit,
                         @QtyIn, @QtyBalance,
                         @UnitPrice, @CIFValueTHB, @DutyRate, @DutyAmount, @VATAmount,
                         @ImportTaxIncId, @BOICardNo, @ProductionFormulaNo,
                         @CreatedBy, @Remark)",
                    new
                    {
                        TransactionDate = importDate ?? DateTime.UtcNow,
                        PrivilegeType = privilegeType,
                        ImportDeclarNo = r.DeclarNo,
                        ImportItemNo = r.ItemDeclarNo,
                        ImportDate = importDate,
                        RawMaterialCode = rawMaterialCode,
                        ProductCode = r.ProductCode?.Trim(),
                        ProductDescription = r.DescriptionTh1?.Trim(),
                        Unit = unit,
                        QtyIn = qtyIn,
                        QtyBalance = newBalance,
                        UnitPrice = r.UnitPrice,
                        CIFValueTHB = r.CIFTHB,
                        DutyRate = r.DutyRate,
                        DutyAmount = r.TotalDutyVAT,
                        VATAmount = r.Vat,
                        ImportTaxIncId = r.ImportTaxInc?.Trim(),
                        BOICardNo = r.BOICardNo?.Trim(),
                        ProductionFormulaNo = r.ProductionFormula?.Trim(),
                        CreatedBy = userName,
                        Remark = $"นำเข้าจาก Excel: {r.DeclarNo}/{r.ItemDeclarNo}"
                    }, tx);

                // 2. Upsert stock_m29_on_hand
                await conn.ExecuteAsync(@"
                    MERGE imp.stock_m29_on_hand AS target
                    USING (SELECT @RawMaterialCode AS RawMaterialCode) AS source
                    ON target.RawMaterialCode = source.RawMaterialCode
                    WHEN MATCHED THEN
                        UPDATE SET
                            QtyIn = target.QtyIn + @QtyIn,
                            QtyBalance = target.QtyBalance + @QtyIn,
                            ProductCode = COALESCE(@ProductCode, target.ProductCode),
                            ProductDescription = COALESCE(@ProductDescription, target.ProductDescription),
                            DutyRate = COALESCE(@DutyRate, target.DutyRate),
                            DutyPerUnit = COALESCE(@DutyPerUnit, target.DutyPerUnit),
                            LastUpdatedBy = @UserName,
                            LastUpdatedDate = SYSUTCDATETIME()
                    WHEN NOT MATCHED THEN
                        INSERT (RawMaterialCode, ProductCode, ProductDescription, Unit,
                                QtyIn, QtyOut, QtyBalance, DutyRate, DutyPerUnit,
                                LastUpdatedBy)
                        VALUES (@RawMaterialCode, @ProductCode, @ProductDescription, @Unit,
                                @QtyIn, 0, @QtyIn, @DutyRate, @DutyPerUnit,
                                @UserName);",
                    new
                    {
                        RawMaterialCode = rawMaterialCode,
                        ProductCode = r.ProductCode?.Trim(),
                        ProductDescription = r.DescriptionTh1?.Trim(),
                        Unit = unit,
                        QtyIn = qtyIn,
                        DutyRate = r.DutyRate,
                        DutyPerUnit = dutyPerUnit,
                        UserName = userName
                    }, tx);

                // 3. Insert stock_m29_lot (FIFO queue for cutting)
                await conn.ExecuteAsync(@"
                    MERGE imp.stock_m29_lot AS target
                    USING (SELECT @ImportDeclarNo AS ImportDeclarNo, @ImportItemNo AS ImportItemNo) AS source
                    ON target.ImportDeclarNo = source.ImportDeclarNo AND target.ImportItemNo = source.ImportItemNo
                    WHEN NOT MATCHED THEN
                        INSERT (ImportDeclarNo, ImportItemNo, ImportDate, PrivilegeType,
                                RawMaterialCode, ProductCode, ProductDescription, Unit,
                                QtyOriginal, QtyBalance,
                                UnitPrice, CIFValueTHB, DutyRate, DutyPerUnit, TotalDutyVAT,
                                ImportTaxIncId, BOICardNo, ProductionFormulaNo,
                                Status, ExpiryDate, CreatedBy)
                        VALUES (@ImportDeclarNo, @ImportItemNo, @ImportDate, @PrivilegeType,
                                @RawMaterialCode, @ProductCode, @ProductDescription, @Unit,
                                @QtyOriginal, @QtyBalance,
                                @UnitPrice, @CIFValueTHB, @DutyRate, @DutyPerUnit, @TotalDutyVAT,
                                @ImportTaxIncId, @BOICardNo, @ProductionFormulaNo,
                                'ACTIVE', @ExpiryDate, @CreatedBy);",
                    new
                    {
                        ImportDeclarNo = r.DeclarNo,
                        ImportItemNo = r.ItemDeclarNo,
                        ImportDate = importDate ?? DateTime.UtcNow,
                        PrivilegeType = privilegeType,
                        RawMaterialCode = rawMaterialCode,
                        ProductCode = r.ProductCode?.Trim(),
                        ProductDescription = r.DescriptionTh1?.Trim(),
                        Unit = unit,
                        QtyOriginal = qtyIn,
                        QtyBalance = qtyIn,
                        UnitPrice = r.UnitPrice,
                        CIFValueTHB = r.CIFTHB,
                        DutyRate = r.DutyRate,
                        DutyPerUnit = dutyPerUnit,
                        TotalDutyVAT = r.TotalDutyVAT,
                        ImportTaxIncId = r.ImportTaxInc?.Trim(),
                        BOICardNo = r.BOICardNo?.Trim(),
                        ProductionFormulaNo = r.ProductionFormula?.Trim(),
                        ExpiryDate = importDate?.AddYears(1),
                        CreatedBy = userName
                    }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static DateTime? ParseFlexibleDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;
        dateStr = dateStr.Trim();

        // Try standard formats first (yyyy-MM-dd, dd/MM/yyyy, etc.)
        if (DateTime.TryParse(dateStr, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;

        // Try Thai Buddhist year: dd/MM/yyyy where yyyy > 2400 (e.g. 14/02/2568)
        var parts = dateStr.Split('/', '-', '.');
        if (parts.Length == 3)
        {
            if (int.TryParse(parts[0], out var p0) && int.TryParse(parts[1], out var p1) && int.TryParse(parts[2], out var p2))
            {
                // dd/MM/BBBB (Buddhist year)
                if (p2 > 2400)
                    try { return new DateTime(p2 - 543, p1, p0); } catch { }

                // BBBB/MM/dd or BBBB-MM-dd
                if (p0 > 2400)
                    try { return new DateTime(p0 - 543, p1, p2); } catch { }

                // dd/MM/yyyy (CE year)
                if (p2 >= 1900 && p2 <= 2100)
                    try { return new DateTime(p2, p1, p0); } catch { }

                // yyyy/MM/dd (CE year)
                if (p0 >= 1900 && p0 <= 2100)
                    try { return new DateTime(p0, p1, p2); } catch { }
            }
        }

        return null;
    }

    private static readonly System.Text.Encoding Latin1 = System.Text.Encoding.GetEncoding("iso-8859-1");
    private static readonly System.Text.Encoding Thai874 = System.Text.Encoding.GetEncoding(874);

    private static string? GetString(IExcelDataReader reader, int index)
    {
        if (index >= reader.FieldCount) return null;
        var value = reader.GetValue(index);
        if (value == null) return null;
        var str = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(str)) return null;

        // Fix Thai encoding: ExcelDataReader may read cp874 bytes as latin-1
        // Detect by checking if string contains chars in 0xA0-0xFF range (latin-1 mapped Thai)
        if (NeedsThaiReEncode(str))
        {
            try
            {
                var bytes = Latin1.GetBytes(str);
                str = Thai874.GetString(bytes);
            }
            catch
            {
                // If re-encoding fails, return original
            }
        }

        return str;
    }

    private static bool NeedsThaiReEncode(string str)
    {
        foreach (var c in str)
        {
            // Thai cp874 chars mapped to latin-1 fall in range 0xA1-0xFB
            if (c >= '\u00A1' && c <= '\u00FB')
                return true;
        }
        return false;
    }

    private static decimal? GetDecimal(IExcelDataReader reader, int index)
    {
        if (index >= reader.FieldCount) return null;
        var value = reader.GetValue(index);
        if (value == null) return null;
        if (value is double d) return (decimal)d;
        if (decimal.TryParse(value.ToString(), out var result)) return result;
        return null;
    }

    private static int GetInt(IExcelDataReader reader, int index)
    {
        if (index >= reader.FieldCount) return 0;
        var value = reader.GetValue(index);
        if (value == null) return 0;
        if (value is double d) return (int)d;
        if (int.TryParse(value.ToString(), out var result)) return result;
        return 0;
    }
}
