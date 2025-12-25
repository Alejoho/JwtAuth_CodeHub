using System;
using Microsoft.AspNetCore.Identity;
using WebApiRefreshTokenDemo.Models;

namespace WebApiRefreshTokenDemo;

public static class TestConfiguration
{
    private const string UserName = "test";
    private const string Email = "test@test.test";
    private const string Password = "Test1234.";
    private const string RoleName = "Admin";
    public static async Task ConfigureTestData(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        await CreateUser(services);
        await CreateRole(RoleName, services);
        await AssingRole(services);
    }

    private static async Task CreateUser(IServiceProvider services)
    {
        var userManager = services
            .GetRequiredService<UserManager<ApplicationUser>>();

        // Ensure user exists 
        var user = await userManager.FindByEmailAsync(Email);
        if (user != null)
        {
            return;
        }

        user = new ApplicationUser
        {
            UserName = UserName,
            Email = Email
        };

        var result = await userManager.CreateAsync(user, Password);

        if (result.Succeeded is false)
        {
            throw new Exception("Test user was not able to be created.");
        }
    }

    private static async Task CreateRole(string name, IServiceProvider services)
    {
        var roleManager = services
            .GetRequiredService<RoleManager<IdentityRole>>();

        if (await roleManager.RoleExistsAsync(name))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole(name));

        if (result.Succeeded is false)
        {
            throw new Exception("Test admin role was not able to be created.");
        }
    }

    private static async Task AssingRole(IServiceProvider services)
    {
        var userManager = services
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(Email);

        if (user is null)
        {
            throw new Exception("The test user doesn't exist.");
        }

        if (await userManager.IsInRoleAsync(user, RoleName))
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(user, RoleName);

        if (result.Succeeded is false)
        {
            throw new Exception("The role admin can't be assign to the test user.");
        }
    }
}
