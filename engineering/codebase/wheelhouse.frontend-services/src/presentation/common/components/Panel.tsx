import type { ReactNode } from 'react';
import { Card, Heading, Text } from '@wow-two-beta/ui/presentation/display';

/** Props for {@link Panel}. */
export interface PanelProps {
  title: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  children: ReactNode;
}

/** A titled dashboard section with optional header actions. */
export function Panel(props: PanelProps) {
  return (
    <Card className="rounded-xl border border-border shadow-[0_1px_3px_rgb(14_39_66/0.06)]">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border/70 px-5 py-4">
        <div>
          <Heading level={2} size="md">{props.title}</Heading>
          {props.description && <Text color="muted" size="sm">{props.description}</Text>}
        </div>
        {props.actions && <div className="flex items-center gap-2">{props.actions}</div>}
      </div>
      <div className="p-5">{props.children}</div>
    </Card>
  );
}
