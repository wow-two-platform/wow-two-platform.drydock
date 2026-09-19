# DryDock — Flows

*Last updated: 2026-09-19*

- **Publish a candidate:** commit intended source to `main` → validate in GitHub → tag the chosen commit →
  CI publishes and smoke-tests both images → attach the digest bundle to a draft → publish the completed release.
- **Deploy:** refresh published artifacts → choose release and configured environment → submit →
  target lock and pull → health-gated replacement → durable outcome.
- **Recover:** inspect the target journal → restore compatible prior images, or explicitly recover data →
  verify health. The operator runner works without the dashboard. Database restoration is never automatic.
- **Integrate a VPS:** verify the provider/host → add its provider enum/integration if needed →
  add host and environment bindings in code → mount credentials → test and rebuild DryDock.
  No server-registration action appears in the UI.
- **Move a workload:** prepare another code-owned target → copy/restore data and keys → verify →
  perform an explicit cutover. Selecting another host does not transfer persistent data.

Future domain purchasing, DNS changes, backup automation and teardown need their own reviewed flows.
Execution details: [deployment operations](../../engineering/deployment/deployment.md).
