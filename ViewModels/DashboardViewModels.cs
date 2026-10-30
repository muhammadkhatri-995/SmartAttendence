using SmartAttendance.Models;
using SmartAttendance.Web.Models;

namespace SmartAttendance.Web.ViewModels;

public class AdminDashboardViewModel
{
    public string InstitutionName { get; set; } = string.Empty;
    public DateOnly Today { get; set; }

    public int ActiveTeachers { get; set; }
    public int ActiveSubjects { get; set; }
    public int ActiveClasses { get; set; }
    public int ActiveStudents { get; set; }
    public int ActiveAssignments { get; set; }

    public int SubmittedToday { get; set; }
    public int DraftsToday { get; set; }
    public int FailedNotifications { get; set; }
}

public class TeacherDashboardViewModel
{
    public DateOnly Today { get; set; }
    public bool HasTeacherProfile { get; set; }
    public List<AssignedClassItem> Assignments { get; set; } = new();
}

public class AssignedClassItem
{
    public int TeacherAssignmentId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;

    /// <summary>Null means no attendance session exists yet for today.</summary>
    public AttendanceStatus? TodayStatus { get; set; }

    /// <summary>True when the user got this through an operator assignment rather than owning it.</summary>
    public bool IsDelegated { get; set; }
}