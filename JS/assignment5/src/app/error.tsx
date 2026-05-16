"use client";

import { RouteError } from "@/components/layout/RouteError";

export default function RootError({
  error,
  unstable_retry,
}: {
  error: Error & { digest?: string };
  unstable_retry: () => void;
}) {
  return (
    <RouteError
      title="Something went wrong"
      error={error}
      retry={unstable_retry}
    />
  );
}
