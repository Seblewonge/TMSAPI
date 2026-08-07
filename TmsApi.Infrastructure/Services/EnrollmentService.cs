using Microsoft.Extensions.Logging;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Application.DTOs.Enrollment;
namespace TmsApi.Infrastructure.Services;


public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int
    id, CancellationToken ct) =>
    context.Enrollments
    .AsNoTracking()
    .Where(e => e.Id == id && e.CourseId == courseId)
    .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId,e.Course.Title, e.
    StudentId, e.Student.Name, e.Status, e.EnrolledAt))
    .FirstOrDefaultAsync(ct);
    public async Task<EnrollmentResponseDto> CreateAsync(int courseId,
    EnrollStudentRequest request, CancellationToken ct)
    {
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created enrollment {EnrollmentId} for Course {CourseId} and Student {StudentId}",
            enrollment.Id,
            enrollment.CourseId,
            enrollment.StudentId);

        return (await GetByIdAsync(courseId, enrollment.Id, ct))!;
    }
    public async Task<List<EnrollmentResponseDto>> GetByCourseAsync(
        int courseId,
        CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto
            (
                 e.Id,
                 e.CourseId,
                 e.Course.Title,
                 e.StudentId,
                 e.Student.Name,
                 e.Status,
                 e.EnrolledAt
            ))
            .ToListAsync(ct);
    }
    public async Task<bool> ExistsAsync(
        int studentId,
        string courseCode,
        CancellationToken ct)
    {
        return await context.Enrollments
            .Include(e => e.Course)
            .AnyAsync(e =>
                e.StudentId == studentId &&
                e.Course.Code == courseCode,
                ct);
    }
    public async Task AddAsync(
        Enrollment enrollment,
        CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
    }
public async Task<List<Enrollment>> GetByStudentIdAsync(
    int studentId,
    CancellationToken ct)
{
    return await context.Enrollments
        .Include(e => e.Course)
        .Where(e => e.StudentId == studentId)
        .ToListAsync(ct);
}
}