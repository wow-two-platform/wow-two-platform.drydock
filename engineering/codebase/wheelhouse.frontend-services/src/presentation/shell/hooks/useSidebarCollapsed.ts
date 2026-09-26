import { useCallback, useEffect, useState } from 'react';

const STORAGE_KEY = 'wheelhouse.sidebar.collapsed';

function read() {
  try {
    return window.localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}

/** Whether the desktop sidebar is a narrow icon rail; remembered per browser, toggled with Ctrl/⌘ + B. */
export function useSidebarCollapsed() {
  const [collapsed, setCollapsed] = useState(read);

  const toggle = useCallback(() => setCollapsed((current) => !current), []);

  // ---- Remember the choice; storage can be unavailable in private windows ----
  useEffect(() => {
    try {
      window.localStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0');
    } catch {
      /* the rail still works for this visit */
    }
  }, [collapsed]);

  // ---- Keyboard toggle, as in most editors ----
  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'b') {
        event.preventDefault();
        toggle();
      }
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [toggle]);

  return { collapsed, toggle } as const;
}
