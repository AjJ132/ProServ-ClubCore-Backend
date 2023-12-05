using Microsoft.EntityFrameworkCore;

namespace ProServ_ClubCore_Server_API.Database;

public class ProServDbContextFactory : IDbContextFactory<ProServDbContext>
{
    private readonly DbContextOptions<ProServDbContext> _options;

    public ProServDbContextFactory(DbContextOptions<ProServDbContext> options)
    {
        _options = options;
    }

    public ProServDbContext CreateDbContext()
    {
        return new ProServDbContext(_options);
    }
}