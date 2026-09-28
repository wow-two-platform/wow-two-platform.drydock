/** Whether a site answered through the ingress, by its host name, right after its rollout. */
export interface SiteProbe {
  ok: boolean;
  /** The HTTP status the site answered with, when it answered. */
  status?: number;
  /** Why the site did not answer. */
  detail?: string;
}

/** One site a verified release answers on through the ingress. */
export interface PublishedSite {
  /** The site's name in the product's deploy.yml, such as `app`. */
  name: string;
  service: string;
  /** `private` sites answer only on the private network. */
  exposure: 'public' | 'private';
  url: string;
  /** Absent when the target's ingress has no probe address for this site's exposure. */
  probe?: SiteProbe;
}
