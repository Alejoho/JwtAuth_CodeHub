using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApiRefreshTokenDemo.Models;

namespace WebApiRefreshTokenDemo.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
}
