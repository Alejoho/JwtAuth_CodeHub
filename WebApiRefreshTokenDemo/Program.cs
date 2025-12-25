using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using WebApiRefreshTokenDemo;
using WebApiRefreshTokenDemo.Data;
using WebApiRefreshTokenDemo.Models;
using WebApiRefreshTokenDemo.Options;
using WebApiRefreshTokenDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddOptions<JwtSettingsOptions>()
    .BindConfiguration(JwtSettingsOptions.SectionPath)
    .ValidateDataAnnotations();

builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseInMemoryDatabase("AuthDb"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
{
    opts.Password.RequireDigit = true;
    opts.Password.RequiredLength = 8;
    opts.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var jwtOptions = builder.Configuration
    .GetSection(JwtSettingsOptions.SectionPath)
    .Get<JwtSettingsOptions>();

if (jwtOptions is null)
{
    throw new InvalidOperationException("Unable to bind configuration section to object JwtSettingOptions");
}

var key = Encoding.UTF8.GetBytes(jwtOptions.SecurityKey);

builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.ConfigureTestData();

    app.MapOpenApi();

    app.MapScalarApiReference(string.Empty);
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
