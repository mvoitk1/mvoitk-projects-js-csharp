"use client";

import { RouteError } from "@/components/layout/RouteError";

export default function PrioritiesError({
  error,
  unstable_retry,
}: {
  error: Error & { digest?: string };
  unstable_retry: () => void;
}) {
  return (
    <RouteError
      title="Couldn’t load priorities"
      error={error}
      retry={unstable_retry}
    />
  );
}
