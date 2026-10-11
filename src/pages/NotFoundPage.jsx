import { Link } from 'react-router-dom';
import { EmptyState } from '../components/ui';

export default function NotFoundPage() {
  return (
    <div className="page">
      <EmptyState title="Không tìm thấy trang này" action={<Link className="btn primary" to="/dashboard">Về trang tổng quan</Link>}>
        Đường dẫn có thể đã bị đổi hoặc nhập sai.
      </EmptyState>
    </div>
  );
}
