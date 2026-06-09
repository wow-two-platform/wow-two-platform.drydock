using Drydock.Application.Servers.Models;
using Drydock.Domain.Results;
using MediatR;

namespace Drydock.Application.Servers.Queries.ListServers;

/// <summary>Lists all registered servers.</summary>
public sealed record ListServersQuery : IRequest<Result<IReadOnlyList<ServerDto>>>;
