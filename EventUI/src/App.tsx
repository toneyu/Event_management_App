import React from 'react';
import { EventDetailPanel } from './components/EventDetailPanel';
import { EventFormModal } from './components/EventFormModal';
import { useEventsDashboard } from './hooks/useEventsDashboard';
import { FilterMode, calculateCapacityPercent } from './utils/eventRules';

const FILTER_TABS: [FilterMode, string][] = [
  ['all', 'All'],
  ['upcoming', 'Upcoming'],
  ['full', 'Full'],
  ['past', 'Past'],
];

/**
 * Presentational root component for the Event Management dashboard.
 * State and API handlers are managed by `useEventsDashboard`.
 */
export const App: React.FC = () => {
  const {
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
  } = useEventsDashboard();

  return (
    <div className="app-shell">
      <header className="top-nav">
        <div className="brand">
          <div className="brand-logo">O</div>
          <div>
            <h1>Origin Event Management</h1>
            <p className="brand-subtitle">
              Event Scheduling & Capacity Registration Portal
            </p>
          </div>
        </div>
        <div className="nav-actions">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => loadEvents(true)}
          >
            ↻ Refresh
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={handleOpenCreate}
          >
            + Create Event
          </button>
        </div>
      </header>

      <section className="kpi-grid">
        <div className="kpi-card">
          <span className="kpi-label">Total Events</span>
          <strong className="kpi-value">{stats.total}</strong>
        </div>
        <div className="kpi-card">
          <span className="kpi-label">Open & Upcoming</span>
          <strong className="kpi-value kpi-accent">{stats.upcomingOpen}</strong>
        </div>
        <div className="kpi-card">
          <span className="kpi-label">Events at Capacity</span>
          <strong className="kpi-value kpi-warn">{stats.fullCount}</strong>
        </div>
        <div className="kpi-card">
          <span className="kpi-label">Total Registrations</span>
          <strong className="kpi-value">{stats.totalRegistrations}</strong>
        </div>
      </section>

      {apiError && (
        <div className="alert alert-error banner-error">
          <div>
            <strong>Backend Connection Notice:</strong> {apiError}
          </div>
          <button
            type="button"
            className="btn btn-secondary btn-sm"
            onClick={() => loadEvents(true)}
          >
            Retry Connection
          </button>
        </div>
      )}

      <main className="workspace-grid">
        <section className="events-sidebar">
          <div className="sidebar-controls">
            <input
              type="search"
              className="search-input"
              placeholder="Search events or attendees..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            <div className="filter-tabs" role="tablist">
              {FILTER_TABS.map(([mode, label]) => (
                <button
                  key={mode}
                  type="button"
                  role="tab"
                  aria-selected={filterMode === mode}
                  className={`filter-tab ${filterMode === mode ? 'active' : ''}`}
                  onClick={() => setFilterMode(mode)}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          {loading ? (
            <div className="loading-box">Loading events...</div>
          ) : filteredEvents.length === 0 ? (
            <div className="empty-list-box">
              <p>No matching events found.</p>
              <button
                type="button"
                className="btn btn-primary btn-sm"
                onClick={handleOpenCreate}
              >
                + Create First Event
              </button>
            </div>
          ) : (
            <div className="event-card-list">
              {filteredEvents.map((ev) => {
                const isSelected = ev.id === selectedEventId;
                const pct = calculateCapacityPercent(
                  ev.registeredCount,
                  ev.maxCapacity
                );
                return (
                  <article
                    key={ev.id}
                    className={`event-card ${isSelected ? 'selected' : ''}`}
                    onClick={() => setSelectedEventId(ev.id)}
                  >
                    <div className="event-card-top">
                      <h3 className="event-card-title">{ev.title}</h3>
                      {ev.isPast ? (
                        <span className="badge badge-past">Past</span>
                      ) : ev.isFull ? (
                        <span className="badge badge-full">Full</span>
                      ) : (
                        <span className="badge badge-open">Open</span>
                      )}
                    </div>

                    <div className="event-card-date">
                      📅{' '}
                      {new Date(ev.date).toLocaleDateString(undefined, {
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </div>

                    <div className="event-card-footer">
                      <span className="capacity-text">
                        👥 {ev.registeredCount} / {ev.maxCapacity} registered
                      </span>
                      <div className="mini-progress">
                        <div
                          className={`mini-fill ${
                            ev.isFull ? 'fill-full' : 'fill-normal'
                          }`}
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </section>

        <section className="events-detail-column">
          <EventDetailPanel
            event={selectedEvent}
            onEdit={handleOpenEdit}
            onDelete={handleDeleteEvent}
            onRegister={handleRegisterAttendee}
            onUnregister={handleUnregisterAttendee}
          />
        </section>
      </main>

      <EventFormModal
        isOpen={isModalOpen}
        initialEvent={editingEvent}
        onClose={handleCloseModal}
        onSubmit={handleSaveEvent}
      />
    </div>
  );
};

export default App;
