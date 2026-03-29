using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.ML.OnnxRuntime;
using HumanRepProj.Data;
using HumanRepProj.HealthChecks;
using HumanRepProj.Models;
using HumanRepProj.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.WithOrigins("http://localhost:7036")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<DbContextHealthCheck<ApplicationDbContext>>("database_check", HealthStatus.Unhealthy, new[] { "ready" });

// ONNX Runtime - YOLOv8n-face
var env = builder.Environment;
var wwwrootPath = env.WebRootPath;
var modelPath = Path.Combine(wwwrootPath, "models", "yolov8n-face.onnx");

if (!File.Exists(modelPath))
    throw new FileNotFoundException($"Model file not found at {modelPath}");

builder.Services.AddSingleton<InferenceSession>(provider =>
    new InferenceSession(modelPath, new Microsoft.ML.OnnxRuntime.SessionOptions
    {
        ExecutionMode = Microsoft.ML.OnnxRuntime.ExecutionMode.ORT_SEQUENTIAL
    }));

// Face Recognition Service
builder.Services.AddScoped<FaceRecognitionService>();
builder.Services.AddSingleton<IOnnxFaceDetectionService, OnnxFaceDetectionService>();

var app = builder.Build();

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();     // ✅ Serves static files (e.g., ONNX models in `wwwroot/`)
app.UseRouting();         // ✅ Routes HTTP requests to endpoints
app.UseCors("DevelopmentCors");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health");

app.MapControllers();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/Login"));

// Ensure database exists and then verify connectivity during startup.
await EnsureDatabaseCreated(app.Services, app.Logger);
await ResetAccountsAndCreateAdmin(app.Services, app.Logger);
await VerifyDatabaseConnection(app.Services, app.Logger);

await app.RunAsync();

async Task EnsureDatabaseCreated(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        logger.LogInformation("Ensuring local database exists...");
        await dbContext.Database.EnsureCreatedAsync();
        logger.LogInformation("Database is ready");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize database");
    }
}

async Task VerifyDatabaseConnection(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        logger.LogInformation("Verifying database connection...");
        if (await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("Database connection successful");
        }
        else
        {
            logger.LogError("Database connection failed");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database verification failed");
    }
}

async Task ResetAccountsAndCreateAdmin(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation("Resetting existing application accounts...");

        var existingUsers = await dbContext.ApplicationUsers.ToListAsync();
        if (existingUsers.Count > 0)
        {
            dbContext.ApplicationUsers.RemoveRange(existingUsers);
            await dbContext.SaveChangesAsync();
        }

        var adminDepartment = await dbContext.Departments.FirstOrDefaultAsync(d => d.Name == "Administration");
        if (adminDepartment == null)
        {
            adminDepartment = new Department
            {
                Name = "Administration",
                Description = "System administration department",
                Performance = 100,
                Budget = 0,
                Status = "Active",
                DateCreated = DateTime.UtcNow
            };

            dbContext.Departments.Add(adminDepartment);
            await dbContext.SaveChangesAsync();
        }

        var adminEmployee = await dbContext.Employees.FirstOrDefaultAsync(e => e.Email == "admin@hris.local");
        if (adminEmployee == null)
        {
            adminEmployee = new Employee
            {
                FirstName = "System",
                LastName = "Admin",
                Email = "admin@hris.local",
                PhoneNumber = null,
                Address = "Local",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Other",
                DepartmentID = adminDepartment.DepartmentID,
                Position = "Administrator",
                Salary = 0,
                DateHired = DateTime.UtcNow.Date,
                EmploymentType = "Full-time",
                Status = "Active",
                IsManager = true
            };

            dbContext.Employees.Add(adminEmployee);
            await dbContext.SaveChangesAsync();
        }

        dbContext.ApplicationUsers.Add(new ApplicationUser
        {
            EmployeeID = adminEmployee.EmployeeID,
            Username = "admin",
            Password = "admin@123",
            LastLogin = null,
            FailedAttempts = 0,
            IsLocked = false
        });

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Admin account is ready. Username: admin");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to reset accounts and create admin user");
    }
}