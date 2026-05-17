import type { TokenPair } from "../auth.js";

export interface AccountTokenResponse {
  token: string;
  refreshToken: string;
  firstName: string | null;
  lastName: string | null;
}

export function tokenView(pair: TokenPair): AccountTokenResponse {
  return {
    token: pair.token,
    refreshToken: pair.refreshToken,
    firstName: pair.firstName,
    lastName: pair.lastName,
  };
}
