/** A service's version: the release in which it last changed. An older version means no change since then. */
export interface ServiceVersion {
  version: string;
  changedIn: string;
}
