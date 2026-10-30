using Microsoft.AspNetCore.Identity;
using SmartAttendance.Web.Models;
namespace SmartAttendance.Models
{
    public class ApplicationUser : IdentityUser

    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        /// <summary>Optional one-to-one link. Admins and operators have no teacher profile.</summary>
        public Teacher? TeacherProfile { get; set; }

    }
}
