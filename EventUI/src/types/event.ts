export interface Registration {
  id: string;
  eventId: string;
  userName: string;
  userEmail: string;
  registeredAt: string;
}

export interface EventItem {
  id: string;
  title: string;
  description: string;
  date: string;
  maxCapacity: number;
  registeredCount: number;
  availableSpots: number;
  isPast: boolean;
  isFull: boolean;
  createdAt: string;
  updatedAt: string;
  registrations: Registration[];
}

export interface CreateEventPayload {
  title: string;
  description: string;
  date: string;
  maxCapacity: number;
}

export interface UpdateEventPayload {
  title: string;
  description: string;
  date: string;
  maxCapacity: number;
}

export interface RegisterAttendeePayload {
  userName: string;
  userEmail: string;
}

export interface ApiErrorResponse {
  message: string;
  errorType?: string;
}

