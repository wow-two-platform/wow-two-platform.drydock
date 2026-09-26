import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

const PageActionsContext = createContext<{
  target: HTMLElement | null;
  setTarget: (element: HTMLElement | null) => void;
} | null>(null);

/** Connects the shared page header with the page rendered under it. */
export function PageActionsProvider(props: { children: ReactNode }) {
  const [target, setTarget] = useState<HTMLElement | null>(null);
  const value = useMemo(() => ({ target, setTarget }), [target]);
  return <PageActionsContext.Provider value={value}>{props.children}</PageActionsContext.Provider>;
}

/** Where the current page's primary actions appear, beside its title. */
export function PageActionsOutlet(props: { className?: string }) {
  const context = useContext(PageActionsContext);
  return <div ref={context?.setTarget} className={props.className} />;
}

/**
 * The page's primary actions, shown in the shared page header. The page keeps the state they act on — the dialog
 * a button opens lives in the page, only the button moves.
 */
export function PageActions(props: { children: ReactNode }) {
  const context = useContext(PageActionsContext);
  return context?.target ? createPortal(props.children, context.target) : null;
}
