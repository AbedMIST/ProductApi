using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductApi.Data;
using ProductApi.OpenApi;
using ProductApi.Options;
using ProductApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Service registration (dependency injection container) ----
builder.Services.AddControllers();
builder.Services.AddProblemDetails(); // RFC 7807 error bodies
builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer<BearerSchemeDocumentTransformer>();     // "Authorize" button
    o.AddOperationTransformer<BearerRequirementOperationTransformer>(); // lock icon on [Authorize] endpoints
});

// builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("ProductDb"));
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IProductService, ProductService>(); // one instance per HTTP request
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// ---- Authentication: validate incoming JWTs ----
// Fails at startup if the Jwt section (esp. Key) is missing or too short.
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30), // default is 5 min of grace after expiry
            NameClaimType = "unique_name"
        };
        o.MapInboundClaims = false; // keep claim names as written in the token ("sub", not the long URI)
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// SQLite does not create tables on its own. EnsureCreated builds the schema on first run
// (no-op if the database already exists). Switch to EF migrations if the schema will change.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

// ---- HTTP request pipeline (order matters) ----
app.UseExceptionHandler();      // unhandled exceptions -> 500 ProblemDetails
app.UseStatusCodePages();       // add response body for HTTP error 400-599 -> ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();           //expose openapi documentation at /openapi/v1.json

    // Swagger UI at /swagger, rendering the built-in OpenAPI document above
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "ProductApi v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();        // 1. read the token and build the user  (must come before...)
app.UseAuthorization();         // 2. ...check [Authorize] against that user
app.MapControllers();

app.Run();
