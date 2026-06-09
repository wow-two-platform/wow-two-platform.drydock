import { useState, type ReactNode } from 'react';
import { Button } from '@wow-two-beta/ui/actions';
import { Card } from '@wow-two-beta/ui/display';
import { Alert } from '@wow-two-beta/ui/feedback';
import { TextInput } from '@wow-two-beta/ui/forms';
import { api, ApiError } from '../api/client';

interface RegisterServerFormProps {
  onRegistered: () => void;
  onCancel: () => void;
}

/** Inline form to register a new Hetzner VPS as a deploy target. */
export function RegisterServerForm({ onRegistered, onCancel }: RegisterServerFormProps) {
  const [name, setName] = useState('');
  const [host, setHost] = useState('');
  const [sshUser, setSshUser] = useState('root');
  const [sshPort, setSshPort] = useState('22');
  const [region, setRegion] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit() {
    setSubmitting(true);
    setError(null);
    try {
      await api.registerServer({
        name,
        host,
        sshUser,
        sshPort: Number.parseInt(sshPort, 10) || 22,
        region: region.trim() === '' ? null : region.trim(),
      });
      onRegistered();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Failed to register server.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Card className="border border-border p-5">
      <form
        className="flex flex-col gap-4"
        onSubmit={(e) => {
          e.preventDefault();
          void submit();
        }}
      >
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Field label="Name">
            <TextInput value={name} onChange={(e) => setName(e.target.value)} placeholder="hel1-prod" />
          </Field>
          <Field label="Host (IP or DNS)">
            <TextInput value={host} onChange={(e) => setHost(e.target.value)} placeholder="203.0.113.10" />
          </Field>
          <Field label="SSH user">
            <TextInput value={sshUser} onChange={(e) => setSshUser(e.target.value)} placeholder="root" />
          </Field>
          <Field label="SSH port">
            <TextInput value={sshPort} onChange={(e) => setSshPort(e.target.value)} placeholder="22" />
          </Field>
          <Field label="Region (optional)">
            <TextInput value={region} onChange={(e) => setRegion(e.target.value)} placeholder="hel1" />
          </Field>
        </div>

        {error && <Alert severity="danger" title="Could not register" description={error} />}

        <div className="flex items-center justify-end gap-2">
          <Button type="button" variant="ghost" tone="neutral" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="button" variant="solid" tone="primary" isLoading={submitting} onClick={() => void submit()}>
            Register server
          </Button>
        </div>
      </form>
    </Card>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-xs font-medium text-muted-foreground">{label}</span>
      {children}
    </label>
  );
}
