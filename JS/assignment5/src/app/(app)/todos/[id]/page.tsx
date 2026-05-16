import { notFound, redirect } from "next/navigation";
import { Unauthorized } from "@/lib/api/client";
import {
  getTask,
  listCategories,
  listPriorities,
} from "@/lib/api/endpoints";
import { TodoEditForm } from "@/components/todos/TodoEditForm";

export default async function TodoEditPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  let task, categories, priorities;
  try {
    [task, categories, priorities] = await Promise.all([
      getTask(id),
      listCategories(),
      listPriorities(),
    ]);
  } catch (err) {
    if (err instanceof Unauthorized) redirect("/login");
    throw err;
  }

  if (!task.ok) {
    if (task.error.status === 404) notFound();
    throw new Error(task.error.message);
  }
  if (!categories.ok) throw new Error(categories.error.message);
  if (!priorities.ok) throw new Error(priorities.error.message);

  return (
    <main>
      <h1 style={{ padding: "1.25rem 1.25rem 0" }}>Edit task</h1>
      <TodoEditForm
        task={task.data}
        categories={categories.data}
        priorities={priorities.data}
      />
    </main>
  );
}
