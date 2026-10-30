using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SmartAttendance.Web.Infrastructure;

public static class QueryHelpers
{
    /// <summary>Builds an ILIKE "contains" pattern, escaping the wildcards the user may type.</summary>
    public static string ContainsPattern(string term) =>
        "%" + term.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";

    /// <summary>Returns "active", "inactive" or "all".</summary>
    public static string NormalizeStatus(string? status) =>
        status?.ToLowerInvariant() switch
        {
            "active" => "active",
            "inactive" => "inactive",
            _ => "all"
        };

    /// <summary>True for PostgreSQL error 23505 (unique constraint violated).</summary>
    public static bool IsUniqueViolation(this DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}