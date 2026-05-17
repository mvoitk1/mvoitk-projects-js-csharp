using App.DAL.EF;
using App.Domain.Identity;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace WebApp.Setup;

public static class IdentitySetupExtensions
{
    public static IServiceCollection AddAppIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddIdentity<AppUser, AppRole>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddDefaultUI()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JWT:Key"]!)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JWT:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }
}
