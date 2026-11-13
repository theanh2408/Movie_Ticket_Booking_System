import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

export default function Showtime() {
  const { id } = useParams();
  const navigate = useNavigate();

  useEffect(() => {
    if (id) {
      navigate(`/movie/${id}`);
    } else {
      navigate('/');
    }
  }, [id, navigate]);

  return null;
}
