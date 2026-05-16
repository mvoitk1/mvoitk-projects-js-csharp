export interface AuthUser {
  email: string;
  firstName: string | null;
  lastName: string | null;
}

export type AuthState =
  | { status: "anon"; user: null }
  | { status: "authed"; user: AuthUser };

export type AuthAction =
  | { type: "HYDRATE"; user: AuthUser | null }
  | { type: "CLEAR" };

export const anonState: AuthState = { status: "anon", user: null };

export function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case "HYDRATE":
      return action.user ? { status: "authed", user: action.user } : anonState;
    case "CLEAR":
      return anonState;
    default:
      return state;
  }
}
