namespace SmartAttendance.Web.Authorization;

public static class PolicyNames
{
    /// <summary>Administrators only.</summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>Anyone who may take attendance (admins can, per the project spec).</summary>
    public const string AttendanceAccess = "AttendanceAccess";

    /// <summary>The teacher/operator portal (their own assignments).</summary>
    public const string TeacherPortal = "TeacherPortal";
}