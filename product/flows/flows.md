# Wheelhouse — Flows

*Last updated: 2026-09-25*

- **Publish a candidate:** commit intended source to `main` → validate in GitHub → tag the chosen commit →
  CI publishes and smoke-tests both images → attach the digest bundle to a draft → publish the completed release.
- **Deploy:** refresh published artifacts → choose release and configured environment → submit →
  target lock and pull → health-gated replacement → durable outcome.
- **Recover:** inspect the target → reconcile the locked rollout in the dashboard → redeploy a verified release,
  or explicitly recover data → verify health. The operator runner works without the dashboard. Database restoration is never automatic.
- **Manage secrets:** choose a vault → namespace per product environment → add or rotate values (write-only) →
  mint a product token, copy it once into the product's mounted settings → revoke tokens when rotating access.
- **Integrate a VPS:** verify the provider/host → add its provider enum/integration if needed →
  add host and environment bindings in code → mount credentials → test and rebuild Wheelhouse.
  No server-registration action appears in the UI.
- **Move a workload:** prepare another code-owned target → copy/restore data and keys → verify →
  perform an explicit cutover. Selecting another host does not transfer persistent data.

Future domain purchasing, DNS changes, backup automation and teardown need their own reviewed flows.
Execution details: [deployment operations](../../engineering/deployment/deployment.md).
