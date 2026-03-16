namespace imp_api.Models;

public class M29BatchHeader
{
    public int Id { get; set; }
    public string BatchDocNo { get; set; } = string.Empty;
    public string Status { get; set; } = "DRAFT";
    public int TotalItems { get; set; }
    public decimal TotalNetWeight { get; set; }
    public decimal TotalFOBTHB { get; set; }
    public string? Remark { get; set; }

    // Audit
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledDate { get; set; }
}
