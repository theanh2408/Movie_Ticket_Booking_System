import { ApiError } from './errors';
import { getToken } from './token';

const BASE = import.meta.env.VITE_API_BASE || '/api';

let unauthorizedHandler = () => {};
export const setUnauthorizedHandler = (fn) => { unauthorizedHandler = fn; };
export const handleUnauthorized = () => unauthorizedHandler();

// Gọi API thật: tự gắn JWT, tự xử lý 401 (hết hạn) và chuyển lỗi về ApiError.
export async function request(method, path, { params, body } = {}) {
  const url = new URL(BASE + path, window.location.origin);
  Object.entries(params || {}).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') url.searchParams.set(k, v);
  });
  const token = getToken();
  let res;
  try {
    res = await fetch(url, {
      method,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new ApiError('Không kết nối được máy chủ. Hãy kiểm tra mạng và thử lại.', { status: 0, code: 'NETWORK' });
  }
  if (res.status === 401 && !path.startsWith('/auth/')) handleUnauthorized();
  const data = res.status === 204 ? null : await res.json().catch(() => null);
  if (!res.ok) {
    throw new ApiError(data?.message || 'Có lỗi xảy ra, vui lòng thử lại.', {
      status: res.status,
      code: data?.code,
      fields: data?.fields,
      data: data?.data,
    });
  }
  return data;
}
export const http = {
  get: (p, params) => request('GET', p, { params }),
  post: (p, body) => request('POST', p, { body }),
  put: (p, body) => request('PUT', p, { body }),
  patch: (p, body) => request('PATCH', p, { body }),
  delete: (p) => request('DELETE', p),
};
