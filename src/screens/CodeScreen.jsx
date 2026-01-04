import { StyleSheet, Text, View, TextInput, TouchableOpacity } from 'react-native'
import React, { useState, useRef } from 'react'
import ScreenWrapper from '../components/ScreenWrapper'
import { useNavigation } from '@react-navigation/native';
import { useRoute } from '@react-navigation/native';
// import otpService from '../services/otpService';


const CodeScreen = () => {
    const route = useRoute();
    const navigation = useNavigation();
    const { phoneNumber } = route.params;
    const [code, setCode] = useState(new Array(6).fill(''));
    const [verifying, setVerifying] = useState(false);
    const inputs = useRef([]);

    const handleSubmit = async () => {
        const fullCode = code.join('');
        if (fullCode.length < 6) {
            alert("Please enter the full 6-digit code");
            return;
        }

        setVerifying(true);
        setTimeout(() => {
            setVerifying(false);
            navigation.navigate('Home');
        }, 1000);
    };
    const handleChange = (text, index) => {
        const newCode = [...code];
        newCode[index] = text;
        setCode(newCode);
        if (text && index < 5) {
            inputs.current[index + 1].focus();
        }
    };

    const handleKeyPress = (e, index) => {
        if (e.nativeEvent.key === 'Backspace' && !code[index] && index > 0) {
            inputs.current[index - 1].focus();
        }
    };

    return (
        <ScreenWrapper style={styles.wrapper}>
            <View style={styles.container}>
                <Text style={styles.title}>Enter the 6-digit code sent to your phone number</Text>
                <View style={styles.otpContainer}>
                    {code.map((digit, index) => (
                        <TextInput
                            key={index}
                            style={styles.otpInput}
                            keyboardType="numeric"
                            maxLength={1}
                            onChangeText={(text) => handleChange(text, index)}
                            onKeyPress={(e) => handleKeyPress(e, index)}
                            value={digit}
                            ref={(input) => inputs.current[index] = input}
                            placeholder=""
                            placeholderTextColor="#ccc"
                        />
                    ))}

                </View>
                <Text style={styles.txt}> Full code is need to activate</Text>

                <TouchableOpacity style={styles.button} onPress={() => handleSubmit()}>
                    <Text style={styles.buttonText}>Verify</Text>
                </TouchableOpacity>

                <Text style={styles.btmtxt}>By providing your phone number, you agree Marsh
                    Tech may send you texts with notifications and security codes</Text>
            </View>
        </ScreenWrapper>
    )
}

export default CodeScreen

const styles = StyleSheet.create({
    wrapper: {
        flex: 1,
    },
    container: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        paddingHorizontal: 20,
    },
    title: {
        fontSize: 24,
        color: '#ffffff',
        marginBottom: 40,
        textAlign: 'center',
    },
    otpContainer: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        width: '100%',
        marginBottom: 40,
    },
    otpInput: {
        width: 45,
        height: 50,
        borderBottomWidth: 2,
        borderBottomColor: '#ffffff',
        color: '#ffffff',
        fontSize: 24,
        textAlign: 'center',
    },
    txt: {
        fontSize: 20,
        color: '#ffffff',
        textAlign: 'center',
        marginTop: 20,
        marginBottom: 20,
        fontWeight: 'bold',
    },
    btmtxt: {
        fontSize: 12,
        color: '#ffffff',
        textAlign: 'center',
        fontWeight: 'bold',
        position: 'absolute',
        bottom: 30,
        width: '100%',
    },
    button: {
        backgroundColor: '#ffffff',
        paddingVertical: 15,
        paddingHorizontal: 60,
        borderRadius: 30,
    },
    buttonText: {
        color: '#5E0380',
        fontSize: 18,
        fontWeight: 'bold',
    },
})