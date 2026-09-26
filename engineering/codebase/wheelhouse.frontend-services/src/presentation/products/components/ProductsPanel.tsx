import { Fragment, useState, type ReactNode } from 'react';
import { Eye, Package, Pencil, Trash2 } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Code, Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow, Text } from '@wow-two-beta/ui/presentation/display';
import { StatusIndicator } from '@wow-two-beta/ui/presentation/feedback';
import { useProducts } from '@/application/products';
import { ProductStatus, type Product } from '@/domain/products';
import { Expand, LoadState, Panel, TableStyles, useExpand } from '@/presentation/common/components';
import { Skeleton } from '@/presentation/common/skeleton';
import { RegisterProductForm } from './RegisterProductForm';

const STATUS_TONE: Record<ProductStatus, 'neutral' | 'success' | 'warning' | 'destructive'> = {
  Active: 'success',
  Paused: 'warning',
  Killed: 'destructive',
  Draft: 'neutral',
};

const COLUMNS = 5;

// The first load's stand-in rows, in the loaded table's shape.
const PLACEHOLDER: Product[] = [1, 2, 3].map((index) => ({
  id: `placeholder-${index}`, slug: 'product-slug', name: 'Product name', repo: 'owner/repository',
  status: ProductStatus.Active, createdAtUtc: '2026-01-01T00:00:00Z',
}));

/** Props for {@link ProductsPanel}. */
export interface ProductsPanelProps {
  /** The registration form is open; the page header's action opens it. */
  registering: boolean;
  onRegisteringChange: (registering: boolean) => void;
}

/** The Products registry — list portfolio products and create / view / edit / delete them. */
export function ProductsPanel(props: ProductsPanelProps) {
  const { products, loading, error, reload, create, update, remove } = useProducts();
  const [editing, setEditing] = useState<Product | null>(null);
  const [viewing, setViewing] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  function closeForm() {
    props.onRegisteringChange(false);
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
    <Panel title="Registry" description="Each product's slug, source repository and lifecycle status.">
      <div className="flex flex-col gap-5">
        <Expand open={props.registering || editing !== null} className="-mb-5">
          <div className="pb-5">
            <RegisterProductForm key={editing?.id ?? 'create'} product={editing ?? undefined} create={create}
              update={update} onSaved={closeForm} onCancel={closeForm} />
          </div>
        </Expand>
        <LoadState loading={loading} error={error ? { message: error } : null} empty={products.length === 0}
          emptyIcon={<Package size={28} />} emptyTitle="No products yet"
          emptyDescription="Register your first portfolio product to start deploying."
          emptyActions={props.registering ? undefined : (
            <Button variant="solid" tone="primary" size="sm" onClick={() => props.onRegisteringChange(true)}>
              Register product
            </Button>
          )}
          onRetry={() => void reload()}
          skeleton={
            <Skeleton.Group loading label="Loading products">
              <ProductsTable products={PLACEHOLDER} />
            </Skeleton.Group>
          }>
          <ProductsTable products={products}
            actions={(product) => (
              <div className="flex items-center justify-end gap-1">
                <Button variant="ghost" tone="neutral" size="sm" aria-label={`View ${product.name}`} leadingSlot={<Eye size={15} />}
                  onClick={() => setViewing((current) => (current === product.id ? null : product.id))} />
                <Button variant="ghost" tone="neutral" size="sm" aria-label={`Edit ${product.name}`} leadingSlot={<Pencil size={15} />}
                  onClick={() => {
                    props.onRegisteringChange(false);
                    setEditing(product);
                  }} />
                <Button variant="ghost" tone="danger" size="sm" aria-label={`Delete ${product.name}`} leadingSlot={<Trash2 size={15} />}
                  onClick={() => setConfirmDelete(product.id)} />
              </div>
            )}
            details={(product) => (
              <>
                <DetailRow open={viewing === product.id} className="bg-muted/40">
                  <dl className="grid grid-cols-2 gap-x-6 gap-y-2 px-3 py-4 text-xs sm:grid-cols-4">
                    <Detail label="Id"><Code className="break-all">{product.id}</Code></Detail>
                    <Detail label="Repo"><Code className="break-all">{product.repo}</Code></Detail>
                    <Detail label="Status">{product.status}</Detail>
                    <Detail label="Created">{new Date(product.createdAtUtc).toLocaleString()}</Detail>
                  </dl>
                </DetailRow>
                <DetailRow open={confirmDelete === product.id} className="bg-destructive-soft">
                  <div className="flex items-center justify-between gap-4 px-3 py-3">
                    <Text size="sm">Delete <span className="font-medium">{product.name}</span>? This cannot be undone.</Text>
                    <div className="flex items-center gap-2">
                      <Button variant="ghost" tone="neutral" size="sm" onClick={() => setConfirmDelete(null)}>Cancel</Button>
                      <Button variant="solid" tone="danger" size="sm" isLoading={deletingId === product.id}
                        onClick={() => void onDelete(product.id)}>
                        Delete
                      </Button>
                    </div>
                  </div>
                </DetailRow>
              </>
            )} />
        </LoadState>
      </div>
    </Panel>
  );
}

// ---- Table ----

function ProductsTable(props: {
  products: Product[];
  actions?: (product: Product) => ReactNode;
  /** Rows under a product: its details or the delete confirmation. */
  details?: (product: Product) => ReactNode;
}) {
  return (
    <Table density="compact" isHoverable className="table-fixed" containerClassName={TableStyles.scrollBox}>
      {/* Fixed columns: an opening detail row never re-lays the header. */}
      <colgroup>
        <col className="w-[22%]" />
        <col className="w-[18%]" />
        <col />
        <col className="w-32" />
        <col className="w-32" />
      </colgroup>
      <TableHead className={TableStyles.stickyHead}>
        <TableRow>
          <TableHeaderCell>Name</TableHeaderCell><TableHeaderCell>Slug</TableHeaderCell>
          <TableHeaderCell>Repo</TableHeaderCell><TableHeaderCell>Status</TableHeaderCell>
          <TableHeaderCell><span className="sr-only">Actions</span></TableHeaderCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {props.products.map((product) => (
          <Fragment key={product.id}>
            <TableRow>
              <TableCell className="truncate font-medium"><Skeleton.Slot>{product.name}</Skeleton.Slot></TableCell>
              <TableCell className="truncate font-mono text-xs text-muted-foreground"><Skeleton.Slot>{product.slug}</Skeleton.Slot></TableCell>
              <TableCell className="truncate font-mono text-xs text-muted-foreground"><Skeleton.Slot>{product.repo}</Skeleton.Slot></TableCell>
              <TableCell>
                <Skeleton.Slot><StatusIndicator tone={STATUS_TONE[product.status]} label={product.status} /></Skeleton.Slot>
              </TableCell>
              <TableCell>{props.actions?.(product)}</TableCell>
            </TableRow>
            {props.details?.(product)}
          </Fragment>
        ))}
      </TableBody>
    </Table>
  );
}

// A row under a product that opens and closes by height; it leaves no empty row behind once closed.
function DetailRow(props: { open: boolean; className: string; children: ReactNode }) {
  const expand = useExpand(props.open);
  if (!expand.mounted) return null;
  return (
    <TableRow className={props.className}>
      <TableCell colSpan={COLUMNS} className="p-0">
        <div {...expand.region}>
          <div className="min-h-0 overflow-hidden">{props.children}</div>
        </div>
      </TableCell>
    </TableRow>
  );
}

function Detail(props: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="font-medium uppercase tracking-wide text-muted-foreground">{props.label}</dt>
      <dd className="text-foreground">{props.children}</dd>
    </div>
  );
}
