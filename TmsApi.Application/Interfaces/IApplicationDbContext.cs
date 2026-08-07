using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Enrollment> Enrollments { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}