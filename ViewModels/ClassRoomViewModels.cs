using System.ComponentModel.DataAnnotations;
using SmartAttendance.Web.Infrastructure;

namespace SmartAttendance.Web.ViewModels;

public class ClassFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Class name is required.")]
    [StringLength(100, ErrorMessage = "Class name cannot exceed 100 characters.")]
    [Display(Name = "Class name")]
    public string ClassName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Section is required. Use A if the class has no sections.")]
    [StringLength(20, ErrorMessage = "Section cannot exceed 20 characters.")]
    public string Section { get; set; } = string.Empty;

    [Range(1, 12, ErrorMessage = "Semester must be between 1 and 12.")]
    public int Semester { get; set; } = 1;

    [Required(ErrorMessage = "Academic year is required.")]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "Use the format 2025-2026.")]
    [Display(Name = "Academic year")]
    public string AcademicYear { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(AcademicYear) &&
            System.Text.RegularExpressions.Regex.IsMatch(AcademicYear, @"^\d{4}-\d{4}$"))
        {
            var parts = AcademicYear.Split('-');
            if (int.Parse(parts[1]) != int.Parse(parts[0]) + 1)
                yield return new ValidationResult(
                    "The second year must be exactly one more than the first (for example 2025-2026).",
                    new[] { nameof(AcademicYear) });
        }
    }
}

public class ClassListItem
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public int Semester { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int StudentCount { get; set; }
    public int AssignmentCount { get; set; }
}

public class ClassIndexViewModel
{
    public string? Search { get; set; }
    public string Status { get; set; } = "all";
    public PagedResult<ClassListItem> Result { get; set; } = new();

    public Dictionary<string, string> RouteValues
    {
        get
        {
            var d = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(Search)) d["search"] = Search;
            if (Status != "all") d["status"] = Status;
            return d;
        }
    }
}

public class ClassDetailsViewModel
{
    public int Id { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public int Semester { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ActiveStudentCount { get; set; }
    public int TotalStudentCount { get; set; }
    public int AssignmentCount { get; set; }
}