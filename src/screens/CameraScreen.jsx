import { Utils, useCameraDevice, useCameraPermission, Camera } from 'react-native-vision-camera';
import { StyleSheet, Text, View, TouchableOpacity, ActivityIndicator, Dimensions } from 'react-native';
import React, { useEffect } from 'react';
import Animated, { useAnimatedStyle, useDerivedValue, useSharedValue, useAnimatedSensor, SensorType, withSpring } from 'react-native-reanimated';
import { useNavigation } from '@react-navigation/native';

const { width, height } = Dimensions.get('window');

const CameraScreen = () => {
  const device = useCameraDevice('back');
  const { hasPermission, requestPermission } = useCameraPermission();
  const navigation = useNavigation();

  useEffect(() => {
    if (!hasPermission) {
      requestPermission();
    }
  }, [hasPermission]);


  const sensor = useAnimatedSensor(SensorType.ROTATION, { interval: 20 });

  const cylinderStyle = useAnimatedStyle(() => {

    const sensitivity = 300;
    const translateX = withSpring(sensor.sensor.value.y * sensitivity);
    const translateY = withSpring(sensor.sensor.value.x * sensitivity);

    return {
      transform: [
        { translateX },
        { translateY },
        { perspective: 1000 },
        { rotateX: '10deg' }
      ],
    };
  });

  if (!hasPermission) return <View style={styles.center}><Text>No Camera Permission</Text></View>;
  if (device == null) return <View style={styles.center}><ActivityIndicator size="large" color="#8a56ac" /></View>;

  return (
    <View style={styles.container}>
      <Camera
        style={StyleSheet.absoluteFill}
        device={device}
        isActive={true}
      />

      <View style={styles.arLayer}>
        <Animated.View style={[styles.cylinderContainer, cylinderStyle]}>

          <View style={styles.cylinderBody} />
          <View style={styles.cylinderTop} />
          <View style={styles.cylinderBottom} />
        </Animated.View>
      </View>


      <View style={styles.overlayContainer}>

        <View style={styles.topControls}>
        </View>

        <View style={styles.bottomCardContainer}>
          <View style={styles.infoCard}>
            <Text style={styles.infoText}>Walk into the{"\n"}stamp to collect it</Text>
          </View>
        </View>

        <View style={styles.bottomControls}>
          <TouchableOpacity style={styles.iconButton}>
            <Text style={styles.iconText}>📍</Text>
          </TouchableOpacity>

          <TouchableOpacity style={styles.iconButton}>
            <Text style={styles.iconText}>🥽</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={styles.closeButton}
            onPress={() => navigation.goBack()}
          >
            <Text style={styles.closeText}>✕</Text>
          </TouchableOpacity>
        </View>
      </View>
    </View>
  );
};

export default CameraScreen;

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: 'black',
  },
  center: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center'
  },
  arLayer: {
    ...StyleSheet.absoluteFillObject,
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 1,
  },
  cylinderContainer: {
    width: 150,
    height: 400,
    position: 'relative',
    alignItems: 'center',
  },
  cylinderBody: {
    width: 150,
    height: 400,
    backgroundColor: 'rgba(138, 86, 172, 0.85)',
    borderRadius: 75,
    zIndex: 2,
  },
  cylinderTop: {
    position: 'absolute',
    top: -20,
    width: 150,
    height: 40,
    borderRadius: 75,
    backgroundColor: '#a465d3',
    zIndex: 3,
    transform: [{ scaleY: 0.5 }]
  },
  cylinderBottom: {
    position: 'absolute',
    bottom: -20,
    width: 150,
    height: 40,
    borderRadius: 75,
    backgroundColor: '#5e2d83',
    zIndex: 1,
    transform: [{ scaleY: 0.5 }]
  },
  overlayContainer: {
    ...StyleSheet.absoluteFillObject,
    zIndex: 10,
    justifyContent: 'space-between',
    padding: 20,
  },
  topControls: {
    flexDirection: 'row',
    justifyContent: 'flex-start',
    marginTop: 40,
  },
  bottomCardContainer: {
    marginBottom: 20,
    alignItems: 'center',
  },
  infoCard: {
    backgroundColor: '#8d55ff',
    width: '100%',
    padding: 25,
    borderRadius: 20,
  },
  infoText: {
    color: 'white',
    fontSize: 24,
    fontWeight: '600',
    textAlign: 'left',
  },
  bottomControls: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 20,
  },
  iconButton: {
    width: 60,
    height: 40,
    backgroundColor: 'white',
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
  },
  iconText: {
    fontSize: 20,
    color: '#8a56ac'
  },
  closeButton: {
    width: 80,
    height: 50,
    backgroundColor: '#333',
    borderRadius: 15,
    justifyContent: 'center',
    alignItems: 'center',
  },
  closeText: {
    color: 'white',
    fontSize: 24,
    fontWeight: 'bold',
  }
});
