import React, { useEffect, useState } from 'react';
import { EventItem, RegisterAttendeePayload } from '../types/event';

interface EventDetailPanelProps {
  event: EventItem | null;
  onEdit: (event: EventItem) => void;
  onDelete: (eventId: string) => Promise<void>;
  onRegister: (
    eventId: string,
    payload: RegisterAttendeePayload
  ) => Promise<void>;
  onUnregister: (eventId: string, userEmail: string) => Promise<void>;
}

export const EventDetailPanel: React.FC<EventDetailPanelProps> = ({
  event,
  onEdit,
  onDelete,
  onRegister,
  onUnregister,
}) => {
  const [userName, setUserName] = useState('');
  const [userEmail, setUserEmail] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [removingEmail, setRemovingEmail] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{
    type: 'success' | 'error';
    message: string;
  } | null>(null);

  useEffect(() => {
    setFeedback(null);
  }, [event?.id]);

  if (!event) {
    return (
      <div className="detail-panel empty-state">
        <div className="empty-icon">📅</div>
        <h3>Select an Event</h3>
        <p>
          Choose an event from the list on the left to view full details,
          capacity status, and manage attendee registrations.
        </p>
      </div>
    );
  }

  const capacityPercent = Math.min(
    100,
    Math.round((event.registeredCount / Math.max(1, event.maxCapacity)) * 100)
  );

  const handleRegisterSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFeedback(null);

    if (!userName.trim() || !userEmail.trim()) {
      setFeedback({
        type: 'error',
        message: 'Please enter both attendee name and email address.',
      });
      return;
    }

    setSubmitting(true);
    try {
      await onRegister(event.id, {
        userName: userName.trim(),
        userEmail: userEmail.trim(),
      });
      setFeedback({
        type: 'success',
        message: `Successfully registered ${userName.trim()} (${userEmail
          .trim()
          .toLowerCase()}).`,
      });
      setUserName('');
      setUserEmail('');
    } catch (err) {
      setFeedback({
        type: 'error',
        message:
          err instanceof Error ? err.message : 'Failed to register attendee.',
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleUnregisterClick = async (email: string, name: string) => {
    setFeedback(null);
    setRemovingEmail(email);
    try {
      await onUnregister(event.id, email);
      setFeedback({
        type: 'success',
        message: `Unregistered ${name} (${email}) from this event.`,
      });
    } catch (err) {
      setFeedback({
        type: 'error',
        message:
          err instanceof Error ? err.message : 'Failed to unregister attendee.',
      });
    } finally {
      setRemovingEmail(null);
    }
  };

  const formattedDate = new Date(event.date).toLocaleString(undefined, {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

  return (
    <div className="detail-panel">
      <div className="detail-header">
        <div className="badge-row">
          {event.isPast ? (
            <span className="badge badge-past">Past Event</span>
          ) : event.isFull ? (
            <span className="badge badge-full">At Capacity</span>
          ) : (
            <span className="badge badge-open">Open for Registration</span>
          )}
          <span className="event-date-chip">🕒 {formattedDate}</span>
        </div>

        <div className="detail-title-row">
          <h2>{event.title}</h2>
          <div className="detail-actions">
            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => onEdit(event)}
            >
              Edit Event
            </button>
            <button
              type="button"
              className="btn btn-danger-outline btn-sm"
              onClick={() => onDelete(event.id)}
            >
              Delete
            </button>
          </div>
        </div>

        <p className="detail-description">
          {event.description || 'No description provided for this event.'}
        </p>
      </div>

      <div className="capacity-box">
        <div className="capacity-stats">
          <div>
            <strong>Capacity Usage:</strong> {event.registeredCount} /{' '}
            {event.maxCapacity} attendees ({capacityPercent}%)
          </div>
          <div className="spots-pill">
            {event.availableSpots === 0
              ? '0 spots remaining'
              : `${event.availableSpots} spot${
                  event.availableSpots === 1 ? '' : 's'
                } remaining`}
          </div>
        </div>
        <div className="progress-track">
          <div
            className={`progress-fill ${
              event.isFull
                ? 'fill-full'
                : capacityPercent >= 80
                  ? 'fill-warning'
                  : 'fill-normal'
            }`}
            style={{ width: `${capacityPercent}%` }}
          />
        </div>
      </div>

      {feedback && (
        <div
          className={`alert ${
            feedback.type === 'success' ? 'alert-success' : 'alert-error'
          }`}
        >
          <span>{feedback.message}</span>
          <button
            type="button"
            className="alert-dismiss"
            onClick={() => setFeedback(null)}
          >
            ✕
          </button>
        </div>
      )}

      <div className="registration-section">
        <h3>Register Attendee</h3>
        {event.isPast && (
          <div className="rule-banner rule-banner-past">
            ⚠️ <strong>Business Rule:</strong> This event occurred in the past.
            Attempting to register will be rejected by the API.
          </div>
        )}
        {!event.isPast && event.isFull && (
          <div className="rule-banner rule-banner-full">
            🔒 <strong>Business Rule:</strong> This event has reached its max
            capacity ({event.maxCapacity}). Unregister an attendee or increase
            capacity to add more.
          </div>
        )}

        <form onSubmit={handleRegisterSubmit} className="register-form">
          <div className="form-group">
            <label htmlFor="reg-name">Full Name</label>
            <input
              id="reg-name"
              type="text"
              placeholder="e.g., Jordan Taylor"
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="reg-email">Email Address (Unique per Event)</label>
            <input
              id="reg-email"
              type="email"
              placeholder="e.g., jordan@company.com"
              value={userEmail}
              onChange={(e) => setUserEmail(e.target.value)}
              required
            />
          </div>
          <button
            type="submit"
            className="btn btn-primary register-submit-btn"
            disabled={submitting}
          >
            {submitting ? 'Registering...' : 'Register for Event'}
          </button>
        </form>
      </div>

      <div className="attendees-section">
        <div className="attendees-header">
          <h3>Registered Attendees ({event.registrations.length})</h3>
        </div>

        {event.registrations.length === 0 ? (
          <div className="empty-attendees">
            No attendees registered yet. Be the first to register above!
          </div>
        ) : (
          <ul className="attendee-list">
            {event.registrations.map((reg) => (
              <li key={reg.id} className="attendee-item">
                <div className="attendee-avatar">
                  {reg.userName.charAt(0).toUpperCase()}
                </div>
                <div className="attendee-info">
                  <div className="attendee-name">{reg.userName}</div>
                  <div className="attendee-meta">
                    <span>{reg.userEmail}</span>
                    <span className="dot-sep">•</span>
                    <span>
                      Registered{' '}
                      {new Date(reg.registeredAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
                <button
                  type="button"
                  className="btn btn-danger-outline btn-xs"
                  disabled={removingEmail === reg.userEmail}
                  onClick={() =>
                    handleUnregisterClick(reg.userEmail, reg.userName)
                  }
                >
                  {removingEmail === reg.userEmail
                    ? 'Removing...'
                    : 'Unregister'}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};

