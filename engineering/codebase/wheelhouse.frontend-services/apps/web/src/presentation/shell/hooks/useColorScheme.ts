import { computed, onScopeDispose, ref, watch } from 'vue';

/** Names the operator's persisted theme preference. */
export const ColorScheme = { System: 'system', Light: 'light', Dark: 'dark' } as const;
export type ColorScheme = (typeof ColorScheme)[keyof typeof ColorScheme];

/** Applies the saved scheme while tracking operating-system changes. */
export function useColorScheme() {
  const media = window.matchMedia('(prefers-color-scheme: dark)');
  const scheme = ref<ColorScheme>(read());
  const prefersDark = ref(media.matches);
  const isDark = computed(() => scheme.value === 'dark' || (scheme.value === 'system' && prefersDark.value));
  const onChange = (event: MediaQueryListEvent) => {
    prefersDark.value = event.matches;
  };
  media.addEventListener('change', onChange);
  onScopeDispose(() => media.removeEventListener('change', onChange));
  watch(
    isDark,
    (dark) => {
      document.documentElement.classList.toggle('dark', dark);
      document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
    },
    { immediate: true },
  );
  function setScheme(value: ColorScheme): void {
    scheme.value = value;
    try {
      localStorage.setItem('wheelhouse.theme', value);
    } catch {
      /* This visit still receives the selected theme. */
    }
  }
  return { scheme, isDark, setScheme };
}

/** Reads the value shared with the pre-paint theme script. @internal */
function read(): ColorScheme {
  try {
    const stored = localStorage.getItem('wheelhouse.theme');
    return stored === 'light' || stored === 'dark' ? stored : 'system';
  } catch {
    return 'system';
  }
}
