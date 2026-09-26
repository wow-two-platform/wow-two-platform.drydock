import { Navigate, Route, Routes } from 'react-router-dom';
import { Spinner } from '@wow-two-beta/ui/presentation/feedback';
import { useAuth } from '@/application/auth';
import { SignInScreen } from '@/presentation/auth';
import { AppLayout } from './AppLayout';
import { APP_ROUTES } from './routes';

/** The dashboard root, gated behind the operator's GitHub session. */
export function App() {
  const auth = useAuth();
  if (auth.loading)
    return <div className="flex min-h-full items-center justify-center"><Spinner label="Loading Wheelhouse" /></div>;
  if (!auth.user) return <SignInScreen onSignIn={auth.signIn} />;
  return (
    <AppLayout user={auth.user} onSignOut={() => void auth.signOut()}>
      <Routes>
        {APP_ROUTES.map((route) => <Route key={route.path} path={route.path} element={<route.page />} />)}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppLayout>
  );
}
