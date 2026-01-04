import { StyleSheet, Text, View, ActivityIndicator, TouchableOpacity, Image } from 'react-native'
import React, { useState, useEffect } from 'react'
import MapView, { Marker, UrlTile, Polyline, PROVIDER_GOOGLE } from 'react-native-maps';
import { useNavigation } from '@react-navigation/native';

const MapScreen = () => {
  const navigation = useNavigation();
  const [loading, setLoading] = useState(true);
  const [destination, setDestination] = useState(null);

  const [region, setRegion] = useState({
    latitude: 37.78825,
    longitude: -122.4324,
    latitudeDelta: 0.05,
    longitudeDelta: 0.05,
  });

  const currentLocation = {
    latitude: 37.78825,
    longitude: -122.4324,
  };

  const handleMapReady = () => {
    setLoading(false);
  };

  const handlePress = (e) => {
    setDestination(e.nativeEvent.coordinate);
  };

  return (
    <View style={styles.container}>
      <MapView
        provider={PROVIDER_GOOGLE}
        style={styles.map}
        region={region}
        onMapReady={handleMapReady}
        onPress={handlePress}
      >
        {/* <UrlTile
          urlTemplate="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          maximumZ={19}
          flipY={false}
        /> */}

        <Marker coordinate={currentLocation}>
          <View style={styles.currentLocationMarker}>
            <View style={styles.innerMarker} />
          </View>
        </Marker>

        {destination && (
          <Marker coordinate={destination} pinColor="#a85fe3" />
        )}

        {destination && (
          <Polyline
            coordinates={[currentLocation, destination]}
            strokeColor="#8a56ac"
            strokeWidth={4}
            lineDashPattern={[1]}
          />
        )}
      </MapView>

      {loading && (
        <View style={styles.loaderContainer}>
          <ActivityIndicator size="large" color="#5E0380" />
        </View>
      )}

      <View style={styles.bottomContainer}>
        {destination && (
          <View style={styles.card}>
            <View style={styles.cardImagePlaceholder} />
            <View style={styles.cardInfo}>
              <Text style={styles.cardTitle}>Brown Mountains</Text>
              <Text style={styles.cardSubtitle}>📍 10 Mtr Left</Text>
            </View>
          </View>
        )}

        <View style={styles.controlsRow}>
          <View style={styles.leftControls}>
            <TouchableOpacity style={styles.iconButton}>
              <Text style={styles.iconText}>📍</Text>
            </TouchableOpacity>
            <TouchableOpacity
              style={styles.iconButton}
              onPress={() => navigation.navigate('Camera')}
            >
              <Text style={styles.iconText}>📷</Text>
            </TouchableOpacity>
          </View>

          {destination && (
            <TouchableOpacity style={styles.navigateButton}>
              <Text style={styles.navigateText}>➤</Text>
            </TouchableOpacity>
          )}
        </View>
      </View>
    </View>
  )
}

export default MapScreen

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  map: {
    ...StyleSheet.absoluteFillObject,
  },
  loaderContainer: {
    ...StyleSheet.absoluteFillObject,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(255,255,255,0.8)',
  },
  currentLocationMarker: {
    width: 20,
    height: 20,
    borderRadius: 10,
    backgroundColor: 'rgba(138, 86, 172, 0.3)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  innerMarker: {
    width: 10,
    height: 10,
    borderRadius: 5,
    backgroundColor: '#8a56ac',
  },
  bottomContainer: {
    position: 'absolute',
    bottom: 30,
    left: 20,
    right: 20,
  },
  card: {
    backgroundColor: '#8a56ac',
    borderRadius: 15,
    padding: 15,
    flexDirection: 'row',
    marginBottom: 20,
    alignItems: 'center',
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
    elevation: 5,
  },
  cardImagePlaceholder: {
    width: 50,
    height: 50,
    borderRadius: 10,
    backgroundColor: '#ccc',
    marginRight: 15,
  },
  cardInfo: {
    flex: 1,
  },
  cardTitle: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  cardSubtitle: {
    color: '#eee',
    fontSize: 12,
    marginTop: 4,
  },
  controlsRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-end',
  },
  leftControls: {
    flexDirection: 'row',
    gap: 10,
  },
  iconButton: {
    width: 50,
    height: 50,
    borderRadius: 15,
    backgroundColor: '#fff',
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.2,
    shadowRadius: 1.41,
    elevation: 2,
  },
  iconText: {
    fontSize: 24,
  },
  navigateButton: {
    width: 60,
    height: 60,
    borderRadius: 20,
    backgroundColor: '#8a56ac',
    justifyContent: 'center',
    alignItems: 'center',
    shadowColor: "#000",
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
    elevation: 5,
  },
  navigateText: {
    fontSize: 30,
    color: '#fff',
  },
})