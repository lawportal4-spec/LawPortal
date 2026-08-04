import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { api } from "./api";
import type { AuthResultDto } from "./authApi";

/** Same localStorage-token stopgap as every other app in this project — see web-client's
 * authContext for the shared caveat (not the plan's target HttpOnly-cookie shape yet). */
const ACCESS_TOKEN_KEY = "lp_admin_access_token";
const REFRESH_TOKEN_KEY = "lp_admin_refresh_token";

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(ACCESS_TOKEN_KEY);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// See web-client's identical comment: a 401 while a token exists means the session genuinely
// expired, not a login-page wrong-password 401 (which never had a token to begin with).
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
