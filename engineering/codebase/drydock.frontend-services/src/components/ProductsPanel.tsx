import { Fragment, useState } from 'react';
import { Eye, Package, Pencil, Plus, Trash2 } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/actions';
import { Badge, Card, Code, EmptyState, Heading, Text } from '@wow-two-beta/ui/display';
import { Alert, Spinner } from '@wow-two-beta/ui/feedback';
import { useProducts } from '../hooks/useProducts';
import { RegisterProductForm } from './RegisterProductForm';
import type { ProductDto, ProductStatus } from '../api/types';

type BadgeTone = 'neutral' | 'success' | 'danger' | 'warning';

const STATUS_TONE: Record<ProductStatus, BadgeTone> = {
  Active: 'success',
  Paused: 'warning',
  Killed: 'danger',
  Draft: 'neutral',
};

/** The Products registry — list portfolio products and create / view / edit / delete them. */
export function ProductsPanel() {
  const { products, loading, error, reload, create, update, remove } = useProducts();
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<ProductDto | null>(null);
  const [viewing, setViewing] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  function closeForm() {
    setShowForm(false);
    setEditing(null);
  }

  async function onDelete(id: string) {
    setDeletingId(id);
    try {
      await remove(id);
      setConfirmDelete(null);
      if (viewing === id) setViewing(null);
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <Card className="border border-border">
      <div className="flex items-center justify-between border-b border-border p-5">
        <div>
          <Heading level={2} size="md">
            Products
          </Heading>
          <Text color="muted" size="sm">
            Portfolio products (one repo → one image)
          </Text>
        </div>
        <Button
          variant="solid"
          tone="primary"
          size="sm"
          leadingSlot={<Plus size={16} />}
          onClick={() => {
            setEditing(null);
            setShowForm((v) => !v);
          }}
        >
          {showForm && editing === null ? 'Close' : 'Register product'}
        </Button>
      </div>

      {(showForm || editing) && (
        <div className="border-b border-border p-5">
          <RegisterProductForm
            product={editing ?? undefined}
            create={create}
            update={update}
            onSaved={closeForm}
            onCancel={closeForm}
          />
        </div>
      )}

      <div className="p-5">
        {loading ? (
          <div className="flex justify-center py-10">
            <Spinner label="Loading products" />
          </div>
        ) : error ? (
          <Alert
            severity="danger"
            title="Couldn't load products"
            description={error}
            actions={
              <Button variant="soft" tone="danger" size="sm" onClick={() => void reload()}>
                Retry
              </Button>
            }
          />
        ) : products.length === 0 ? (
          <EmptyState
            icon={<Package size={28} />}
            title="No products yet"
            description="Register your first portfolio product to start deploying."
            actions={
              <Button
                variant="solid"
                tone="primary"
                size="sm"
                onClick={() => {
                  setEditing(null);
                  setShowForm(true);
                }}
              >
                Register product
              </Button>
            }
          />
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left text-xs uppercase tracking-wide text-muted-foreground">
                <th className="pb-2 font-medium">Name</th>
                <th className="pb-2 font-medium">Slug</th>
                <th className="pb-2 font-medium">Repo</th>
                <th className="pb-2 font-medium">Status</th>
                <th className="pb-2 font-medium text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {products.map((p) => (
                <Fragment key={p.id}>
                  <tr className="border-b border-border/60 last:border-0">
                    <td className="py-3 font-medium">{p.name}</td>
                    <td className="py-3 font-mono text-xs text-muted-foreground">{p.slug}</td>
                    <td className="py-3 font-mono text-xs text-muted-foreground">{p.repo}</td>
                    <td className="py-3">
                      <Badge variant={STATUS_TONE[p.status]}>{p.status}</Badge>
                    </td>
                    <td className="py-3">
                      <div className="flex items-center justify-end gap-1">
                        <Button
                          variant="ghost"
                          tone="neutral"
                          size="sm"
                          aria-label="View"
                          leadingSlot={<Eye size={15} />}
                          onClick={() => setViewing((v) => (v === p.id ? null : p.id))}
                        />
                        <Button
                          variant="ghost"
                          tone="neutral"
                          size="sm"
                          aria-label="Edit"
                          leadingSlot={<Pencil size={15} />}
                          onClick={() => {
                            setShowForm(false);
                            setEditing(p);
                          }}
                        />
                        <Button
                          variant="ghost"
                          tone="danger"
                          size="sm"
                          aria-label="Delete"
                          leadingSlot={<Trash2 size={15} />}
                          onClick={() => setConfirmDelete(p.id)}
                        />
                      </div>
                    </td>
                  </tr>

                  {viewing === p.id && (
                    <tr className="border-b border-border/60 bg-muted/30">
                      <td colSpan={5} className="px-3 py-4">
                        <dl className="grid grid-cols-2 gap-x-6 gap-y-2 text-xs sm:grid-cols-4">
                          <Detail label="Id">
                            <Code>{p.id}</Code>
                          </Detail>
                          <Detail label="Repo">
                            <Code>{p.repo}</Code>
                          </Detail>
                          <Detail label="Status">{p.status}</Detail>
                          <Detail label="Created">{new Date(p.createdAtUtc).toLocaleString()}</Detail>
                        </dl>
                      </td>
                    </tr>
                  )}

                  {confirmDelete === p.id && (
                    <tr className="border-b border-border/60 bg-danger/5">
                      <td colSpan={5} className="px-3 py-3">
                        <div className="flex items-center justify-between gap-4">
                          <Text size="sm">
                            Delete <span className="font-medium">{p.name}</span>? This cannot be undone.
                          </Text>
                          <div className="flex items-center gap-2">
                            <Button
                              variant="ghost"
                              tone="neutral"
                              size="sm"
                              onClick={() => setConfirmDelete(null)}
                            >
                              Cancel
                            </Button>
                            <Button
                              variant="solid"
                              tone="danger"
                              size="sm"
                              isLoading={deletingId === p.id}
                              onClick={() => void onDelete(p.id)}
                            >
                              Delete
                            </Button>
                          </div>
                        </div>
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </Card>
  );
}

function Detail({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-foreground">{children}</dd>
    </div>
  );
}
