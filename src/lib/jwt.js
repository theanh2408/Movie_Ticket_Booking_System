// Giải mã / kiểm tra JWT phía client. Việc XÁC THỰC chữ ký luôn do backend làm;
// client chỉ đọc claims để hiển thị tên, kiểm tra role và hạn dùng.
const b64urlDecode = (s) => {
  const padded = s.replace(/-/g, '+').replace(/_/g, '/') + '='.repeat((4 - (s.length % 4)) % 4);
  const bin = atob(padded);
  return new TextDecoder().decode(Uint8Array.from(bin, (c) => c.charCodeAt(0)));
};
const b64urlEncode = (str) => {
  const bytes = new TextEncoder().encode(str);
  let bin = '';
  bytes.forEach((b) => { bin += String.fromCharCode(b); });
  return btoa(bin).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
};

export function decodeJwt(token) {
  try {
    const [, payload] = token.split('.');
    return JSON.parse(b64urlDecode(payload));
  } catch {
    return null;
  }
}

export function isExpired(claims, skewSeconds = 5) {
  if (!claims?.exp) return true;
  return claims.exp * 1000 <= Date.now() + skewSeconds * 1000;
}

// Chỉ dùng cho chế độ mock (chữ ký giả).
export function signMockJwt(claims, ttlSeconds = 3600) {
  const iat = Math.floor(Date.now() / 1000);
  const header = b64urlEncode(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = b64urlEncode(JSON.stringify({ ...claims, iat, exp: iat + ttlSeconds }));
  return `${header}.${payload}.mock-signature`;
}
