"use server";

import { redirect } from "next/navigation";
import { login, register } from "@/lib/api/endpoints";
import { clearTokens, writeTokens } from "@/lib/auth/tokens";

// Shape returned to useActionState. `error` is the form-level message; per-field
// errors live in `fieldErrors` so the UI can show them inline.
export type AuthFormState = {
  error?: string;
  fieldErrors?: Partial<Record<"email" | "password" | "firstName" | "lastName", string>>;
};

function readString(data: FormData, name: string): string {
  const v = data.get(name);
  return typeof v === "string" ? v.trim() : "";
}

function validateLogin(email: string, password: string): AuthFormState | null {
  const fieldErrors: AuthFormState["fieldErrors"] = {};
  if (!email) fieldErrors.email = "Email is required";
  else if (!email.includes("@")) fieldErrors.email = "Enter a valid email";
  if (!password) fieldErrors.password = "Password is required";
  return Object.keys(fieldErrors).length ? { fieldErrors } : null;
}

function validateRegister(s: {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}): AuthFormState | null {
  const fieldErrors: AuthFormState["fieldErrors"] = {};
  if (!s.email) fieldErrors.email = "Email is required";
  else if (!s.email.includes("@")) fieldErrors.email = "Enter a valid email";
  if (!s.password) fieldErrors.password = "Password is required";
  else if (s.password.length < 6)
    fieldErrors.password = "Password must be at least 6 characters";
  if (!s.firstName) fieldErrors.firstName = "First name is required";
  if (!s.lastName) fieldErrors.lastName = "Last name is required";
  return Object.keys(fieldErrors).length ? { fieldErrors } : null;
}

export async function loginAction(
  _prev: AuthFormState,
  formData: FormData,
): Promise<AuthFormState> {
  const email = readString(formData, "email");
  const password = readString(formData, "password");

  const invalid = validateLogin(email, password);
  if (invalid) return invalid;

  const result = await login({ email, password });
  if (!result.ok) {
    return {
      error:
        result.error.status === 404 || result.error.status === 401
          ? "Invalid email or password"
          : result.error.message || "Login failed",
    };
  }
  await writeTokens(result.data.token, result.data.refreshToken);
  redirect("/todos");
}

export async function registerAction(
  _prev: AuthFormState,
  formData: FormData,
): Promise<AuthFormState> {
  const fields = {
    email: readString(formData, "email"),
    password: readString(formData, "password"),
    firstName: readString(formData, "firstName"),
    lastName: readString(formData, "lastName"),
  };

  const invalid = validateRegister(fields);
  if (invalid) return invalid;

  const result = await register(fields);
  if (!result.ok) {
    return {
      error: result.error.message || "Registration failed",
    };
  }
  await writeTokens(result.data.token, result.data.refreshToken);
  redirect("/todos");
}

export async function logoutAction(): Promise<void> {
  await clearTokens();
  redirect("/login");
}
