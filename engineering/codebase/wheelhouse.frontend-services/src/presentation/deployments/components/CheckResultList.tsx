import { CircleCheck, CircleX } from 'lucide-react';
import type { TargetCheck } from '@/domain/deployments';

/** The read-only readiness probes of one target, failures first in reading order. */
export function CheckResultList(props: { check: TargetCheck }) {
  return (
    <ul className="flex flex-col gap-1.5 text-sm" aria-label="Readiness checks">
      {props.check.checks.map((item) => (
        <li key={item.name} className="flex items-start gap-2">
          {item.ok
            ? <CircleCheck size={16} className="mt-0.5 shrink-0 text-success" aria-label="Passed" />
            : <CircleX size={16} className="mt-0.5 shrink-0 text-danger" aria-label="Failed" />}
          <span className="font-medium">{item.name}</span>
          <span className={item.ok ? 'text-muted-foreground' : 'text-danger'}>{item.detail}</span>
        </li>
      ))}
    </ul>
  );
}
