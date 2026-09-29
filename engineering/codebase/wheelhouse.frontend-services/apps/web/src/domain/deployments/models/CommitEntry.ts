/** One commit of a product branch, with its build when one exists. */
export interface CommitEntry {
  sha: string;
  /** The subject line only. */
  message: string;
  author: string;
  date?: string | null;
  /** The catalog ID of the commit's build; null until a build exists. */
  buildId: string | null;
  /** False when the product has no build workflow, so no build can start; absent from older runners. */
  canBuild?: boolean | undefined;
  /** The commit's permalink on GitHub. */
  url?: string | undefined;
}
