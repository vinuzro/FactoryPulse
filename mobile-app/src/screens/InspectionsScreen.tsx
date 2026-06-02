import React, { useCallback, useEffect, useState } from 'react';
import {
  View, Text, FlatList, TouchableOpacity,
  StyleSheet, RefreshControl, ActivityIndicator,
} from 'react-native';
import { useRouter } from 'expo-router';
import { Inspection, inspectionApi } from '../services/api';
import { statusHub, InspectionSubmittedEvent } from '../services/signalr';

const RESULT_COLORS: Record<string, string> = {
  Pass:           '#27ae60',
  Fail:           '#c0392b',
  NeedsAttention: '#e67e22',
};

const RESULT_LABELS: Record<string, string> = {
  Pass:           'Pass',
  Fail:           'Fail',
  NeedsAttention: 'Needs Attention',
};

export default function InspectionsScreen() {
  const router = useRouter();
  const [inspections, setInspections] = useState<Inspection[]>([]);
  const [loading,     setLoading]     = useState(true);
  const [refreshing,  setRefreshing]  = useState(false);

  const load = useCallback(async (showRefresh = false) => {
    if (showRefresh) setRefreshing(true);
    try {
      const data = await inspectionApi.getAll();
      setInspections(data);
    } catch (err) {
      console.warn('Failed to load inspections:', err);
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    load();

    // Prepend new inspections as they come in live
    const onNewInspection = (evt: InspectionSubmittedEvent) => {
      // Reload to get full data — could optimistically insert but this keeps it simple
      load();
    };

    statusHub.on<InspectionSubmittedEvent>('InspectionSubmitted', onNewInspection);
    return () => statusHub.off('InspectionSubmitted', onNewInspection);
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
        data={inspections}
        keyExtractor={(item) => item.id.toString()}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={() => load(true)} tintColor="#1e6fbf" />
        }
        contentContainerStyle={styles.list}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <Text style={styles.equipmentName}>
                {item.equipment?.name ?? `Equipment #${item.equipmentId}`}
              </Text>
              <View style={[styles.badge, { backgroundColor: RESULT_COLORS[item.result] ?? '#555' }]}>
                <Text style={styles.badgeText}>{RESULT_LABELS[item.result] ?? item.result}</Text>
              </View>
            </View>
            <Text style={styles.meta}>
              👷 {item.inspectorName}
            </Text>
            {item.notes ? (
              <Text style={styles.notes} numberOfLines={2}>{item.notes}</Text>
            ) : null}
            <Text style={styles.date}>
              {new Date(item.createdAt).toLocaleString()}
            </Text>
          </View>
        )}
        ListEmptyComponent={
          <Text style={styles.empty}>No inspections found.</Text>
        }
      />

      <TouchableOpacity
        style={styles.fab}
        onPress={() => router.push('/inspections/new')}
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0f1923' },
  centered:  { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: '#0f1923' },
  list:      { padding: 16, gap: 12, paddingBottom: 100 },
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
    fontSize: 15,
    fontWeight: '600',
    color: '#ffffff',
    flex: 1,
    marginRight: 8,
  },
  badge: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: 20,
  },
  badgeText: { color: '#fff', fontSize: 12, fontWeight: '600' },
  meta:  { color: '#8899aa', fontSize: 13, marginBottom: 4 },
  notes: { color: '#aabbcc', fontSize: 13, marginBottom: 4, fontStyle: 'italic' },
  date:  { color: '#55677a', fontSize: 12 },
  empty: { color: '#8899aa', textAlign: 'center', marginTop: 60, fontSize: 15 },
  fab: {
    position: 'absolute',
    bottom: 24,
    right: 24,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: '#1e6fbf',
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOpacity: 0.4,
    shadowRadius: 8,
    elevation: 6,
  },
  fabText: { color: '#fff', fontSize: 28, lineHeight: 32 },
});
