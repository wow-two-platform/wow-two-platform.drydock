using AwesomeAssertions;
using Wheelhouse.Application.Vaults.Hygiene;

namespace Wheelhouse.Tests.Unit.Vaults;

/// <summary>Tests for <see cref="VaultHygieneRules"/>: which secrets and product tokens are due for rotation.</summary>
public sealed class VaultHygieneRulesTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-26T12:00:00Z");

    private static SecretFacts Secret(string key, int ageDays, string state = "active") =>
        new("billing", key, state, Now.AddDays(-ageDays));

    private static TokenFacts Token(string name, int ageDays, int? expiresInDays = null, bool revoked = false) =>
        new(Guid.NewGuid(), name, Now.AddDays(-ageDays), expiresInDays is { } days ? Now.AddDays(days) : null, revoked);

    [Fact]
    public void Active_secrets_older_than_the_threshold_are_overdue_oldest_first()
    {
        var hygiene = VaultHygieneRules.Evaluate("pilot-vault",
        [
            new NamespaceInventory("billing",
                [Secret("FRESH", 10), Secret("STALE", 91), Secret("STALEST", 400), Secret("OFF", 500, "disabled")], [])
        ], Now);

        hygiene.OverdueSecrets.Select(secret => (secret.Key, secret.AgeDays)).Should().Equal(("STALEST", 400), ("STALE", 91));
        (hygiene.Namespaces, hygiene.Secrets, hygiene.DisabledSecrets).Should().Be((1, 4, 1));
    }

    [Fact]
    public void Tokens_are_flagged_by_expiry_then_age_and_revoked_ones_are_ignored()
    {
        var hygiene = VaultHygieneRules.Evaluate("pilot-vault",
        [
            new NamespaceInventory("billing",
            [],
            [
                Token("young", 5), Token("old", 200), Token("expired", 30, expiresInDays: -1),
                Token("soon", 30, expiresInDays: 7), Token("revoked", 900, revoked: true)
            ])
        ], Now);

        hygiene.OverdueTokens.Select(token => (token.Name, token.Reason))
            .Should().Equal(("old", "rotation due"), ("expired", "expired"), ("soon", "expires soon"));
        hygiene.Tokens.Should().Be(4);
    }

    [Fact]
    public void An_empty_vault_has_nothing_overdue()
    {
        var hygiene = VaultHygieneRules.Evaluate("pilot-vault", [], Now);

        (hygiene.Namespaces, hygiene.OverdueSecrets.Count, hygiene.OverdueTokens.Count).Should().Be((0, 0, 0));
        (hygiene.SecretRotationDays, hygiene.TokenRotationDays).Should().Be((90, 180));
    }
}
