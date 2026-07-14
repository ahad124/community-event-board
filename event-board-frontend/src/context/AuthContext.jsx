import { createContext, useContext, useState, useEffect } from 'react';
import axios from 'axios';

const AuthContext = createContext(null);

const parseJwt = (token) => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch (e) {
    return null;
  }
};

export const AuthProvider = ({ children }) => {
  const [token, setToken] = useState(localStorage.getItem('token') || null);
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // Set default auth header for Axios
  useEffect(() => {
    if (token) {
      axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
      const payload = parseJwt(token);
      if (payload) {
        // Expiry check
        if (payload.exp * 1000 < Date.now()) {
          logout();
        } else {
          // Extract sub (id), email, role from token
          const roleKey = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
          setUser({
            id: payload.sub,
            email: payload.email,
            role: payload[roleKey] || payload.role || 'User',
            userName: payload.email ? payload.email.split('@')[0] : 'User' // default fallback username
          });
        }
      } else {
        logout();
      }
    } else {
      delete axios.defaults.headers.common['Authorization'];
      setUser(null);
    }
    setLoading(false);
  }, [token]);

  const login = async (email, password) => {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    const response = await axios.post(`${baseUrl}/auth/login`, { email, password });
    const { token: userToken } = response.data;
    localStorage.setItem('token', userToken);
    setToken(userToken);
    
    const payload = parseJwt(userToken);
    const roleKey = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    const role = payload ? (payload[roleKey] || payload.role || 'User') : 'User';
    
    return { ...response.data, role };
  };

  const register = async (userName, email, password, role = "User") => {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    const response = await axios.post(`${baseUrl}/auth/register`, { userName, email, password, role });
    return response.data;
  };

  const logout = () => {
    localStorage.removeItem('token');
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ token, user, login, register, logout, isAuthenticated: !!user, loading }}>
      {!loading && children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
