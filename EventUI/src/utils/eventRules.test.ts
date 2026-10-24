import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { EventItem } from '../types/event.ts';
import { validateEventForm, validateRegistration } from './eventRules.ts';

const fixedNow = new Date('2026-10-24T12:00:00.000Z');

function createMockEvent(overrides: Partial<EventItem> = {}): EventItem {
  return {
    id: 'evt-1',
    title: 'Cloud Architecture Summit',
    description: 'Deep dive into distributed systems',
    date: '2026-11-10T15:00:00.000Z',
    maxCapacity: 2,
    registeredCount: 1,
    availableSpots: 1,
    isPast: false,
    isFull: false,
    createdAt: '2026-10-01T12:00:00.000Z',
    updatedAt: '2026-10-01T12:00:00.000Z',
    registrations: [
      {
        id: 'reg-1',
        eventId: 'evt-1',
        userName: 'Alice Rivera',
        userEmail: 'alice@origin.dev',
        registeredAt: '2026-10-20T12:00:00.000Z',
      },
    ],
    ...overrides,
  };
}

describe('Frontend Event & Registration Business Logic', () => {
  it('validates event creation/update inputs and capacity limits', () => {
    const valid = validateEventForm({
      title: 'Engineering Demo Day',
      description: 'Quarterly showcase',
      dateInput: '2026-11-15T14:00',
      maxCapacity: 25,
    });
    assert.equal(valid.isValid, true);

    const invalidCapacityReduction = validateEventForm({
      title: 'Engineering Demo Day',
      description: 'Quarterly showcase',
      dateInput: '2026-11-15T14:00',
      maxCapacity: 1,
      currentRegisteredCount: 3,
    });
    assert.equal(invalidCapacityReduction.isValid, false);
  });

  it('enforces past-event, capacity, and duplicate registration rules', () => {
    const openEvent = createMockEvent();
    assert.equal(
      validateRegistration(openEvent, 'Bob Smith', 'bob@origin.dev', fixedNow).isValid,
      true
    );

    // Duplicate user on same event
    assert.equal(
      validateRegistration(openEvent, 'Alice', 'ALICE@origin.dev', fixedNow).isValid,
      false
    );

    // Past event
    const pastEvent = createMockEvent({ date: '2026-10-01T12:00:00.000Z', isPast: true });
    assert.equal(
      validateRegistration(pastEvent, 'Bob Smith', 'bob@origin.dev', fixedNow).isValid,
      false
    );

    // Full capacity event
    const fullEvent = createMockEvent({ maxCapacity: 1, registeredCount: 1, isFull: true });
    assert.equal(
      validateRegistration(fullEvent, 'Bob Smith', 'bob@origin.dev', fixedNow).isValid,
      false
    );
  });
});
