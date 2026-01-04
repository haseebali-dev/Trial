import { StyleSheet, Text, View } from 'react-native'
import React from 'react'
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import SplashScreen from './../screens/SplashScreen';
import SignupScreen from './../screens/SignupScreen';
import HomeScreen from './../screens/HomeScreen';
import LoginScreen from './../screens/LoginScreen';
import MapScreen from './../screens/MapScreen';
import CameraScreen from './../screens/CameraScreen';

import CodeScreen from './../screens/CodeScreen';

const Stack = createNativeStackNavigator();
const NavigationsStack = () => {
  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        <Stack.Screen name="Splash" component={SplashScreen} />
        <Stack.Screen name="Signup" component={SignupScreen} />
        <Stack.Screen name="Code" component={CodeScreen} />
        <Stack.Screen name="Home" component={HomeScreen} />
        <Stack.Screen name="Camera" component={CameraScreen} />
        <Stack.Screen name="Login" component={LoginScreen} />
        <Stack.Screen name="Map" component={MapScreen} />
      </Stack.Navigator>
    </NavigationContainer>
  )
}

export default NavigationsStack

const styles = StyleSheet.create({})