import type { CanvasViewport } from "./CanvasArea.vue";

/** A measured box, in CSS pixels. */
export interface CanvasSize {
  readonly width: number;
  readonly height: number;
}

/** A point in the area's own pixels, measured from its top-left corner. */
export interface CanvasPoint {
  readonly x: number;
  readonly y: number;
}

/** Wheel pixels that change the zoom by a factor of `e`: small enough for trackpad pinches, one notch ≈ 20%. */
const WheelZoomRate = 0.002;

/** The largest wheel delta one event may apply, so a line- or page-mode notch never jumps. */
const WheelDeltaLimit = 100;

/** Keeps a zoom level inside its bounds. */
export function clampZoom(zoom: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, zoom));
}

/** Zooms to `zoom` while the area point `anchor` keeps showing the same content point. */
export function zoomAt(
  viewport: CanvasViewport,
  zoom: number,
  anchor: CanvasPoint,
): CanvasViewport {
  const ratio = zoom / viewport.zoom;
  return {
    zoom,
    x: anchor.x - (anchor.x - viewport.x) * ratio,
    y: anchor.y - (anchor.y - viewport.y) * ratio,
  };
}

/** Keeps content inside the area: an axis longer than the area scrolls between its edges, a shorter one centers. */
export function clampToContent(
  viewport: CanvasViewport,
  area: CanvasSize,
  content: CanvasSize,
): CanvasViewport {
  return {
    zoom: viewport.zoom,
    x: clampAxis(viewport.x, area.width, content.width * viewport.zoom),
    y: clampAxis(viewport.y, area.height, content.height * viewport.zoom),
  };
}

/** The centered viewport that shows all content inside `padding`, zoomed no further than `maxZoom`. */
export function fitViewport(
  area: CanvasSize,
  content: CanvasSize,
  padding: number,
  minZoom: number,
  maxZoom: number,
): CanvasViewport {
  if (
    area.width <= 0 ||
    area.height <= 0 ||
    content.width <= 0 ||
    content.height <= 0
  )
    return { x: 0, y: 0, zoom: 1 };
  const zoom = clampZoom(
    Math.min(
      (area.width - padding * 2) / content.width,
      (area.height - padding * 2) / content.height,
    ),
    minZoom,
    maxZoom,
  );
  return {
    zoom,
    x: (area.width - content.width * zoom) / 2,
    y: (area.height - content.height * zoom) / 2,
  };
}

/** A wheel event's movement in pixels, whatever unit the device reported; Shift turns a vertical wheel sideways. */
export function wheelPixels(
  event: WheelEvent,
  pageHeight: number,
): CanvasPoint {
  const unit =
    event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? pageHeight : 1;
  const x = event.deltaX * unit;
  const y = event.deltaY * unit;
  return event.shiftKey && x === 0 ? { x: y, y: 0 } : { x, y };
}

/** The zoom factor one wheel movement asks for: up zooms in, down zooms out. */
export function wheelZoomFactor(deltaY: number): number {
  return Math.exp(
    -Math.max(-WheelDeltaLimit, Math.min(WheelDeltaLimit, deltaY)) *
      WheelZoomRate,
  );
}

/** Whether two viewports render identically. */
export function sameViewport(a: CanvasViewport, b: CanvasViewport): boolean {
  return a.x === b.x && a.y === b.y && a.zoom === b.zoom;
}

/** One axis of `clampToContent`. @internal */
function clampAxis(offset: number, area: number, extent: number): number {
  if (extent <= area) return (area - extent) / 2;
  return Math.min(0, Math.max(area - extent, offset));
}
