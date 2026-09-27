/** An accepted request to build one commit; the build joins the catalog when its workflow finishes. */
export interface BuildRequest {
  product: string;
  commit: string;
  status: 'requested';
}
