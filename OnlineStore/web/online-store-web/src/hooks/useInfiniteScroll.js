import { useState, useEffect, useRef, useCallback } from 'react';

/**
 * useInfiniteScroll — A reusable hook for IntersectionObserver-based infinite scroll.
 *
 * @param {function} fetchFn - async function(page, limit) => { data, page, limit, total, hasMore }
 * @param {object} options
 * @param {number} options.limit - Items per page (default: 9)
 * @param {Array} options.deps - Dependencies that trigger a reset & refetch from page 1
 * @param {boolean} options.enabled - Whether this hook is active (default: true). When false, no fetching occurs.
 * @returns {{ items, isLoading, hasMore, error, total, sentinelRef, retry, reset, initialLoad }}
 */
export default function useInfiniteScroll(fetchFn, { limit = 9, deps = [], enabled = true } = {}) {
  const [items, setItems] = useState([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [error, setError] = useState(null);
  const [total, setTotal] = useState(0);
  const [initialLoad, setInitialLoad] = useState(true);

  const sentinelRef = useRef(null);
  const observerRef = useRef(null);
  const fetchFnRef = useRef(fetchFn);
  const isMountedRef = useRef(true);
  const fetchingRef = useRef(false);
  const enabledRef = useRef(enabled);

  // Keep refs up to date
  fetchFnRef.current = fetchFn;
  enabledRef.current = enabled;

  // Fetch a specific page
  const fetchPage = useCallback(async (page, append = true) => {
    if (fetchingRef.current || !enabledRef.current) return;
    fetchingRef.current = true;
    setIsLoading(true);
    setError(null);

    try {
      const result = await fetchFnRef.current(page, limit);
      if (!isMountedRef.current) return;

      const newItems = Array.isArray(result) ? result : (result.data || result.items || []);
      if (append && page > 1) {
        setItems(prev => [...prev, ...newItems]);
      } else {
        setItems(newItems);
      }
      setCurrentPage(result.page);
      setHasMore(result.hasMore);
      setTotal(result.total);
      setInitialLoad(false);
    } catch (err) {
      if (!isMountedRef.current) return;
      setError(err.message || 'Failed to load. Please try again.');
      setInitialLoad(false);
    } finally {
      if (isMountedRef.current) {
        setIsLoading(false);
        fetchingRef.current = false;
      }
    }
  }, [limit]);

  // Reset and fetch page 1
  const reset = useCallback(() => {
    setItems([]);
    setCurrentPage(1);
    setHasMore(true);
    setError(null);
    setTotal(0);
    setInitialLoad(true);
    fetchingRef.current = false;
    if (enabledRef.current) {
      fetchPage(1, false);
    }
  }, [fetchPage]);

  // Retry the last failed fetch
  const retry = useCallback(() => {
    fetchPage(currentPage, currentPage > 1);
  }, [fetchPage, currentPage]);

  // Reset when deps change (only if enabled)
  useEffect(() => {
    if (enabled) {
      reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, enabled]);

  // Cleanup on unmount
  useEffect(() => {
    isMountedRef.current = true;
    return () => {
      isMountedRef.current = false;
    };
  }, []);

  // IntersectionObserver setup
  useEffect(() => {
    if (observerRef.current) {
      observerRef.current.disconnect();
    }

    if (!enabled) return;

    observerRef.current = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (entry.isIntersecting && !fetchingRef.current && hasMore && !isLoading) {
          fetchPage(currentPage + 1, true);
        }
      },
      { rootMargin: '200px' }
    );

    const sentinel = sentinelRef.current;
    if (sentinel && hasMore) {
      observerRef.current.observe(sentinel);
    }

    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }
    };
  }, [hasMore, isLoading, currentPage, fetchPage, enabled]);

  return {
    items,
    isLoading,
    hasMore,
    error,
    total,
    sentinelRef,
    retry,
    reset,
    initialLoad,
    setItems,
  };
}
