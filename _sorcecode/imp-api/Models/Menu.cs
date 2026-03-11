namespace imp_api.Models;

public class Menu
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string MenuCode { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
