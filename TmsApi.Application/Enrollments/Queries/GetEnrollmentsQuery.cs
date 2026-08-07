using MediatR;
using TmsApi.Application.DTOs.Enrollment;

namespace TmsApi.Application.Enrollments.Queries;

public record GetEnrollmentsQuery()
    : IRequest<IReadOnlyList<EnrollmentResponseDto>>;