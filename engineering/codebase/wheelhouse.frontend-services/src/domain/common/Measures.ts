const UNITS = ['B', 'KiB', 'MiB', 'GiB', 'TiB'] as const;
const DAY_MS = 86_400_000;
const MOMENT = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });

/** Pure display rules for sizes, durations, ratios and ages. */
export const Measures = {
  /** Bytes in binary units: `512 MiB`, `1.5 GiB`. */
  bytes: (value: number | null | undefined) => {
    if (value == null) return '—';
    let size = value;
    let unit = 0;
    while (size >= 1024 && unit < UNITS.length - 1) {
      size /= 1024;
      unit++;
    }
    return `${size >= 10 || unit === 0 ? Math.round(size) : size.toFixed(1)} ${UNITS[unit]}`;
  },

  /** A duration in its two largest units: `45s`, `3m 20s`, `2h 5m`, `3d 4h`. */
  duration: (seconds: number | null | undefined) => {
    if (seconds == null) return '—';
    const total = Math.round(seconds);
    const parts: Array<[number, string]> = [
      [Math.floor(total / 86_400), 'd'],
      [Math.floor((total % 86_400) / 3600), 'h'],
      [Math.floor((total % 3600) / 60), 'm'],
      [total % 60, 's'],
    ];
    const first = parts.findIndex(([amount]) => amount > 0);
    if (first < 0) return '0s';
    return parts
      .slice(first, first + 2)
      .filter(([amount]) => amount > 0)
      .map(([amount, unit]) => `${amount}${unit}`)
      .join(' ');
  },

  /** A 0–1 ratio as a whole percentage. */
  percent: (ratio: number | null | undefined) => (ratio == null ? '—' : `${Math.round(ratio * 100)}%`),

  /** Whole days from an ISO instant to `now`. */
  ageDays: (iso: string, now: number = Date.now()) => Math.max(0, Math.floor((now - Date.parse(iso)) / DAY_MS)),

  /** An age as short text: `just now`, `5 hours ago`, `1 day ago`, `12 days ago`. */
  age: (iso: string, now: number = Date.now()) => {
    const hours = Math.max(0, Math.floor((now - Date.parse(iso)) / 3_600_000));
    if (hours < 1) return 'just now';
    if (hours < 24) return hours === 1 ? '1 hour ago' : `${hours} hours ago`;
    const days = Math.floor(hours / 24);
    return days === 1 ? '1 day ago' : `${days} days ago`;
  },

  /** An instant as short local text for dense tables: `Sep 26, 11:10 AM`. */
  moment: (iso: string | null | undefined) => (iso ? MOMENT.format(new Date(iso)) : '—'),

  /** The used share of a capacity, 0–100. */
  usedPercent: (total: number, free: number) => (total > 0 ? ((total - free) / total) * 100 : 0),
} as const;
