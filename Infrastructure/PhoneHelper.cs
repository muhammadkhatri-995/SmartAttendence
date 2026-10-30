using System.Text.RegularExpressions;

namespace SmartAttendance.Web.Infrastructure;

public static class PhoneHelper
{
    /// <summary>Accepts 03001234567, 923001234567, +923001234567. Returns +E.164 or null if invalid.</summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var d = new string(input.Where(c => char.IsDigit(c) || c == '+').ToArray());

        if (d.StartsWith('+')) { }
        else if (d.StartsWith("00")) d = "+" + d[2..];
        else if (d.StartsWith('0')) d = "+92" + d[1..];
        else if (d.StartsWith("92")) d = "+" + d;
        else return null;

        return Regex.IsMatch(d, @"^\+[1-9][0-9]{7,14}$") ? d : null;
    }
}