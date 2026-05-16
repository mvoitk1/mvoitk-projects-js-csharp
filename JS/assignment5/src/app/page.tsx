import { redirect } from "next/navigation";
import { readTokens } from "@/lib/auth/tokens";

export default async function Home() {
  const { at } = await readTokens();
  redirect(at ? "/todos" : "/login");
}
