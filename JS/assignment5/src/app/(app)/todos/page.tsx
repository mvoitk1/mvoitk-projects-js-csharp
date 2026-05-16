import { redirect } from "next/navigation";
import { listCategories, listPriorities, listTasks } from "@/lib/api/endpoints";
import { Unauthorized } from "@/lib/api/client";
import { TodoList } from "@/components/todos/TodoList";
import { TodosProvider } from "@/state/todos/TodosContext";

export default async function TodosPage() {
  let tasks, categories, priorities;
  try {
    [tasks, categories, priorities] = await Promise.all([
      listTasks(),
      listCategories(),
      listPriorities(),
    ]);
  } catch (err) {
    if (err instanceof Unauthorized) redirect("/login");
    throw err;
  }

  if (!tasks.ok || !categories.ok || !priorities.ok) {
    const first =
      (!tasks.ok && tasks.error) ||
      (!categories.ok && categories.error) ||
      (!priorities.ok && priorities.error);
    throw new Error(first ? first.message : "Failed to load todos");
  }

  return (
    <TodosProvider
      initialTasks={tasks.data}
      categories={categories.data}
      priorities={priorities.data}
    >
      <TodoList />
    </TodosProvider>
  );
}
