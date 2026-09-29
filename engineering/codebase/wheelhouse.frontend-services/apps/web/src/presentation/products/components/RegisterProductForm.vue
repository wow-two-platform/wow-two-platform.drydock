<script lang="ts">
import type { ProductOperations } from "@/application/products";
import type { Product } from "@/domain/products";

/** Inputs seed one create or edit form instance; the parent keys edited products by ID. */
export interface RegisterProductFormProps {
  readonly initialProduct?: Product;
  readonly create: ProductOperations["create"];
  readonly update: ProductOperations["update"];
}
</script>

<script setup lang="ts">
import { computed, watch } from "vue";
import { z } from "zod";
import { Button } from "@wow-two-beta/ui-vue/presentation/actions";
import { Alert } from "@wow-two-beta/ui-vue/presentation/feedback";
import {
  Field,
  TextInput,
  SelectPicker,
  SelectPickerTrigger,
  SelectPickerValue,
  SelectPickerContent,
  SelectPickerItem,
} from "@wow-two-beta/ui-vue/presentation/forms";
import { useAppForm } from "@/bootstrap/form";
import { parseRepoInput, ProductStatus } from "@/domain/products";

/** Edits a product with inline validation and resets only after confirmed success. */
defineOptions({ name: "RegisterProductForm" });
const props = defineProps<RegisterProductFormProps>();
const emit = defineEmits<{
  saved: [];
  cancel: [];
  pending: [value: boolean];
}>();

const statuses = Object.values(ProductStatus);
const schema = z.object({
  slug: z.string().trim().min(1, "Slug is required"),
  name: z.string().trim().min(1, "Name is required"),
  repo: z.string().superRefine((value, context) => {
    const parsed = parseRepoInput(value);
    if (parsed.provider === null)
      context.addIssue({ code: "custom", message: parsed.error });
  }),
  status: z.enum(ProductStatus),
});
const form = useAppForm({
  defaultValues: {
    slug: props.initialProduct?.slug ?? "",
    name: props.initialProduct?.name ?? "",
    repo: props.initialProduct?.repo ?? "",
    status: props.initialProduct?.status ?? ProductStatus.Draft,
  },
  schema,
  onSubmit: (values) => {
    const parsed = parseRepoInput(values.repo);
    const repo = parsed.provider === null ? values.repo.trim() : parsed.repo;
    return props.initialProduct
      ? props.update(props.initialProduct.id, {
          name: values.name.trim(),
          repo,
          status: values.status,
        })
      : props.create({
          slug: values.slug.trim(),
          name: values.name.trim(),
          repo,
        });
  },
});
const editing = computed(() => props.initialProduct !== undefined);

/** Exposes submission state so the surrounding modal cannot dismiss a pending write. */
watch(
  () => form.state.isSubmitting,
  (pending) => emit("pending", pending),
);

/** Preserves invalid input while normalizing accepted GitHub references. */
function normalizeRepo(value: string): void {
  const parsed = parseRepoInput(value);
  form.setValue("repo", parsed.provider === null ? value : parsed.repo);
}

/** Closes the editor only after schema validation and server confirmation. */
async function submit(event: Event): Promise<void> {
  if (await form.handleSubmit(event)) emit("saved");
}
</script>

<template>
  <form class="flex flex-col gap-5" @submit.prevent="submit">
    <div class="grid gap-5 sm:grid-cols-2">
      <form.Field
        v-slot="field"
        name="name"
        :is-disabled="form.state.isSubmitting"
      >
        <Field label="Product name">
          <TextInput
            v-model="field.value"
            placeholder="My product"
            autocomplete="off"
            @blur="field.onBlur"
          />
        </Field>
      </form.Field>
      <form.Field
        v-slot="field"
        name="slug"
        :is-disabled="editing || form.state.isSubmitting"
      >
        <Field
          label="Slug"
          :helper="
            editing
              ? 'The product identifier stays fixed.'
              : 'A stable product identifier.'
          "
        >
          <TextInput
            v-model="field.value"
            placeholder="my-product"
            autocomplete="off"
            @blur="field.onBlur"
          />
        </Field>
      </form.Field>
    </div>
    <div class="grid gap-5 sm:grid-cols-[10rem_1fr]">
      <Field label="Provider">
        <SelectPicker
          model-value="github"
          :is-disabled="form.state.isSubmitting"
        >
          <SelectPickerTrigger class="w-full"
            ><SelectPickerValue
          /></SelectPickerTrigger>
          <SelectPickerContent>
            <SelectPickerItem item-key="github" label="GitHub" />
            <SelectPickerItem
              item-key="gitlab"
              label="GitLab (soon)"
              is-disabled
            />
            <SelectPickerItem
              item-key="bitbucket"
              label="Bitbucket (soon)"
              is-disabled
            />
          </SelectPickerContent>
        </SelectPicker>
      </Field>
      <form.Field
        v-slot="field"
        name="repo"
        :is-disabled="form.state.isSubmitting"
      >
        <Field
          label="Repository"
          helper="Paste a GitHub URL, SSH remote, or owner/repo."
        >
          <TextInput
            :model-value="String(field.value)"
            placeholder="owner/repository"
            autocomplete="off"
            @update:model-value="normalizeRepo"
            @blur="field.onBlur"
          />
        </Field>
      </form.Field>
    </div>
    <form.Field
      v-if="editing"
      v-slot="field"
      name="status"
      :is-disabled="form.state.isSubmitting"
    >
      <Field label="Lifecycle">
        <SelectPicker
          :model-value="String(field.value)"
          @update:model-value="field.setValue"
        >
          <SelectPickerTrigger class="w-full"
            ><SelectPickerValue
          /></SelectPickerTrigger>
          <SelectPickerContent>
            <SelectPickerItem
              v-for="status in statuses"
              :key="status"
              :item-key="status"
              :label="status"
            />
          </SelectPickerContent>
        </SelectPicker>
      </Field>
    </form.Field>
    <Alert
      v-if="form.state.submitError"
      severity="danger"
      title="Product could not be saved"
      :description="form.state.submitError.message"
    />
    <div class="flex justify-end gap-2 border-t border-border pt-5">
      <Button
        type="button"
        variant="ghost"
        tone="neutral"
        :is-disabled="form.state.isSubmitting"
        @click="emit('cancel')"
        >Cancel</Button
      >
      <Button type="submit" :is-loading="form.state.isSubmitting">
        {{ editing ? "Save changes" : "Register product" }}
      </Button>
    </div>
  </form>
</template>
