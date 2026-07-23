import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useNavigate } from "react-router-dom";
import * as usersApi from "../api/users";
import { isApiError } from "../lib/apiError";
import { setUnauthorizedHandler } from "../lib/apiClient";
import type { RegisterPayload, UserProfile } from "../types/user";

interface AuthContextValue {
  user: UserProfile | null;
  isAuthenticated: boolean;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (payload: RegisterPayload) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate();
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);

  const refreshUser = useCallback(async () => {
    try {
      const profile = await usersApi.fetchCurrentUser();
      setUser(profile);
    } catch (err) {
      if (isApiError(err) && err.status === 401) {
        setUser(null);
        return;
      }

      throw err;
    }
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(() => {
      setUser(null);
      const path = window.location.pathname;
      const publicAuthPaths = [
        "/login",
        "/register",
        "/forgot-password",
        "/reset-password",
        "/verify-email",
      ];
      if (publicAuthPaths.some((p) => path === p || path.startsWith(`${p}/`))) {
        return;
      }

      navigate("/login", { replace: true });
    });

    return () => setUnauthorizedHandler(null);
  }, [navigate]);

  useEffect(() => {
    async function bootstrap() {
      try {
        await refreshUser();
      } catch (err) {
        if (isApiError(err) && err.status === 401) {
          setUser(null);
        }
      } finally {
        setLoading(false);
      }
    }

    void bootstrap();
  }, [refreshUser]);

  const login = useCallback(async (email: string, password: string) => {
    const response = await usersApi.login(email, password);
    setUser(response.user);
  }, []);

  const register = useCallback(async (payload: RegisterPayload) => {
    await usersApi.register(payload);
  }, []);

  const logout = useCallback(async () => {
    try {
      await usersApi.logout();
    } finally {
      setUser(null);
    }
  }, []);

  const value = useMemo(
    () => ({
      user,
      isAuthenticated: user !== null,
      loading,
      login,
      register,
      logout,
      refreshUser,
    }),
    [user, loading, login, register, logout, refreshUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return context;
}
