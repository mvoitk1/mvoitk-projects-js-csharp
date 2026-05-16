"use client";

import Link from "next/link";
import { useActionState } from "react";
import { registerAction, type AuthFormState } from "@/app/actions/auth";
import styles from "./AuthForm.module.css";

const initialState: AuthFormState = {};

export function RegisterForm() {
  const [state, formAction, pending] = useActionState(registerAction, initialState);

  return (
    <form className={styles.wrap} action={formAction}>
      <h1 className={styles.title}>Register</h1>

      <div className={styles.field}>
        <label htmlFor="firstName">First name</label>
        <input id="firstName" name="firstName" type="text" autoComplete="given-name" required />
        {state.fieldErrors?.firstName && (
          <span className={styles.fieldError}>{state.fieldErrors.firstName}</span>
        )}
      </div>

      <div className={styles.field}>
        <label htmlFor="lastName">Last name</label>
        <input id="lastName" name="lastName" type="text" autoComplete="family-name" required />
        {state.fieldErrors?.lastName && (
          <span className={styles.fieldError}>{state.fieldErrors.lastName}</span>
        )}
      </div>

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
          autoComplete="new-password"
          required
          minLength={6}
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
        {pending ? "Creating account…" : "Create account"}
      </button>

      <p className={styles.footer}>
        Already have an account? <Link href="/login">Log in</Link>
      </p>
    </form>
  );
}
