// Applies the remembered colour scheme before the first paint, so a dark deck never flashes light.
// A file rather than an inline script, so a strict Content-Security-Policy keeps it working.
try {
  var scheme = localStorage.getItem('wheelhouse.theme');
  var dark = scheme === 'dark' || (scheme !== 'light' && matchMedia('(prefers-color-scheme: dark)').matches);
  document.documentElement.classList.toggle('dark', dark);
  document.documentElement.style.colorScheme = dark ? 'dark' : 'light';
} catch (error) {
  // Storage is blocked: the app applies the system scheme once it mounts.
}
