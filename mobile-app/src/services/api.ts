import axios from 'axios';
import * as SecureStore from 'expo-secure-store';

const API_BASE = process.env.EXPO_PUBLIC_API_URL ?? 'http://10.0.2.2:5000'; // Android emulator localhost

const api = axios.create({ baseURL: API_BASE });

// Attach JWT to every request
api.interceptors.request.use(async (config) => {
  const token = await SecureStore.getItemAsync('auth_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Global 401 handler — boot back to login
api.interceptors.response.use(
  (res) => res,
  async (err) => {
    if (err.response?.status === 401) {
      await SecureStore.deleteItemAsync('auth_token');
      // Navigation reset handled by auth context listener
    }
    return Promise.reject(err);
  }
);

export const authApi = {
  login: async (username: string, password: string) => {
    const res = await api.post('/api/auth/login', { username, password });
    return res.data as { token: string; username: string; fullName: string; role: string };
  },
};

export const equipmentApi = {
  getAll: async () => {
    const res = await api.get('/api/equipment');
    return res.data as Equipment[];
  },

  updateStatus: async (id: number, status: string) => {
    const res = await api.put(`/api/equipment/${id}/status`, { status });
    return res.data as Equipment;
  },
};

export const inspectionApi = {
  getAll: async (equipmentId?: number) => {
    const params = equipmentId ? { equipmentId } : {};
    const res = await api.get('/api/inspections', { params });
    return res.data as Inspection[];
  },

  create: async (payload: CreateInspectionPayload) => {
    const res = await api.post('/api/inspections', payload);
    return res.data as Inspection;
  },
};

// --- Types ---

export interface Equipment {
  id: number;
  name: string;
  location: string;
  status: 'Online' | 'Offline' | 'Maintenance' | 'Fault';
  lastUpdated: string;
}

export interface Inspection {
  id: number;
  equipmentId: number;
  inspectorName: string;
  result: 'Pass' | 'Fail' | 'NeedsAttention';
  notes: string | null;
  createdAt: string;
  equipment?: Equipment;
}

export interface CreateInspectionPayload {
  equipmentId: number;
  result: 'Pass' | 'Fail' | 'NeedsAttention';
  notes?: string;
}

export default api;
