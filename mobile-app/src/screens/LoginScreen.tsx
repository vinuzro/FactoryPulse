import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity,
  StyleSheet, KeyboardAvoidingView, Platform, Alert, ActivityIndicator,
} from 'react-native';
import { useAuth } from '../services/AuthContext';
import { useRouter } from 'expo-router';

export default function LoginScreen() {
  const { login } = useAuth();
  const router    = useRouter();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading,  setLoading]  = useState(false);

  const handleLogin = async () => {
    if (!username.trim() || !password.trim()) {
      Alert.alert('Validation', 'Please enter your username and password.');
      return;
    }

    setLoading(true);
    try {
      await login(username.trim(), password);
      router.replace('/(tabs)/equipment');
    } catch (err: any) {
      const msg = err.response?.data?.error ?? 'Login failed. Check your credentials.';
      Alert.alert('Login Failed', msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <View style={styles.card}>
        <Text style={styles.logo}>⚙️ FactoryPulse</Text>
        <Text style={styles.subtitle}>Factory Monitoring System</Text>

        <TextInput
          style={styles.input}
          placeholder="Username"
          placeholderTextColor="#999"
          autoCapitalize="none"
          autoCorrect={false}
          value={username}
          onChangeText={setUsername}
          editable={!loading}
        />

        <TextInput
          style={styles.input}
          placeholder="Password"
          placeholderTextColor="#999"
          secureTextEntry
          value={password}
          onChangeText={setPassword}
          editable={!loading}
          onSubmitEditing={handleLogin}
          returnKeyType="go"
        />

        <TouchableOpacity
          style={[styles.button, loading && styles.buttonDisabled]}
          onPress={handleLogin}
          disabled={loading}
        >
          {loading
            ? <ActivityIndicator color="#fff" />
            : <Text style={styles.buttonText}>Sign In</Text>}
        </TouchableOpacity>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0f1923',
    justifyContent: 'center',
    paddingHorizontal: 24,
  },
  card: {
    backgroundColor: '#1a2634',
    borderRadius: 16,
    padding: 28,
    shadowColor: '#000',
    shadowOpacity: 0.4,
    shadowRadius: 12,
    elevation: 8,
  },
  logo: {
    fontSize: 28,
    fontWeight: '700',
    color: '#ffffff',
    textAlign: 'center',
    marginBottom: 6,
  },
  subtitle: {
    fontSize: 13,
    color: '#8899aa',
    textAlign: 'center',
    marginBottom: 32,
  },
  input: {
    backgroundColor: '#0f1923',
    borderRadius: 10,
    paddingHorizontal: 16,
    paddingVertical: 14,
    fontSize: 15,
    color: '#ffffff',
    marginBottom: 14,
    borderWidth: 1,
    borderColor: '#2a3a4a',
  },
  button: {
    backgroundColor: '#1e6fbf',
    borderRadius: 10,
    paddingVertical: 15,
    alignItems: 'center',
    marginTop: 8,
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  buttonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '600',
  },
});
