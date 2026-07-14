import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';

const UserDashboard = () => {
  const [bookings, setBookings] = useState([]);
  const [favorites, setFavorites] = useState([]);
  const [myEvents, setMyEvents] = useState([]);
  const [loadingBookings, setLoadingBookings] = useState(true);
  const [loadingFavorites, setLoadingFavorites] = useState(true);
  const [error, setError] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const { user } = useAuth();

  const filteredBookings = bookings.filter((booking) => {
    const query = searchQuery.toLowerCase();
    return (
      booking.eventTitle.toLowerCase().includes(query) ||
      booking.categoryName.toLowerCase().includes(query) ||
      (booking.eventLocation && booking.eventLocation.toLowerCase().includes(query)) ||
      booking.status.toLowerCase().includes(query)
    );
  });

  const filteredFavorites = favorites.filter((fav) => {
    const query = searchQuery.toLowerCase();
    return (
      fav.eventTitle.toLowerCase().includes(query) ||
      fav.categoryName.toLowerCase().includes(query)
    );
  });

  const fetchBookings = async () => {
    try {
      setLoadingBookings(true);
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const response = await axios.get(`${baseUrl}/bookings/my`);
      setBookings(response.data);
    } catch (err) {
      console.error('Error fetching bookings:', err);
      setError('Failed to fetch bookings.');
    } finally {
      setLoadingBookings(false);
    }
  };

  const fetchFavorites = async () => {
    try {
      setLoadingFavorites(true);
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const response = await axios.get(`${baseUrl}/favorites`);
      setFavorites(response.data);
    } catch (err) {
      console.error('Error fetching favorites:', err);
    } finally {
      setLoadingFavorites(false);
    }
  };

  const fetchMyEvents = async () => {
    try {
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const response = await axios.get(`${baseUrl}/events/mine`);
      setMyEvents(response.data);
    } catch (err) {
      console.error('Error fetching created events:', err);
    }
  };

  const handleRemoveFavorite = async (eventId) => {
    try {
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      await axios.delete(`${baseUrl}/favorites/${eventId}`);
      // Filter out removed favorite
      setFavorites(favorites.filter((fav) => fav.eventId !== eventId));
    } catch (err) {
      console.error('Failed to remove favorite:', err);
    }
  };

  useEffect(() => {
    if (user) {
      fetchBookings();
      fetchFavorites();
      fetchMyEvents();
    }
  }, [user]);

  const getStatusBadgeClass = (status) => {
    switch (status) {
      case 'Yes':
        return 'bg-success text-white';
      case 'No':
        return 'bg-danger text-white';
      case 'Maybe':
      default:
        return 'bg-warning text-dark';
    }
  };

  return (
    <div className="container py-5">
      <div className="row mb-5">
        <div className="col">
          <div className="card border-0 bg-gradient-primary-dark text-white p-4 rounded-4 shadow-sm position-relative">
            <div className="position-relative z-1 d-flex flex-wrap justify-content-between align-items-center gap-3">
              <div>
                <h1 className="fw-bold mb-1">Hello, {user?.userName || 'User'}!</h1>
                <p className="text-white-50 mb-0">Manage your event registrations, bookmarks, and account details here.</p>
              </div>
              <div className="input-group rounded-pill overflow-hidden border border-secondary-soft" style={{ maxWidth: '350px', background: 'rgba(0,0,0,0.2)' }}>
                <span className="input-group-text bg-transparent border-0 text-white-50 ps-3">
                  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-search" viewBox="0 0 16 16">
                    <path d="M11.742 10.344a6.5 6.5 0 1 0-1.397 1.398h-.001q.044.06.098.115l3.85 3.85a1 1 0 0 0 1.415-1.414l-3.85-3.85a1 1 0 0 0-.115-.1zM12 6.5a5.5 5.5 0 1 1-11 0 5.5 5.5 0 0 1 11 0"/>
                  </svg>
                </span>
                <input 
                  type="text" 
                  className="form-control bg-transparent border-0 py-2 ps-1 shadow-none text-white" 
                  placeholder="Search bookings & favorites..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>
            </div>
            <div className="gradient-overlay"></div>
          </div>
        </div>
      </div>

      {/* My Created Events */}
      <div className="row mb-4">
        <div className="col">
          <div className="card border-0 shadow-sm rounded-4">
            <div className="card-body p-4">
              <div className="d-flex justify-content-between align-items-center mb-3">
                <h2 className="h4 fw-bold mb-0 d-flex align-items-center gap-2">
                  <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" fill="currentColor" className="bi bi-calendar-plus text-primary" viewBox="0 0 16 16">
                    <path d="M8 7a.5.5 0 0 1 .5.5V9H10a.5.5 0 0 1 0 1H8.5v1.5a.5.5 0 0 1-1 0V10H6a.5.5 0 0 1 0-1h1.5V7.5A.5.5 0 0 1 8 7"/>
                    <path d="M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5M1 4v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V4z"/>
                  </svg>
                  My Created Events ({myEvents.length})
                </h2>
                <Link to="/create" className="btn btn-primary btn-sm rounded-pill px-3">+ New Event</Link>
              </div>

              {myEvents.length === 0 ? (
                <div className="text-center py-4 text-muted border border-dashed rounded-3">
                  <p className="mb-0">You haven't created any events yet.</p>
                </div>
              ) : (
                <div className="row g-3">
                  {myEvents.map((evt) => (
                    <div key={evt.id} className="col-md-6 col-xl-4">
                      <div className="card border-light bg-light-soft shadow-xs rounded-3 h-100 p-3">
                        <span className="badge bg-secondary-soft text-secondary mb-2 align-self-start">{evt.categoryName}</span>
                        <h3 className="h6 fw-bold mb-1">
                          <Link to={`/event/${evt.id}`} className="text-dark text-decoration-none">{evt.title}</Link>
                        </h3>
                        <small className="text-muted d-block mb-2">
                          {new Date(evt.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}
                        </small>
                        <small className="text-muted">RSVPs: {evt.rsvpTotalCount ?? 0} (Yes {evt.rsvpYesCount ?? 0})</small>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      <div className="row g-4">
        {/* RSVPs Column */}
        <div className="col-lg-7">
          <div className="card border-0 shadow-sm rounded-4 h-100">
            <div className="card-body p-4">
              <h2 className="h4 fw-bold mb-4 d-flex align-items-center gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" fill="currentColor" className="bi bi-ticket-perforated-fill text-primary" viewBox="0 0 16 16">
                  <path d="M0 4.5A1.5 1.5 0 0 1 1.5 3h13A1.5 1.5 0 0 1 16 4.5V6a.5.5 0 0 1-.5.5 1.5 1.5 0 0 0 0 3 .5.5 0 0 1 .5.5v1.5a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 0 11.5V10a.5.5 0 0 1 .5-.5 1.5 1.5 0 0 0 0-3A.5.5 0 0 1 0 6zm4 1v1h8V5.5zm0 3v1h8v-1z"/>
                </svg>
                My RSVPs ({filteredBookings.length})
              </h2>

              {error && <div className="alert alert-danger border-0 small">{error}</div>}

              {loadingBookings ? (
                <div className="text-center py-5">
                  <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading RSVPs...</span>
                  </div>
                </div>
              ) : bookings.length === 0 ? (
                <div className="text-center py-5 text-muted border border-dashed rounded-3">
                  <p className="mb-0">You haven't RSVP'd to any events yet.</p>
                  <Link to="/" className="btn btn-outline-primary btn-sm rounded-pill mt-3 px-4">Browse Events</Link>
                </div>
              ) : filteredBookings.length === 0 ? (
                <div className="text-center py-5 text-muted border border-dashed rounded-3">
                  <p className="mb-0">No RSVPs match your search.</p>
                </div>
              ) : (
                <div className="d-flex flex-column gap-3">
                  {filteredBookings.map((booking) => (
                    <div key={booking.id} className="card border-light bg-light-soft shadow-xs rounded-3 p-3">
                      <div className="d-flex justify-content-between align-items-start gap-2 flex-wrap">
                        <div>
                          <span className="badge bg-secondary-soft text-secondary mb-2 me-2">
                            {booking.categoryName}
                          </span>
                          <span className={`badge ${getStatusBadgeClass(booking.status)} mb-2`}>
                            {booking.status}
                          </span>
                          <h3 className="h6 fw-bold mb-1">{booking.eventTitle}</h3>
                          <div className="text-muted small d-flex flex-column gap-1">
                            <span className="d-flex align-items-center gap-1">
                              <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" className="bi bi-calendar" viewBox="0 0 16 16">
                                <path d="M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5"/>
                              </svg>
                              {new Date(booking.eventDate).toLocaleDateString(undefined, {
                                month: 'short',
                                day: 'numeric',
                                year: 'numeric',
                                hour: '2-digit',
                                minute: '2-digit'
                              })}
                            </span>
                            {booking.eventLocation && (
                              <span className="d-flex align-items-center gap-1">
                                <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" className="bi bi-geo-alt" viewBox="0 0 16 16">
                                  <path d="M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10m0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6"/>
                                </svg>
                                {booking.eventLocation}
                              </span>
                            )}
                          </div>
                        </div>
                        <Link to={`/event/${booking.eventId}`} className="btn btn-outline-primary btn-sm rounded-pill px-3">
                          Details
                        </Link>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Favorites Column */}
        <div className="col-lg-5">
          <div className="card border-0 shadow-sm rounded-4 h-100">
            <div className="card-body p-4">
              <h2 className="h4 fw-bold mb-4 d-flex align-items-center gap-2">
                <svg xmlns="http://www.w3.org/2000/svg" width="22" height="22" fill="currentColor" className="bi bi-heart-fill text-danger" viewBox="0 0 16 16">
                  <path fillRule="evenodd" d="M8 1.314C12.438-3.248 23.534 4.735 8 15-7.534 4.736 3.562-3.248 8 1.314"/>
                </svg>
                My Favorites ({filteredFavorites.length})
              </h2>

              {loadingFavorites ? (
                <div className="text-center py-5">
                  <div className="spinner-border text-primary" role="status">
                    <span className="visually-hidden">Loading favorites...</span>
                  </div>
                </div>
              ) : favorites.length === 0 ? (
                <div className="text-center py-5 text-muted border border-dashed rounded-3">
                  <p className="mb-0">You haven't bookmarked any events.</p>
                </div>
              ) : filteredFavorites.length === 0 ? (
                <div className="text-center py-5 text-muted border border-dashed rounded-3">
                  <p className="mb-0">No favorites match your search.</p>
                </div>
              ) : (
                <div className="d-flex flex-column gap-3">
                  {filteredFavorites.map((fav) => (
                    <div key={fav.eventId} className="d-flex justify-content-between align-items-center p-3 rounded-3 border-bottom border-light">
                      <div className="pe-3">
                        <span className="badge bg-primary-soft text-primary mb-1 small">{fav.categoryName}</span>
                        <h4 className="h6 fw-bold mb-0 text-truncate" style={{ maxWidth: '200px' }}>
                          <Link to={`/event/${fav.eventId}`} className="text-dark text-decoration-none hover-primary">
                            {fav.eventTitle}
                          </Link>
                        </h4>
                        <small className="text-muted">
                          {new Date(fav.eventDate).toLocaleDateString(undefined, {
                            month: 'short',
                            day: 'numeric'
                          })}
                        </small>
                      </div>
                      <button
                        onClick={() => handleRemoveFavorite(fav.eventId)}
                        className="btn btn-outline-danger btn-sm rounded-circle p-2 d-flex align-items-center justify-content-center"
                        style={{ width: '32px', height: '32px' }}
                        title="Remove from Favorites"
                      >
                        <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" className="bi bi-trash" viewBox="0 0 16 16">
                          <path d="M5.5 5.5A.5.5 0 0 1 6 6v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5m2.5 0a.5.5 0 0 1 .5.5v6a.5.5 0 0 1-1 0V6a.5.5 0 0 1 .5-.5m3 .5a.5.5 0 0 0-1 0v6a.5.5 0 0 0 1 0z"/>
                          <path d="M14.5 3a1 1 0 0 1-1 1H13v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V4h-.5a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1H6a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1h3.5a1 1 0 0 1 1 1zM4.118 4 4 4.059V13a1 1 0 0 0 1 1h6a1 1 0 0 0 1-1V4.059L11.882 4zM2.5 3h11V2h-11z"/>
                        </svg>
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default UserDashboard;
