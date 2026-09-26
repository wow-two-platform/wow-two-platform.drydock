import { z } from "zod";
import type { CurrentUser } from "@/domain/auth";
import { request, requestEmpty } from "@/integration/common";

const CurrentUserSchema = z.object({
  login: z.string(),
  name: z.string(),
  avatar: z.string().nullable(),
});

/** The GitHub sign-in session endpoints. */
export const authApi = {
  getCurrentUser: (signal?: AbortSignal) =>
    request<CurrentUser>("/api/identity/me", CurrentUserSchema, { signal }),
  signOut: () => requestEmpty("/api/identity/sign-out", { method: "POST" }),
  signInUrl: (returnUrl = window.location.pathname) =>
    `/api/identity/sign-in?returnUrl=${encodeURIComponent(returnUrl)}`,
};
