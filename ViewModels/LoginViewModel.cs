using System.ComponentModel.DataAnnotations;

namespace SmartAttendance.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in on this device")]
    public bool RememberMe { get; set; }
}