using System.ComponentModel.DataAnnotations;

namespace Wheelhouse.Api.Requests;

/// <summary>Names the one commit to build; accepts no branch, workflow or input of the caller's choosing.</summary>
public sealed record DeploymentBuildRequest([Required, RegularExpression("^[a-f0-9]{40}$")] string Commit);
