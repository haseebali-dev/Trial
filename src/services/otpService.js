import axios from 'axios';

const API_BASE_URL = 'http://192.168.1.88:5018/api/Otp';

const otpService = {
    sendOtp: async (mobileNumber) => {
        try {
            const response = await axios.get(`${API_BASE_URL}/get-code`, {
                params: { mobileNumber }
            });
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : "Network Error";
        }
    },

    verifyOtp: async (mobileNumber, code) => {
        try {
            const response = await axios.get(`${API_BASE_URL}/verify-code`, {
                params: { mobileNumber, code }
            });
            return response.data;
        } catch (error) {
            throw error.response ? error.response.data : "Invalid OTP";
        }
    }
};

export default otpService;