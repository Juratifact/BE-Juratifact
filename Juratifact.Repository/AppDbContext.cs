using Microsoft.EntityFrameworkCore;

namespace Juratifact.Repository;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}