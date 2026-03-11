namespace imp_api.DTOs;

public class FormulaBoiListItem
{
    public int Id { get; set; }
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? ProductType { get; set; }
    public int DetailCount { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedDate { get; set; }
}

public class FormulaBoiDetail
{
    public int Id { get; set; }
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? ProductType { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public string? ModifiedDate { get; set; }
    public List<FormulaBoiDetailItem> Details { get; set; } = new();
}

public class FormulaBoiDetailItem
{
    public int Id { get; set; }
    public int BomBoiHdId { get; set; }
    public int No { get; set; }
    public string? RawMaterialCode { get; set; }
    public string? ProductType { get; set; }
    public string? Unit { get; set; }
    public decimal? Ratio { get; set; }
    public decimal? Scrap { get; set; }
    public string? Remark { get; set; }
}

public class CreateFormulaBoiRequest
{
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? ProductType { get; set; }
    public List<FormulaBoiDetailRequest> Details { get; set; } = new();
}

public class UpdateFormulaBoiRequest
{
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? ProductType { get; set; }
    public List<FormulaBoiDetailRequest> Details { get; set; } = new();
}

public class FormulaBoiDetailRequest
{
    public int No { get; set; }
    public string? RawMaterialCode { get; set; }
    public string? ProductType { get; set; }
    public string? Unit { get; set; }
    public decimal? Ratio { get; set; }
    public decimal? Scrap { get; set; }
    public string? Remark { get; set; }
}
