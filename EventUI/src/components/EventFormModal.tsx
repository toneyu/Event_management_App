import React, { useEffect, useState } from 'react';
import { CreateEventPayload, EventItem } from '../types/event';
import { validateEventForm } from '../utils/eventRules';

interface EventFormModalProps {
  isOpen: boolean;
  initialEvent?: EventItem | null;
  onClose: () => void;
  onSubmit: (payload: CreateEventPayload) => Promise<void>;
}

function toLocalDateTimeInputValue(isoDate?: string): string {
  const date = isoDate ? new Date(isoDate) : new Date(Date.now() + 7 * 86400000);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  const tzOffsetMs = date.getTimezoneOffset() * 60000;
  const localISOTime = new Date(date.getTime() - tzOffsetMs)
    .toISOString()
    .slice(0, 16);
  return localISOTime;
}

export const EventFormModal: React.FC<EventFormModalProps> = ({
  isOpen,
  initialEvent,
  onClose,
  onSubmit,
}) => {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [dateTimeLocal, setDateTimeLocal] = useState('');
  const [maxCapacity, setMaxCapacity] = useState<number>(20);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isEditing = Boolean(initialEvent);

  useEffect(() => {
    if (isOpen) {
      setError(null);
      if (initialEvent) {
        setTitle(initialEvent.title);
        setDescription(initialEvent.description);
        setDateTimeLocal(toLocalDateTimeInputValue(initialEvent.date));
        setMaxCapacity(initialEvent.maxCapacity);
      } else {
        setTitle('');
        setDescription('');
        setDateTimeLocal(toLocalDateTimeInputValue());
        setMaxCapacity(20);
      }
    }
  }, [isOpen, initialEvent]);

  if (!isOpen) {
    return null;
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const validation = validateEventForm({
      title,
      description,
      dateInput: dateTimeLocal,
      maxCapacity,
      currentRegisteredCount: initialEvent?.registeredCount ?? 0,
    });

    if (!validation.isValid) {
      setError(validation.error);
      return;
    }

    const parsedDate = new Date(dateTimeLocal);

    setSubmitting(true);
    try {
      await onSubmit({
        title: title.trim(),
        description: description.trim(),
        date: parsedDate.toISOString(),
        maxCapacity,
      });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save event.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div
        className="modal-card"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
        <div className="modal-header">
          <h2 id="modal-title">
            {isEditing ? 'Update Event' : 'Create New Event'}
          </h2>
          <button
            type="button"
            className="btn-icon"
            onClick={onClose}
            aria-label="Close modal"
          >
            ✕
          </button>
        </div>

        {error && <div className="alert alert-error">{error}</div>}

        <form onSubmit={handleSubmit} className="form-stack">
          <div className="form-group">
            <label htmlFor="event-title">Event Title *</label>
            <input
              id="event-title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g., Q4 Engineering Architecture Review"
              required
              maxLength={150}
            />
          </div>

          <div className="form-group">
            <label htmlFor="event-description">Description</label>
            <textarea
              id="event-description"
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe the agenda, speakers, or prerequisites..."
              maxLength={2000}
            />
          </div>

          <div className="form-row-2">
            <div className="form-group">
              <label htmlFor="event-date">Date & Time *</label>
              <input
                id="event-date"
                type="datetime-local"
                value={dateTimeLocal}
                onChange={(e) => setDateTimeLocal(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="event-capacity">Max Capacity *</label>
              <input
                id="event-capacity"
                type="number"
                min={1}
                max={100000}
                value={maxCapacity}
                onChange={(e) =>
                  setMaxCapacity(parseInt(e.target.value || '0', 10))
                }
                required
              />
              {isEditing && initialEvent && initialEvent.registeredCount > 0 && (
                <span className="field-hint">
                  Currently {initialEvent.registeredCount} registered
                </span>
              )}
            </div>
          </div>

          <div className="modal-actions">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={onClose}
              disabled={submitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={submitting}
            >
              {submitting
                ? 'Saving...'
                : isEditing
                  ? 'Save Changes'
                  : 'Create Event'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

