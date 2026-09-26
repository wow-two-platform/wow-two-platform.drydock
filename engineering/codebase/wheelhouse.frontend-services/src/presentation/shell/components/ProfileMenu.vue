<script lang="ts">
import type { CurrentUser } from '@/domain/auth';
import type { ColorScheme } from '../hooks/useColorScheme';

/** Defines account controls for the top navigation bar. */
export interface ProfileMenuProps {
  readonly user: CurrentUser;
  readonly connection: string;
  readonly scheme: ColorScheme;
}
</script>
<script setup lang="ts">
import { Check, ChevronDown, LogOut, Monitor, Moon, Sun } from 'lucide-vue-next';
import { Avatar } from '@wow-two-beta/ui-vue/presentation/display';
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  MenuItem,
  MenuLabel,
  MenuSeparator,
} from '@wow-two-beta/ui-vue/presentation/nav';

/** Keeps appearance and account actions available from every workspace destination. */
defineOptions({ name: 'ProfileMenu' });
const props = defineProps<ProfileMenuProps>();
const emit = defineEmits<{ scheme: [value: ColorScheme]; signOut: [] }>();
const schemes = [
  { value: 'light', label: 'Light', icon: Sun },
  { value: 'dark', label: 'Dark', icon: Moon },
  { value: 'system', label: 'System', icon: Monitor },
] as const;
</script>
<template>
  <DropdownMenu placement="bottom-end">
    <DropdownMenuTrigger as-child>
      <button
        type="button"
        :aria-label="`Account menu for ${props.user.name || props.user.login}`"
        class="flex items-center gap-2 rounded-xl p-1.5 text-foreground hover:bg-muted focus-visible:outline-2 focus-visible:outline-ring"
      >
        <Avatar
          v-bind="props.user.avatar ? { src: props.user.avatar } : {}"
          :name="props.user.name || props.user.login"
          size="sm"
        />
        <ChevronDown :size="14" class="hidden text-muted-foreground sm:block" />
      </button>
    </DropdownMenuTrigger>
    <DropdownMenuContent aria-label="Account" class="w-64">
      <MenuLabel>{{ props.user.name || props.user.login }}</MenuLabel>
      <p class="px-2 pb-2 text-xs text-muted-foreground">@{{ props.user.login }} · API {{ props.connection }}</p>
      <MenuSeparator />
      <MenuLabel>Appearance</MenuLabel>
      <MenuItem v-for="item in schemes" :key="item.value" @select="emit('scheme', item.value)">
        <component :is="item.icon" :size="15" />{{ item.label }}
        <Check v-if="props.scheme === item.value" :size="14" class="ml-auto" />
      </MenuItem>
      <MenuSeparator />
      <MenuItem state="destructive" @select="emit('signOut')"><LogOut :size="15" />Sign out</MenuItem>
    </DropdownMenuContent>
  </DropdownMenu>
</template>
