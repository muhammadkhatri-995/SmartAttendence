namespace SmartAttendance.Web.Infrastructure;

/// <summary>Outcome of a service operation. Errors are safe to show to the user.</summary>
public class ServiceResult
{
    public bool Succeeded { get; private init; }
    public bool IsNotFound { get; private init; }
    public string? Error { get; private init; }

    public static ServiceResult Ok() => new() { Succeeded = true };
    public static ServiceResult Fail(string error) => new() { Error = error };
    public static ServiceResult NotFound() => new() { IsNotFound = true, Error = "The record was not found." };
}