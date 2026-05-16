import { redirect } from "next/navigation";
import { Unauthorized } from "@/lib/api/client";
import { listPriorities } from "@/lib/api/endpoints";
import { PrioritiesTable } from "@/components/crud/PrioritiesTable";

export default async function PrioritiesPage() {
  let result;
  try {
    result = await listPriorities();
  } catch (err) {
    if (err instanceof Unauthorized) redirect("/login");
    throw err;
  }
  if (!result.ok) throw new Error(result.error.message);

  return <PrioritiesTable initial={result.data} />;
}
