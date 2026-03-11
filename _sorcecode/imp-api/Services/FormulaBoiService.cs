using imp_api.DTOs;
using imp_api.Models;
using imp_api.Repositories;

namespace imp_api.Services;

public class FormulaBoiService : IFormulaBoiService
{
    private readonly IBomBoiRepository _repo;

    public FormulaBoiService(IBomBoiRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResponse<FormulaBoiListItem>> SearchAsync(string? formulaNo, string? description, string? productType, int page, int pageSize)
    {
        var totalCount = await _repo.CountAsync(formulaNo, description, productType);
        var records = await _repo.SearchAsync(formulaNo, description, productType, page, pageSize);

        var items = records.Select(r => new FormulaBoiListItem
        {
            Id = r.Id,
            ProductionFormulaNo = r.ProductionFormulaNo,
            DescriptionEn1 = r.DescriptionEn1,
            DescriptionTh1 = r.DescriptionTh1,
            ProductType = r.ProductType,
            DetailCount = r.DetailCount,
            CreatedBy = r.CreatedBy,
            CreatedDate = r.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
        });

        return new PagedResponse<FormulaBoiListItem>
        {
            Data = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<FormulaBoiDetail> GetByIdAsync(int id)
    {
        var hd = await _repo.GetByIdAsync(id)
            ?? throw new AppException("NOT_FOUND", "ไม่พบสูตรการผลิตที่ต้องการ");

        var details = await _repo.GetDetailsByHdIdAsync(hd.Id);

        return new FormulaBoiDetail
        {
            Id = hd.Id,
            ProductionFormulaNo = hd.ProductionFormulaNo,
            DescriptionEn1 = hd.DescriptionEn1,
            DescriptionTh1 = hd.DescriptionTh1,
            ProductType = hd.ProductType,
            CreatedBy = hd.CreatedBy,
            CreatedDate = hd.CreatedDate.ToString("yyyy-MM-dd HH:mm"),
            ModifiedBy = hd.ModifiedBy,
            ModifiedDate = hd.ModifiedDate?.ToString("yyyy-MM-dd HH:mm"),
            Details = details.Select(d => new FormulaBoiDetailItem
            {
                Id = d.Id,
                BomBoiHdId = d.BomBoiHdId,
                No = d.No,
                RawMaterialCode = d.RawMaterialCode,
                ProductType = d.ProductType,
                Unit = d.Unit,
                Ratio = d.Ratio,
                Scrap = d.Scrap,
                Remark = d.Remark,
            }).ToList(),
        };
    }

    public async Task<int> CreateAsync(CreateFormulaBoiRequest request, string userName)
    {
        if (string.IsNullOrWhiteSpace(request.ProductionFormulaNo))
            throw new AppException("VALIDATION_ERROR", "กรุณากรอกเลขที่สูตรการผลิต");

        var existing = await _repo.GetByFormulaNoAsync(request.ProductionFormulaNo.Trim());
        if (existing != null)
            throw new AppException("DUPLICATE", "เลขที่สูตรการผลิตนี้มีอยู่ในระบบแล้ว");

        var hd = new BomBoiHd
        {
            ProductionFormulaNo = request.ProductionFormulaNo.Trim(),
            DescriptionEn1 = request.DescriptionEn1,
            DescriptionTh1 = request.DescriptionTh1,
            ProductType = request.ProductType,
            CreatedBy = userName,
        };

        var hdId = await _repo.InsertHdAsync(hd);

        foreach (var dtReq in request.Details)
        {
            var dt = new BomBoiDt
            {
                BomBoiHdId = hdId,
                No = dtReq.No,
                RawMaterialCode = dtReq.RawMaterialCode,
                ProductType = dtReq.ProductType,
                Unit = dtReq.Unit,
                Ratio = dtReq.Ratio,
                Scrap = dtReq.Scrap,
                Remark = dtReq.Remark,
                CreatedBy = userName,
            };
            await _repo.InsertDetailAsync(dt);
        }

        return hdId;
    }

    public async Task UpdateAsync(int id, UpdateFormulaBoiRequest request, string userName)
    {
        var existing = await _repo.GetByIdAsync(id)
            ?? throw new AppException("NOT_FOUND", "ไม่พบสูตรการผลิตที่ต้องการแก้ไข");

        existing.DescriptionEn1 = request.DescriptionEn1;
        existing.DescriptionTh1 = request.DescriptionTh1;
        existing.ProductType = request.ProductType;
        existing.ModifiedBy = userName;

        await _repo.UpdateHdAsync(id, existing);

        await _repo.DeleteDetailsByHdIdAsync(id);

        foreach (var dtReq in request.Details)
        {
            var dt = new BomBoiDt
            {
                BomBoiHdId = id,
                No = dtReq.No,
                RawMaterialCode = dtReq.RawMaterialCode,
                ProductType = dtReq.ProductType,
                Unit = dtReq.Unit,
                Ratio = dtReq.Ratio,
                Scrap = dtReq.Scrap,
                Remark = dtReq.Remark,
                CreatedBy = userName,
            };
            await _repo.InsertDetailAsync(dt);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var deleted = await _repo.DeleteAsync(id);
        if (!deleted)
            throw new AppException("NOT_FOUND", "ไม่พบสูตรการผลิตที่ต้องการลบ");
    }
}
