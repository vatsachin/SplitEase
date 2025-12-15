using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SplitEase.Helpers;
using SplitEase.Models;
using SplitEase.ProfileMapper;
using SplitEase.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load JWT settings
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JWTSettings>(jwtSettingsSection);
var jwtSettings = jwtSettingsSection.Get<JWTSettings>();

// Register services
builder.Services.AddSingleton<IJwtService, JwtService>(); 
builder.Services.AddScoped<IUserService, UserServices>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IExpensiveService, ExpenseService>();
builder.Services.AddScoped<IExpensiveSplitService, ExpenseSplitService>();

builder.Services.AddHttpContextAccessor();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs"))
);

// Add AutoMapper
var mapConfig = new MapperConfiguration(mc =>
{
    mc.AddProfile<YourMappingProfile>();
});
IMapper mapper = mapConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

// Add Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add JWT Authentication
if(jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Key) ||
    string.IsNullOrEmpty(jwtSettings.Issuer) || string.IsNullOrEmpty(jwtSettings.Audience))
{
    throw new Exception("JWT settings are not configured properly in appsettings.json!");
}

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure middleware

    app.UseSwagger();
    app.UseSwaggerUI();


//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "SplitEase API is running ");

app.Run();

