"use client";

import Link from "next/link";
import { useActionState } from "react";
import { loginAction, type AuthFormState } from "@/app/actions/auth";
import styles from "./AuthForm.module.css";

const initialState: AuthFormState = {};

export function LoginForm() {
  const [state, formAction, pending] = useActionState(loginAction, initialState);

  return (
    <form className={styles.wrap} action={formAction}>
      <h1 className={styles.title}>Log in</h1>

      <div className={styles.field}>
        <label htmlFor="email">Email</label>
        <input id="email" name="email" type="email" autoComplete="email" required />
        {state.fieldErrors?.email && (
          <span className={styles.fieldError}>{state.fieldErrors.email}</span>
        )}
      </div>

      <div className={styles.field}>
        <label htmlFor="password">Password</label>
        <input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
        />
        {state.fieldErrors?.password && (
          <span className={styles.fieldError}>{state.fieldErrors.password}</span>
        )}
      </div>

      {state.error && (
        <p className={styles.error} aria-live="polite">
          {state.error}
        </p>
      )}

      <button type="submit" className={styles.button} disabled={pending}>
        {pending ? "Logging in…" : "Log in"}
      </button>

      <p className={styles.footer}>
        No account? <Link href="/register">Register</Link>
      </p>
    </form>
  );
}
