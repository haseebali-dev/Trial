import { NavigationContainer } from '@react-navigation/native';
import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Text } from 'react-native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import NavigationsStack from './src/navigation/NavigationsStack';

const Stack = createNativeStackNavigator();
function App() {
  return (
    <NavigationsStack />
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});

export default App;
