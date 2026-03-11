namespace imp_api.Models;

public class UserMenuPermission
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int MenuId { get; set; }
    public bool Visible { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanReadOnly { get; set; }
    public bool CanDelete { get; set; }
}
