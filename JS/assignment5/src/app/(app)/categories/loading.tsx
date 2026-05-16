import { RouteSkeleton } from "@/components/layout/RouteSkeleton";

export default function Loading() {
  return <RouteSkeleton title="Loading categories…" rows={5} />;
}
