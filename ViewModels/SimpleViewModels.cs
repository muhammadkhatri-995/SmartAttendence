using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartAttendance.Web.Models;

namespace SmartAttendance.Web.ViewModels;

public class StudentsIndexViewModel
{
    public ClassRoom Class { get; set; } = null!;
    public List<Student> Students { get; set; } = new();
}

public class StudentEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Roll number is required.")]
    [StringLength(30)]
    [Display(Name = "Roll number")]
    public string RollNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150)]
    [Display(Name = "Name")]
    public string FullName { get; set; } = string.Empty;
}

public class TeacherFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Teacher name is required.")]
    [StringLength(150, ErrorMessage = "Name is too long.")]
    [Display(Name = "Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "WhatsApp number is required.")]
    [StringLength(25, ErrorMessage = "Number is too long.")]
    [Display(Name = "WhatsApp number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

    public class TimetableRow
{
    public int Id { get; set; }
    public DayOfWeek Day { get; set; }
    public TimeOnly Start { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
}

public class TimetableIndexViewModel
{
    public ClassRoom Class { get; set; } = null!;
    public List<TimetableRow> Rows { get; set; } = new();
    public List<SelectListItem> Subjects { get; set; } = new();
    public List<SelectListItem> Teachers { get; set; } = new();
}

public class SlotCard
{
    public int AssignmentId { get; set; }
    public TimeOnly Start { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public int? SessionId { get; set; }
}

public class AttendanceIndexViewModel
{
    public DateOnly Date { get; set; }
    public DateOnly Today { get; set; }
    public ClassRoom Class { get; set; } = null!;
    public List<SlotCard> Slots { get; set; } = new();
}

public class MarkStudent
{
    public int Id { get; set; }
    public string Roll { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsAbsent { get; set; }
}

public class MarkViewModel
{
    public int AssignmentId { get; set; }
    public DateOnly Date { get; set; }
    public string ClassLabel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public bool IsEdit { get; set; }
    public List<MarkStudent> Students { get; set; } = new();
}

public class AttendanceResultViewModel
{
    public int AssignmentId { get; set; }
    public DateOnly Date { get; set; }
    public string ClassLabel { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Present { get; set; }
    public List<string> AbsentRolls { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public string WhatsAppUrl { get; set; } = string.Empty;
}

public class HistoryRow
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public int Present { get; set; }
    public int Absent { get; set; }
}