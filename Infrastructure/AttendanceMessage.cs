using System.Text;

namespace SmartAttendance.Web.Infrastructure;

public static class AttendanceMessage
{
    public static string Build(string classLabel, string subject, DateOnly date,
        int total, int present, IEnumerable<string> absentRolls)
    {
        var rolls = absentRolls.OrderBy(r => r.Length).ThenBy(r => r, StringComparer.Ordinal).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"Attendance Report - {classLabel}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine($"Date: {date:dd-MMM-yyyy}");
        sb.AppendLine($"Total: {total}");
        sb.AppendLine($"Present: {present}");
        sb.AppendLine($"Absent: {rolls.Count}");
        sb.Append("Absent roll numbers: ").Append(rolls.Count == 0 ? "None" : string.Join(", ", rolls));
        return sb.ToString();
    }

    public static string WhatsAppUrl(string phoneE164, string message) =>
        $"https://wa.me/{phoneE164.TrimStart('+')}?text={Uri.EscapeDataString(message)}";
}