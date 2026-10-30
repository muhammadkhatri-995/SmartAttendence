namespace SmartAttendance.Web.Services.Interfaces;

public interface IInstitutionClock
{
    /// <summary>Today's calendar date in the institution's configured time zone.</summary>
    Task<DateOnly> TodayAsync(CancellationToken ct = default);
}