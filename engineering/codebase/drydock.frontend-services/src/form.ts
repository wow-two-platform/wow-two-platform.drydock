// The app's forms-engine pin — the only vendor-touching line in the app
// (wow-two-ws/conventions/development/frontend/presentation/forms.md). Screens import
// `useAppForm` from here; swapping engines (tanstack ↔ house) = editing this one line.
export { useAppForm } from '@wow-two-beta/ui/forms-engine/tanstack';
