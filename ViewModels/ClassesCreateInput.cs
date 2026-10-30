using System.ComponentModel.DataAnnotations;

namespace SmartAttendance.Web.ViewModels;

public class ClassesCreateInput
{
    [Required(ErrorMessage = "Class name is required.")]
    [StringLength(100)]
    [Display(Name = "Class name")]
    public string ClassName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Section is required.")]
    [StringLength(20)]
    public string Section { get; set; } = "A";

    [Required(ErrorMessage = "Academic year is required.")]
    [StringLength(20)]
    [Display(Name = "Academic year")]
    public string AcademicYear { get; set; } = "2026-2027";

    [Range(1, 8, ErrorMessage = "Semester must be between 1 and 8.")]
    public int Semester { get; set; } = 1;
}