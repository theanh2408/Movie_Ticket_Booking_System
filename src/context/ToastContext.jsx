import { createContext, useCallback, useContext, useState } from 'react';

const ToastContext = createContext(null);
export const useToast = () => useContext(ToastContext);

export function ToastProvider({ children }) {
  const [items, setItems] = useState([]);
  const dismiss = useCallback((id) => setItems((xs) => xs.filter((x) => x.id !== id)), []);
  const push = useCallback((type, text) => {
    const id = Math.random().toString(36).slice(2);
    setItems((xs) => [...xs, { id, type, text }]);
    setTimeout(() => dismiss(id), type === 'error' ? 6000 : 3500);
  }, [dismiss]);
  const toast = {
    success: (t) => push('success', t),
    error: (t) => push('error', t),
    info: (t) => push('info', t),
  };
  return (
    <ToastContext.Provider value={toast}>
      {children}
      <div className="toasts" aria-live="polite">
        {items.map((t) => (
          <div key={t.id} className={`toast toast-${t.type}`} role={t.type === 'error' ? 'alert' : 'status'}>
            <span>{t.text}</span>
            <button className="toast-x" onClick={() => dismiss(t.id)} aria-label="Đóng thông báo">×</button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
