namespace SmartAttendance.Web.Models;

/// <summary>Entities implementing this get UpdatedAt stamped (UTC) automatically on save.</summary>
public interface IHasUpdatedAt
{
    DateTime UpdatedAt { get; set; }
}