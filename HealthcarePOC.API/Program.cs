using FluentValidation.AspNetCore;
using HealthcarePOC.API.Data;
using HealthcarePOC.API.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole(); // Log to Console
builder.Logging.AddDebug(); // Log Debug Info

// FIXED: Correct logger initialization
var loggerFactory = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});
var logger = loggerFactory.CreateLogger("GlobalLogger");

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularUI",
        policy => policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// Add Controllers with FluentValidation
builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Context (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// Fetch JWT Secret Key from AWS Secrets Manager
var jwtSecretKey = await Helper.GetSecretAsync("SCP");
var privateKey = await Helper.GetSecretAsync("PKCS_SECRET_KEY");
EncryptionService.LoadPrivateKey(privateKey);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;  // Allow HTTP during local dev
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)) // Use secret from AWS
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate(); // Ensure database is up to date

    Helper.SeedDatabase(context);
}


// Log middleware for tracking requests
app.Use(async (context, next) =>
{
    logger.LogInformation($"Incoming Request: {context.Request.Method} {context.Request.Path}");
    await next();
    logger.LogInformation($"Outgoing Response: {context.Response.StatusCode}");
});

// Swagger middleware (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware structured
if (app.Environment.IsDevelopment() && builder.Configuration["SecuritySettings:Mode"] == "Insecure")
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularUI");  // Explicitly specify your policy here
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

