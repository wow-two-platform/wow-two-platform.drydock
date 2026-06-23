import { useState, type ReactNode } from 'react';
import { Button } from '@wow-two-beta/ui/actions';
import { Card } from '@wow-two-beta/ui/display';
import { Alert } from '@wow-two-beta/ui/feedback';
import { Select, TextInput } from '@wow-two-beta/ui/forms';
import { ApiError } from '../api/client';
import type { ProductDto, ProductStatus } from '../api/types';
import { parseRepoInput } from '../lib/ParseRepoInput';
import { useProducts } from '../hooks/useProducts';

const STATUS_OPTIONS: ProductStatus[] = ['Draft', 'Active', 'Paused', 'Killed'];

// Provider is implied GitHub on the wire (no backend field). GitLab/Bitbucket are listed but
// disabled — an unsupported provider can't be selected; pasting one of their URLs surfaces an
// inline message instead. The `value` doubles as the host the parser maps an unsupported URL to.
const PROVIDER_OPTIONS = [
  { value: 'github', label: 'GitHub', disabled: false },
  { value: 'gitlab', label: 'GitLab (soon)', disabled: true },
  { value: 'bitbucket', label: 'Bitbucket (soon)', disabled: true },
] as const;

interface RegisterProductFormProps {
  /** When set, the form edits this product (slug locked); otherwise it creates a new one. */
  product?: ProductDto;
  create: ReturnType<typeof useProducts>['create'];
  update: ReturnType<typeof useProducts>['update'];
  onSaved: () => void;
  onCancel: () => void;
}

/** Inline form to register a new product, or edit an existing one (slug is immutable on edit). */
export function RegisterProductForm({ product, create, update, onSaved, onCancel }: RegisterProductFormProps) {
  const isEdit = product !== undefined;
  const [slug, setSlug] = useState(product?.slug ?? '');
  const [name, setName] = useState(product?.name ?? '');
  const [provider, setProvider] = useState('github');
  const [repo, setRepo] = useState(product?.repo ?? '');
  const [repoError, setRepoError] = useState<string | null>(null);
  const [status, setStatus] = useState<ProductStatus>(product?.status ?? 'Draft');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Runs on every repo-input change (covers paste): a supported URL is stripped to owner/repo and
  // the field is rewritten; an unsupported host / unparseable value sets an inline error that
  // blocks submit; a bare owner/repo is accepted as-is.
  function onRepoChange(value: string) {
    const result = parseRepoInput(value);
    if (result.provider === null) {
      // Keep what the user typed (don't strip) and explain why it won't submit.
      setRepo(value);
      setRepoError(result.error);
      return;
    }
    setRepo(result.repo);
    setProvider('github');
    setRepoError(null);
  }

  async function submit() {
    // An unsupported / unparseable repo blocks submit — the inline message already explains why.
    if (repoError !== null) return;

    setSubmitting(true);
    setError(null);
    try {
      // Provider is implied GitHub — only `owner/repo` goes to the backend.
      if (isEdit) {
        await update(product.id, { name, repo: repo.trim(), status });
      } else {
        await create({ slug: slug.trim(), name, repo: repo.trim() });
      }
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : `Failed to ${isEdit ? 'update' : 'register'} product.`);
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
          <Field label="Slug">
            <TextInput
              value={slug}
              onChange={(e) => setSlug(e.target.value)}
              placeholder="my-product"
              disabled={isEdit}
            />
          </Field>
          <Field label="Name">
            <TextInput value={name} onChange={(e) => setName(e.target.value)} placeholder="My Product" />
          </Field>
          <Field label="Repo (owner/repo or URL)">
            <div className="flex items-stretch gap-2">
              <Select
                value={provider}
                onValueChange={(opt) => setProvider(opt?.value ?? 'github')}
              >
                <Select.Trigger className="h-9 shrink-0" aria-label="Repository provider">
                  <Select.Value />
                </Select.Trigger>
                <Select.Content>
                  {PROVIDER_OPTIONS.map((p) => (
                    <Select.Item key={p.value} itemKey={p.value} label={p.label} isDisabled={p.disabled} />
                  ))}
                </Select.Content>
              </Select>
              <div className="flex-1">
                <TextInput
                  value={repo}
                  onChange={(e) => onRepoChange(e.target.value)}
                  placeholder="octocat/hello-world"
                  aria-invalid={repoError !== null}
                />
              </div>
            </div>
            {repoError && <span className="text-xs text-danger-600">{repoError}</span>}
          </Field>
          {isEdit && (
            <Field label="Status">
              <Select<ProductStatus>
                value={status}
                onValueChange={(opt) => setStatus(opt?.value ?? status)}
              >
                <Select.Trigger className="h-9">
                  <Select.Value />
                </Select.Trigger>
                <Select.Content>
                  {STATUS_OPTIONS.map((s) => (
                    <Select.Item key={s} itemKey={s} label={s} />
                  ))}
                </Select.Content>
              </Select>
            </Field>
          )}
        </div>

        {error && (
          <Alert severity="danger" title={`Could not ${isEdit ? 'save' : 'register'}`} description={error} />
        )}

        <div className="flex items-center justify-end gap-2">
          <Button type="button" variant="ghost" tone="neutral" onClick={onCancel}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="solid"
            tone="primary"
            isLoading={submitting}
            disabled={repoError !== null}
            onClick={() => void submit()}
          >
            {isEdit ? 'Save changes' : 'Register product'}
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
