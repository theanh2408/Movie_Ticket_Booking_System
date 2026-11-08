import { useState, useEffect, useCallback, useMemo } from 'react';
import showtimeApi from '../api/showtimeApi';

const MAX_SEATS = Number(import.meta.env?.VITE_MAX_SEATS_PER_BOOKING) || 8;

export const useShowtimeSeats = (showtimeId) => {
  const [seats, setSeats] = useState([]);
  const [selectedSeats, setSelectedSeats] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [warningMessage, setWarningMessage] = useState('');

  // Tải danh sách ghế của suất chiếu từ Backend
  const fetchSeats = useCallback(async () => {
    if (!showtimeId) return;
    setLoading(true);
    setError(null);
    try {
      const res = await showtimeApi.getShowtimeSeats(showtimeId);
      if (res && res.data && Array.isArray(res.data.seats)) {
        setSeats(res.data.seats);
      } else {
        setSeats([]);
      }
    } catch (err) {
      console.error('Lỗi khi tải sơ đồ ghế:', err);
      setError(err?.message || 'Không thể tải sơ đồ ghế cho suất chiếu này.');
      setSeats([]);
    } finally {
      setLoading(false);
    }
  }, [showtimeId]);

  useEffect(() => {
    setSelectedSeats([]);
    setWarningMessage('');
    fetchSeats();
  }, [fetchSeats]);

  // Phân nhóm ghế theo hàng (Row: A, B, C...)
  const { rows, seatsByRow, maxCols } = useMemo(() => {
    const rowMap = {};
    let maxColNum = 8;

    seats.forEach((seat) => {
      const row = seat.seatNumber?.charAt(0) || 'A';
      const colNum = parseInt(seat.seatNumber?.slice(1), 10) || 1;
      if (colNum > maxColNum) maxColNum = colNum;

      if (!rowMap[row]) {
        rowMap[row] = [];
      }
      rowMap[row].push(seat);
    });

    const sortedRows = Object.keys(rowMap).sort();
    sortedRows.forEach((r) => {
      rowMap[r].sort((a, b) => {
        const colA = parseInt(a.seatNumber?.slice(1), 10) || 0;
        const colB = parseInt(b.seatNumber?.slice(1), 10) || 0;
        return colA - colB;
      });
    });

    return {
      rows: sortedRows,
      seatsByRow: rowMap,
      maxCols: maxColNum,
    };
  }, [seats]);

  // Danh sách mã ghế đang chọn: ['A1', 'A2', ...]
  const selectedSeatNumbers = useMemo(
    () => selectedSeats.map((s) => s.seatNumber),
    [selectedSeats]
  );

  // Tổng tiền các ghế đang chọn
  const totalPrice = useMemo(
    () => selectedSeats.reduce((sum, s) => sum + (s.price || 0), 0),
    [selectedSeats]
  );

  // Chọn hoặc bỏ chọn một ghế
  const toggleSeat = useCallback(
    (seatOrNumber) => {
      setWarningMessage('');

      let targetSeat = null;
      if (typeof seatOrNumber === 'string') {
        targetSeat = seats.find((s) => s.seatNumber === seatOrNumber);
      } else {
        targetSeat = seatOrNumber;
      }

      if (!targetSeat) return;

      const upperStatus = targetSeat.status?.toUpperCase();

      // Kiểm tra trạng thái ghế từ DB (AVAILABLE, HELD, BOOKED)
      if (upperStatus !== 'AVAILABLE') {
        setWarningMessage(
          upperStatus === 'HELD'
            ? `Ghế ${targetSeat.seatNumber} đang được người khác giữ chỗ tạm thời.`
            : `Ghế ${targetSeat.seatNumber} đã được đặt.`
        );
        return;
      }

      const isAlreadySelected = selectedSeats.some(
        (s) => s.seatId === targetSeat.seatId || s.seatNumber === targetSeat.seatNumber
      );

      if (isAlreadySelected) {
        // Bỏ chọn ghế
        setSelectedSeats((prev) =>
          prev.filter(
            (s) => s.seatId !== targetSeat.seatId && s.seatNumber !== targetSeat.seatNumber
          )
        );
      } else {
        // Kiểm tra ràng buộc tối đa số ghế cho phép
        if (selectedSeats.length >= MAX_SEATS) {
          setWarningMessage(
            `Bạn chỉ có thể chọn tối đa ${MAX_SEATS} ghế trong một lượt đặt vé!`
          );
          return;
        }

        setSelectedSeats((prev) => [...prev, targetSeat]);
      }
    },
    [seats, selectedSeats]
  );

  const clearSelectedSeats = useCallback(() => {
    setSelectedSeats([]);
    setWarningMessage('');
  }, []);

  return {
    seats,
    rows,
    seatsByRow,
    maxCols,
    selectedSeats,
    selectedSeatNumbers,
    totalPrice,
    maxSeats: MAX_SEATS,
    warningMessage,
    setWarningMessage,
    toggleSeat,
    clearSelectedSeats,
    loading,
    error,
    refetch: fetchSeats,
  };
};

export default useShowtimeSeats;
