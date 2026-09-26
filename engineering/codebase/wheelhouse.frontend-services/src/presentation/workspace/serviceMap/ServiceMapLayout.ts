import { TopologyAvailability, type ServiceTopology } from "@/domain/topology";

/** Visual roles in a map of declared Compose resources. */
export const MapNodeKind = {
  Service: "service",
  Network: "network",
  Volume: "volume",
} as const;
export type MapNodeKind = (typeof MapNodeKind)[keyof typeof MapNodeKind];
/** Relationships describe membership or startup ordering, never inferred calls. */
export const MapEdgeKind = {
  Network: "network",
  Volume: "volume",
  Dependency: "dependency",
} as const;
export type MapEdgeKind = (typeof MapEdgeKind)[keyof typeof MapEdgeKind];

/** One stable resource box in the map's scrollable coordinate space. */
export interface ServiceMapNode {
  readonly id: string;
  readonly kind: MapNodeKind;
  readonly name: string;
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
  readonly external: boolean;
}

/** A connector with explicit endpoints and a plain-language equivalent. */
export interface ServiceMapEdge {
  readonly id: string;
  readonly kind: MapEdgeKind;
  readonly from: string;
  readonly to: string;
  readonly services: readonly string[];
  readonly path: string;
  readonly description: string;
}

/** A deterministic layout, independent of DOM measurements and input ordering. */
export interface ServiceMapLayout {
  readonly width: number;
  readonly height: number;
  readonly nodes: readonly ServiceMapNode[];
  readonly edges: readonly ServiceMapEdge[];
}

/** Places shared resource hubs beside service cards, routing cycles through a separate dependency gutter. */
export function buildServiceMapLayout(
  topology: ServiceTopology,
  showVolumes = false,
): ServiceMapLayout {
  if (topology.availability !== TopologyAvailability.Available)
    return { width: 600, height: 0, nodes: [], edges: [] };
  const services = [...topology.services].sort(byName);
  const networks = [...topology.networks].sort(byName);
  const volumes = showVolumes ? [...topology.volumes].sort(byName) : [];
  const contentHeight = Math.max(
    88,
    services.length * 112 - 24,
    networks.length * 88 - 24,
    volumes.length * 88 - 24,
  );
  const nodes: ServiceMapNode[] = [
    ...services.map((service, index) => ({
      id: resourceId(MapNodeKind.Service, service.name),
      kind: MapNodeKind.Service,
      name: service.name,
      x: 244,
      y: 48 + index * 112,
      width: 240,
      height: 88,
      external: false,
    })),
    ...networks.map((network, index) => ({
      id: resourceId(MapNodeKind.Network, network.name),
      kind: MapNodeKind.Network,
      name: network.name,
      x: 16,
      y: 48 + (contentHeight - (networks.length * 88 - 24)) / 2 + index * 88,
      width: 160,
      height: 64,
      external: network.external,
    })),
    ...volumes.map((volume, index) => ({
      id: resourceId(MapNodeKind.Volume, volume.name),
      kind: MapNodeKind.Volume,
      name: volume.name,
      x: 568,
      y: 48 + (contentHeight - (volumes.length * 88 - 24)) / 2 + index * 88,
      width: 168,
      height: 64,
      external: volume.external,
    })),
  ];
  const byId = new Map(nodes.map((node) => [node.id, node]));
  const edges: ServiceMapEdge[] = [];
  for (const service of services) {
    const node = byId.get(resourceId(MapNodeKind.Service, service.name))!;
    for (const name of [...service.networks].sort()) {
      const hub = byId.get(resourceId(MapNodeKind.Network, name));
      if (hub)
        edges.push({
          id: JSON.stringify([MapEdgeKind.Network, service.name, name]),
          kind: MapEdgeKind.Network,
          from: hub.id,
          to: node.id,
          services: [service.name],
          path: curve(
            hub.x + hub.width,
            hub.y + hub.height / 2,
            node.x,
            node.y + node.height / 2,
          ),
          description: `${service.name} joins network ${name}`,
        });
    }
    for (const name of [...service.volumes].sort()) {
      const hub = byId.get(resourceId(MapNodeKind.Volume, name));
      if (hub)
        edges.push({
          id: JSON.stringify([MapEdgeKind.Volume, service.name, name]),
          kind: MapEdgeKind.Volume,
          from: node.id,
          to: hub.id,
          services: [service.name],
          path: curve(
            node.x + node.width,
            node.y + node.height / 2,
            hub.x,
            hub.y + hub.height / 2,
          ),
          description: `${service.name} mounts named volume ${name}`,
        });
    }
  }
  const dependencies = [...topology.dependencies].sort(
    (left, right) =>
      compare(left.from, right.from) || compare(left.to, right.to),
  );
  dependencies.forEach((dependency, index) => {
    const from = byId.get(resourceId(MapNodeKind.Service, dependency.from));
    const to = byId.get(resourceId(MapNodeKind.Service, dependency.to));
    if (!from || !to) return;
    const rail = from.x + from.width + 28 + (index % 4) * 12;
    const startY = from.y + from.height * 0.3;
    const endY = to.y + to.height * 0.7;
    edges.push({
      id: JSON.stringify([
        MapEdgeKind.Dependency,
        dependency.from,
        dependency.to,
      ]),
      kind: MapEdgeKind.Dependency,
      from: from.id,
      to: to.id,
      services: [dependency.from, dependency.to],
      path: `M ${from.x + from.width} ${startY} C ${rail} ${startY}, ${rail} ${endY}, ${to.x + to.width} ${endY}`,
      description: `${dependency.from} waits for ${dependency.to}`,
    });
  });
  return {
    width: showVolumes && volumes.length ? 752 : 584,
    height: contentHeight + 72,
    nodes,
    edges,
  };
}

/** Gives different resource kinds distinct identities even when their logical names match. @internal */
function resourceId(kind: MapNodeKind, name: string): string {
  return `${kind}:${name}`;
}
/** Orders names without locale or source-array dependencies. @internal */
function compare(left: string, right: string): number {
  return left < right ? -1 : left > right ? 1 : 0;
}
/** Orders named declarations without mutating their inputs. @internal */
function byName(left: { name: string }, right: { name: string }): number {
  return compare(left.name, right.name);
}
/** Connects resource lanes with a shared horizontal bend. @internal */
function curve(fromX: number, fromY: number, toX: number, toY: number): string {
  const bend = (fromX + toX) / 2;
  return `M ${fromX} ${fromY} C ${bend} ${fromY}, ${bend} ${toY}, ${toX} ${toY}`;
}
