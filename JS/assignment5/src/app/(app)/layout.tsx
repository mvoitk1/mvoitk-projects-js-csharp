import { getSession } from "@/lib/auth/session";
import { NavBar } from "@/components/layout/NavBar";
import { AuthProvider } from "@/state/auth/AuthContext";

export default async function AppLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getSession();
  const initialUser = session.email
    ? {
        email: session.email,
        firstName: session.firstName,
        lastName: session.lastName,
      }
    : null;

  return (
    <AuthProvider initialUser={initialUser}>
      <NavBar />
      {children}
    </AuthProvider>
  );
}
