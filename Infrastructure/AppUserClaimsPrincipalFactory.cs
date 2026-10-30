using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartAttendance.Models;
using SmartAttendance.Web.Models;
using System.Security.Claims;

namespace SmartAttendance.Web.Infrastructure;

/// <summary>Adds the user's full name to the auth cookie so views can show it without a DB call.</summary>
public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public AppUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(UserPrincipalExtensions.FullNameClaim, user.FullName));
        return identity;
    }
}