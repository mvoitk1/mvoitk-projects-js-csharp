"use client";

import { useEffect } from "react";
import styles from "./RouteStates.module.css";

export function RouteError({
  title,
  error,
  retry,
}: {
  title: string;
  error: Error & { digest?: string };
  retry: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <div className={styles.wrap} role="alert">
      <p className={styles.errorTitle}>{title}</p>
      <p className={styles.errorMessage}>{error.message || "Unexpected error."}</p>
      {error.digest ? (
        <p className={styles.errorDigest}>ref: {error.digest}</p>
      ) : null}
      <button type="button" className={styles.button} onClick={retry}>
        Try again
      </button>
    </div>
  );
}
