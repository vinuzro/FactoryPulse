import React, { useEffect, useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity,
  StyleSheet, ScrollView, Alert, ActivityIndicator,
} from 'react-native';
import { Picker } from '@react-native-picker/picker';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { Equipment, equipmentApi, inspectionApi } from '../services/api';

type Result = 'Pass' | 'Fail' | 'NeedsAttention';

const RESULTS: { label: string; value: Result; color: string }[] = [
  { label: '✅ Pass',            value: 'Pass',           color: '#27ae60' },
  { label: '❌ Fail',            value: 'Fail',           color: '#c0392b' },
  { label: '⚠️  Needs Attention', value: 'NeedsAttention', color: '#e67e22' },
];

export default function NewInspectionScreen() {
  const router  = useRouter();
  const params  = useLocalSearchParams<{ equipmentId?: string }>();

  const [equipment,   setEquipment]   = useState<Equipment[]>([]);
  const [equipmentId, setEquipmentId] = useState<number | null>(
    params.equipmentId ? parseInt(params.equipmentId) : null
  );
  const [result, setResult] = useState<Result>('Pass');
  const [notes,  setNotes]  = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    equipmentApi.getAll().then(setEquipment).catch(console.warn);
  }, []);

  const handleSubmit = async () => {
    if (!equipmentId) {
      Alert.alert('Validation', 'Please select a piece of equipment.');
      return;
    }

    setSaving(true);
    try {
      await inspectionApi.create({
        equipmentId,
        result,
        notes: notes.trim() || undefined,
      });
      Alert.alert('Submitted', 'Inspection logged successfully.', [
        { text: 'OK', onPress: () => router.back() },
      ]);
    } catch (err: any) {
      Alert.alert('Error', err.response?.data?.error ?? 'Failed to submit inspection.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.sectionLabel}>Equipment</Text>
      <View style={styles.pickerWrapper}>
        <Picker
          selectedValue={equipmentId?.toString() ?? ''}
          onValueChange={(val) => setEquipmentId(val ? parseInt(val) : null)}
          style={styles.picker}
          dropdownIconColor="#8899aa"
        >
          <Picker.Item label="Select equipment..." value="" color="#8899aa" />
          {equipment.map((eq) => (
            <Picker.Item key={eq.id} label={`${eq.name} — ${eq.location}`} value={eq.id.toString()} color="#ffffff" />
          ))}
        </Picker>
      </View>

      <Text style={styles.sectionLabel}>Result</Text>
      <View style={styles.resultRow}>
        {RESULTS.map((r) => (
          <TouchableOpacity
            key={r.value}
            style={[
              styles.resultButton,
              result === r.value && { backgroundColor: r.color, borderColor: r.color },
            ]}
            onPress={() => setResult(r.value)}
          >
            <Text style={[styles.resultText, result === r.value && { color: '#fff' }]}>
              {r.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <Text style={styles.sectionLabel}>Notes (optional)</Text>
      <TextInput
        style={styles.notesInput}
        placeholder="Any observations, issues, or follow-up actions..."
        placeholderTextColor="#55677a"
        multiline
        numberOfLines={4}
        value={notes}
        onChangeText={setNotes}
        editable={!saving}
        textAlignVertical="top"
      />

      <TouchableOpacity
        style={[styles.submitButton, saving && styles.submitDisabled]}
        onPress={handleSubmit}
        disabled={saving}
      >
        {saving
          ? <ActivityIndicator color="#fff" />
          : <Text style={styles.submitText}>Submit Inspection</Text>}
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#0f1923' },
  content:   { padding: 20, paddingBottom: 40 },
  sectionLabel: {
    color: '#8899aa',
    fontSize: 12,
    fontWeight: '600',
    textTransform: 'uppercase',
    letterSpacing: 1,
    marginBottom: 8,
    marginTop: 20,
  },
  pickerWrapper: {
    backgroundColor: '#1a2634',
    borderRadius: 10,
    borderWidth: 1,
    borderColor: '#2a3a4a',
    overflow: 'hidden',
  },
  picker: { color: '#ffffff', height: 52 },
  resultRow: { flexDirection: 'column', gap: 10 },
  resultButton: {
    borderWidth: 1.5,
    borderColor: '#2a3a4a',
    borderRadius: 10,
    paddingVertical: 14,
    paddingHorizontal: 16,
  },
  resultText: { color: '#aabbcc', fontSize: 15, fontWeight: '500' },
  notesInput: {
    backgroundColor: '#1a2634',
    borderRadius: 10,
    borderWidth: 1,
    borderColor: '#2a3a4a',
    padding: 14,
    color: '#ffffff',
    fontSize: 14,
    minHeight: 110,
  },
  submitButton: {
    backgroundColor: '#1e6fbf',
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
    marginTop: 30,
  },
  submitDisabled: { opacity: 0.6 },
  submitText: { color: '#fff', fontSize: 16, fontWeight: '600' },
});
