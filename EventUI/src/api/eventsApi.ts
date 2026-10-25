import {
  CreateEventPayload,
  EventItem,
  RegisterAttendeePayload,
  Registration,
  UpdateEventPayload,
} from '../types/event';

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:5150/api/events';

/**
 * Parses an HTTP `fetch` response into the expected TypeScript type `T`.
 * - Returns `undefined` for `204 No Content` responses (e.g., DELETE endpoints).
 * - Safely parses JSON or falls back to plain text.
 * - Throws an `Error` with the backend's validation/conflict message when `!response.ok`.
 */
async function handleResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T;
  }

  let body: unknown = null;
  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    body = await response.json();
  } else {
    const text = await response.text();
    body = text ? { message: text } : null;
  }

  if (!response.ok) {
    const errObj = body as { message?: string; title?: string } | null;
    const message =
      errObj?.message ||
      errObj?.title ||
      `Request failed with status ${response.status}`;
    throw new Error(message);
  }

  return body as T;
}

/**
 * REST client for interacting with the EventBE `/api/events` endpoints.
 */
export const eventsApi = {
  /**
   * Fetches all events ordered by date (`GET /api/events`).
   */
  async getAll(): Promise<EventItem[]> {
    const res = await fetch(API_BASE_URL);
    return handleResponse<EventItem[]>(res);
  },

  /**
   * Fetches a single event and its attendee registrations by ID (`GET /api/events/{id}`).
   */
  async getById(id: string): Promise<EventItem> {
    const res = await fetch(`${API_BASE_URL}/${id}`);
    return handleResponse<EventItem>(res);
  },

  /**
   * Creates a new event (`POST /api/events`).
   */
  async create(payload: CreateEventPayload): Promise<EventItem> {
    const res = await fetch(API_BASE_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handleResponse<EventItem>(res);
  },

  /**
   * Updates an existing event's title, description, date, and max capacity (`PUT /api/events/{id}`).
   */
  async update(id: string, payload: UpdateEventPayload): Promise<EventItem> {
    const res = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handleResponse<EventItem>(res);
  },

  /**
   * Deletes an event by ID (`DELETE /api/events/{id}`).
   */
  async remove(id: string): Promise<void> {
    const res = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'DELETE',
    });
    return handleResponse<void>(res);
  },

  /**
   * Registers an attendee for an event (`POST /api/events/{id}/registrations`).
   * Throws an error if business rules (past event, max capacity, or duplicate email) are violated.
   */
  async registerAttendee(
    eventId: string,
    payload: RegisterAttendeePayload
  ): Promise<Registration> {
    const res = await fetch(`${API_BASE_URL}/${eventId}/registrations`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handleResponse<Registration>(res);
  },

  /**
   * Unregisters an attendee from an event by email (`DELETE /api/events/{id}/registrations/{userEmail}`).
   */
  async unregisterAttendee(eventId: string, userEmail: string): Promise<void> {
    const encodedEmail = encodeURIComponent(userEmail.trim());
    const res = await fetch(
      `${API_BASE_URL}/${eventId}/registrations/${encodedEmail}`,
      {
        method: 'DELETE',
      }
    );
    return handleResponse<void>(res);
  },
};
