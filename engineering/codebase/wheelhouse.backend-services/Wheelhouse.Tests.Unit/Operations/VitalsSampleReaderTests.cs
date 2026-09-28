using System.Text.Json;
using AwesomeAssertions;
using Wheelhouse.Application.Operations;

namespace Wheelhouse.Tests.Unit.Operations;

/// <summary>Tests for <see cref="VitalsSampleReader"/>: one runner vitals pass into compact per-target samples.</summary>
public sealed class VitalsSampleReaderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-28T12:00:00Z");

    private static JsonElement Vitals(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void A_readable_target_yields_load_memory_fullest_disk_and_container_health()
    {
        var samples = VitalsSampleReader.Read(Vitals("""
            {"collectedAt":"2026-09-28T12:00:00Z","targets":[{"targetId":"pin-dev","serverId":"local","ok":true,
              "host":{"cpus":4,"load":[2.0,1.0,0.5],"memoryTotalBytes":1000,"memoryAvailableBytes":250,"uptimeSeconds":9,
                      "disks":[{"path":"/","totalBytes":100,"freeBytes":60},{"path":"/var/lib/docker","totalBytes":200,"freeBytes":20}]},
              "containers":[{"service":"api","state":"running","health":"healthy","restarts":2},
                            {"service":"worker","state":"running","health":null,"restarts":0},
                            {"service":"edge","state":"exited","health":null,"restarts":5}]}]}
            """), Now);

        var sample = samples.Should().ContainSingle().Subject;
        (sample.TargetId, sample.ServerId, sample.SampledAtUtc, sample.Readable).Should().Be(("pin-dev", "local", Now, true));
        (sample.LoadPercent, sample.MemoryPercent, sample.DiskPercent).Should().Be((50.0, 75.0, 90.0));
        (sample.Containers, sample.HealthyContainers, sample.Restarts).Should().Be((3, 2, 7));
    }

    [Fact]
    public void An_unreadable_target_is_kept_without_figures_and_malformed_rows_are_skipped()
    {
        var samples = VitalsSampleReader.Read(Vitals("""
            {"targets":[{"targetId":"pin-test","serverId":"local","ok":false,"reason":"SSH connection refused"},
                        {"serverId":"local","ok":true},
                        {"targetId":"pin-prod","serverId":"local","ok":true,"host":{"cpus":0,"load":[1,1,1]}}]}
            """), Now);

        samples.Select(s => (s.TargetId, s.Readable, s.LoadPercent, s.MemoryPercent, s.Containers))
            .Should().Equal(("pin-test", false, null, null, 0), ("pin-prod", true, null, null, 0));
    }

    [Fact]
    public void A_pass_without_targets_yields_nothing() =>
        VitalsSampleReader.Read(Vitals("""{"collectedAt":"2026-09-28T12:00:00Z"}"""), Now).Should().BeEmpty();
}
