// using TmsApi.Application.DTOs.Course;

// namespace TmsApi.Application.Interfaces;

// public interface ICachedCourseService
// {
//     Task<CourseResponseDto> GetCourseAsync(string code, CancellationToken ct);

//     Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct);

//     Task InvalidateCourseCacheAsync(CancellationToken ct);
// }
using TmsApi.Application.DTOs.Course;

namespace TmsApi.Application.Interfaces;

public interface ICachedCourseService
{
    Task<List<CourseResponseDto>> GetAllCoursesAsync(
        CancellationToken ct);


    Task<CourseResponseDto?> GetCourseAsync(
        string code,
        CancellationToken ct);


    Task InvalidateCourseCacheAsync(
        CancellationToken ct);
}