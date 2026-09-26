import { useEffect, useState } from 'react';
import { useMediaQuery } from '@wow-two-beta/ui/foundation/hooks';

/** The operator's colour scheme; `system` follows the operating system. */
export const ColorScheme = {
  System: 'system',
  Light: 'light',
  Dark: 'dark',
} as const;
export type ColorScheme = (typeof ColorScheme)[keyof typeof ColorScheme];

// Shared with public/theme.js, which applies the scheme before the first paint.
const STORAGE_KEY = 'wheelhouse.theme';

function read(): ColorScheme {
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY);
    return stored === ColorScheme.Light || stored === ColorScheme.Dark ? stored : ColorScheme.System;
  } catch {
    return ColorScheme.System;
  }
}

/** The colour scheme, remembered per browser, applied as the `dark` class the UI SDK themes on. */
export function useColorScheme() {
  const [scheme, setScheme] = useState(read);
  const prefersDark = useMediaQuery('(prefers-color-scheme: dark)');
  const isDark = scheme === ColorScheme.Dark || (scheme === ColorScheme.System && prefersDark);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', isDark);
    document.documentElement.style.colorScheme = isDark ? 'dark' : 'light';
  }, [isDark]);

  // ---- Remember the choice; storage can be unavailable in private windows ----
  useEffect(() => {
    try {
      window.localStorage.setItem(STORAGE_KEY, scheme);
    } catch {
      /* the scheme still applies for this visit */
    }
  }, [scheme]);

  return { scheme, setScheme, isDark } as const;
}
