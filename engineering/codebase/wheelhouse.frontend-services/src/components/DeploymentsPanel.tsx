import { useEffect, useState } from 'react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Heading, Text } from '@wow-two-beta/ui/presentation/display';

type Target = { id: string; product: string; environment: string; host: string };
type Release = { id: string; product: string; release: string; prerelease: boolean };
type Job = { id: string; status: string; release?: string; failure?: string };
const pending = new Set(['queued', 'running', 'submitting']);

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch('/api/deployments' + path, options);
  if (!response.ok) throw new Error('Deployment operation unavailable. Check the target status before retrying.');
  return (await response.json()).data as T;
}

/** Deploys imported releases to explicitly provisioned environments. */
export function DeploymentsPanel() {
  const [targets, setTargets] = useState<Target[]>([]);
  const [releases, setReleases] = useState<Release[]>([]);
  const [target, setTarget] = useState('');
  const [release, setRelease] = useState('');
  const [job, setJob] = useState<Job | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [refreshKey, setRefreshKey] = useState(0);
  const selectedTarget = targets.find(item => item.id === target);
  const available = releases.filter(item => item.product === selectedTarget?.product);

  useEffect(() => {
    let disposed = false;
    setError('');
    Promise.all([request<Target[]>('/targets'), request<Release[]>('/releases')])
      .then(([loadedTargets, loadedReleases]) => {
        if (!disposed) { setTargets(loadedTargets); setReleases(loadedReleases); }
      })
      .catch((reason: Error) => { if (!disposed) setError(reason.message); });
    return () => { disposed = true; };
  }, [refreshKey]);

  useEffect(() => {
    if (!job || !pending.has(job.status)) return;
    const timer = window.setTimeout(() => {
      request<Job>('/' + job.id).then(setJob).catch((reason: Error) => {
        setError(reason.message);
        setJob(current => current ? { ...current, status: 'unknown' } : null);
      });
    }, 3000);
    return () => window.clearTimeout(timer);
  }, [job]);

  async function deploy() {
    setBusy(true);
    setError('');
    try {
      setJob(await request<Job>('', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Wheelhouse-Action': 'deploy' },
        body: JSON.stringify({ target, release }),
      }));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Deployment response unavailable.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="rounded-xl border border-border bg-card p-6">
      <div className="flex items-center justify-between gap-3">
        <Heading level={2} size="md">Deployments</Heading>
        <Button variant="ghost" onClick={() => setRefreshKey(value => value + 1)}>Refresh artifacts</Button>
      </div>
      <Text color="muted" size="sm">Deploy a published artifact to a configured environment.</Text>
      {error && <p role="alert" className="mt-3 text-sm text-destructive">{error}</p>}
      {!targets.length && !error && <p className="mt-3 text-sm">No deployment targets configured.</p>}
      {!releases.length && !error && <p className="mt-3 text-sm">No published deployment artifacts available.</p>}
      {!!targets.length && (
        <div className="mt-4 flex flex-wrap items-end gap-3">
          <label className="flex flex-col gap-1 text-sm">
            Environment
            <select className="rounded border border-border bg-background p-2" value={target}
              onChange={event => { setTarget(event.target.value); setRelease(''); }}>
              <option value="">Select environment</option>
              {targets.map(item => <option key={item.id} value={item.id}>
                {item.product} / {item.environment} / {item.host}
              </option>)}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-sm">
            Release
            <select className="rounded border border-border bg-background p-2" value={release}
              onChange={event => setRelease(event.target.value)}>
              <option value="">Select published release</option>
              {available.map(item => <option key={item.id} value={item.id}>
                {item.release}{item.prerelease ? ' (prerelease)' : ''}
              </option>)}
            </select>
          </label>
          <Button disabled={busy || !target || !available.some(item => item.id === release) || (!!job && pending.has(job.status))}
            onClick={() => void deploy()}>
            {busy ? 'Submitting…' : 'Deploy release'}
          </Button>
        </div>
      )}
      {job && (
        <div role="status" className="mt-4 text-sm">
          <p>Deployment {job.id}: {job.status.replaceAll('_', ' ')}</p>
          {job.failure && <p>Inspect the target before another deployment.</p>}
          {job.status === 'unknown' && <Button variant="ghost" onClick={() => {
            request<Job>('/' + job.id).then(setJob).catch((reason: Error) => setError(reason.message));
          }}>Refresh outcome</Button>}
        </div>
      )}
    </section>
  );
}
