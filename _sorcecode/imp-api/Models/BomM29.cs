namespace imp_api.Models;

public class BomM29Hd
{
    public int Id { get; set; }
    public string ProductionFormulaNo { get; set; } = string.Empty;
    public string? DescriptionEn1 { get; set; }
    public string? DescriptionTh1 { get; set; }
    public string? ProductType { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Computed — populated by SearchAsync query only
    public int DetailCount { get; set; }
}

public class BomM29Dt
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
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}
