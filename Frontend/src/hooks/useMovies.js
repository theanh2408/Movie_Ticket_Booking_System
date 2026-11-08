import { useState, useEffect, useCallback, useMemo } from 'react';
import movieApi from '../api/movieApi';

export const useMovies = (filters = {}) => {
  const [items, setItems] = useState([]);
  const [genres, setGenres] = useState([]);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [internalPage, setInternalPage] = useState(filters.page || 1);
  const [internalPageSize, setInternalPageSize] = useState(filters.pageSize || 10);
  const [internalSearch, setInternalSearch] = useState(filters.search || '');
  const [internalGenreId, setInternalGenreId] = useState(filters.genreId || null);
  const [internalStatus, setInternalStatus] = useState(filters.status || '');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // Ưu tiên filter truyền qua prop của hook (được cập nhật động theo re-render của parent component)
  const activeStatus = filters.status !== undefined ? filters.status : internalStatus;
  const activeSearch = filters.search !== undefined ? filters.search : internalSearch;
  const activeGenreId = filters.genreId !== undefined ? filters.genreId : internalGenreId;
  const activePage = filters.page !== undefined ? filters.page : internalPage;
  const activePageSize = filters.pageSize !== undefined ? filters.pageSize : internalPageSize;

  // Chuẩn hóa dữ liệu phim: phân loại rõ ràng NOW_SHOWING và COMING_SOON
  const normalizeMovie = useCallback((m) => {
    const rawStatus = (m.status || '').trim().toUpperCase();
    const isComingSoon = rawStatus === 'COMING_SOON' || rawStatus === 'COMING-SOON';
    const isNowShowing = rawStatus === 'NOW_SHOWING' || rawStatus === 'NOW-SHOWING';

    const normalizedStatus = isComingSoon ? 'COMING_SOON' : (isNowShowing ? 'NOW_SHOWING' : rawStatus || 'NOW_SHOWING');
    const normalizedStatusTab = isComingSoon ? 'coming-soon' : 'now-showing';

    return {
      ...m,
      id: m.movieId ?? m.id,
      movieId: m.movieId ?? m.id,
      poster: m.posterUrl || m.poster || 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800',
      posterUrl: m.posterUrl || m.poster || 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800',
      backdrop: m.posterUrl || m.poster,
      duration: m.durationMinutes ?? m.duration ?? 120,
      durationMinutes: m.durationMinutes ?? m.duration ?? 120,
      synopsis: m.description || m.synopsis || '',
      description: m.description || m.synopsis || '',
      genre: Array.isArray(m.genres)
        ? m.genres.map((g) => g.name || g).join(' • ')
        : (m.genre || 'Hành động'),
      rating: m.rating || 9.2,
      voteCount: m.voteCount || '12.4k đánh giá',
      ageRating: m.ageRating || 'C13',
      releaseDate: m.releaseDate || '2026-10-01',
      language: m.language || 'Tiếng Việt - Phụ đề',
      status: normalizedStatus,
      statusTab: normalizedStatusTab,
      isNowShowing: !isComingSoon && isNowShowing,
      isComingSoon: isComingSoon,
    };
  }, []);

  // Tải danh sách thể loại từ API
  const fetchGenres = useCallback(async () => {
    try {
      const res = await movieApi.getGenres();
      if (res && res.data) {
        setGenres(res.data);
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách thể loại:', err);
    }
  }, []);

  // Tải danh sách phim với các bộ lọc
  const fetchMovies = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const params = {
        page: activePage,
        pageSize: activePageSize,
      };

      if (activeSearch && activeSearch.trim()) {
        params.search = activeSearch.trim();
      }

      if (activeGenreId) {
        params.genreId = activeGenreId;
      }

      if (activeStatus) {
        // Chuyển đổi chính xác: NOW_SHOWING hoặc COMING_SOON
        params.status = activeStatus.toUpperCase().replace('-', '_');
      }

      const res = await movieApi.getMovies(params);
      if (res && res.data) {
        const rawItems = res.data.items || [];
        setItems(rawItems.map(normalizeMovie));
        setTotalItems(res.data.totalItems || rawItems.length);
        setTotalPages(res.data.totalPages || 1);
      } else {
        setItems([]);
        setTotalItems(0);
        setTotalPages(1);
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách phim:', err);
      setError(err?.message || 'Không thể kết nối đến máy chủ để tải danh sách phim.');
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [activePage, activePageSize, activeSearch, activeGenreId, activeStatus, normalizeMovie]);

  // Load genres một lần khi mount
  useEffect(() => {
    fetchGenres();
  }, [fetchGenres]);

  // Load movies khi dependencies thay đổi
  useEffect(() => {
    fetchMovies();
  }, [fetchMovies]);

  const resetFilters = useCallback(() => {
    setInternalSearch('');
    setInternalGenreId(null);
    setInternalStatus('');
    setInternalPage(1);
  }, []);

  return {
    items,
    genres,
    totalItems,
    totalPages,
    page: activePage,
    setPage: setInternalPage,
    pageSize: activePageSize,
    setPageSize: setInternalPageSize,
    search: activeSearch,
    setSearch: setInternalSearch,
    genreId: activeGenreId,
    setGenreId: setInternalGenreId,
    status: activeStatus,
    setStatus: setInternalStatus,
    loading,
    error,
    refetch: fetchMovies,
    resetFilters,
  };
};

export default useMovies;
