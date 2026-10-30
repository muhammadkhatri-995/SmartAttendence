using SmartAttendance.Models;

namespace SmartAttendance.Web.Models;

public class ImportBatch
{
    public int ImportBatchId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public ImportType ImportType { get; set; }
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public ImportStatus Status { get; set; }

    public string ImportedByUserId { get; set; } = string.Empty;
    public ApplicationUser ImportedByUser { get; set; } = null!;

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}