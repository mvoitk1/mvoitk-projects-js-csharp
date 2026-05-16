import styles from "./RouteStates.module.css";

export function RouteSkeleton({
  title,
  rows = 5,
}: {
  title: string;
  rows?: number;
}) {
  return (
    <div className={styles.wrap} aria-busy="true" aria-live="polite">
      <p className={styles.title}>{title}</p>
      {Array.from({ length: rows }, (_, i) => (
        <div key={i} className={styles.skeletonRow} />
      ))}
    </div>
  );
}
