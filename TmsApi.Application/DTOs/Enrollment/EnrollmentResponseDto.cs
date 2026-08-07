namespace TmsApi.Application.DTOs.Enrollment;
public record EnrollmentResponseDto(
int Id,
int CourseId,
string CourseName,
int StudentId,
string StudentName,
string Status,
DateTime EnrolledAt);