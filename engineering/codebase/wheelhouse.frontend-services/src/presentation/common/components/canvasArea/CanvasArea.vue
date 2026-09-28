<script lang="ts">
// Copies the unreleased SDK `CanvasArea` (`@wow-two-beta/ui-vue/presentation/layout`) until the next
// published version; delete this folder and import the SDK's after the re-pin.

/** Where the plane sits: content point `p` renders at `p * zoom + (x, y)`, in the area's own pixels. */
export interface CanvasViewport {
  /** The offset of the content's left edge from the area's left edge. */
  readonly x: number;
  /** The offset of the content's top edge from the area's top edge. */
  readonly y: number;
  /** The scale factor; `1` is actual size. */
  readonly zoom: number;
}

/** How far the plane may move. */
export const CanvasBounds = {
  /** Scroll-like: content never leaves the area, and an axis shorter than the area centers. */
  Content: "content",
  /** An open plane that pans anywhere, as a node editor does. */
  None: "none",
} as const;

export type CanvasBounds = (typeof CanvasBounds)[keyof typeof CanvasBounds];

/** What an unmodified wheel does. A pinch, or a wheel with Ctrl or ⌘ held, always zooms. */
export const CanvasWheel = {
  /** Moves the plane as a scroll region would; the page scrolls on once the content's edge is reached. */
  Pan: "pan",
  /** Zooms about the pointer, for a full-screen editor with nothing to scroll around it. */
  Zoom: "zoom",
} as const;

export type CanvasWheel = (typeof CanvasWheel)[keyof typeof CanvasWheel];

/** The state and actions the `controls` slot receives. */
export interface CanvasControls {
  /** The rendered zoom factor. */
  readonly zoom: number;
  /** The rendered zoom as a whole percentage. */
  readonly percent: number;
  /** Whether a zoom-in step has room below `maxZoom`. */
  readonly canZoomIn: boolean;
  /** Whether a zoom-out step has room above `minZoom`. */
  readonly canZoomOut: boolean;
  /** Zooms in one step about the area's center. */
  readonly zoomIn: () => void;
  /** Zooms out one step about the area's center. */
  readonly zoomOut: () => void;
  /** Returns to 100%, keeping the area's center in place. */
  readonly reset: () => void;
  /** Shows all content, centered, never above 100%. */
  readonly fit: () => void;
}

export interface CanvasAreaProps {
  /** The controlled viewport. Pair it with `update:viewport`; omit it and the area keeps its own. */
  readonly viewport?: CanvasViewport;
  /** Where an uncontrolled area opens: a zoom factor, or `'fit'` to show all content once measured. Default `1`. */
  readonly initialZoom?: number | "fit";
  /** The lower zoom bound. Default `0.25`. */
  readonly minZoom?: number;
  /** The upper zoom bound. Default `4`. */
  readonly maxZoom?: number;
  /** The factor one button or key step multiplies the zoom by. Default `1.25`. */
  readonly zoomStep?: number;
  /** How far the plane may move. Default `content`. */
  readonly bounds?: CanvasBounds;
  /** What an unmodified wheel does. Default `pan`. */
  readonly wheel?: CanvasWheel;
  /** The gap `fit` keeps around the content, in pixels. Default `16`. */
  readonly fitPadding?: number;
  /** Whether the built-in zoom controls render in the bottom-end corner. Default `true`. */
  readonly hasControls?: boolean;
  /** Localized labels for the built-in controls and the area's role description. */
  readonly labels?: Partial<
    Record<
      "canvas" | "controls" | "zoomIn" | "zoomOut" | "reset" | "fit",
      string
    >
  >;
}

/** One pointer's drag of the plane; it moves nothing until the pointer passes the drag threshold. */
interface PanGesture {
  readonly kind: "pan";
  readonly pointerId: number;
  readonly startX: number;
  readonly startY: number;
  readonly origin: CanvasViewport;
  readonly isDragging: boolean;
}

/** Two pointers scaling about their midpoint, anchored where the pinch began so it stays reversible. */
interface PinchGesture {
  readonly kind: "pinch";
  readonly ids: readonly [number, number];
  readonly startDistance: number;
  readonly startCenter: { readonly x: number; readonly y: number };
  readonly origin: CanvasViewport;
}
</script>

<script setup lang="ts">
import {
  computed,
  onBeforeUnmount,
  onMounted,
  shallowRef,
  useAttrs,
  useTemplateRef,
} from "vue";
import { Maximize, Minus, Plus } from "lucide-vue-next";
import { cn } from "@wow-two-beta/ui-vue/foundation/styles";
import { Icon } from "@wow-two-beta/ui-vue/foundation/icons";
import {
  clampToContent,
  clampZoom,
  fitViewport,
  sameViewport,
  wheelPixels,
  wheelZoomFactor,
  zoomAt,
  type CanvasPoint,
  type CanvasSize,
} from "./CanvasMath";

/**
 * Renders caller content on a plane that pans and zooms inside a fixed box. Drag, wheel, pinch, keys and the
 * built-in controls all move one viewport; a click inside still reaches the content unless the pointer dragged.
 */
defineOptions({ name: "CanvasArea", inheritAttrs: false });

const props = withDefaults(defineProps<CanvasAreaProps>(), {
  initialZoom: 1,
  minZoom: 0.25,
  maxZoom: 4,
  zoomStep: 1.25,
  bounds: CanvasBounds.Content,
  wheel: CanvasWheel.Pan,
  fitPadding: 16,
  hasControls: true,
});

const emit = defineEmits<{
  /** Requests the next viewport after a drag, wheel, pinch, key or control. */
  "update:viewport": [viewport: CanvasViewport];
}>();

defineSlots<{
  /** The content placed on the plane, laid out at actual size. */
  default?(): unknown;
  /** Replaces the built-in zoom buttons inside the corner group. */
  controls?(props: CanvasControls): unknown;
}>();

/** Pixels a pointer travels before a press becomes a drag: a mouse is precise, a finger is not. */
const DragThreshold = { mouse: 3, other: 8 } as const;

/** Pixels one arrow key moves the plane; Shift multiplies it. */
const PanStep = 40;

const labels = computed(() => ({
  canvas: "canvas",
  controls: "Zoom",
  zoomIn: "Zoom in",
  zoomOut: "Zoom out",
  reset: "reset zoom",
  fit: "Fit to view",
  ...props.labels,
}));
const attrs = useAttrs();
const el = useTemplateRef<HTMLDivElement>("el");
const content = useTemplateRef<HTMLDivElement>("content");

/** Fixed at setup, as `useControlled` fixes it: a controlled area never keeps a copy of the parent's viewport. */
const isControlled = props.viewport !== undefined;
const internal = shallowRef<CanvasViewport>({
  x: 0,
  y: 0,
  zoom:
    typeof props.initialZoom === "number"
      ? clampZoom(props.initialZoom, props.minZoom, props.maxZoom)
      : 1,
});
const area = shallowRef<CanvasSize>({ width: 0, height: 0 });
const plane = shallowRef<CanvasSize>({ width: 0, height: 0 });
const isPanning = shallowRef(false);

/** The viewport as rendered: zoom inside its bounds, and the content inside the area when bounded. */
const view = computed<CanvasViewport>(() =>
  settle(isControlled ? (props.viewport ?? internal.value) : internal.value),
);
const canZoomIn = computed(() => view.value.zoom < props.maxZoom - 1e-6);
const canZoomOut = computed(() => view.value.zoom > props.minZoom + 1e-6);
const percent = computed(() => Math.round(view.value.zoom * 100));

/** Clamps a requested viewport the same way the render does. @internal */
function settle(next: CanvasViewport): CanvasViewport {
  const zoomed = {
    ...next,
    zoom: clampZoom(next.zoom, props.minZoom, props.maxZoom),
  };
  return props.bounds === CanvasBounds.Content
    ? clampToContent(zoomed, area.value, plane.value)
    : zoomed;
}

/** Applies a movement: stores it when uncontrolled and reports it either way. Returns whether the view moved. */
function commit(next: CanvasViewport): boolean {
  const settled = settle(next);
  if (sameViewport(settled, view.value)) return false;
  if (!isControlled) internal.value = settled;
  emit("update:viewport", settled);
  return true;
}

/** The area's center, in its own pixels. @internal */
function center(): CanvasPoint {
  return { x: area.value.width / 2, y: area.value.height / 2 };
}

/** Converts a viewport-relative pointer position into the area's own pixels. @internal */
function local(point: CanvasPoint): CanvasPoint {
  const rect = el.value?.getBoundingClientRect();
  return rect ? { x: point.x - rect.left, y: point.y - rect.top } : point;
}

/** Scales the zoom by `factor` about `anchor`. Returns whether the view moved. @internal */
function zoomBy(factor: number, anchor: CanvasPoint = center()): boolean {
  const current = view.value;
  return commit(
    zoomAt(
      current,
      clampZoom(current.zoom * factor, props.minZoom, props.maxZoom),
      anchor,
    ),
  );
}

/** Zooms in one step about the area's center. */
function zoomIn(): void {
  zoomBy(props.zoomStep);
}

/** Zooms out one step about the area's center. */
function zoomOut(): void {
  zoomBy(1 / props.zoomStep);
}

/** Zooms to `zoom`, keeping `anchor` (default: the area's center) over the same content point. */
function zoomTo(zoom: number, anchor: CanvasPoint = center()): void {
  commit(
    zoomAt(view.value, clampZoom(zoom, props.minZoom, props.maxZoom), anchor),
  );
}

/** Returns to 100%, keeping the area's center in place. */
function reset(): void {
  zoomTo(1);
}

/** Shows all content, centered, never above 100%. */
function fit(): void {
  commit(
    fitViewport(
      area.value,
      plane.value,
      props.fitPadding,
      props.minZoom,
      Math.min(1, props.maxZoom),
    ),
  );
}

/** Moves the plane by a pixel offset. Returns whether it moved. */
function panBy(dx: number, dy: number): boolean {
  const current = view.value;
  return commit({ ...current, x: current.x + dx, y: current.y + dy });
}

const controls = computed<CanvasControls>(() => ({
  zoom: view.value.zoom,
  percent: percent.value,
  canZoomIn: canZoomIn.value,
  canZoomOut: canZoomOut.value,
  zoomIn,
  zoomOut,
  reset,
  fit,
}));

/** Whether an event started inside a field that owns its own keys and pointer. @internal */
function isEditable(target: EventTarget | null): boolean {
  return (
    target instanceof HTMLElement &&
    (target.isContentEditable ||
      /^(?:INPUT|TEXTAREA|SELECT)$/.test(target.tagName))
  );
}

let observer: ResizeObserver | null = null;
let isPlaced = false;

/** Reads the area and the content's untransformed size; places an uncontrolled `'fit'` area once both exist. */
function measure(): void {
  const root = el.value;
  const inner = content.value;
  if (!root || !inner) return;
  const nextArea = { width: root.clientWidth, height: root.clientHeight };
  const nextPlane = { width: inner.offsetWidth, height: inner.offsetHeight };
  if (
    nextArea.width !== area.value.width ||
    nextArea.height !== area.value.height
  )
    area.value = nextArea;
  if (
    nextPlane.width !== plane.value.width ||
    nextPlane.height !== plane.value.height
  )
    plane.value = nextPlane;
  if (isPlaced || nextArea.width <= 0 || nextPlane.width <= 0) return;
  isPlaced = true;
  // The opening placement seeds uncontrolled state; it is not the reader's intent, so it emits nothing.
  if (!isControlled && props.initialZoom === "fit")
    internal.value = fitViewport(
      nextArea,
      nextPlane,
      props.fitPadding,
      props.minZoom,
      Math.min(1, props.maxZoom),
    );
}

// Wheel handling must be a non-passive native listener — a template `@wheel` binding is attached passively, so
// `preventDefault` inside it could not stop the page from scrolling or zooming.
function onWheel(event: WheelEvent): void {
  const root = el.value;
  if (!root) return;
  const delta = wheelPixels(event, root.clientHeight);
  if (event.ctrlKey || event.metaKey || props.wheel === CanvasWheel.Zoom) {
    // Always claimed, or the browser zooms the whole page on a trackpad pinch.
    event.preventDefault();
    zoomBy(
      wheelZoomFactor(delta.y),
      local({ x: event.clientX, y: event.clientY }),
    );
    return;
  }
  // Claimed only when the plane moved, so the page scrolls on past the content's edge.
  if (panBy(-delta.x, -delta.y)) event.preventDefault();
}

function onKeydown(event: KeyboardEvent): void {
  if (
    event.defaultPrevented ||
    event.altKey ||
    event.ctrlKey ||
    event.metaKey ||
    isEditable(event.target)
  )
    return;
  const step = event.shiftKey ? PanStep * 4 : PanStep;
  switch (event.key) {
    case "+":
    case "=":
      zoomIn();
      break;
    case "-":
    case "_":
      zoomOut();
      break;
    case "0":
      reset();
      break;
    case "ArrowLeft":
      if (!panBy(step, 0)) return;
      break;
    case "ArrowRight":
      if (!panBy(-step, 0)) return;
      break;
    case "ArrowUp":
      if (!panBy(0, step)) return;
      break;
    case "ArrowDown":
      if (!panBy(0, -step)) return;
      break;
    default:
      return;
  }
  event.preventDefault();
}

/** Live position of every pointer down on the area, in viewport coordinates, keyed by `pointerId`. */
const pointers = new Map<number, CanvasPoint>();
let gesture: PanGesture | PinchGesture | null = null;

/** Set once a press becomes a drag, so the click that ends it does not activate the content under it. */
let suppressClick = false;

function capture(pointerId: number): void {
  try {
    el.value?.setPointerCapture(pointerId);
  } catch {
    // The pointer already went away; the area still hears its remaining events.
  }
}

function release(pointerId: number): void {
  try {
    if (el.value?.hasPointerCapture(pointerId))
      el.value.releasePointerCapture(pointerId);
  } catch {
    // Capture was never held, or the pointer id is stale.
  }
}

function midpoint(a: CanvasPoint, b: CanvasPoint): CanvasPoint {
  return { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
}

function beginPinch(): void {
  const [first, second] = [...pointers.entries()];
  if (!first || !second) return;
  gesture = {
    kind: "pinch",
    ids: [first[0], second[0]],
    startDistance: Math.max(
      1,
      Math.hypot(first[1].x - second[1].x, first[1].y - second[1].y),
    ),
    startCenter: local(midpoint(first[1], second[1])),
    origin: view.value,
  };
  capture(first[0]);
  capture(second[0]);
  suppressClick = true;
  isPanning.value = true;
}

function onPointerDown(event: PointerEvent): void {
  if (event.button !== 0 || isEditable(event.target)) return;
  pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
  if (pointers.size === 1) {
    suppressClick = false;
    gesture = {
      kind: "pan",
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
      origin: view.value,
      isDragging: false,
    };
  } else if (pointers.size === 2) beginPinch();
}

function onPointerMove(event: PointerEvent): void {
  if (!pointers.has(event.pointerId)) return;
  pointers.set(event.pointerId, { x: event.clientX, y: event.clientY });
  const current = gesture;
  if (!current) return;
  if (current.kind === "pan") {
    if (event.pointerId !== current.pointerId) return;
    const dx = event.clientX - current.startX;
    const dy = event.clientY - current.startY;
    if (!current.isDragging) {
      if (
        Math.hypot(dx, dy) <
        (event.pointerType === "mouse"
          ? DragThreshold.mouse
          : DragThreshold.other)
      )
        return;
      gesture = { ...current, isDragging: true };
      capture(event.pointerId);
      suppressClick = true;
      isPanning.value = true;
    }
    commit({
      ...current.origin,
      x: current.origin.x + dx,
      y: current.origin.y + dy,
    });
    return;
  }
  const a = pointers.get(current.ids[0]);
  const b = pointers.get(current.ids[1]);
  if (!a || !b) return;
  // Measured from the pinch's own start, never frame to frame, so fingers returning home restore the view.
  const zoom = clampZoom(
    current.origin.zoom *
      (Math.hypot(a.x - b.x, a.y - b.y) / current.startDistance),
    props.minZoom,
    props.maxZoom,
  );
  const scaled = zoomAt(current.origin, zoom, current.startCenter);
  const moved = local(midpoint(a, b));
  commit({
    zoom,
    x: scaled.x + moved.x - current.startCenter.x,
    y: scaled.y + moved.y - current.startCenter.y,
  });
}

function onPointerEnd(event: PointerEvent): void {
  if (!pointers.delete(event.pointerId)) return;
  release(event.pointerId);
  const remaining = [...pointers.entries()][0];
  if (gesture?.kind === "pinch" && pointers.size === 1 && remaining) {
    // The finger left behind carries on as a drag from where the pinch ended.
    gesture = {
      kind: "pan",
      pointerId: remaining[0],
      startX: remaining[1].x,
      startY: remaining[1].y,
      origin: view.value,
      isDragging: true,
    };
    return;
  }
  if (pointers.size > 0) return;
  gesture = null;
  isPanning.value = false;
  // The click a drag produces arrives in this same task; a drag with no click must not swallow a later one.
  if (suppressClick) setTimeout(() => (suppressClick = false), 0);
}

function onClickCapture(event: MouseEvent): void {
  if (!suppressClick) return;
  suppressClick = false;
  event.preventDefault();
  event.stopPropagation();
}

onMounted(() => {
  el.value?.addEventListener("wheel", onWheel, { passive: false });
  measure();
  if (typeof ResizeObserver === "undefined") return;
  observer = new ResizeObserver(measure);
  if (el.value) observer.observe(el.value);
  if (content.value) observer.observe(content.value);
});

onBeforeUnmount(() => {
  el.value?.removeEventListener("wheel", onWheel);
  observer?.disconnect();
  observer = null;
  for (const pointerId of pointers.keys()) release(pointerId);
  pointers.clear();
  gesture = null;
});

const classes = computed(() =>
  cn(
    "relative h-96 w-full touch-none select-none overflow-hidden rounded-md border border-border bg-muted/30",
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
    isPanning.value ? "cursor-grabbing" : "cursor-grab",
    attrs.class as string | undefined,
  ),
);

/** Everything but `class`, which is re-applied through `cn` above. */
const rest = computed(() => {
  const { class: _class, ...others } = attrs;
  return others;
});

/** Vue does not append `px` to a numeric `:style` value — every length is spelled out. */
const planeStyle = computed(() => ({
  transform: `translate(${view.value.x}px, ${view.value.y}px) scale(${view.value.zoom})`,
}));

const ControlClass =
  "inline-flex h-7 min-w-7 items-center justify-center rounded-md px-1.5 text-xs text-muted-foreground " +
  "hover:bg-muted hover:text-foreground disabled:pointer-events-none disabled:opacity-40 " +
  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring";
const ResetClass = cn(ControlClass, "min-w-12 tabular-nums");

defineExpose({ el, zoomIn, zoomOut, zoomTo, reset, fit, panBy });
</script>

<template>
  <div
    ref="el"
    tabindex="0"
    role="group"
    :aria-roledescription="labels.canvas"
    v-bind="rest"
    :class="classes"
    @pointerdown="onPointerDown"
    @pointermove="onPointerMove"
    @pointerup="onPointerEnd"
    @pointercancel="onPointerEnd"
    @click.capture="onClickCapture"
    @dragstart.prevent
    @keydown="onKeydown"
  >
    <div
      ref="content"
      data-canvas-plane
      class="absolute left-0 top-0 w-max origin-top-left"
      :style="planeStyle"
    >
      <slot />
    </div>
    <div
      v-if="hasControls"
      role="group"
      :aria-label="labels.controls"
      class="absolute bottom-3 end-3 flex cursor-auto items-center gap-0.5 rounded-lg border border-border bg-card p-0.5 shadow-sm"
      @pointerdown.stop
    >
      <slot name="controls" v-bind="controls">
        <button
          type="button"
          :class="ControlClass"
          :aria-label="labels.zoomOut"
          :disabled="!canZoomOut"
          @click="zoomOut"
        >
          <Icon :icon="Minus" :size="14" />
        </button>
        <button
          type="button"
          :class="ResetClass"
          :aria-label="`${percent}%, ${labels.reset}`"
          @click="reset"
        >
          {{ percent }}%
        </button>
        <button
          type="button"
          :class="ControlClass"
          :aria-label="labels.zoomIn"
          :disabled="!canZoomIn"
          @click="zoomIn"
        >
          <Icon :icon="Plus" :size="14" />
        </button>
        <button
          type="button"
          :class="ControlClass"
          :aria-label="labels.fit"
          @click="fit"
        >
          <Icon :icon="Maximize" :size="13" />
        </button>
      </slot>
    </div>
  </div>
</template>
