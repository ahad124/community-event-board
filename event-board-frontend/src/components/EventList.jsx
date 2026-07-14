import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';

const EventList = () => {
  const [events, setEvents] = useState([]);
  const [categories, setCategories] = useState([]);
  const [activeCategoryId, setActiveCategoryId] = useState('all');
  const [favoritedIds, setFavoritedIds] = useState(new Set());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();

  const fetchCategories = async () => {
    try {
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const response = await axios.get(`${baseUrl}/categories`);
      setCategories(response.data);
    } catch (err) {
      console.error('Error fetching categories:', err);
    }
  };

  const fetchEvents = async () => {
    try {
      setLoading(true);
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      let url = `${baseUrl}/events`;
      if (activeCategoryId !== 'all') {
        url = `${baseUrl}/events/category/${activeCategoryId}`;
      }
      const response = await axios.get(url);
      setEvents(response.data);
      setError(null);
    } catch (err) {
      console.error('Error fetching events:', err);
      setError('Failed to fetch events. Please verify the backend is running and try again.');
    } finally {
      setLoading(false);
    }
  };

  const fetchUserFavorites = async () => {
    if (!isAuthenticated) return;
    try {
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const response = await axios.get(`${baseUrl}/favorites`);
      const favIds = response.data.map(fav => fav.eventId);
      setFavoritedIds(new Set(favIds));
    } catch (err) {
      console.error('Error fetching user favorites:', err);
    }
  };

  const handleToggleFavorite = async (eventId, e) => {
    e.preventDefault(); // Prevent navigating to detail page if clicked inside card header
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    try {
      const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
      const res = await axios.post(`${baseUrl}/favorites/${eventId}`);
      const updatedFavs = new Set(favoritedIds);
      if (res.data.isFavorite) {
        updatedFavs.add(eventId);
      } else {
        updatedFavs.delete(eventId);
      }
      setFavoritedIds(updatedFavs);
    } catch (err) {
      console.error('Failed to toggle favorite:', err);
    }
  };

  useEffect(() => {
    fetchCategories();
  }, []);

  useEffect(() => {
    fetchEvents();
    fetchUserFavorites();
  }, [activeCategoryId, isAuthenticated]);

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-50 py-5">
        <div className="spinner-border text-primary role-status-spinner" role="status" style={{ width: '3rem', height: '3rem' }}>
          <span className="visually-hidden">Loading events...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container mt-4">
        <div className="alert alert-danger shadow-sm border-start border-danger border-4 d-flex align-items-center" role="alert">
          <svg className="bi flex-shrink-0 me-2" width="24" height="24" role="img" aria-label="Danger:" fill="currentColor" viewBox="0 0 16 16">
            <path d="M8.982 1.566a1.13 1.13 0 0 0-1.96 0L.165 13.233c-.457.778.091 1.767.98 1.767h13.713c.889 0 1.438-.99.98-1.767L8.982 1.566zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/>
          </svg>
          <div>
            <strong>Error:</strong> {error}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container py-4">
      <div className="row mb-4 align-items-center">
        <div className="col">
          <h1 className="fw-bold text-gradient display-5">Upcoming Events</h1>
          <p className="text-muted">Discover and join amazing events happening around you.</p>
        </div>
      </div>

      {/* Category Filter Pills */}
      <div className="d-flex flex-wrap gap-2 mb-4">
        <button
          onClick={() => setActiveCategoryId('all')}
          className={`btn btn-sm rounded-pill px-4 py-2 fw-semibold border-0 transition-all ${
            activeCategoryId === 'all' ? 'btn-primary text-white' : 'btn-light text-muted'
          }`}
        >
          All Events
        </button>
        {categories.map((cat) => (
          <button
            key={cat.id}
            onClick={() => setActiveCategoryId(cat.id)}
            className={`btn btn-sm rounded-pill px-4 py-2 fw-semibold border-0 transition-all ${
              activeCategoryId === cat.id ? 'btn-primary text-white' : 'btn-light text-muted'
            }`}
          >
            {cat.name}
          </button>
        ))}
      </div>

      {events.length === 0 ? (
        <div className="text-center py-5 shadow-sm rounded bg-light border">
          <p className="lead text-muted mb-0">No events found in this category.</p>
        </div>
      ) : (
        <div className="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4">
          {events.map((event) => (
            <div className="col" key={event.id}>
              <div className="card h-100 shadow-sm event-card border-0 overflow-hidden">
                {event.imageUrl ? (
                  <img
                    src={event.imageUrl}
                    alt={event.title}
                    className="card-img-top"
                    style={{ height: 160, objectFit: 'cover' }}
                    loading="lazy"
                  />
                ) : (
                  // Placeholder thumbnail so every event card has a visual
                  <div
                    className="card-img-top d-flex align-items-center justify-content-center bg-gradient-primary-dark text-white"
                    style={{ height: 160 }}
                    aria-label={`${event.title} thumbnail`}
                  >
                    <svg xmlns="http://www.w3.org/2000/svg" width="40" height="40" fill="currentColor" className="bi bi-calendar-event opacity-75" viewBox="0 0 16 16">
                      <path d="M11 6.5a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5z"/>
                      <path d="M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5M1 4v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V4z"/>
                    </svg>
                  </div>
                )}
                <div className="card-body d-flex flex-column p-4">
                  <div className="d-flex justify-content-between align-items-center mb-3">
                    <span className="badge rounded-pill bg-primary-soft text-primary px-3 py-2 fw-semibold">
                      {event.categoryName || 'General'}
                    </span>
                    <div className="d-flex align-items-center gap-2">
                      {/* Favorite Toggle Button */}
                      <button
                        onClick={(e) => handleToggleFavorite(event.id, e)}
                        className="btn btn-link p-0 border-0 text-decoration-none shadow-none text-danger"
                        title={favoritedIds.has(event.id) ? "Remove from Favorites" : "Add to Favorites"}
                      >
                        {favoritedIds.has(event.id) ? (
                          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" className="bi bi-heart-fill" viewBox="0 0 16 16">
                            <path fillRule="evenodd" d="M8 1.314C12.438-3.248 23.534 4.735 8 15-7.534 4.736 3.562-3.248 8 1.314"/>
                          </svg>
                        ) : (
                          <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" className="bi bi-heart" viewBox="0 0 16 16">
                            <path d="m8 2.748-.717-.737C5.6.281 2.514.878 1.4 3.053c-.523 1.023-.641 2.5.314 4.385.92 1.815 2.834 3.989 6.286 6.357 3.452-2.368 5.365-4.542 6.286-6.357.955-1.886.838-3.362.314-4.385C13.486.878 10.4.28 8.717 2.01zM8 15C-7.333 4.868 3.279-3.04 7.824 1.143c.06.055.119.112.176.171a3.12 3.12 0 0 1 .176-.17C12.72-3.042 23.333 4.867 8 15"/>
                          </svg>
                        )}
                      </button>
                      
                      <span className="text-muted small d-flex align-items-center">
                        <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" fill="currentColor" className="bi bi-calendar-event me-1" viewBox="0 0 16 16">
                          <path d="M11 6.5a.5.5 0 0 1 .5-.5h1a.5.5 0 0 1 .5.5v1a.5.5 0 0 1-.5.5h-1a.5.5 0 0 1-.5-.5z"/>
                          <path d="M3.5 0a.5.5 0 0 1 .5.5V1h8V.5a.5.5 0 0 1 1 0V1h1a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h1V.5a.5.5 0 0 1 .5-.5M1 4v10a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V4z"/>
                        </svg>
                        {new Date(event.date).toLocaleDateString(undefined, {
                          month: 'short',
                          day: 'numeric',
                          year: 'numeric',
                        })}
                      </span>
                    </div>
                  </div>
                  <h3 className="card-title h5 fw-bold text-dark mb-2 text-line-clamp-2">
                    {event.title}
                  </h3>
                  <p className="card-text text-muted mb-4 flex-grow-1 text-line-clamp-3">
                    {event.description}
                  </p>
                  <div className="pt-3 border-top d-flex justify-content-between align-items-center mt-auto">
                    <span className="text-muted small d-flex align-items-center">
                      <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" className="bi bi-geo-alt-fill text-danger me-1" viewBox="0 0 16 16">
                        <path d="M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10m0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6"/>
                      </svg>
                      {event.location || 'Online'}
                    </span>
                    <Link
                      to={`/event/${event.id}`}
                      className="btn btn-outline-primary btn-sm px-3 rounded-pill btn-hover-filled"
                    >
                      View Details
                    </Link>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default EventList;
