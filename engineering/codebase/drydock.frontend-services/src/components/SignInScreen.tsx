import { Github } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/actions';
import { Card, Heading, Text } from '@wow-two-beta/ui/display';

interface SignInScreenProps {
  onSignIn: () => void;
}

/** Pre-auth gate — the single admin signs in with GitHub to reach the dashboard. */
export function SignInScreen({ onSignIn }: SignInScreenProps) {
  return (
    <div className="flex min-h-full items-center justify-center bg-background px-6 py-16 text-foreground">
      <Card className="w-full max-w-sm border border-border p-8">
        <div className="flex flex-col items-center gap-6 text-center">
          <div>
            <Heading level={1} size="lg">
              Drydock
            </Heading>
            <Text color="muted" size="sm">
              Product ops &amp; deploy control plane
            </Text>
          </div>

          <Text color="muted" size="sm">
            This control plane is private. Sign in with the authorized GitHub account to continue.
          </Text>

          <Button
            variant="solid"
            tone="primary"
            size="lg"
            isFullWidth
            leadingSlot={<Github size={18} />}
            onClick={onSignIn}
          >
            Sign in with GitHub
          </Button>
        </div>
      </Card>
    </div>
  );
}
