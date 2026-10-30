using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SmartAttendance.Web.Infrastructure;

public static class ModelStateExtensions
{
    public static string ErrorText(this ModelStateDictionary state) =>
        string.Join(" ", state.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
}