using System.Runtime.CompilerServices;
using WoW.Two.Sdk.Backend.Beta.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Backend.Beta.Testing.Data.EntityFrameworkCore;

namespace Drydock.Migrations.Tests.Harness;

/// <summary>
/// Selects the relational provider for the EF test-DB tier (<see cref="DrydockTestDb"/>) once, before any test runs.
/// Postgres is the default (the fidelity baseline). Set <c>DRYDOCK_TEST_DB=sqlite</c> to flip the whole
/// <see cref="RelationalTestDb{TContext}"/>-backed suite to in-memory SQLite — the migrator suite (<c>ApplyTests</c> over
/// <see cref="WoW.Two.Sdk.Backend.Beta.Testing.Data.Migrations.MigratorPostgresFixture"/>) stays on Postgres regardless,
/// since it asserts the real Postgres SQL migrations.
/// </summary>
internal static class TestProviderInitializer
{
    /// <summary>Reads <c>DRYDOCK_TEST_DB</c> and points <see cref="TestSetupOptions.Current"/> at SQLite when it is <c>sqlite</c>.</summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("DRYDOCK_TEST_DB"), "sqlite", StringComparison.OrdinalIgnoreCase))
            TestSetupOptions.Current.Database = DatabaseProvider.Sqlite;
    }
}
