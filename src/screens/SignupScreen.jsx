import { StyleSheet, Text, TextInput, View, TouchableOpacity } from 'react-native'
import React, { useState } from 'react'
import { useNavigation } from '@react-navigation/native'

import ScreenWrapper from '../components/ScreenWrapper'

const SignupScreen = () => {
  const [phoneNumber, setPhoneNumber] = useState('03148398912');
  const [showButton, setShowButton] = useState(false);
  const navigation = useNavigation();
  const handleLogin = () => {
    navigation.navigate('Code');
    console.log(" your code is 123456")
  };
  return (
    <ScreenWrapper style={styles.container}>
      <Text style={styles.signupText}> Sign Up </Text>
      <View style={styles.contentContainer}>

        <Text style={styles.welcomeText}>Welcome to</Text>
        <Text style={styles.brandText}>Stizi</Text>

        <View style={styles.inputWrapper}>
          <View style={styles.countryCodeContainer}>
            <Text style={styles.countryCodeText}>+1</Text>
          </View>
          <TextInput
            placeholder="Phone Number"
            style={styles.input}
            placeholderTextColor="rgba(255,255,255,0.7)"
            value={phoneNumber}
            onChangeText={setPhoneNumber}
            keyboardType="numeric"
            onFocus={() => setShowButton(true)}
          />
        </View>

        {showButton && (
          <TouchableOpacity
            style={styles.button}
            onPress={() => handleLogin()}
          >
            <Text style={styles.buttonText}>Get code</Text>
          </TouchableOpacity>
        )}
        <TouchableOpacity style={styles.loginButton} onPress={() => navigation.navigate('Login')}>
          <Text style={styles.buttonText} >Already have an account?</Text>
        </TouchableOpacity>
      </View>
    </ScreenWrapper>
  )
}

export default SignupScreen

const styles = StyleSheet.create({
  container: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  contentContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    width: '100%',
    paddingHorizontal: 20,
  },
  welcomeText: {
    fontSize: 20,
    color: '#ffffff',
    marginBottom: 10,
    fontWeight: '500',
  },
  brandText: {
    fontSize: 64,
    fontWeight: 'bold',
    color: '#f8f8f8ff',
    marginBottom: 60,
    fontStyle: 'italic',
  },
  signupText: {
    fontSize: 64,
    fontWeight: 'bold',
    color: '#f8f8f8ff',
    marginTop: 60,
    fontStyle: 'italic',
  },
  loginButton: {
    backgroundColor: '#ffffffff',
    paddingVertical: 15,
    width: '100%',
    borderRadius: 30,
    alignItems: 'center',
    marginTop: 20,
    bottom: 30,
    position: 'absolute',
  },

  inputWrapper: {
    flexDirection: 'row',
    width: '100%',
    height: 55,
    backgroundColor: 'rgba(255, 255, 255, 0.15)',
    borderRadius: 12,
    overflow: 'hidden',
    marginBottom: 30,
  },
  countryCodeContainer: {
    width: 60,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.2)',
    borderRightWidth: 1,
    borderRightColor: 'rgba(255,255,255,0.1)',
  },
  countryCodeText: {
    color: '#ffffff',
    fontSize: 18,
    fontWeight: 'bold',
  },
  input: {
    flex: 1,
    color: '#ffffff',
    fontSize: 18,
    paddingHorizontal: 15,
    fontWeight: 'bold',
  },
  button: {
    backgroundColor: '#ffffffff',
    paddingVertical: 15,
    width: '100%',
    borderRadius: 30,
    alignItems: 'center',
    marginTop: 20,
  },
  buttonText: {
    color: '#a75cc2ff',
    fontSize: 18,
    fontWeight: 'bold',
  },
})