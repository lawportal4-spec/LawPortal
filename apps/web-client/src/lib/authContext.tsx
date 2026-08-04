import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { api } from "./api";
import type { AuthResultDto } from "./authApi";

/**
 * Tokens live in localStorage for now, not the plan's target shape (refresh token in an
 * HttpOnly cookie, access token in memory only) — that shape needs cookie-issuing endpoints
 * that don't exist yet. This is the P3 client-login stopgap; harden before real case documents
 * flow through this app, matching how P1's OTP sender and reCAPTCHA verifier are dev-only stubs.
 */
const ACCESS_TOKEN_KEY = "lp_access_token";
const REFRESH_TOKEN_KEY = "lp_refresh_token";

// Registered once at module scope, not inside a component effect: an effect only runs after
// its component mounts, and React fires child effects before parent effects — so a query
// fired from deep in the tree on first render could race ahead of interceptor registration
// and go out with no Authorization header. Reading the token fresh from localStorage per
// request means this never needs to depend on component lifecycle at all.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem(ACCESS_TOKEN_KEY);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// A 401 while a token exists means the session genuinely expired (access tokens live 15
// minutes) — without this, the app was showing a permanently blank page on every query after
// expiry: RequireAuth only checks "does a token exist in storage", never whether it's still
// valid, so nothing else was ever going to notice or recover. Guarding on "a token existed"
// (rather than reacting to every 401) keeps this from also firing on a plain wrong-password
// login attempt, which legitimately 401s with no token ever having been set.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && localStorage.getItem(ACCESS_TOKEN_KEY)) {
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      if (window.location.pathname !== "/login") {
        window.location.assign("/login");
      }
    }
    return Promise.reject(error);
  },
);

interface AuthContextValue {
  isAuthenticated: boolean;
  login: (result: AuthResultDto) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(() => localStorage.getItem(ACCESS_TOKEN_KEY));

  const value = useMemo<AuthContextValue>(
    () => ({
      isAuthenticated: accessToken != null,
      login: (result) => {
        localStorage.setItem(ACCESS_TOKEN_KEY, result.accessToken);
        localStorage.setItem(REFRESH_TOKEN_KEY, result.refreshToken);
        setAccessToken(result.accessToken);
      },
      logout: () => {
        localStorage.removeItem(ACCESS_TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        setAccessToken(null);
      },
    }),
    [accessToken],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
