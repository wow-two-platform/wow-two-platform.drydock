/** A published release bundle available for deployment. */
export interface ReleaseArtifact {
  id: string;
  product: string;
  release: string;
  prerelease: boolean;
  publishedAt: string;
  provider: string;
}
