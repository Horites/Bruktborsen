using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Bruktborsen.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["Admin", "User"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(
                    role
                ))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(
                        role
                    )
                );

                EnsureSuccess(
                    result,
                    $"Creating role {role}"
                );
            }
        }

        var users = new[]
        {
            new SeedUser(
                "admin@brukt.no",
                "Administrator",
                "Admin"
            ),
            new SeedUser(
                "ola@brukt.no",
                "Ola Nordmann",
                "User"
            ),
            new SeedUser(
                "kari@brukt.no",
                "Kari Nordmann",
                "User"
            ),
            new SeedUser(
                "per@brukt.no",
                "Per Hansen",
                "User"
            ),
        };

        foreach (var seedUser in users)
        {
            var user = await userManager.FindByEmailAsync(
                seedUser.Email
            );

            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = seedUser.Email,
                    Email = seedUser.Email,
                    EmailConfirmed = true,
                    FullName = seedUser.FullName,
                };

                var result = await userManager.CreateAsync(
                    user,
                    "BruktDemo123!"
                );

                EnsureSuccess(
                    result,
                    $"Creating user {seedUser.Email}"
                );
            }

            if (!await userManager.IsInRoleAsync(
                    user,
                    seedUser.Role
                ))
            {
                var result = await userManager.AddToRoleAsync(
                    user,
                    seedUser.Role
                );

                EnsureSuccess(
                    result,
                    $"Assigning role {seedUser.Role}"
                );
            }
        }
    }

    private static void EnsureSuccess(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(e => e.Description
            )
        );

        throw new InvalidOperationException(
            $"{operation} failed: {errors}"
        );
    }

    private sealed record SeedUser(string Email, string FullName, string Role);
}
