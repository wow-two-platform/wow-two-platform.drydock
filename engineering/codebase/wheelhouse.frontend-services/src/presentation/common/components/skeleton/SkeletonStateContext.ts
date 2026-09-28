// Copies the unreleased SDK skeleton parts (`@wow-two-beta/ui-vue/presentation/feedback`) until the next published
// version; delete this folder and import the SDK's `SkeletonStateGroup` / `SkeletonStateSlot` after the re-pin.
import { inject, type ComputedRef, type InjectionKey } from "vue";

/** What a `SkeletonStateGroup` shares with the placeholders inside it. */
export interface SkeletonStateGroupContext {
  /** Whether the region is loading; every `SkeletonStateSlot` without its own flag follows it. */
  readonly isLoading: ComputedRef<boolean>;
}

export const skeletonStateGroupKey: InjectionKey<SkeletonStateGroupContext> =
  Symbol("wheelhouse.skeletonStateGroup");

/** Reads the nearest `SkeletonStateGroup`, or `null` outside one. */
export function useSkeletonStateGroup(): SkeletonStateGroupContext | null {
  return inject(skeletonStateGroupKey, null);
}
