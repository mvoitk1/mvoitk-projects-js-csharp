import { redirect } from "next/navigation";
import { Unauthorized } from "@/lib/api/client";
import { listCategories } from "@/lib/api/endpoints";
import { CategoriesTable } from "@/components/crud/CategoriesTable";

export default async function CategoriesPage() {
  let result;
  try {
    result = await listCategories();
  } catch (err) {
    if (err instanceof Unauthorized) redirect("/login");
    throw err;
  }
  if (!result.ok) throw new Error(result.error.message);

  return <CategoriesTable initial={result.data} />;
}
