using System.ComponentModel.DataAnnotations;
using SmartAttendance.Web.Infrastructure;

namespace SmartAttendance.Web.ViewModels;

public class SubjectFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Subject code is required.")]
    [StringLength(20, ErrorMessage = "Subject code cannot exceed 20 characters.")]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9\-_.]*$",
        ErrorMessage = "Use letters, digits, dash, underscore or dot (for example CS-101).")]
    [Display(Name = "Subject code")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject name is required.")]
    [StringLength(150, ErrorMessage = "Subject name cannot exceed 150 characters.")]
    [Display(Name = "Subject name")]
    public string SubjectName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}

public class SubjectListItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int AssignmentCount { get; set; }
}

public class SubjectIndexViewModel
{
    public string? Search { get; set; }
    public string Status { get; set; } = "all";
    public PagedResult<SubjectListItem> Result { get; set; } = new();

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

public class SubjectDetailsViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int AssignmentCount { get; set; }
}