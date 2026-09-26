import { useState } from 'react';
import { Plus } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { PageActions } from '@/presentation/common/components';
import { ProductsPanel } from '../components/ProductsPanel';

/** The portfolio product registry. */
export function ProductsPage() {
  const [registering, setRegistering] = useState(false);
  return (
    <>
      <PageActions>
        <Button variant="solid" tone="primary" leadingSlot={<Plus size={16} />} isDisabled={registering}
          onClick={() => setRegistering(true)}>
          Register product
        </Button>
      </PageActions>
      <ProductsPanel registering={registering} onRegisteringChange={setRegistering} />
    </>
  );
}
