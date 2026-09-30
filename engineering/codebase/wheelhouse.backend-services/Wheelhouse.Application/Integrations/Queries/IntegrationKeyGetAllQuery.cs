using Wheelhouse.Application.Integrations.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Integrations.Queries;

/// <summary>Represents a query to get every integration key, revoked ones included.</summary>
public sealed record IntegrationKeyGetAllQuery : IQuery<AppResult<IReadOnlyList<IntegrationKeyDto>>>;
