using imp_api.DTOs;
using imp_api.Models;
using imp_api.Repositories;

namespace imp_api.Services;

public class ExportManageService : IExportManageService
{
    private readonly IExportExcelRepository _repo;

    public ExportManageService(IExportExcelRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResponse<ExportManageListItem>> SearchAsync(
        string? declarNo, string? invoiceNo, string? productCode, string? buyerName,
        int page, int pageSize)
    {
        var totalCount = await _repo.CountAsync(declarNo, invoiceNo, productCode, buyerName);
        var records = await _repo.SearchAsync(declarNo, invoiceNo, productCode, buyerName, page, pageSize);

        var items = records.Select(r => new ExportManageListItem
        {
            Id = r.Id,
            DeclarNo = r.DeclarNo,
            ItemDeclarNo = r.ItemDeclarNo,
            ExporterName = r.ExporterName,
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
        });

        return new PagedResponse<ExportManageListItem>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<ExportExcel> GetByIdAsync(int id)
    {
        var record = await _repo.GetByIdAsync(id);
        if (record is null)
            throw new AppException("NOT_FOUND", "ไม่พบข้อมูลที่ต้องการ");
        return record;
    }

    public async Task UpdateAsync(int id, UpdateExportManageRequest request, string userName)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            throw new AppException("NOT_FOUND", "ไม่พบข้อมูลที่ต้องการแก้ไข");

        existing.ExporterName = request.ExporterName;
        existing.TaxId = request.TaxId;
        existing.BuyerName = request.BuyerName;
        existing.InvoiceNo = request.InvoiceNo;
        existing.InvDate = request.InvDate;
        existing.ProductCode = request.ProductCode;
        existing.DescriptionEn1 = request.DescriptionEn1;
        existing.DescriptionEn2 = request.DescriptionEn2;
        existing.DescriptionTh1 = request.DescriptionTh1;
        existing.DescriptionTh2 = request.DescriptionTh2;
        existing.Brand = request.Brand;
        existing.QtyInvoice = request.QtyInvoice;
        existing.QtyInvoiceUnit = request.QtyInvoiceUnit;
        existing.UnitPrice = request.UnitPrice;
        existing.CurrencyCode = request.CurrencyCode;
        existing.FOBTHB = request.FOBTHB;
        existing.CurrentStatus = request.CurrentStatus;
        existing.Remark = request.Remark;

        await _repo.UpdateAsync(id, existing, userName);
    }

    public async Task DeleteAsync(int id)
    {
        var deleted = await _repo.DeleteAsync(id);
        if (!deleted)
            throw new AppException("NOT_FOUND", "ไม่พบข้อมูลที่ต้องการลบ");
    }
}
