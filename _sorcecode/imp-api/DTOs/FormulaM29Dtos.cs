namespace imp_api.DTOs;

public class FormulaM29ListItem
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

public class FormulaM29Detail
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
    public List<FormulaM29DetailItem> Details { get; set; } = new();
}

public class FormulaM29DetailItem
{
    public int Id { get; set; }
    public int BomM29HdId { get; set; }
    public int No { get; set; }
    public string? RawMaterialCode { get; set; }
    public string? ProductType { get; set; }
    public string? Unit { get; set; }
    public decimal? Ratio { get; set; }
    public decimal? Scrap { get; set; }
    public string? Remark { get; set; }
}

public class CreateFormulaM29Request
{
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? ProductType { get; set; }
    public List<FormulaM29DetailRequest> Details { get; set; } = new();
}

public class UpdateFormulaM29Request
{
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? ProductType { get; set; }
    public List<FormulaM29DetailRequest> Details { get; set; } = new();
}

public class FormulaM29DetailRequest
{
    public int No { get; set; }
    public string? RawMaterialCode { get; set; }
    public string? ProductType { get; set; }
    public string? Unit { get; set; }
    public decimal? Ratio { get; set; }
    public decimal? Scrap { get; set; }
    public string? Remark { get; set; }
}
