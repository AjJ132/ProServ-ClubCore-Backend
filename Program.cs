using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProServ_ClubCore_Server_API.Database;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContextFactory<ProServDbContext>(options =>
    options.UseNpgsql(
        configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptionsAction: npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        }));

builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<ProServDbContext>();

/*
builder.Services.AddAuthentication(options =>
{
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/api/auth/login"; // Your login path
    options.LogoutPath = "/api/auth/logout"; // Your logout path
    options.AccessDeniedPath = "/api/auth/accessdenied"; // Your logout path

    // Set SameSite and Secure Policy
    options.Cookie.Name = "Authorization";
    options.Cookie.SameSite = SameSiteMode.None; // Set SameSite to None
    options.Cookie.HttpOnly = true; // Set HttpOnly to true
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Set SecurePolicy to Always
    options.ExpireTimeSpan = TimeSpan.FromDays(5); // Set cookie expiration
});
 */


builder.Services.AddCors(options =>
{
    options.AddPolicy("MyAllowSpecificOrigins",
    builder =>
    {
        builder.WithOrigins("https://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


/*
  builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.AllowedForNewUsers = false;
    options.Password.RequireNonAlphanumeric = false;

})
.AddEntityFrameworkStores<ProServDbContext>();
 */

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ProServDbContext>();
        context.Database.EnsureCreated();
    }
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.MapGet("/WeatherForecast", () => "Hello World!")
//    .WithName("GetWeatherForecast")
//    .WithOpenApi()
//    .RequireAuthorization();


app.UseCors("MyAllowSpecificOrigins");

app.UseHttpsRedirection();

app.MapIdentityApi<IdentityUser>();

app.MapControllers();   

app.Run();

