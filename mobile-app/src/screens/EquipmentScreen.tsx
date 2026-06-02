import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, TouchableOpacity,
  StyleSheet, RefreshControl, ActivityIndicator,
} from 'react-native';
import { useRouter } from 'expo-router';
import { Equipment, equipmentApi } from '../services/api';
import { statusHub, EquipmentStatusEvent } from '../services/signalr';

const STATUS_COLORS: Record<string, string> = {
  Online:      '#27ae60',
  Offline:     '#7f8c8d',
  Maintenance: '#e67e22',
  Fault:       '#c0392b',
};

export default function EquipmentScreen() {
  const router = useRouter();
  const [equipment, setEquipment] = useState<Equipment[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async (showRefresh = false) => {
    if (showRefresh) setRefreshing(true);
    try {
      const data = await equipmentApi.getAll();
      setEquipment(data);
    } catch (err) {
      console.warn('Failed to load equipment:', err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    load();

    // Live status updates from SignalR
    const onStatusChanged = (evt: EquipmentStatusEvent) => {
      setEquipment((prev) =>
        prev.map((eq) =>
          eq.id === evt.equipmentId
            ? { ...eq, status: evt.newStatus as Equipment['status'], lastUpdated: evt.timestamp }
            : eq
        )
      );
    };

    statusHub.on<EquipmentStatusEvent>('EquipmentStatusChanged', onStatusChanged);

    return () => {
      statusHub.off('EquipmentStatusChanged', onStatusChanged);
    };
  }, [load]);

  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color="#1e6fbf" />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <FlatList
        data={equipment}
        keyExtractor={(item) => item.id.toString()}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={() => load(true)} tintColor="#1e6fbf" />
        }
        contentContainerStyle={styles.list}
        renderItem={({ item }) => (
          <TouchableOpacity
            style={styles.card}
            onPress={() => router.push(`/equipment/${item.id}`)}
            activeOpacity={0.75}
          >
            <View style={styles.cardHeader}>
              <Text style={styles.equipmentName}>{item.name}</Text>
              <View style={[styles.statusBadge, { backgroundColor: STATUS_COLORS[item.status] ?? '#555' }]}>
                <Text style={styles.statusText}>{item.status}</Text>
              </View>
            </View>
            <Text style={styles.location}>📍 {item.location}</Text>
            <Text style={styles.lastUpdated}>
              Updated {new Date(item.lastUpdated).toLocaleString()}
            </Text>
          </TouchableOpacity>
        )}
        ListEmptyComponent={
          <Text style={styles.empty}>No equipment found.</Text>
        }
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container:  { flex: 1, backgroundColor: '#0f1923' },
  centered:   { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: '#0f1923' },
  list:       { padding: 16, gap: 12 },
  card: {
    backgroundColor: '#1a2634',
    borderRadius: 12,
    padding: 16,
    borderWidth: 1,
    borderColor: '#2a3a4a',
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  equipmentName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#ffffff',
    flex: 1,
    marginRight: 8,
  },
  statusBadge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 20,
  },
  statusText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
  location: {
    color: '#8899aa',
    fontSize: 13,
    marginBottom: 4,
  },
  lastUpdated: {
    color: '#55677a',
    fontSize: 12,
  },
  empty: {
    color: '#8899aa',
    textAlign: 'center',
    marginTop: 60,
    fontSize: 15,
  },
});
