import { Github } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Card, Heading, Text } from '@wow-two-beta/ui/presentation/display';

/** Props for {@link SignInScreen}. */
export interface SignInScreenProps {
  onSignIn: () => void;
}

/** The private sign-in gate for the authorized GitHub operator. */
export function SignInScreen(props: SignInScreenProps) {
  return (
    <div className="flex min-h-full items-center justify-center bg-background px-6 py-16 text-foreground">
      <Card className="w-full max-w-sm border border-border p-8">
        <div className="flex flex-col items-center gap-6 text-center">
          <div>
            <Heading level={1} size="lg">Wheelhouse</Heading>
            <Text color="muted" size="sm">Infrastructure control plane</Text>
          </div>
          <Text color="muted" size="sm">This control plane is private. Sign in with the authorized GitHub account.</Text>
          <Button variant="solid" tone="primary" size="lg" isFullWidth leadingSlot={<Github size={18} />} onClick={props.onSignIn}>
            Sign in with GitHub
          </Button>
        </div>
      </Card>
    </div>
  );
}
