using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartAttendance.Models;
using SmartAttendance.Web.Infrastructure;
using SmartAttendance.Web.Models;

namespace SmartAttendance.Web.Data.Seed;

public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedOptions _options;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        AppDbContext db,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<SeedOptions> options,
        ILogger<DbSeeder> logger)
    {
        _db = db;
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var pending = (await _db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count > 0)
        {
            _logger.LogWarning(
                "Skipping seed: {Count} pending migration(s). Run 'dotnet ef database update' first.", pending.Count);
            return;
        }

        await SeedRolesAsync();
        await SeedSettingsAsync(ct);
        await SeedUsersAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (await _roleManager.RoleExistsAsync(role)) continue;

            var result = await _roleManager.CreateAsync(new IdentityRole(role));
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Could not create role '{role}': {string.Join("; ", result.Errors.Select(e => e.Description))}");

            _logger.LogInformation("Created role {Role}", role);
        }
    }

    private async Task SeedSettingsAsync(CancellationToken ct)
    {
        var existing = await _db.AppSettings.Select(s => s.Key).ToListAsync(ct);

        var missing = AppSettingKeys.Defaults
            .Where(d => !existing.Contains(d.Key))
            .Select(d => new AppSetting { Key = d.Key, Value = d.Value })
            .ToList();

        if (missing.Count == 0) return;

        _db.AppSettings.AddRange(missing);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} default application setting(s)", missing.Count);
    }

    private async Task SeedUsersAsync()
    {
        await SeedUserAsync(_options.AdminEmail, _options.AdminPassword, _options.AdminFullName);
        await SeedUserAsync(_options.CrEmail, _options.CrPassword, _options.CrFullName);
    }

    private async Task SeedUserAsync(string? emailInput, string? password, string fullName)
    {
        if (string.IsNullOrWhiteSpace(emailInput) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("User seed skipped: email or password not configured.");
            return;
        }

        var email = emailInput.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true
            };

            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                // Log Identity's descriptions only: never log the password itself.
                _logger.LogError("User seed failed for {Email}: {Errors}", email,
                    string.Join("; ", create.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Created user {Email}", email);
        }
        else if (!user.IsActive)
        {
            user.IsActive = true;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Re-activated user {Email}", email);
        }

        if (!await _userManager.IsInRoleAsync(user, Roles.Admin))
        {
            var add = await _userManager.AddToRoleAsync(user, Roles.Admin);
            if (!add.Succeeded)
                _logger.LogError("Could not add {Email} to role: {Errors}", email,
                    string.Join("; ", add.Errors.Select(e => e.Description)));
        }
    }
}