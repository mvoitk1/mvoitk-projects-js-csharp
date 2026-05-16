"use client";

import { createContext, useContext, useReducer, type ReactNode } from "react";
import {
  anonState,
  authReducer,
  type AuthAction,
  type AuthState,
  type AuthUser,
} from "./authReducer";

interface AuthContextValue {
  state: AuthState;
  dispatch: (action: AuthAction) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({
  initialUser,
  children,
}: {
  initialUser: AuthUser | null;
  children: ReactNode;
}) {
  const [state, dispatch] = useReducer(
    authReducer,
    initialUser ? { status: "authed" as const, user: initialUser } : anonState,
  );
  return (
    <AuthContext.Provider value={{ state, dispatch }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
