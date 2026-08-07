using TmsApi.Application.DTOs.Course;
using TmsApi.Domain.Entities;
using TmsApi.Application.DTOs.Paged;
using TmsApi.Application.Courses.Commands;
namespace TmsApi.Application.Interfaces;

public interface ICourseService
{
     Task<Course?> GetByCodeAsync(
        string code,
        CancellationToken ct);
    Task<CourseResponseDto?> GetByIdAsync(
         int id,
         CancellationToken ct);
    Task<CourseResponseDto> CreateAsync(
       CreateCourseRequest request,
       CancellationToken ct);

    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken ct);
    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest
    request, CancellationToken ct);
Task<List<Course>> GetAllAsync(
    CancellationToken ct);
Task UpdateAsync(
    UpdateCourseCommand command,
    CancellationToken ct);

}