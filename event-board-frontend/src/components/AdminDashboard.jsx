import { useState, useEffect } from 'react';
import axios from 'axios';
import { useAuth } from '../context/AuthContext';

const AdminDashboard = () => {
  const [activeTab, setActiveTab] = useState('events'); // events, categories, bookings, users
  const [events, setEvents] = useState([]);
  const [categories, setCategories] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [users, setUsers] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  // Form states for creating/editing events
  const [showEventForm, setShowEventForm] = useState(false);
  const [editingEventId, setEditingEventId] = useState(null);
  const [eventTitle, setEventTitle] = useState('');
  const [eventDesc, setEventDesc] = useState('');
  const [eventDate, setEventDate] = useState('');
  const [eventLocation, setEventLocation] = useState('');
  const [eventCategoryId, setEventCategoryId] = useState('');
  const [eventImageUrl, setEventImageUrl] = useState(''); // existing/uploaded image URL
  const [eventImageFile, setEventImageFile] = useState(null); // newly selected file
  const [imageUploading, setImageUploading] = useState(false);

  // Form states for creating category
  const [categoryName, setCategoryName] = useState('');

  // Selected event filter for bookings moderation
  const [selectedEventId, setSelectedEventId] = useState('');

  const { user } = useAuth();

  const fetchData = async () => {
    setLoading(true);
    setError('');
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    try {
      if (activeTab === 'events') {
        const res = await axios.get(`${baseUrl}/events`);
        setEvents(res.data);
        const catRes = await axios.get(`${baseUrl}/categories`);
        setCategories(catRes.data);
      } else if (activeTab === 'categories') {
        const res = await axios.get(`${baseUrl}/categories`);
        setCategories(res.data);
      } else if (activeTab === 'bookings') {
        const evRes = await axios.get(`${baseUrl}/events`);
        setEvents(evRes.data);
        
        let url = `${baseUrl}/bookings`;
        if (selectedEventId && selectedEventId !== 'all') {
          url = `${baseUrl}/bookings/event/${selectedEventId}`;
        } else if (!selectedEventId) {
          setSelectedEventId('all');
        }
        
        const bookRes = await axios.get(url);
        setBookings(bookRes.data);
      } else if (activeTab === 'users') {
        const res = await axios.get(`${baseUrl}/users`);
        setUsers(res.data);
      }
    } catch (err) {
      console.error(err);
      setError('Failed to fetch dashboard data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [activeTab, selectedEventId]);

  // Load summary statistics once for the dashboard header.
  useEffect(() => {
    const fetchStats = async () => {
      try {
        const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
        const res = await axios.get(`${baseUrl}/reports/stats`);
        setStats(res.data);
      } catch (err) {
        console.error('Failed to load stats:', err);
      }
    };
    fetchStats();
  }, [activeTab]);

  // Handle Event submit (create or update)
  const handleEventSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    try {
      // If a new image file was selected, upload it first and use the returned URL.
      let imageUrl = eventImageUrl;
      if (eventImageFile) {
        setImageUploading(true);
        const formData = new FormData();
        formData.append('file', eventImageFile);
        const uploadRes = await axios.post(`${baseUrl}/events/upload-image`, formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        });
        imageUrl = uploadRes.data.imageUrl;
        setImageUploading(false);
      }

      const payload = {
        title: eventTitle,
        description: eventDesc,
        date: eventDate,
        location: eventLocation,
        imageUrl: imageUrl || null,
        categoryId: parseInt(eventCategoryId),
        organizerId: user.id
      };

      if (editingEventId) {
        await axios.put(`${baseUrl}/events/${editingEventId}`, payload);
        setSuccess('Event updated successfully!');
      } else {
        await axios.post(`${baseUrl}/events`, payload);
        setSuccess('Event created successfully!');
      }

      // Reset
      resetEventForm();
      fetchData();
    } catch (err) {
      console.error(err);
      setImageUploading(false);
      setError(err.response?.data?.message || err.response?.data || 'Failed to save event.');
    }
  };

  const resetEventForm = () => {
    setShowEventForm(false);
    setEditingEventId(null);
    setEventTitle('');
    setEventDesc('');
    setEventDate('');
    setEventLocation('');
    setEventCategoryId('');
    setEventImageUrl('');
    setEventImageFile(null);
  };

  const handleEditEvent = (evt) => {
    setEditingEventId(evt.id);
    setEventTitle(evt.title);
    setEventDesc(evt.description || '');
    // Format date string to match datetime-local input (YYYY-MM-DDTHH:MM)
    const formattedDate = new Date(evt.date).toISOString().slice(0, 16);
    setEventDate(formattedDate);
    setEventLocation(evt.location || '');
    setEventCategoryId(evt.categoryId);
    setEventImageUrl(evt.imageUrl || '');
    setEventImageFile(null);
    setShowEventForm(true);
  };

  const handleDeleteEvent = async (id) => {
    if (!window.confirm('Are you sure you want to delete this event?')) return;
    setError('');
    setSuccess('');
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    try {
      await axios.delete(`${baseUrl}/events/${id}`);
      setSuccess('Event deleted successfully.');
      fetchData();
    } catch (err) {
      console.error(err);
      setError('Failed to delete event.');
    }
  };

  // Categories CRUD operations
  const handleCreateCategory = async (e) => {
    e.preventDefault();
    if (!categoryName.trim()) return;
    setError('');
    setSuccess('');
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    try {
      await axios.post(`${baseUrl}/categories`, { name: categoryName });
      setSuccess(`Category "${categoryName}" added successfully.`);
      setCategoryName('');
      fetchData();
    } catch (err) {
      console.error(err);
      setError(err.response?.data || 'Failed to create category.');
    }
  };

  const handleDeleteCategory = async (id) => {
    if (!window.confirm('Delete this category?')) return;
    setError('');
    setSuccess('');
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    try {
      await axios.delete(`${baseUrl}/categories/${id}`);
      setSuccess('Category deleted.');
      fetchData();
    } catch (err) {
      console.error(err);
      setError(err.response?.data || 'Failed to delete category.');
    }
  };

  // User management operations
  const handleUpdateUserRole = async (userId, role) => {
    setError('');
    setSuccess('');
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    try {
      await axios.put(`${baseUrl}/users/${userId}/role`, { role });
      setSuccess(`User role updated to ${role}.`);
      fetchData();
    } catch (err) {
      console.error(err);
      setError(err.response?.data || 'Failed to update user role.');
    }
  };

  const handleToggleUserStatus = async (userId, isActive) => {
    setError('');
    setSuccess('');
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    try {
      await axios.put(`${baseUrl}/users/${userId}/status`, { isActive });
      setSuccess(isActive ? 'Account enabled.' : 'Account disabled.');
      fetchData();
    } catch (err) {
      console.error(err);
      setError(err.response?.data || 'Failed to update account status.');
    }
  };

  return (
    <div className="container py-5">
      <div className="row mb-5 align-items-center">
        <div className="col-md-8">
          <h1 className="fw-bold text-gradient display-5">Admin Dashboard</h1>
          <p className="text-muted mb-0">Manage users, events, categories, and view RSVPs.</p>
        </div>
        <div className="col-md-4 text-md-end mt-3 mt-md-0">
          <button 
            onClick={() => {
              setActiveTab('events');
              setShowEventForm(true);
              setEditingEventId(null);
            }} 
            className="btn btn-primary rounded-pill px-4 shadow-xs"
          >
            + Create Event
          </button>
        </div>
      </div>

      {/* Statistics */}
      {stats && (
        <div className="row g-3 mb-4">
          {[
            { label: 'Users', value: stats.totalUsers, icon: '👥' },
            { label: 'Events', value: stats.totalEvents, icon: '📅' },
            { label: 'Categories', value: stats.totalCategories, icon: '🏷️' },
            { label: 'RSVPs', value: stats.totalRsvps, icon: '🎟️' },
            { label: 'Favorites', value: stats.totalFavorites, icon: '❤️' },
          ].map((s) => (
            <div key={s.label} className="col-6 col-md">
              <div className="card border-0 shadow-sm rounded-4 h-100 text-center p-3">
                <div style={{ fontSize: '1.5rem' }}>{s.icon}</div>
                <div className="h3 fw-bold mb-0">{s.value}</div>
                <small className="text-muted">{s.label}</small>
              </div>
            </div>
          ))}
        </div>
      )}
      {stats && (
        <p className="text-muted small mb-4">
          RSVP breakdown — Yes: {stats.yesRsvps} · Maybe: {stats.maybeRsvps} · No: {stats.noRsvps}
        </p>
      )}

      {error && <div className="alert alert-danger border-0 shadow-xs mb-4">{error}</div>}
      {success && <div className="alert alert-success border-0 shadow-xs mb-4">{success}</div>}

      <div className="row g-4">
        {/* Navigation Pills */}
        <div className="col-md-3">
          <div className="card border-0 shadow-sm rounded-4 p-3">
            <div className="nav flex-column nav-pills gap-2">
              <button
                className={`nav-link text-start rounded-pill py-2.5 px-4 fw-semibold ${activeTab === 'events' ? 'active bg-primary' : 'text-dark hover-light'}`}
                onClick={() => { setActiveTab('events'); resetEventForm(); }}
              >
                Manage Events
              </button>
              <button
                className={`nav-link text-start rounded-pill py-2.5 px-4 fw-semibold ${activeTab === 'categories' ? 'active bg-primary' : 'text-dark hover-light'}`}
                onClick={() => setActiveTab('categories')}
              >
                Manage Categories
              </button>
              <button
                className={`nav-link text-start rounded-pill py-2.5 px-4 fw-semibold ${activeTab === 'bookings' ? 'active bg-primary' : 'text-dark hover-light'}`}
                onClick={() => setActiveTab('bookings')}
              >
                Event RSVPs
              </button>
              <button
                className={`nav-link text-start rounded-pill py-2.5 px-4 fw-semibold ${activeTab === 'users' ? 'active bg-primary' : 'text-dark hover-light'}`}
                onClick={() => setActiveTab('users')}
              >
                Manage Users
              </button>
            </div>
          </div>
        </div>

        {/* Workspace Column */}
        <div className="col-md-9">
          <div className="card border-0 shadow-sm rounded-4 p-4 min-vh-50">
            {/* EVENTS TAB */}
            {activeTab === 'events' && (
              <div>
                <div className="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
                  <h2 className="h4 fw-bold mb-0">Events Management</h2>
                  {!showEventForm && (
                    <button onClick={() => setShowEventForm(true)} className="btn btn-primary rounded-pill px-4 btn-sm">
                      + Add New Event
                    </button>
                  )}
                </div>

                {showEventForm ? (
                  <div className="card bg-light border-0 p-4 rounded-3 mb-4">
                    <h3 className="h6 fw-bold mb-3">{editingEventId ? 'Edit Event' : 'Create Event'}</h3>
                    <form onSubmit={handleEventSubmit}>
                      <div className="row g-3">
                        <div className="col-md-6">
                          <label className="form-label text-muted small">Event Title</label>
                          <input type="text" className="form-control rounded-3" value={eventTitle} onChange={(e)=>setEventTitle(e.target.value)} required />
                        </div>
                        <div className="col-md-6">
                          <label className="form-label text-muted small">Category</label>
                          <select className="form-select rounded-3" value={eventCategoryId} onChange={(e)=>setEventCategoryId(e.target.value)} required>
                            <option value="">Select Category</option>
                            {categories.map((cat)=>(
                              <option key={cat.id} value={cat.id}>{cat.name}</option>
                            ))}
                          </select>
                        </div>
                        <div className="col-md-6">
                          <label className="form-label text-muted small">Date & Time</label>
                          <input type="datetime-local" className="form-control rounded-3" value={eventDate} onChange={(e)=>setEventDate(e.target.value)} required />
                        </div>
                        <div className="col-md-6">
                          <label className="form-label text-muted small">Location</label>
                          <input type="text" className="form-control rounded-3" value={eventLocation} onChange={(e)=>setEventLocation(e.target.value)} />
                        </div>
                        <div className="col-12">
                          <label className="form-label text-muted small">Description</label>
                          <textarea className="form-control rounded-3" rows="3" value={eventDesc} onChange={(e)=>setEventDesc(e.target.value)} required></textarea>
                        </div>
                        <div className="col-12">
                          <label className="form-label text-muted small">Event Image</label>
                          <div className="d-flex align-items-center gap-3">
                            {(eventImageFile || eventImageUrl) && (
                              <img
                                src={eventImageFile ? URL.createObjectURL(eventImageFile) : eventImageUrl}
                                alt="Event preview"
                                className="rounded-3"
                                style={{ width: 72, height: 72, objectFit: 'cover' }}
                              />
                            )}
                            <input
                              type="file"
                              accept="image/png, image/jpeg, image/gif, image/webp"
                              className="form-control rounded-3"
                              onChange={(e) => setEventImageFile(e.target.files?.[0] || null)}
                            />
                          </div>
                          <small className="text-muted">PNG, JPG, GIF or WEBP · max 5&nbsp;MB{editingEventId && eventImageUrl && !eventImageFile ? ' · leave empty to keep current image' : ''}</small>
                        </div>
                      </div>
                      <div className="d-flex justify-content-end gap-2 mt-4">
                        <button type="button" onClick={resetEventForm} className="btn btn-outline-secondary rounded-pill px-4 btn-sm">
                          Cancel
                        </button>
                        <button type="submit" className="btn btn-primary rounded-pill px-4 btn-sm" disabled={imageUploading}>
                          {imageUploading ? 'Uploading…' : (editingEventId ? 'Save Changes' : 'Create Event')}
                        </button>
                      </div>
                    </form>
                  </div>
                ) : null}

                {loading ? (
                  <div className="text-center py-5"><div className="spinner-border text-primary" role="status"></div></div>
                ) : events.length === 0 ? (
                  <div className="table-empty">
                    <div className="table-empty-icon">📅</div>
                    <p className="table-empty-title">No events yet</p>
                    <p className="table-empty-text">Create your first event to see it listed here.</p>
                  </div>
                ) : (
                  <div className="admin-table">
                    <div className="table-responsive">
                      <table className="table align-middle">
                        <thead>
                          <tr>
                            <th>Event</th>
                            <th>Category</th>
                            <th>Date</th>
                            <th>Location</th>
                            <th className="text-center">RSVPs</th>
                            <th className="text-center">Actions</th>
                          </tr>
                        </thead>
                        <tbody>
                          {events.map((evt) => (
                            <tr key={evt.id}>

                              <td data-label="Event">
                                <div className="event-title">
                                  {evt.title}
                                </div>
                              </td>

                              <td data-label="Category">
                                <span className="category-badge">
                                  {evt.categoryName}
                                </span>
                              </td>

                              <td data-label="Date">
                                <span className="cell-subtle">
                                  <span className="cell-icon">📅</span>
                                  {new Date(evt.date).toLocaleDateString(undefined, {
                                    month: "short",
                                    day: "numeric",
                                    year: "numeric",
                                  })}
                                </span>
                              </td>

                              <td data-label="Location">
                                {evt.location ? (
                                  <span className="event-location">
                                    <span className="cell-icon">📍</span>
                                    {evt.location}
                                  </span>
                                ) : (
                                  <span className="location-pill">Online</span>
                                )}
                              </td>

                              <td data-label="RSVPs" className="text-center">
                                <span className="badge bg-primary-soft text-primary" title={`Yes ${evt.rsvpYesCount} · Maybe ${evt.rsvpMaybeCount} · No ${evt.rsvpNoCount}`}>
                                  {evt.rsvpTotalCount ?? 0}
                                </span>
                              </td>

                              <td data-label="Actions">
                                <div className="action-buttons">

                                  <button
                                    onClick={() => handleEditEvent(evt)}
                                    className="action-btn edit-btn"
                                    title="Edit"
                                  >
                                    ✏️
                                  </button>

                                  <button
                                    onClick={() => handleDeleteEvent(evt.id)}
                                    className="action-btn delete-btn"
                                    title="Delete"
                                  >
                                    🗑️
                                  </button>

                                </div>
                              </td>

                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* CATEGORIES TAB */}
            {activeTab === 'categories' && (
              <div>
                <h2 className="h4 fw-bold mb-4">Categories Management</h2>
                <form onSubmit={handleCreateCategory} className="mb-4">
                  <div className="input-group">
                    <input
                      type="text"
                      className="form-control rounded-start-pill py-2.5 px-4 shadow-none"
                      placeholder="Enter category name (e.g., Hackathon)"
                      value={categoryName}
                      onChange={(e) => setCategoryName(e.target.value)}
                      required
                    />
                    <button type="submit" className="btn btn-primary rounded-end-pill px-4">
                      Add Category
                    </button>
                  </div>
                </form>

                {loading ? (
                  <div className="text-center py-5"><div className="spinner-border text-primary" role="status"></div></div>
                ) : categories.length === 0 ? (
                  <div className="table-empty">
                    <div className="table-empty-icon">🏷️</div>
                    <p className="table-empty-title">No categories yet</p>
                    <p className="table-empty-text">Add a category above to get started.</p>
                  </div>
                ) : (
                  <div className="admin-panel">
                    {categories.map((cat) => (
                      <div key={cat.id} className="admin-panel-row">
                        <span className="category-name">
                          <span className="cell-icon">🏷️</span>
                          {cat.name}
                        </span>
                        <button
                          onClick={() => handleDeleteCategory(cat.id)}
                          className="action-btn delete-btn"
                          title="Delete Category"
                        >
                          🗑️
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {/* BOOKINGS TAB */}
            {activeTab === 'bookings' && (
              <div>
                <h2 className="h4 fw-bold mb-4">Event RSVPs</h2>

                <div className="mb-4 col-md-6">
                  <label className="form-label text-muted small">Select Event to Filter</label>
                  <select
                    className="form-select rounded-3"
                    value={selectedEventId}
                    onChange={(e) => setSelectedEventId(e.target.value)}
                  >
                    <option value="all">All RSVPs (Across All Events)</option>
                    {events.map((evt) => (
                      <option key={evt.id} value={evt.id}>{evt.title}</option>
                    ))}
                  </select>
                </div>

                {loading ? (
                  <div className="text-center py-5"><div className="spinner-border text-primary" role="status"></div></div>
                ) : bookings.length === 0 ? (
                  <div className="table-empty">
                    <div className="table-empty-icon">🎟️</div>
                    <p className="table-empty-title">No RSVPs found</p>
                    <p className="table-empty-text">RSVPs for the selected event will appear here.</p>
                  </div>
                ) : (
                  <div className="admin-table">
                    <div className="table-responsive">
                      <table className="table align-middle">
                        <thead>
                          <tr>
                            <th>Event</th>
                            <th>User Email</th>
                            <th>RSVP Date</th>
                            <th>Response</th>
                          </tr>
                        </thead>
                        <tbody>
                        {bookings.map((booking) => (
                          <tr key={booking.id}>
                            <td data-label="Event">
                              <div className="event-title">{booking.eventTitle}</div>
                              <span className="category-badge mt-1">{booking.categoryName}</span>
                            </td>
                            <td data-label="User Email">
                              <span className="cell-subtle">{booking.userEmail}</span>
                            </td>
                            <td data-label="RSVP Date">
                              <span className="cell-subtle">
                                <span className="cell-icon">📅</span>
                                {new Date(booking.bookingDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}
                              </span>
                            </td>
                            <td data-label="Response">
                              <span className={`badge ${
                                booking.status === 'Yes' ? 'bg-success' : booking.status === 'No' ? 'bg-danger' : 'bg-warning text-dark'
                              }`}>
                                {booking.status}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* USERS TAB */}
            {activeTab === 'users' && (
              <div>
                <h2 className="h4 fw-bold mb-4">Users Management</h2>

                {loading ? (
                  <div className="text-center py-5"><div className="spinner-border text-primary" role="status"></div></div>
                ) : users.length === 0 ? (
                  <div className="table-empty">
                    <div className="table-empty-icon">👥</div>
                    <p className="table-empty-title">No users found</p>
                  </div>
                ) : (
                  <div className="admin-table">
                    <div className="table-responsive">
                      <table className="table align-middle">
                        <thead>
                          <tr>
                            <th>User</th>
                            <th>Role</th>
                            <th>Status</th>
                            <th className="text-end">Actions</th>
                          </tr>
                        </thead>
                        <tbody>
                          {users.map((u) => {
                            const isSelf = u.id === user?.id;
                            return (
                              <tr key={u.id}>
                                <td data-label="User">
                                  <div className="event-title">{u.userName}</div>
                                  <span className="cell-subtle">{u.email}</span>
                                </td>
                                <td data-label="Role">
                                  <span className={`badge ${u.role === 'Admin' ? 'bg-primary' : 'bg-secondary'}`}>
                                    {u.role}
                                  </span>
                                </td>
                                <td data-label="Status">
                                  <span className={`badge ${u.isActive ? 'bg-success' : 'bg-danger'}`}>
                                    {u.isActive ? 'Active' : 'Disabled'}
                                  </span>
                                </td>
                                <td data-label="Actions" className="text-end">
                                  {u.role === 'Admin' ? (
                                    <button
                                      onClick={() => handleUpdateUserRole(u.id, 'User')}
                                      className="btn btn-outline-secondary btn-sm rounded-pill px-3 me-1"
                                      disabled={isSelf}
                                      title={isSelf ? 'You cannot change your own role' : 'Demote to User'}
                                    >
                                      Demote
                                    </button>
                                  ) : (
                                    <button
                                      onClick={() => handleUpdateUserRole(u.id, 'Admin')}
                                      className="btn btn-outline-primary btn-sm rounded-pill px-3 me-1"
                                    >
                                      Promote
                                    </button>
                                  )}
                                  {u.isActive ? (
                                    <button
                                      onClick={() => handleToggleUserStatus(u.id, false)}
                                      className="btn btn-danger btn-sm rounded-pill px-3"
                                      disabled={isSelf}
                                      title={isSelf ? 'You cannot disable your own account' : 'Disable account'}
                                    >
                                      Disable
                                    </button>
                                  ) : (
                                    <button
                                      onClick={() => handleToggleUserStatus(u.id, true)}
                                      className="btn btn-success btn-sm rounded-pill px-3"
                                    >
                                      Enable
                                    </button>
                                  )}
                                </td>
                              </tr>
                            );
                          })}
                        </tbody>
                      </table>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboard;

