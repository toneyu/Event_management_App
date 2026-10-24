import {
  CreateEventPayload,
  EventItem,
  RegisterAttendeePayload,
  Registration,
  UpdateEventPayload,
} from '../types/event';

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:5150/api/events';

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

export const eventsApi = {
  async getAll(): Promise<EventItem[]> {
    const res = await fetch(API_BASE_URL);
    return handleResponse<EventItem[]>(res);
  },

  async getById(id: string): Promise<EventItem> {
    const res = await fetch(`${API_BASE_URL}/${id}`);
    return handleResponse<EventItem>(res);
  },

  async create(payload: CreateEventPayload): Promise<EventItem> {
    const res = await fetch(API_BASE_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handleResponse<EventItem>(res);
  },

  async update(id: string, payload: UpdateEventPayload): Promise<EventItem> {
    const res = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    return handleResponse<EventItem>(res);
  },

  async remove(id: string): Promise<void> {
    const res = await fetch(`${API_BASE_URL}/${id}`, {
      method: 'DELETE',
    });
    return handleResponse<void>(res);
  },

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

