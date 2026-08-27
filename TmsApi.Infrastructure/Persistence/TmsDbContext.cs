// using Microsoft.EntityFrameworkCore;
// //using Microsoft.EntityFrameworkCore.Metadata.Builders;
// using TmsApi.Domain.Entities;
// using TmsApi.Application.Interfaces;
// namespace TmsApi.Infrastructure.Persistence;
// public class TmsDbContext(DbContextOptions<TmsDbContext> options) : DbContext(options),IApplicationDbContext
// {
//       public DbSet<Student> Students => Set<Student>();
//       public DbSet<Course> Courses => Set<Course>();
//       public DbSet<Enrollment> Enrollments => Set<Enrollment>();
//   public DbSet<Assessment> Assessment => Set<Assessment>();
//   public DbSet<Certificate> Certificate => Set<Certificate>();

// protected override void OnModelCreating(ModelBuilder modelBuilder)
// {
//     modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);

//     base.OnModelCreating(modelBuilder);
// }
// }

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Identity;

namespace TmsApi.Infrastructure.Persistence;

public class TmsDbContext : IdentityDbContext<TmsUser>, IApplicationDbContext
{
    public TmsDbContext(DbContextOptions<TmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessment => Set<Assessment>();
    public DbSet<Certificate> Certificate => Set<Certificate>();
public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TmsDbContext).Assembly);
    }
}