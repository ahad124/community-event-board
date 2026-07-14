import { useState, useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';

const EventDetail = () => {
  const { id } = useParams();
  const [event, setEvent] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // RSVP & Favorite state
  const [rsvpStatus, setRsvpStatus] = useState(null); // 'Yes' | 'Maybe' | 'No' | null
  const [isFavorited, setIsFavorited] = useState(false);
  const [actionLoading, setActionLoading] = useState(false);

  // Weather state
  const [weather, setWeather] = useState(null);
  const [weatherLoading, setWeatherLoading] = useState(true);

  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const fetchEventDetail = async () => {
    try {
      setLoading(true);
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const response = await axios.get(`${baseUrl}/events/${id}`);
      setEvent(response.data);
      setError(null);
    } catch (err) {
      console.error('Error fetching event details:', err);
      setError('Failed to fetch event details. Please verify the event exists and try again.');
    } finally {
      setLoading(false);
    }
  };

  const checkUserRelations = async () => {
    if (!isAuthenticated) return;
    try {
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';

      // 1. Check favorites
      const favRes = await axios.get(`${baseUrl}/favorites`);
      const isFav = favRes.data.some((fav) => fav.eventId === parseInt(id));
      setIsFavorited(isFav);

      // 2. Check existing RSVP
      const bookRes = await axios.get(`${baseUrl}/bookings/my`);
      const existingRsvp = bookRes.data.find((b) => b.eventId === parseInt(id));
      setRsvpStatus(existingRsvp ? existingRsvp.status : null);
    } catch (err) {
      console.error('Error checking user event status:', err);
    }
  };

  const fetchWeather = async () => {
    try {
      setWeatherLoading(true);
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const res = await axios.get(`${baseUrl}/weather/event/${id}`);
      // Backend always returns a WeatherDto; `available: false` means show the fallback.
      setWeather(res.data?.available ? res.data : null);
    } catch (err) {
      console.error('Error fetching weather:', err);
      setWeather(null);
    } finally {
      setWeatherLoading(false);
    }
  };

  useEffect(() => {
    fetchEventDetail();
    fetchWeather();
  }, [id]);

  useEffect(() => {
    checkUserRelations();
  }, [id, isAuthenticated]);

  const handleRsvp = async (status) => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    try {
      setActionLoading(true);
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const res = await axios.post(`${baseUrl}/bookings`, { eventId: parseInt(id), status });
      setRsvpStatus(res.data.status);
      // Refresh the event so the RSVP counts reflect the change.
      await fetchEventDetail();
    } catch (err) {
      console.error('Failed to RSVP:', err);
      alert(err.response?.data || 'Failed to submit RSVP.');
    } finally {
      setActionLoading(false);
    }
  };

  const handleToggleFavorite = async () => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    try {
      setActionLoading(true);
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const res = await axios.post(`${baseUrl}/favorites/${parseInt(id)}`);
      setIsFavorited(res.data.isFavorite);
    } catch (err) {
      console.error('Failed to toggle favorite:', err);
    } finally {
      setActionLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-50 py-5">
        <div className="spinner-border text-primary role-status-spinner" role="status" style={{ width: '3rem', height: '3rem' }}>
          <span className="visually-hidden">Loading event details...</span>
        </div>
      </div>
    );
  }

  if (error || !event) {
    return (
      <div className="container mt-4">
        <div className="alert alert-danger shadow-sm border-start border-danger border-4 d-flex align-items-center mb-4" role="alert">
          <svg className="bi flex-shrink-0 me-2" width="24" height="24" role="img" aria-label="Danger:" fill="currentColor" viewBox="0 0 16 16">
            <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/>
          </svg>
          <div>
            <strong>Error:</strong> {error || 'Event details not found.'}
          </div>
        </div>
        <Link to="/" className="btn btn-primary rounded-pill px-4">
          Back to Events
        </Link>
      </div>
    );
  }

  return (
    <div className="container py-5">
      <div className="mb-4">
        <Link to="/" className="btn btn-link text-decoration-none d-inline-flex align-items-center ps-0 text-primary fw-semibold">
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-arrow-left me-2" viewBox="0 0 16 16">
            <path fillRule="evenodd" d="M15 8a.5.5 0 0 0-.5-.5H2.707l3.147-3.146a.5.5 0 1 0-.708-.708l-4 4a.5.5 0 0 0 0 .708l4 4a.5.5 0 0 0 .708-.708L2.707 8.5H14.5A.5.5 0 0 0 15 8"/>
          </svg>
          Back to Events
        </Link>
      </div>

      <div className="card shadow-lg border-0 overflow-hidden detail-card">
        {event.imageUrl && (
          <img
            src={event.imageUrl}
            alt={event.title}
            className="w-100"
            style={{ maxHeight: 340, objectFit: 'cover' }}
          />
        )}
        {/* Banner area with a gradient */}
        <div className="bg-gradient-primary-dark text-white p-5 position-relative">
          <div className="position-relative z-1">
            <div className="d-flex justify-content-between align-items-center mb-3 flex-wrap gap-2">
              <span className="badge rounded-pill bg-white text-primary px-3 py-2 fw-semibold">
                {event.categoryName || 'General'}
              </span>

              {/* Favorite Button */}
              <button
                onClick={handleToggleFavorite}
                disabled={actionLoading}
                className="btn btn-dark bg-opacity-25 rounded-circle p-2 d-flex align-items-center justify-content-center border-0 text-white"
                style={{ width: '40px', height: '40px' }}
                title={isFavorited ? "Remove from Favorites" : "Add to Favorites"}
              >
                {isFavorited ? (
                  <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="red" className="bi bi-heart-fill" viewBox="0 0 16 16">
                    <path fillRule="evenodd" d="M8 1.314C12.438-3.248 23.534 4.735 8 15-7.534 4.736 3.562-3.248 8 1.314"/>
                  </svg>
                ) : (
                  <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="white" className="bi bi-heart" viewBox="0 0 16 16">
                    <path d="m8 2.748-.717-.737C5.6.281 2.514.878 1.4 3.053c-.523 1.023-.641 2.5.314 4.385.92 1.815 2.834 3.989 6.286 6.357 3.452-2.368 5.365-4.542 6.286-6.357.955-1.886.838-3.362.314-4.385C13.486.878 10.4.28 8.717 2.01zM8 15C-7.333 4.868 3.279-3.04 7.824 1.143c.06.055.119.112.176.171a3.12 3.12 0 0 1 .176-.17C12.72-3.042 23.333 4.867 8 15"/>
                  </svg>
                )}
              </button>
            </div>
            
            <h1 className="fw-bold display-4 mb-3">{event.title}</h1>
            <div className="d-flex flex-wrap gap-4 text-white-50">
              <span className="d-flex align-items-center">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-calendar-event me-2 text-white" viewBox="0 0 16 16">
                  <path d="M11 6.5a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5z"/>
                  <path d="M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5M1 4v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V4z"/>
                </svg>
                {new Date(event.date).toLocaleDateString(undefined, {
                  weekday: 'long',
                  month: 'long',
                  day: 'numeric',
                  year: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit'
                })}
              </span>
              <span className="d-flex align-items-center">
                <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-geo-alt-fill me-2 text-white" viewBox="0 0 16 16">
                  <path d="M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10m0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6"/>
                </svg>
                {event.location || 'Online'}
              </span>
            </div>
          </div>
          <div className="gradient-overlay"></div>
        </div>

        <div className="card-body p-5">
          <div className="row g-5">
            <div className="col-lg-8">
              <h2 className="h4 fw-bold mb-3 border-bottom pb-2">About the Event</h2>
              <p className="lead text-muted fs-5 lh-base mb-4" style={{ whiteSpace: 'pre-line' }}>
                {event.description}
              </p>
            </div>

            <div className="col-lg-4">
              <div className="card bg-light border-0 p-4 rounded-3 h-100 shadow-xs">
                <h3 className="h5 fw-bold mb-4">Event Details</h3>
                
                <ul className="list-unstyled mb-4">
                  <li className="mb-3 d-flex align-items-start gap-3">
                    <span className="p-2 rounded bg-primary-soft text-primary d-inline-flex">
                      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-envelope-fill" viewBox="0 0 16 16">
                        <path d="M.05 3.555A2 2 0 0 1 2 2h12a2 2 0 0 1 1.95 1.555L8 8.414zM0 4.697v7.104l5.803-3.558zM6.761 8.83l-6.57 4.027A2 2 0 0 0 2 14h12a2 2 0 0 0 1.808-1.144l-6.57-4.027L8 9.586zm3.436-.586L16 11.801V4.697z"/>
                      </svg>
                    </span>
                    <div>
                      <small className="text-muted d-block">Organizer Email</small>
                      <strong className="text-dark">{event.organizerEmail || 'Unknown'}</strong>
                    </div>
                  </li>

                  <li className="mb-3 d-flex align-items-start gap-3">
                    <span className="p-2 rounded bg-primary-soft text-primary d-inline-flex">
                      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-tag-fill" viewBox="0 0 16 16">
                        <path d="M2 1a1 1 0 0 0-1 1v4.586a1 1 0 0 0 .293.707l7 7a1 1 0 0 0 1.414 0l4.586-4.586a1 1 0 0 0 0-1.414l-7-7A1 1 0 0 0 6.586 1zm4 3.5a1.5 1.5 0 1 1-3 0 1.5 1.5 0 0 1 3 0"/>
                      </svg>
                    </span>
                    <div>
                      <small className="text-muted d-block">Category</small>
                      <strong className="text-dark">{event.categoryName || 'General'}</strong>
                    </div>
                  </li>
                </ul>

                {/* Weather block */}
                <div className="mb-4">
                  <small className="text-muted d-block mb-2 fw-semibold text-uppercase" style={{ letterSpacing: '.05em', fontSize: '.7rem' }}>
                    Current Weather
                  </small>
                  {weatherLoading ? (
                    <div className="d-flex align-items-center gap-2 text-muted small">
                      <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                      Loading weather…
                    </div>
                  ) : weather ? (
                    <div className="d-flex align-items-center gap-3 p-3 rounded-3 bg-primary-soft">
                      {weather.icon && (
                        <img
                          src={`https://openweathermap.org/img/wn/${weather.icon}@2x.png`}
                          alt={weather.description}
                          width="56"
                          height="56"
                        />
                      )}
                      <div>
                        <div className="fw-bold text-dark fs-4">{Math.round(weather.temperatureC)}°C</div>
                        <div className="text-muted small text-capitalize">{weather.description}</div>
                        <div className="text-muted small">
                          {weather.city}
                          {weather.humidity ? ` · ${weather.humidity}% humidity` : ''}
                        </div>
                      </div>
                    </div>
                  ) : (
                    <p className="text-muted small mb-0 fst-italic">
                      Weather information is currently unavailable.
                    </p>
                  )}

                  {/* Short forecast */}
                  {weather?.forecast?.length > 0 && (
                    <div className="mt-3">
                      <small className="text-muted d-block mb-2 fw-semibold text-uppercase" style={{ letterSpacing: '.05em', fontSize: '.7rem' }}>
                        Forecast
                      </small>
                      <div className="d-flex gap-2 flex-wrap">
                        {weather.forecast.map((f, idx) => (
                          <div key={idx} className="text-center p-2 rounded-3 bg-light border flex-fill" style={{ minWidth: 64 }}>
                            <div className="small fw-semibold">
                              {new Date(f.dateTime).toLocaleDateString(undefined, { weekday: 'short' })}
                            </div>
                            {f.icon && (
                              <img
                                src={`https://openweathermap.org/img/wn/${f.icon}.png`}
                                alt={f.description}
                                width="40"
                                height="40"
                              />
                            )}
                            <div className="small fw-bold">{Math.round(f.temperatureC)}°C</div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>

                {/* RSVP tallies */}
                <div className="mb-3">
                  <small className="text-muted d-block mb-2 fw-semibold text-uppercase" style={{ letterSpacing: '.05em', fontSize: '.7rem' }}>
                    RSVPs ({event.rsvpTotalCount ?? 0})
                  </small>
                  <div className="d-flex gap-2">
                    <span className="badge rounded-pill bg-success-subtle text-success flex-fill py-2">
                      Yes {event.rsvpYesCount ?? 0}
                    </span>
                    <span className="badge rounded-pill bg-warning-subtle text-dark flex-fill py-2">
                      Maybe {event.rsvpMaybeCount ?? 0}
                    </span>
                    <span className="badge rounded-pill bg-danger-subtle text-danger flex-fill py-2">
                      No {event.rsvpNoCount ?? 0}
                    </span>
                  </div>
                </div>

                {/* RSVP action */}
                <small className="text-muted d-block mb-2 fw-semibold text-uppercase" style={{ letterSpacing: '.05em', fontSize: '.7rem' }}>
                  {isAuthenticated ? 'Your RSVP' : 'RSVP'}
                </small>
                {isAuthenticated ? (
                  <div className="btn-group w-100" role="group" aria-label="RSVP options">
                    {['Yes', 'Maybe', 'No'].map((option) => (
                      <button
                        key={option}
                        type="button"
                        onClick={() => handleRsvp(option)}
                        disabled={actionLoading}
                        className={`btn fw-semibold ${
                          rsvpStatus === option ? 'btn-primary' : 'btn-outline-primary'
                        }`}
                      >
                        {option}
                      </button>
                    ))}
                  </div>
                ) : (
                  <button
                    onClick={() => navigate('/login')}
                    className="btn btn-primary w-100 rounded-pill py-2 fw-semibold shadow-sm"
                  >
                    Sign In to RSVP
                  </button>
                )}
                {rsvpStatus && (
                  <div className="text-center small text-muted mt-2">
                    You responded <strong>{rsvpStatus}</strong>.
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default EventDetail;
