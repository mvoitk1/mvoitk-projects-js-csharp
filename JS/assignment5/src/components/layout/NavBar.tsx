"use client";

import Link from "next/link";
import { logoutAction } from "@/app/actions/auth";
import { useAuth } from "@/state/auth/AuthContext";
import styles from "./NavBar.module.css";

export function NavBar() {
  const { state } = useAuth();

  return (
    <nav className={styles.nav}>
      <div className={styles.links}>
        <Link className={styles.link} href="/todos">
          Todos
        </Link>
        <Link className={styles.link} href="/categories">
          Categories
        </Link>
        <Link className={styles.link} href="/priorities">
          Priorities
        </Link>
      </div>
      <div className={styles.spacer} />
      {state.status === "authed" && (
        <span className={styles.user}>{state.user.email}</span>
      )}
      <form action={logoutAction}>
        <button type="submit" className={styles.logout}>
          Log out
        </button>
      </form>
    </nav>
  );
}
