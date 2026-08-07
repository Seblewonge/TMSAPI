using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Application.DTOs.Course;
using TmsApi.Application.DTOs.Paged;
using TmsApi.Application.Courses.Commands;
namespace TmsApi.Infrastructure.Services;

public class CourseService(
    TmsDbContext context,
    ILogger<CourseService> logger) : ICourseService
{
    // public async Task<Course?> GetByIdAsync(
    //     int id,
    //     CancellationToken ct)
    // {
    //     return await context.Courses
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync(c => c.Id == id, ct);
    // }
public Task<CourseResponseDto?> GetByIdAsync(
    int id,
    CancellationToken ct)
{
    return context.Courses
        .AsNoTracking()
        .Where(c => c.Id == id)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count
        ))
        .FirstOrDefaultAsync(ct);
}
    // public async Task<Course> CreateAsync(
    //     Course course,
    //     CancellationToken ct)
    // {
    //     context.Courses.Add(course);

    //     await context.SaveChangesAsync(ct);

    //     logger.LogInformation(
    //         "Course {Code} created successfully.",
    //         course.Code);

    //     return course;
    // }
public async Task<CourseResponseDto> CreateAsync(
    CreateCourseRequest request,
    CancellationToken ct)
{
    var course = new Course
    {
        Code = request.Code,
        Title = request.Title,
        MaxCapacity = request.MaxCapacity
    };


    context.Courses.Add(course);

    await context.SaveChangesAsync(ct);


    logger.LogInformation(
        "Created course {CourseId} ({Code})",
        course.Id,
        course.Code);


    return (await GetByIdAsync(course.Id, ct))!;
}
    public Task<bool> CodeExistsAsync(
        string code,
        CancellationToken ct)
    {
        return context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, ct);
    }
public async Task<Course?> GetByCodeAsync(
    string code,
    CancellationToken ct)
{
    return await context.Courses
        .Include(c => c.Enrollments)
        .FirstOrDefaultAsync(c => c.Code == code, ct);
}
public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(
    PagedRequest request,
    CancellationToken ct)
{
    // TODO 1
    IQueryable<Course> query = context.Courses.AsNoTracking();

    // TODO 2
    if (!string.IsNullOrWhiteSpace(request.Search))
    {
        query = query.Where(c =>
            EF.Functions.ILike(c.Title, $"%{request.Search}%") ||
            EF.Functions.ILike(c.Code, $"%{request.Search}%"));
    }

    // TODO 3 (Count BEFORE paging)
    var totalCount = await query.CountAsync(ct);

    // TODO 4 (OrderBy)
    IOrderedQueryable<Course> sortedQuery;

    switch (request.OrderBy)
    {
        case "Code":
            sortedQuery = request.Descending
                ? query.OrderByDescending(c => c.Code)
                : query.OrderBy(c => c.Code);
            break;

        case "MaxCapacity":
            sortedQuery = request.Descending
                ? query.OrderByDescending(c => c.MaxCapacity)
                : query.OrderBy(c => c.MaxCapacity);
            break;

        //case "Title":
        default:
            sortedQuery = request.Descending
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title);
            break;
    }

    // TODO 5
    var items = await sortedQuery
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(c => new CourseResponseDto(
            c.Id,
            c.Code,
            c.Title,
            c.MaxCapacity,
            c.Enrollments.Count))
        .ToListAsync(ct);

    // TODO 6
    return new PagedResponse<CourseResponseDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
public async Task<List<Course>> GetAllAsync(
    CancellationToken ct)
{
    return await context.Courses
        .Include(c => c.Enrollments)
        .AsNoTracking()
        .ToListAsync(ct);
}
public async Task UpdateAsync(
    UpdateCourseCommand command,
    CancellationToken ct)
{
    var course = await context.Courses
        .FirstOrDefaultAsync(
            c => c.Id == command.Id,
            ct);

    if (course == null)
    {
        throw new Exception(
            $"Course with id {command.Id} not found");
    }

    course.Code = command.Code;
    course.Title = command.Title;
    course.MaxCapacity = command.MaxCapacity;

    await context.SaveChangesAsync(ct);
}
}
