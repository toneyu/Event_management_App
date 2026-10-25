import { useCallback, useEffect, useMemo, useState } from 'react';
import { eventsApi } from '../api/eventsApi';
import {
  CreateEventPayload,
  EventItem,
  RegisterAttendeePayload,
} from '../types/event';
import {
  FilterMode,
  computeEventStats,
  filterEvents,
} from '../utils/eventRules';

/**
 * Custom hook that manages event dashboard state, filtering, KPI metrics,
 * modal state, and CRUD / registration handlers separately from the UI layer.
 */
export function useEventsDashboard() {
  const [events, setEvents] = useState<EventItem[]>([]);
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [apiError, setApiError] = useState<string | null>(null);
  const [filterMode, setFilterMode] = useState<FilterMode>('all');
  const [searchQuery, setSearchQuery] = useState('');

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEvent, setEditingEvent] = useState<EventItem | null>(null);

  /**
   * Fetches all events from the backend API and updates the active selection.
   */
  const loadEvents = useCallback(async (preserveSelection = true) => {
    try {
      setApiError(null);
      const data = await eventsApi.getAll();
      setEvents(data);

      if (data.length > 0) {
        setSelectedEventId((prev) => {
          if (preserveSelection && prev && data.some((e) => e.id === prev)) {
            return prev;
          }
          return data[0].id;
        });
      } else {
        setSelectedEventId(null);
      }
    } catch (err) {
      setApiError(
        err instanceof Error
          ? err.message
          : 'Unable to reach the EventBE API server on http://localhost:5150.'
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadEvents(false);
  }, [loadEvents]);

  const selectedEvent = useMemo(
    () => events.find((e) => e.id === selectedEventId) || null,
    [events, selectedEventId]
  );

  const filteredEvents = useMemo(
    () => filterEvents(events, filterMode, searchQuery),
    [events, filterMode, searchQuery]
  );

  const stats = useMemo(() => computeEventStats(events), [events]);

  const handleOpenCreate = useCallback(() => {
    setEditingEvent(null);
    setIsModalOpen(true);
  }, []);

  const handleOpenEdit = useCallback((event: EventItem) => {
    setEditingEvent(event);
    setIsModalOpen(true);
  }, []);

  const handleCloseModal = useCallback(() => {
    setIsModalOpen(false);
  }, []);

  const handleSaveEvent = useCallback(
    async (payload: CreateEventPayload) => {
      if (editingEvent) {
        const updated = await eventsApi.update(editingEvent.id, payload);
        await loadEvents(true);
        setSelectedEventId(updated.id);
      } else {
        const created = await eventsApi.create(payload);
        await loadEvents(false);
        setSelectedEventId(created.id);
      }
    },
    [editingEvent, loadEvents]
  );

  const handleDeleteEvent = useCallback(
    async (eventId: string) => {
      await eventsApi.remove(eventId);
      await loadEvents(false);
    },
    [loadEvents]
  );

  const handleRegisterAttendee = useCallback(
    async (eventId: string, payload: RegisterAttendeePayload) => {
      await eventsApi.registerAttendee(eventId, payload);
      await loadEvents(true);
    },
    [loadEvents]
  );

  const handleUnregisterAttendee = useCallback(
    async (eventId: string, userEmail: string) => {
      await eventsApi.unregisterAttendee(eventId, userEmail);
      await loadEvents(true);
    },
    [loadEvents]
  );

  return {
    // State & derived data
    selectedEventId,
    selectedEvent,
    filteredEvents,
    stats,
    loading,
    apiError,
    filterMode,
    searchQuery,
    isModalOpen,
    editingEvent,

    // Setters & action handlers
    setSelectedEventId,
    setFilterMode,
    setSearchQuery,
    loadEvents,
    handleOpenCreate,
    handleOpenEdit,
    handleCloseModal,
    handleSaveEvent,
    handleDeleteEvent,
    handleRegisterAttendee,
    handleUnregisterAttendee,
  };
}

