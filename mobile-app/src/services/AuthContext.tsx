import React, { createContext, useContext, useEffect, useState } from 'react';
import * as SecureStore from 'expo-secure-store';
import { authApi } from '../services/api';
import { statusHub } from '../services/signalr';

interface AuthUser {
  username: string;
  fullName: string;
  role: 'ADMIN' | 'ENGINEER' | 'VIEWER';
}

interface AuthContextValue {
  user: AuthUser | null;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser]         = useState<AuthUser | null>(null);
  const [isLoading, setLoading] = useState(true);

  useEffect(() => {
    // Restore session from secure storage on app launch
    (async () => {
      try {
        const stored = await SecureStore.getItemAsync('auth_user');
        if (stored) {
          setUser(JSON.parse(stored));
          await statusHub.connect();
        }
      } catch {
        // Corrupted storage — start fresh
        await SecureStore.deleteItemAsync('auth_token');
        await SecureStore.deleteItemAsync('auth_user');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const login = async (username: string, password: string) => {
    const data = await authApi.login(username, password);
    const authUser: AuthUser = {
      username: data.username,
      fullName: data.fullName,
      role: data.role as AuthUser['role'],
    };
    await SecureStore.setItemAsync('auth_token', data.token);
    await SecureStore.setItemAsync('auth_user', JSON.stringify(authUser));
    setUser(authUser);
    await statusHub.connect();
  };

  const logout = async () => {
    await statusHub.disconnect();
    await SecureStore.deleteItemAsync('auth_token');
    await SecureStore.deleteItemAsync('auth_user');
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
