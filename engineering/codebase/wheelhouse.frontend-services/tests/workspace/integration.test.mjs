import assert from "node:assert/strict";
import { after, afterEach, test } from "node:test";
import { resolve } from "node:path";
import { build, stop } from "esbuild";

// Bundle the real integration modules in memory; no app server, browser, or live API is needed.
const bundle = await build({
  stdin: {
    contents: `
      export { productsApi } from './src/integration/products';
      export { deploymentsApi } from './src/integration/deployments';
      export { authApi } from './src/integration/auth';
      export { secretsApi } from './src/integration/secrets';
      export { fleetApi } from './src/integration/fleet';
      export { clearHttpSession } from './src/integration/common';
      export { useFleetVitals } from './src/application/fleet/useFleetVitals';
      export { queryClient, queryPlugin } from './src/bootstrap/query';
      export { createRenderer, nextTick } from 'vue';
    `,
    resolveDir: process.cwd(),
  },
  alias: { "@": resolve("src") },
  bundle: true,
  platform: "node",
  format: "esm",
  write: false,
});
const {
  productsApi,
  deploymentsApi,
  authApi,
  secretsApi,
  fleetApi,
  clearHttpSession,
  useFleetVitals,
  queryClient,
  queryPlugin,
  createRenderer,
  nextTick,
} = await import(
  `data:text/javascript;base64,${Buffer.from(bundle.outputFiles[0].text).toString("base64")}`
);
const originalFetch = globalThis.fetch;
afterEach(() => {
  globalThis.fetch = originalFetch;
});
after(() => stop());

function json(data, status = 200, headers = {}) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "Content-Type": "application/json", ...headers },
  });
}

const product = {
  id: "product-id",
  slug: "test",
  name: "Test",
  repo: "owner/repository",
  status: "Draft",
  createdAtUtc: "2026-09-26T00:00:00Z",
};

test("serializes product writes once and preserves same-origin cookie credentials", async () => {
  let captured;
  globalThis.fetch = async (url, options) => {
    captured = { url, options };
    return json({ data: product });
  };
  const payload = {
    slug: product.slug,
    name: product.name,
    repo: product.repo,
  };
  const result = await productsApi.createProduct(payload);
  assert.equal(result.ok, true);
  assert.deepEqual(result.value, product);
  assert.equal(captured.url, "/api/products");
  assert.equal(captured.options.method, "POST");
  assert.equal(captured.options.credentials, "same-origin");
  assert.deepEqual(JSON.parse(captured.options.body), payload);
  assert.equal(
    captured.options.headers.get("Content-Type"),
    "application/json",
  );
});

test("preserves explicit deployment, reconciliation, and vault action headers", async () => {
  const requests = [];
  globalThis.fetch = async (url, options) => {
    requests.push({ url, options });
    return json({
      data: url.startsWith("/api/vaults")
        ? null
        : { id: "job", status: "queued" },
    });
  };
  assert.equal(
    (await deploymentsApi.startDeployment("target", "release")).ok,
    true,
  );
  assert.equal((await deploymentsApi.reconcile("target", "job")).ok, true);
  assert.equal(
    (await secretsApi.createNamespace("vault", "space", "Space")).ok,
    true,
  );
  assert.deepEqual(
    requests.map(({ options }) => options.headers.get("X-Wheelhouse-Action")),
    ["deploy", "reconcile", "vault"],
  );
  assert.deepEqual(JSON.parse(requests[0].options.body), {
    target: "target",
    release: "release",
  });
  assert.deepEqual(JSON.parse(requests[1].options.body), { job: "job" });
});

test("sends a typed prod confirmation and an explicit build action", async () => {
  const requests = [];
  globalThis.fetch = async (url, options) => {
    requests.push({ url, options });
    return json({
      data: url.includes("/builds")
        ? { product: "foreverpin", commit: "e".repeat(40), status: "requested" }
        : { id: "job", status: "queued" },
    });
  };
  assert.equal(
    (
      await deploymentsApi.startDeployment(
        "foreverpin-prod",
        "release",
        "foreverpin-prod",
      )
    ).ok,
    true,
  );
  const build = await deploymentsApi.requestBuild("foreverpin", "e".repeat(40));
  assert.equal(build.ok, true);
  assert.deepEqual(JSON.parse(requests[0].options.body), {
    target: "foreverpin-prod",
    release: "release",
    confirm: "foreverpin-prod",
  });
  assert.equal(
    requests[1].url,
    "/api/deployments/products/foreverpin/builds",
  );
  assert.equal(requests[1].options.headers.get("X-Wheelhouse-Action"), "build");
  assert.deepEqual(JSON.parse(requests[1].options.body), {
    commit: "e".repeat(40),
  });
});

test("decodes environment rules, commit builds, and only http site links", async () => {
  globalThis.fetch = async (url) => {
    if (url.endsWith("/targets"))
      return json({
        data: [
          { id: "foreverpin-dev", product: "foreverpin", environment: "dev",
            serverId: "local", provider: "Local", host: "127.0.0.1",
            acceptsCandidates: true, needsConfirmation: false },
          { id: "legacy", product: "foreverpin", environment: "test",
            serverId: "local", provider: "Local", host: "127.0.0.1" },
        ],
      });
    if (url.endsWith("/releases"))
      return json({
        data: [
          { id: "foreverpin-ci-7", product: "foreverpin", release: "sha-ccccccc",
            kind: "candidate", commit: "c".repeat(40), branch: "main",
            prerelease: true, publishedAt: "2026-09-27T10:00:00Z", provider: "GitHubActions" },
          { id: "foreverpin-gh-1", product: "foreverpin", release: "v0.9.0",
            prerelease: false, publishedAt: "2026-09-19T00:00:00Z", provider: "GitHubReleases" },
        ],
      });
    if (url.includes("/commits?branch=feature%2Fpins"))
      return json({ data: [{ sha: "c".repeat(40), message: "feat: pins", author: "Max", buildId: null }] });
    const site = url.includes("unsafe") ? "javascript:alert(1)" : "http://app-foreverpin.dev.localhost:18080";
    return json({
      data: { targetId: "foreverpin-dev", project: "foreverpin-dev", condition: "ready", active: null,
        current: { id: "job", release: "sha-ccccccc", kind: "candidate",
          versions: { management: { version: "1.2.0+ccccccc", changedIn: "sha-ccccccc" } },
          sites: [{ name: "app", service: "management", exposure: "public", url: site }] } },
    });
  };
  const targets = await deploymentsApi.listTargets();
  assert.deepEqual(
    targets.value.map((item) => [item.acceptsCandidates, item.needsConfirmation]),
    [[true, false], [false, false]],
  );
  const releases = await deploymentsApi.listReleases();
  assert.deepEqual(releases.value.map((item) => item.kind), ["candidate", "release"]);
  const commits = await deploymentsApi.listCommits("foreverpin", "feature/pins");
  assert.equal(commits.value[0].buildId, null);
  const state = await deploymentsApi.getTargetState("foreverpin-dev");
  assert.equal(state.value.current.sites[0].url, "http://app-foreverpin.dev.localhost:18080");
  assert.equal(state.value.current.versions.management.version, "1.2.0+ccccccc");
  const unsafe = await deploymentsApi.getTargetState("unsafe");
  assert.equal(unsafe.ok, false);
  assert.equal(unsafe.failure.code, "protocol");
});

test("accepts explicit empty logout and deletion successes", async () => {
  globalThis.fetch = async () => new Response(null, { status: 204 });
  assert.deepEqual(await authApi.signOut(), { ok: true, value: undefined });
  assert.deepEqual(await productsApi.deleteProduct("product-id"), {
    ok: true,
    value: undefined,
  });
});

test("rejects a missing management envelope and malformed consumed product fields", async () => {
  for (const payload of [
    [product],
    { data: [{ ...product, status: "invented" }] },
    { data: [{ id: "partial" }] },
  ]) {
    globalThis.fetch = async () => json(payload);
    const result = await productsApi.listProducts();
    assert.equal(result.ok, false);
    assert.equal(result.failure.code, "protocol");
  }
});

test("preserves HTTP status, headers and field diagnostics without displaying server details", async () => {
  const problem = {
    detail: "internal server diagnostics",
    errors: { slug: ["Duplicate slug"] },
  };
  globalThis.fetch = async () => json(problem, 409, { "Retry-After": "10" });
  const result = await productsApi.listProducts();
  assert.equal(result.ok, false);
  assert.equal(result.failure.status, 409);
  assert.equal(result.failure.headers["retry-after"], "10");
  assert.deepEqual(result.failure.problem.errors, problem.errors);
  assert.notEqual(result.failure.message, problem.detail);
});

test("fills embedded readiness state target from the runner transport outer identifier", async () => {
  globalThis.fetch = async () =>
    json({
      data: {
        targetId: "target",
        ok: true,
        checks: [{ name: "State", ok: true, detail: "ready" }],
        state: {
          project: "project",
          condition: "ready",
          current: null,
          active: null,
        },
      },
    });
  const result = await deploymentsApi.checkTarget("target");
  assert.equal(result.ok, true);
  assert.equal(result.value.state.targetId, "target");
});

test("accepts unavailable targets and nullable host metrics without inventing healthy data", async () => {
  const snapshot = {
    collectedAt: "2026-09-26T00:00:00Z",
    targets: [
      {
        targetId: "unreachable",
        serverId: "server",
        ok: false,
        reason: "SSH unavailable",
      },
      {
        targetId: "online",
        serverId: "server",
        ok: true,
        host: {
          cpus: null,
          load: null,
          memoryTotalBytes: null,
          memoryAvailableBytes: null,
          uptimeSeconds: null,
          disks: [],
        },
        containers: null,
        problems: ["Container details unavailable"],
      },
    ],
  };
  globalThis.fetch = async () => json({ data: snapshot });
  const result = await fleetApi.getVitals();
  assert.equal(result.ok, true);
  assert.deepEqual(result.value, snapshot);
});

test("rejects malformed minted tokens instead of creating a one-time reveal without a token", async () => {
  globalThis.fetch = async () =>
    json({ data: { id: "id", name: "token", namespace: "space" } });
  const result = await secretsApi.mintToken("vault", "space", "token");
  assert.equal(result.ok, false);
  assert.equal(result.failure.code, "protocol");
});

test("cancels pending private data across logout even when the transport resolves late", async () => {
  let settle;
  globalThis.fetch = () =>
    new Promise((resolve) => {
      settle = resolve;
    });
  const pending = productsApi.listProducts();
  await new Promise((resolve) => setTimeout(resolve, 0));
  clearHttpSession();
  const result = await pending;
  assert.equal(result.ok, false);
  assert.equal(result.failure.code, "cancelled");
  settle(json({ data: [product] }));
});

test("fleet polling preserves an in-flight snapshot and stops after the last panel unmounts", async () => {
  const originalInterval = globalThis.setInterval;
  const originalClearInterval = globalThis.clearInterval;
  const originalDocument = Object.getOwnPropertyDescriptor(
    globalThis,
    "document",
  );
  const snapshot = { collectedAt: "2026-09-26T00:00:00Z", targets: [] };
  const requests = [];
  let poll;
  let timers = 0;
  let cleared = 0;
  const apps = [];
  const renderer = createRenderer({
    createElement: () => ({}),
    createText: () => ({}),
    createComment: () => ({}),
    insert() {},
    remove() {},
    patchProp() {},
    setText() {},
    setElementText() {},
    parentNode: () => null,
    nextSibling: () => null,
  });
  try {
    Object.defineProperty(globalThis, "document", {
      configurable: true,
      value: { visibilityState: "visible" },
    });
    globalThis.setInterval = (callback, delay) => {
      assert.equal(delay, 60_000);
      timers++;
      poll = callback;
      return 123;
    };
    globalThis.clearInterval = () => cleared++;
    globalThis.fetch = (_url, options) =>
      new Promise((settle) =>
        requests.push({ signal: options.signal, settle }),
      );
    queryClient.setQueryData(["fleet", "vitals"], snapshot);
    let vitals;
    for (let index = 0; index < 2; index++) {
      const app = renderer.createApp({
        setup() {
          vitals = useFleetVitals();
          return () => null;
        },
      });
      app.use(queryPlugin(queryClient));
      app.mount({});
      apps.push(app);
    }
    assert.equal(timers, 1);
    const refresh = vitals.refetch();
    await new Promise((resolve) => setImmediate(resolve));
    assert.equal(requests.length, 1);
    poll();
    await nextTick();
    assert.equal(
      requests.length,
      1,
      "the timer must not launch a second SSH snapshot",
    );
    assert.equal(
      requests[0].signal.aborted,
      false,
      "the existing snapshot must stay alive",
    );
    requests[0].settle(json({ data: snapshot }));
    await refresh;
    poll();
    await new Promise((resolve) => setImmediate(resolve));
    assert.equal(
      requests.length,
      2,
      "polling resumes once the previous snapshot completes",
    );
    requests[1].settle(json({ data: snapshot }));
    await new Promise((resolve) => setImmediate(resolve));
    apps.pop().unmount();
    assert.equal(
      cleared,
      0,
      "another mounted panel still owns the shared timer",
    );
    apps.pop().unmount();
    assert.equal(cleared, 1);
  } finally {
    for (const app of apps) app.unmount();
    queryClient.clear();
    globalThis.setInterval = originalInterval;
    globalThis.clearInterval = originalClearInterval;
    if (originalDocument)
      Object.defineProperty(globalThis, "document", originalDocument);
    else delete globalThis.document;
  }
});
