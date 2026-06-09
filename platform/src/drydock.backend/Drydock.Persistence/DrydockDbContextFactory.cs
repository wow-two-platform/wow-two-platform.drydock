using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Drydock.Persistence;

/// <summary>
/// Design-time factory for <see cref="DrydockDbContext"/>. Lets the EF Core tooling
/// (<c>dotnet ef migrations …</c>) construct the context without booting the API host.
/// The connection string is a throwaway local SQLite file used only for model discovery.
/// </summary>
public sealed class DrydockDbContextFactory : IDesignTimeDbContextFactory<DrydockDbContext>
{
    /// <inheritdoc />
    public DrydockDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DrydockDbContext>()
            .UseSqlite("Data Source=drydock-design.db")
            .Options;

        return new DrydockDbContext(options);
    }
}
