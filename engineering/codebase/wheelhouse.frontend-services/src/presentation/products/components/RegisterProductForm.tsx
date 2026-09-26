import { z } from 'zod';

import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Card } from '@wow-two-beta/ui/presentation/display';
import { Alert } from '@wow-two-beta/ui/presentation/feedback';
import { Field, Select, TextInput } from '@wow-two-beta/ui/presentation/forms';

import type { ProductOperations } from '@/application/products';
import { parseRepoInput, type Product, type ProductStatus } from '@/domain/products';
import { useAppForm } from '@/form';

const STATUS_OPTIONS = ['Draft', 'Active', 'Paused', 'Killed'] as const satisfies readonly ProductStatus[];

// Provider is implied GitHub on the wire (no backend field). GitLab/Bitbucket are listed but
// disabled — an unsupported provider can't be selected; pasting one of their URLs surfaces an
// inline message instead. The `value` doubles as the host the parser maps an unsupported URL to.
const PROVIDER_OPTIONS = [
  { value: 'github', label: 'GitHub', disabled: false },
  { value: 'gitlab', label: 'GitLab (soon)', disabled: true },
  { value: 'bitbucket', label: 'Bitbucket (soon)', disabled: true },
] as const;

/** Editable shape of the product form — provider is display-only glue (implied GitHub on the wire). */
interface ProductValues {
  slug: string;
  name: string;
  provider: string;
  repo: string;
  status: ProductStatus;
}

/** Whole-form validation for {@link ProductValues} — the repo rule reuses the parser so an unsupported host blocks submit with its message. */
const ProductSchema = z.object({
  slug: z.string().trim().min(1, 'Slug is required'),
  name: z.string().trim().min(1, 'Name is required'),
  provider: z.string(),
  repo: z.string().superRefine((value, ctx) => {
    const parsed = parseRepoInput(value);
    if (parsed.provider === null) ctx.addIssue({ code: 'custom', message: parsed.error });
  }),
  status: z.enum(STATUS_OPTIONS),
});

interface RegisterProductFormProps {
  /** When set, the form edits this product (slug locked); otherwise it creates a new one. */
  product?: Product | undefined;
  create: ProductOperations['create'];
  update: ProductOperations['update'];
  onSaved: () => void;
  onCancel: () => void;
}

/** Inline form to register a new product, or edit an existing one (slug is immutable on edit). */
export function RegisterProductForm({ product, create, update, onSaved, onCancel }: RegisterProductFormProps) {
  const isEdit = product !== undefined;

  const form = useAppForm<ProductValues>({
    defaultValues: {
      slug: product?.slug ?? '',
      name: product?.name ?? '',
      provider: 'github',
      repo: product?.repo ?? '',
      status: product?.status ?? 'Draft',
    },
    schema: ProductSchema,
    onSubmit: async (values) => {
      // Provider is implied GitHub — only `owner/repo` goes to the backend.
      if (isEdit) {
        await update(product.id, { name: values.name, repo: values.repo.trim(), status: values.status });
      } else {
        await create({ slug: values.slug.trim(), name: values.name, repo: values.repo.trim() });
      }
      onSaved();
    },
  });

  // Runs on every repo-input change (covers paste): a supported URL is stripped to owner/repo and
  // the field is rewritten; an unsupported host / unparseable value keeps what the user typed —
  // the schema re-runs the parser and its message blocks submit; a bare owner/repo is accepted as-is.
  function onRepoChange(value: string, setValue: (value: string) => void) {
    const result = parseRepoInput(value);
    setValue(result.provider === null ? value : result.repo);
  }

  return (
    <Card className="border border-border p-5">
      <form className="flex flex-col gap-4" onSubmit={(e) => void form.handleSubmit(e)}>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <form.Field name="slug">
            {(f) => (
              <Field label="Slug" isDisabled={isEdit}>
                <TextInput value={f.value} onChange={(e) => f.setValue(e.target.value)} onBlur={f.onBlur} placeholder="my-product" />
              </Field>
            )}
          </form.Field>
          <form.Field name="name">
            {(f) => (
              <Field label="Name">
                <TextInput value={f.value} onChange={(e) => f.setValue(e.target.value)} onBlur={f.onBlur} placeholder="My Product" />
              </Field>
            )}
          </form.Field>
          <form.Field name="repo">
            {(f) => (
              <Field label="Repo (owner/repo or URL)">
                <div className="flex items-stretch gap-2">
                  <form.Field name="provider">
                    {(prov) => (
                      // The Select root is full-width; a fixed box keeps the repo input its room.
                      <div className="w-32 shrink-0">
                      <Select value={prov.value} onValueChange={(opt) => prov.setValue(opt?.value ?? 'github')}>
                        <Select.Trigger className="h-9 shrink-0" aria-label="Repository provider">
                          <Select.Value />
                        </Select.Trigger>
                        <Select.Content>
                          {PROVIDER_OPTIONS.map((p) => (
                            <Select.Item key={p.value} itemKey={p.value} label={p.label} isDisabled={p.disabled} />
                          ))}
                        </Select.Content>
                      </Select>
                      </div>
                    )}
                  </form.Field>
                  <div className="min-w-0 flex-1">
                    <TextInput
                      value={f.value}
                      onChange={(e) => onRepoChange(e.target.value, f.setValue)}
                      onBlur={f.onBlur}
                      placeholder="octocat/hello-world"
                    />
                  </div>
                </div>
              </Field>
            )}
          </form.Field>
          {isEdit && (
            <form.Field name="status">
              {(f) => (
                <Field label="Status">
                  <Select<ProductStatus> value={f.value} onValueChange={(opt) => f.setValue(opt?.value ?? f.value)}>
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
            </form.Field>
          )}
        </div>

        <form.Subscribe selector={(s) => s.submitError}>
          {(error) =>
            error && <Alert severity="danger" title={`Could not ${isEdit ? 'save' : 'register'}`} description={error.message} />
          }
        </form.Subscribe>

        <div className="flex items-center justify-end gap-2">
          <Button type="button" variant="ghost" tone="neutral" onClick={onCancel}>
            Cancel
          </Button>
          <form.Subscribe selector={(s) => s.isSubmitting}>
            {(busy) => (
              <Button type="submit" variant="solid" tone="primary" isLoading={busy}>
                {isEdit ? 'Save changes' : 'Register product'}
              </Button>
            )}
          </form.Subscribe>
        </div>
      </form>
    </Card>
  );
}
