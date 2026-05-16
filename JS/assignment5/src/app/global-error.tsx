"use client";

export default function GlobalError({
  error,
  unstable_retry,
}: {
  error: Error & { digest?: string };
  unstable_retry: () => void;
}) {
  return (
    <html lang="en">
      <body
        style={{
          fontFamily: "Arial, Helvetica, sans-serif",
          padding: "1.5rem",
          display: "flex",
          flexDirection: "column",
          gap: "0.75rem",
        }}
      >
        <h2 style={{ color: "#c0392b", margin: 0 }}>
          The application failed to render
        </h2>
        <p style={{ margin: 0 }}>{error.message || "Unexpected error."}</p>
        {error.digest ? (
          <p
            style={{
              margin: 0,
              fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace",
              opacity: 0.6,
              fontSize: "0.8rem",
            }}
          >
            ref: {error.digest}
          </p>
        ) : null}
        <button
          type="button"
          onClick={unstable_retry}
          style={{
            alignSelf: "flex-start",
            padding: "0.35rem 0.8rem",
            font: "inherit",
            cursor: "pointer",
          }}
        >
          Try again
        </button>
      </body>
    </html>
  );
}
