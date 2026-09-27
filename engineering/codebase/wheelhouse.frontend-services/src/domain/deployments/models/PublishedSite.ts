/** One site a verified release answers on through the ingress. */
export interface PublishedSite {
  /** The site's name in the product's deploy.yml, such as `app`. */
  name: string;
  service: string;
  /** `private` sites answer only on the private network. */
  exposure: 'public' | 'private';
  url: string;
}
