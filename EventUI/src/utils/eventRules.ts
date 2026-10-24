import type { EventItem } from '../types/event.ts';

export type FilterMode = 'all' | 'upcoming' | 'full' | 'past';

export interface EventFormInput {
  title: string;
  description: string;
  dateInput: string;
  maxCapacity: number;
  currentRegisteredCount?: number;
}

export interface ValidationOutcome {
  isValid: boolean;
  error: string | null;
}

/**
 * Validates event creation and update inputs, including preventing capacity
 * reduction below the current number of registered attendees.
 */
export function validateEventForm(input: EventFormInput): ValidationOutcome {
  const trimmedTitle = input.title.trim();
  if (!trimmedTitle || trimmedTitle.length < 2) {
    return {
      isValid: false,
      error: 'Title must be at least 2 characters.',
    };
  }

  if (trimmedTitle.length > 150) {
    return {
      isValid: false,
      error: 'Title cannot exceed 150 characters.',
    };
  }

  if (input.description && input.description.trim().length > 2000) {
    return {
      isValid: false,
      error: 'Description cannot exceed 2000 characters.',
    };
  }

  if (!input.dateInput) {
    return {
      isValid: false,
      error: 'Please select a valid event date and time.',
    };
  }

  const parsedDate = new Date(input.dateInput);
  if (Number.isNaN(parsedDate.getTime())) {
    return {
      isValid: false,
      error: 'Invalid date format.',
    };
  }

  if (!Number.isInteger(input.maxCapacity) || input.maxCapacity < 1) {
    return {
      isValid: false,
      error: 'Max capacity must be a whole number of at least 1.',
    };
  }

  const registeredCount = input.currentRegisteredCount ?? 0;
  if (input.maxCapacity < registeredCount) {
    return {
      isValid: false,
      error: `Cannot reduce max capacity to ${input.maxCapacity} because ${registeredCount} attendees are already registered.`,
    };
  }

  return { isValid: true, error: null };
}

/**
 * Enforces business rules for registering an attendee for an event:
 * 1. Cannot register for past events
 * 2. Cannot exceed event capacity
 * 3. Cannot double-register the same user (case-insensitive email) for the same event
 */
export function validateRegistration(
  event: EventItem,
  userName: string,
  userEmail: string,
  now: Date = new Date()
): ValidationOutcome {
  const trimmedName = userName.trim();
  const normalizedEmail = userEmail.trim().toLowerCase();

  if (!trimmedName || !normalizedEmail) {
    return {
      isValid: false,
      error: 'Please enter both attendee name and email address.',
    };
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(normalizedEmail)) {
    return {
      isValid: false,
      error: 'A valid attendee email address is required.',
    };
  }

  // Business Rule 1: Cannot register for past events
  const eventDate = new Date(event.date);
  if (event.isPast || eventDate.getTime() <= now.getTime()) {
    return {
      isValid: false,
      error: 'Cannot register for past events.',
    };
  }

  // Business Rule 3: Cannot double-register same user for same event
  const isDuplicate = event.registrations.some(
    (reg) => reg.userEmail.trim().toLowerCase() === normalizedEmail
  );
  if (isDuplicate) {
    return {
      isValid: false,
      error: `User '${normalizedEmail}' is already registered for this event.`,
    };
  }

  // Business Rule 2: Cannot exceed event capacity
  if (event.isFull || event.registeredCount >= event.maxCapacity) {
    return {
      isValid: false,
      error: 'Cannot exceed event capacity. This event is already full.',
    };
  }

  return { isValid: true, error: null };
}

/**
 * Calculates clamped capacity percentage (0 to 100).
 */
export function calculateCapacityPercent(
  registeredCount: number,
  maxCapacity: number
): number {
  if (maxCapacity <= 0 || registeredCount <= 0) {
    return 0;
  }
  return Math.min(100, Math.round((registeredCount / maxCapacity) * 100));
}

/**
 * Computes dashboard summary KPI metrics across all events.
 */
export function computeEventStats(events: EventItem[]) {
  const total = events.length;
  const upcomingOpen = events.filter((e) => !e.isPast && !e.isFull).length;
  const fullCount = events.filter((e) => e.isFull).length;
  const totalRegistrations = events.reduce(
    (sum, e) => sum + e.registeredCount,
    0
  );
  return { total, upcomingOpen, fullCount, totalRegistrations };
}

/**
 * Filters events by status tab and search query (title, description, or attendee).
 */
export function filterEvents(
  events: EventItem[],
  filterMode: FilterMode,
  searchQuery: string
): EventItem[] {
  const q = searchQuery.trim().toLowerCase();

  return events.filter((ev) => {
    if (filterMode === 'upcoming' && ev.isPast) return false;
    if (filterMode === 'full' && !ev.isFull) return false;
    if (filterMode === 'past' && !ev.isPast) return false;

    if (q) {
      const matchTitle = ev.title.toLowerCase().includes(q);
      const matchDesc = ev.description.toLowerCase().includes(q);
      const matchAttendee = ev.registrations.some(
        (r) =>
          r.userName.toLowerCase().includes(q) ||
          r.userEmail.toLowerCase().includes(q)
      );
      return matchTitle || matchDesc || matchAttendee;
    }

    return true;
  });
}

