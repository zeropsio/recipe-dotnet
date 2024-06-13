using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Entry> Entries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entry>()
            .ToTable("entries")
            .Property(e => e.Id)
            .HasColumnName("id");

        modelBuilder.Entity<Entry>()
            .Property(e => e.Data)
            .HasColumnName("data");
    }
}

public class Entry
{
    public int Id { get; set; }
    public string Data { get; set; }
}
