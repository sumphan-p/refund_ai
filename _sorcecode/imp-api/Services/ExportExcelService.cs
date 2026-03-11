using System.Data;
using Dapper;
using ExcelDataReader;
using imp_api.DTOs;
using imp_api.Models;
using imp_api.Repositories;

namespace imp_api.Services;

public class ExportExcelService : IExportExcelService
{
    private readonly IExportExcelRepository _repository;
    private readonly IDbConnection _db;

    public ExportExcelService(IExportExcelRepository repository, IDbConnection db)
    {
        _repository = repository;
        _db = db;
    }

    // Expected column count for Export Excel structure
    private const int ExpectedColumnCount = 87;

    // Key header names at specific positions to validate Export structure
    private static readonly (int Index, string ExpectedHeader)[] HeaderChecks =
    [
        (3, "เลขที่ใบขนสินค้าขาออก"),    // DeclarNo
        (6, "ลำดับรายการ ในใบขน"),        // ItemDeclarNo
        (14, "รหัสสินค้า"),               // ProductCode
        (32, "ปริมาณ (Invoice)"),          // QtyInvoice
    ];

    public Task<List<ExportExcel>> ParseExcelAsync(Stream fileStream)
    {
        using var reader = ExcelReaderFactory.CreateReader(fileStream, new ExcelReaderConfiguration
        {
            FallbackEncoding = System.Text.Encoding.GetEncoding(874) // Thai Windows (TIS-620)
        });
        var records = new List<ExportExcel>();

        // Read header row
        if (!reader.Read()) return Task.FromResult(records);

        // Validate structure: column count
        if (reader.FieldCount < ExpectedColumnCount)
        {
            throw new AppException("INVALID_STRUCTURE",
                $"โครงสร้างไฟล์ไม่ถูกต้อง: ไฟล์มี {reader.FieldCount} คอลัมน์ แต่ต้องมีอย่างน้อย {ExpectedColumnCount} คอลัมน์ กรุณาใช้ไฟล์โครงสร้าง Export เท่านั้น");
        }

        // Validate structure: check key header names (use GetString for Thai re-encoding)
        var mismatchedHeaders = new List<string>();
        foreach (var (index, expected) in HeaderChecks)
        {
            var actual = GetString(reader, index);
            if (!string.IsNullOrEmpty(actual) && !NormalizeSpaces(actual).Equals(NormalizeSpaces(expected), StringComparison.OrdinalIgnoreCase))
            {
                mismatchedHeaders.Add($"คอลัมน์ที่ {index + 1}: พบ '{actual}' แต่คาดว่า '{expected}'");
            }
        }

        if (mismatchedHeaders.Count > 0)
        {
            throw new AppException("INVALID_STRUCTURE",
                $"โครงสร้างไฟล์ไม่ตรงกับรูปแบบ Export กรุณาใช้ไฟล์โครงสร้าง Export เท่านั้น ({string.Join(", ", mismatchedHeaders)})");
        }

        while (reader.Read())
        {
            var declarNo = GetString(reader, 3);
            var itemDeclarNo = GetInt(reader, 6);

            if (string.IsNullOrWhiteSpace(declarNo)) continue;

            records.Add(new ExportExcel
            {
                ExporterName = GetString(reader, 0),
                TaxId = GetString(reader, 1),
                BranchSeq = GetString(reader, 2),
                DeclarNo = declarNo,
                DocumentType = GetString(reader, 4),
                BuyerName = GetString(reader, 5),
                ItemDeclarNo = itemDeclarNo,
                InvoiceNo = GetString(reader, 7),
                InvDate = GetString(reader, 8),
                InvoiceItemNo = GetIntNullable(reader, 9),
                SubmissionDate = GetString(reader, 10),
                ReleaseDate = GetString(reader, 11),
                LoadingDate = GetString(reader, 12),
                CurrentStatus = GetString(reader, 13),
                ProductCode = GetString(reader, 14),
                Brand = GetString(reader, 15),
                PurchaseOrder = GetString(reader, 16),
                DescriptionEn1 = GetString(reader, 17),
                DescriptionEn2 = GetString(reader, 18),
                DescriptionEn3 = GetString(reader, 19),
                DescriptionEn4 = GetString(reader, 20),
                DescriptionTh1 = GetString(reader, 21),
                DescriptionTh2 = GetString(reader, 22),
                DescriptionTh3 = GetString(reader, 23),
                DescriptionTh4 = GetString(reader, 24),
                TermOfPayment = GetString(reader, 25),
                TariffCode = GetString(reader, 26),
                TariffType = GetString(reader, 27),
                StatisticalCode = GetString(reader, 28),
                StatisticalUnit = GetString(reader, 29),
                PackageCount = GetDecimal(reader, 30),
                PackageUnit = GetString(reader, 31),
                QtyInvoice = GetDecimal(reader, 32),
                QtyInvoiceUnit = GetString(reader, 33),
                QtyDeclar = GetDecimal(reader, 34),
                QtyDeclarUnit = GetString(reader, 35),
                NetWeight = GetDecimal(reader, 36),
                NetWeightUnit = GetString(reader, 37),
                UnitPrice = GetDecimal(reader, 38),
                FOBForeign = GetDecimal(reader, 39),
                CurrencyCode = GetString(reader, 40),
                FOBTHB = GetDecimal(reader, 41),
                VesselName = GetString(reader, 42),
                ExportPortCode = GetString(reader, 43),
                InspectionLocationCode = GetString(reader, 44),
                BuyerCountryCode = GetString(reader, 45),
                DestinationCountryCode = GetString(reader, 46),
                Compensation = GetString(reader, 47),
                CompensationNo = GetString(reader, 48),
                BOI = GetString(reader, 49),
                BOINo = GetString(reader, 50),
                FormulaBOI = GetString(reader, 51),
                Section19Bis = GetString(reader, 52),
                Section19BisNo = GetString(reader, 53),
                RightsTransferNo = GetString(reader, 54),
                Bond = GetString(reader, 55),
                ModelNo = GetString(reader, 56),
                ModelVer = GetString(reader, 57),
                ModelCompTax = GetString(reader, 58),
                EPZ = GetString(reader, 59),
                FZ = GetString(reader, 60),
                ImportTaxIncentiveId = GetString(reader, 61),
                ExportTaxIncentiveId = GetString(reader, 62),
                ReExport = GetString(reader, 63),
                NetReturn = GetString(reader, 64),
                SpecialPrivilegeCode = GetString(reader, 65),
                OriginCountryCode = GetString(reader, 66),
                ImportDeclarNo = GetString(reader, 67),
                ImportDeclarItemNo = GetDecimal(reader, 68),
                ShortDeclar = GetString(reader, 69),
                ShortPack = GetDecimal(reader, 70),
                ShortQty = GetDecimal(reader, 71),
                ShortNetWeight = GetDecimal(reader, 72),
                ShortFOBForeign = GetDecimal(reader, 73),
                ShortFOBTHB = GetDecimal(reader, 74),
                ShortPostDeclar = GetString(reader, 75),
                ShortPostPack = GetDecimal(reader, 76),
                ShortPostQty = GetDecimal(reader, 77),
                ShortPostNetWeight = GetDecimal(reader, 78),
                ShortPostFOBForeign = GetDecimal(reader, 79),
                ShortPostFOBTHB = GetDecimal(reader, 80),
                PermitNo1 = GetString(reader, 81),
                PermitNo2 = GetString(reader, 82),
                PermitNo3 = GetString(reader, 83),
                BookingNo = GetString(reader, 84),
                HouseBLNo = GetString(reader, 85),
                Remark = GetString(reader, 86),
            });
        }

        return Task.FromResult(records);
    }

    public async Task<List<ExportExcelPreviewItem>> PreviewAsync(List<ExportExcel> records)
    {
        var existingKeys = new HashSet<string>();

        if (records.Count > 0)
        {
            var existing = await _db.QueryAsync<(string DeclarNo, int ItemDeclarNo)>(
                "SELECT DeclarNo, ItemDeclarNo FROM imp.export_excel");
            foreach (var e in existing)
                existingKeys.Add($"{e.DeclarNo}|{e.ItemDeclarNo}");
        }

        return records.Select(r => new ExportExcelPreviewItem
        {
            DeclarNo = r.DeclarNo,
            ItemDeclarNo = r.ItemDeclarNo,
            ExporterName = r.ExporterName,
            TaxId = r.TaxId,
            BuyerName = r.BuyerName,
            InvoiceNo = r.InvoiceNo,
            InvDate = r.InvDate,
            ProductCode = r.ProductCode,
            DescriptionTh1 = r.DescriptionTh1,
            Brand = r.Brand,
            QtyInvoice = r.QtyInvoice,
            QtyInvoiceUnit = r.QtyInvoiceUnit,
            UnitPrice = r.UnitPrice,
            CurrencyCode = r.CurrencyCode,
            FOBTHB = r.FOBTHB,
            CurrentStatus = r.CurrentStatus,
            IsExisting = existingKeys.Contains($"{r.DeclarNo}|{r.ItemDeclarNo}")
        }).ToList();
    }

    public async Task<ExportExcelUploadResponse> SaveAsync(List<ExportExcel> records, string userName)
    {
        if (records.Count == 0)
            throw new AppException("NO_DATA", "ไม่มีข้อมูลสำหรับบันทึก");

        var existingKeys = new HashSet<string>();
        var existing = await _db.QueryAsync<(string DeclarNo, int ItemDeclarNo)>(
            "SELECT DeclarNo, ItemDeclarNo FROM imp.export_excel");
        foreach (var e in existing)
            existingKeys.Add($"{e.DeclarNo}|{e.ItemDeclarNo}");

        var insertCount = records.Count(r => !existingKeys.Contains($"{r.DeclarNo}|{r.ItemDeclarNo}"));
        var updateCount = records.Count(r => existingKeys.Contains($"{r.DeclarNo}|{r.ItemDeclarNo}"));

        await _repository.UpsertBatchAsync(records, userName);

        return new ExportExcelUploadResponse
        {
            TotalRows = records.Count,
            InsertedRows = insertCount,
            UpdatedRows = updateCount,
            Message = $"บันทึกสำเร็จ {records.Count} รายการ (เพิ่มใหม่ {insertCount}, อัพเดท {updateCount})"
        };
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

    // Normalize all whitespace (non-breaking space, full-width space, etc.) to regular space
    private static string NormalizeSpaces(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();

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

    private static int? GetIntNullable(IExcelDataReader reader, int index)
    {
        if (index >= reader.FieldCount) return null;
        var value = reader.GetValue(index);
        if (value == null) return null;
        if (value is double d) return (int)d;
        if (int.TryParse(value.ToString(), out var result)) return result;
        return null;
    }
}
