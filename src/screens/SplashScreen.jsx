import { StyleSheet, Text, View } from 'react-native'
import React, { useEffect } from 'react'
import { useNavigation } from '@react-navigation/native'

import ScreenWrapper from '../components/ScreenWrapper'

const SplashScreen = () => {
  const navigation = useNavigation();

  useEffect(() => {
    const timer = setTimeout(() => {
      navigation.replace('Signup');
    }, 3000);

    return () => clearTimeout(timer);
  }, [navigation]);

  return (
    <ScreenWrapper style={styles.container}>

      <Text style={styles.text}>Splash Screen Animation</Text>

      <Text style={styles.btmtext}>A marsh Tech Product </Text>

    </ScreenWrapper>
  )
}

export default SplashScreen

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  text: {
    fontSize: 20,
    color: '#ffffffff',
    fontWeight: 'bold',
    marginTop: 100,
  },
  btmtext: {
    fontSize: 12,
    color: '#ffffff',
    textAlign: 'center',
    fontWeight: 'bold',
    position: 'absolute',
    bottom: 30,
  },


})