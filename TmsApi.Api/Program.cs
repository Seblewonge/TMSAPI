using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;
using TmsApi.Api;
using TmsApi.Api.Filters;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Api.Middlewares;
using TmsApi.Api.Options;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Services;
using TmsApi.Infrastructure.SeedData;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Api.ExceptionHandlers;
//using Microsoft.Extensions.Caching.Hybrid;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Api.RateLimiting;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Transcripts;
using System.Threading.Channels;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Api.Hubs;
using TmsApi.Api.Notifications;
using TmsApi.Application.Notifications;
using Microsoft.AspNetCore.Antiforgery;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddAntiforgery(options =>
{
options.HeaderName = "X-XSRF-TOKEN";
});

// Controllers and services
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();
builder.Services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

// LoggingBehavior FIRST
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var (partitionKey, tier) = ApiKeyResolver.Resolve(httpContext);

            return tier switch
            {
                ApiKeyTier.Paid =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"paid:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 200,
                            TokensPerPeriod = 100,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }),

                ApiKeyTier.Free =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"free:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 30,
                            TokensPerPeriod = 10,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }),

                _ =>
                    RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"anon:{partitionKey}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 10,
                            TokensPerPeriod = 5,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        })
            };
        });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";

        if (context.Lease.TryGetMetadata(
                MetadataName.RetryAfter,
                out var ts))
        {
            retryAfter = ((int)ts.TotalSeconds).ToString();
        }

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "Rate limit exceeded",
                Detail = $"Too many requests. Retry after {retryAfter} seconds.",
                Status = StatusCodes.Status429TooManyRequests,
                Type = "https://tms.local/errors/rate_limit_exceeded"
            },
            ct);
    };
    options.AddConcurrencyLimiter("transcripts", opt =>
    {
        opt.PermitLimit = 5;// 5 in-flight transcripts maximu
        
        opt.QueueLimit = 20;// queue up to 20 more


        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddTokenBucketLimiter("search", opt =>
    {
        opt.TokenLimit = 10;
        opt.TokensPerPeriod = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 2;
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddControllers(options =>
{
options.Filters.Add<AuditLogFilter>();
});
// Authentication
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>(
        "Training", options => { });

builder.Services.AddAuthorization();

// Payment options
builder.Services
    .AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Database
// builder.Services.AddDbContext<TmsDbContext>(options =>
//     options.UseNpgsql(
//         builder.Configuration.GetConnectionString("TmsDatabase"))
//     .LogTo(Console.WriteLine, LogLevel.Information)
//     .EnableSensitiveDataLogging());
builder.Services.AddDbContext<TmsDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase")));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<TmsDbContext>());

builder.Services.AddOpenApi("v1", options =>
{
options.ShouldInclude = description =>
description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
options.ShouldInclude = description =>
description.GroupName == "v2";
});
builder.Services.AddApiVersioning(options =>
{
options.DefaultApiVersion = new ApiVersion(1, 0);
options.AssumeDefaultVersionWhenUnspecified = true;
options.ReportApiVersions = true;
options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
options.GroupNameFormat = "'v'VVV";
options.SubstituteApiVersionInUrl = true;
});

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAngular", policy =>
//     {
//         policy
//             .WithOrigins("http://localhost:4200")
//             .AllowAnyHeader()
//             .AllowAnyMethod();
//     });
// });
builder.Services.AddSingleton < ITranscriptStatusStore, InMemoryTranscriptStatusStore > ();

builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
    new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait
    }));
builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddSignalR();

builder.Services.AddSingleton < ITranscriptNotificationService, SignalRTranscriptNotificationService > ();
// Load allowed origins from appsettings.Development.json
var allowedOrigins = builder.Configuration
.GetSection("AllowedOrigins").Get<string[]>()
?? ["http://localhost:4200"];
// Register the CORS policy in the Dependency Injection container
builder.Services.AddCors(options =>
{
options.AddPolicy("TmsClient", policy =>
{
policy.WithOrigins(allowedOrigins)
.AllowAnyHeader()
.AllowAnyMethod()
.AllowCredentials() // Vital for HttpOnly auth cookies in Session 2
.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
});
});
var app = builder.Build();
app.MapHub<TmsHub>("/hubs/tms");

// Development tools
if (app.Environment.IsDevelopment())
{

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(
                ScalarTarget.CSharp,
                ScalarClient.HttpClient);

        // Tell Scalar to show both API versions
        options
            .AddDocument("v1", "API Version 1.0")
            .AddDocument("v2", "API Version 2.0");
    });
}
else
{
    app.UseExceptionHandler();
}


// Middleware pipeline
app.UseStatusCodePages();
//app.UseCors("AllowAngular");
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRouting();
app.UseRateLimiter();
app.UseCors("TmsClient");
//app.UseCors("AllowAngular");
app.UseAuthentication();

app.UseAuthorization();

app.Use(async (context, next) =>
{
if (context.User.Identity?.IsAuthenticated == true || context.
Request.Cookies.ContainsKey("tms_auth"))
{
var antiforgery = context.RequestServices
.GetRequiredService<IAntiforgery>();
var tokens = antiforgery.GetAndStoreTokens(context);
context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
new CookieOptions
{
HttpOnly = false, // MUST be false so Angular JavaScript can read it!
Secure = !builder.Environment.IsDevelopment(),
SameSite = SameSiteMode.Strict
});
}
await next(context);
});
app.UseMiddleware<V1DeprecationMiddleware>();
//app.UseCors("AllowAngular");
// Controllers
app.MapControllers();


// Protected endpoint
app.MapGet("/api/assessments/results", () =>
{
    return Results.Ok(new
    {
        message = "Assessment results retrieved successfully."
    });
})
.RequireAuthorization();


// Error testing endpoint
app.MapGet("/api/error", () =>
{
    throw new Exception("Simulated database failure for ProblemDetails testing");
});


// Apply migrations and run deterministic seeder
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

    await context.Database.MigrateAsync();
if (!context.Students.Any())
{
var students = new List<Student>
{
new() { RegistrationNumber = "TMS-2026-0001", Name = "AliceSmith", GPA = 3.8m, Age = 23, IsActive = true },
new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m,  Age = 24, IsActive = true },
new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, Age = 26, IsActive = false },
new() { RegistrationNumber = "TMS-2026-0004", Name = "DianaPrince", GPA = 3.9m,  Age = 25,IsActive = true },
new() { RegistrationNumber = "TMS-2026-0005", Name = "EvanWright", GPA = 2.5m,  Age = 23, IsActive = true }
};
context.Students.AddRange(students);
var courses = new List<Course>
{
new() { Code = "CS-101", Title = "Introduction to ComputerScience", MaxCapacity = 30 },
new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity =
40 }
};
context.Courses.AddRange(courses);
context.SaveChanges();
var enrollments = new List<Enrollment>
{
new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
};
context.Enrollments.AddRange(enrollments);
context.SaveChanges();
}
}



if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

    await DataSeeder.SeedAsync(context);
}


app.Run();