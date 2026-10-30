namespace SmartAttendance.Web.Infrastructure;

public class SeedOptions
{
    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string AdminFullName { get; set; } = "Administrator";

    public string? CrEmail { get; set; }
    public string? CrPassword { get; set; }
    public string CrFullName { get; set; } = "Class Representative";
}