using MediatR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs.Enrollment;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public class GetEnrollmentsQueryHandler
    : IRequestHandler<GetEnrollmentsQuery, IReadOnlyList<EnrollmentResponseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEnrollmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> Handle(
        GetEnrollmentsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Enrollments
            .Where(e => !e.IsArchived)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.Course.Title,
                e.StudentId,
                e.Student.Name,
                e.Status,
                e.EnrolledAt
            ))
            .ToListAsync(cancellationToken);
    }
}