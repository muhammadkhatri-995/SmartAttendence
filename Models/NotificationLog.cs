using SmartAttendance.Models;

namespace SmartAttendance.Web.Models;

public class NotificationLog
{
    public int NotificationLogId { get; set; }

    public int AttendanceSessionId { get; set; }
    public AttendanceSession AttendanceSession { get; set; } = null!;

    public string RecipientPhone { get; set; } = string.Empty;
    public NotificationMessageType MessageType { get; set; } = NotificationMessageType.AttendanceReport;

    /// <summary>The WhatsApp message id (wamid...) returned by the provider.</summary>
    public string? ProviderMessageId { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public int AttemptCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
}