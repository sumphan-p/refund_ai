using imp_api.DTOs;
using imp_api.Models;
using imp_api.Repositories;

namespace imp_api.Services;

public class ImportManageService : IImportManageService
{
    private readonly IImportExcelRepository _repo;

    public ImportManageService(IImportExcelRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResponse<ImportManageListItem>> SearchAsync(
        string? declarNo, string? invoiceNo, string? productCode, string? brand,
        int page, int pageSize)
    {
        // Run sequentially — single IDbConnection without MARS
        var totalCount = await _repo.CountAsync(declarNo, invoiceNo, productCode, brand);
        var records = await _repo.SearchAsync(declarNo, invoiceNo, productCode, brand, page, pageSize);

        var items = records.Select(r => new ImportManageListItem
        {
            Id = r.Id,
            DeclarNo = r.DeclarNo,
            ItemDeclarNo = r.ItemDeclarNo,
            CustomerName = r.CustomerName,
            InvoiceNo = r.InvoiceNo,
            InvDate = r.InvDate,
            ProductCode = r.ProductCode,
            DescriptionTh1 = r.DescriptionTh1,
            Brand = r.Brand,
            Quantity = r.Quantity,
            QuantityUnit = r.QuantityUnit,
            UnitPrice = r.UnitPrice,
            Currency = r.Currency,
            CIFTHB = r.CIFTHB,
            DutyRate = r.DutyRate,
            TotalDutyVAT = r.TotalDutyVAT,
            UsePrivilege = r.UsePrivilege,
        });

        return new PagedResponse<ImportManageListItem>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<ImportExcel> GetByIdAsync(int id)
    {
        var record = await _repo.GetByIdAsync(id);
        if (record is null)
            throw new AppException("NOT_FOUND", "ไม่พบข้อมูลที่ต้องการ");
        return record;
    }

    public async Task UpdateAsync(int id, UpdateImportManageRequest request, string userName)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing is null)
            throw new AppException("NOT_FOUND", "ไม่พบข้อมูลที่ต้องการแก้ไข");

        // Map request fields onto existing record
        existing.CustomerName = request.CustomerName;
        existing.CompanyTaxNo = request.CompanyTaxNo;
        existing.RefNo = request.RefNo;
        existing.InvoiceNo = request.InvoiceNo;
        existing.InvDate = request.InvDate;
        existing.ProductCode = request.ProductCode;
        existing.DescriptionEn1 = request.DescriptionEn1;
        existing.DescriptionEn2 = request.DescriptionEn2;
        existing.DescriptionTh1 = request.DescriptionTh1;
        existing.DescriptionTh2 = request.DescriptionTh2;
        existing.Brand = request.Brand;
        existing.Quantity = request.Quantity;
        existing.QuantityUnit = request.QuantityUnit;
        existing.UnitPrice = request.UnitPrice;
        existing.Currency = request.Currency;
        existing.CIFTHB = request.CIFTHB;
        existing.DutyRate = request.DutyRate;
        existing.TotalDutyVAT = request.TotalDutyVAT;
        existing.UsePrivilege = request.UsePrivilege;
        existing.Remark = request.Remark;
        existing.RemarkInternal = request.RemarkInternal;

        await _repo.UpdateAsync(id, existing, userName);
    }

    public async Task DeleteAsync(int id)
    {
        var deleted = await _repo.DeleteAsync(id);
        if (!deleted)
            throw new AppException("NOT_FOUND", "ไม่พบข้อมูลที่ต้องการลบ");
    }
}
