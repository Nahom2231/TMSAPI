using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;
namespace TmsApi.Data;

public class TmsDbContext (DbContextOptions<TmsDbContext> options) :DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<Assessment> Assessments=> Set<Assessment>();
    public DbSet<Certificate> Certificates =>Set <Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);

        modelBuilder.Entity<Student>()
        .Property<DateTime>("LastUpdated");

        modelBuilder.Entity<Student>().Property(s=> s.Version)
        .IsRowVersion();
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)

    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);
        foreach (var entry in entries)
        {
            if (entry.Entity is Student)
            {
                entry.Property("LastUpdated").CurrentValue = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

