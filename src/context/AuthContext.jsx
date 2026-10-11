import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { api } from '../api';
import { ApiError } from '../lib/errors';
import { decodeJwt, isExpired } from '../lib/jwt';
import { clearToken, getToken, setToken } from '../lib/token';
import { setUnauthorizedHandler } from '../lib/http';

const AuthContext = createContext(null);
export const useAuth = () => useContext(AuthContext);

const SESSION_EXPIRED = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';

// Chỉ giữ token còn hạn và đúng role Admin (BR10)
function readValidToken() {
  const t = getToken();
  if (!t) return null;
  const c = decodeJwt(t);
  if (!c || isExpired(c) || c.role !== 'Admin') { clearToken(); return null; }
  return t;
}

export function AuthProvider({ children }) {
  const [token, setTokenState] = useState(readValidToken);
  const [notice, setNotice] = useState('');
  const claims = useMemo(() => (token ? decodeJwt(token) : null), [token]);
  const user = claims && { id: claims.sub, name: claims.name, email: claims.email, role: claims.role };

  const logout = useCallback((message = '') => {
    clearToken();
    setTokenState(null);
    setNotice(message);
  }, []);

  const login = useCallback(async (email, password) => {
    const { token: t } = await api.auth.login({ email, password });
    const c = decodeJwt(t);
    if (!c || c.role !== 'Admin') throw new ApiError('Tài khoản này không có quyền vào trang quản trị.', { status: 403 });
    setToken(t);
    setTokenState(t);
    setNotice('');
  }, []);

  // 401 từ bất kỳ API nào -> đăng xuất
  useEffect(() => { setUnauthorizedHandler(() => logout(SESSION_EXPIRED)); }, [logout]);

  // Tự đăng xuất đúng lúc token hết hạn
  useEffect(() => {
    if (!claims) return undefined;
    const ms = claims.exp * 1000 - Date.now();
    const timer = setTimeout(() => logout(SESSION_EXPIRED), Math.max(ms, 0));
    return () => clearTimeout(timer);
  }, [claims, logout]);

  const value = useMemo(() => ({ user, token, login, logout, notice, clearNotice: () => setNotice('') }), [user, token, login, logout, notice]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
