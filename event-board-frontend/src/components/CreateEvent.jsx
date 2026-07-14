import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';

const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';

const CreateEvent = () => {
  const navigate = useNavigate();

  const [categories, setCategories] = useState([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [date, setDate] = useState('');
  const [location, setLocation] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [imageFile, setImageFile] = useState(null);

  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const res = await axios.get(`${baseUrl}/categories`);
        setCategories(res.data);
        if (res.data.length > 0) setCategoryId(String(res.data[0].id));
      } catch (err) {
        console.error('Failed to load categories:', err);
        setError('Could not load categories.');
      }
    };
    fetchCategories();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (!title.trim() || !date || !categoryId) {
      setError('Title, date and category are required.');
      return;
    }

    try {
      setSubmitting(true);

      // 1. Upload the image first (if provided) and use the returned URL.
      let imageUrl = '';
      if (imageFile) {
        const formData = new FormData();
        formData.append('file', imageFile);
        const uploadRes = await axios.post(`${baseUrl}/events/upload-image`, formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        });
        imageUrl = uploadRes.data.imageUrl;
      }

      // 2. Create the event. OrganizerId is derived from the JWT server-side.
      const payload = {
        title: title.trim(),
        description: description.trim(),
        date: new Date(date).toISOString(),
        location: location.trim(),
        categoryId: parseInt(categoryId),
        imageUrl,
      };
      const res = await axios.post(`${baseUrl}/events`, payload);
      navigate(`/event/${res.data.id}`);
    } catch (err) {
      console.error('Failed to create event:', err);
      setError(err.response?.data?.title || err.response?.data || 'Failed to create event.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="container py-5" style={{ maxWidth: 720 }}>
      <h1 className="h3 fw-bold mb-4">Create an Event</h1>

      {error && (
        <div className="alert alert-danger" role="alert">
          {typeof error === 'string' ? error : 'Failed to create event.'}
        </div>
      )}

      <form onSubmit={handleSubmit} className="card border-0 shadow-sm p-4 rounded-3">
        <div className="mb-3">
          <label className="form-label fw-semibold">Title</label>
          <input
            type="text"
            className="form-control"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="e.g. Neighborhood Cleanup Day"
            required
          />
        </div>

        <div className="mb-3">
          <label className="form-label fw-semibold">Description</label>
          <textarea
            className="form-control"
            rows="4"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="What is this event about?"
          />
        </div>

        <div className="row">
          <div className="col-md-6 mb-3">
            <label className="form-label fw-semibold">Date &amp; Time</label>
            <input
              type="datetime-local"
              className="form-control"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              required
            />
          </div>
          <div className="col-md-6 mb-3">
            <label className="form-label fw-semibold">Category</label>
            <select
              className="form-select"
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
              required
            >
              {categories.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="mb-3">
          <label className="form-label fw-semibold">Location</label>
          <input
            type="text"
            className="form-control"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            placeholder="City / venue"
          />
        </div>

        <div className="mb-4">
          <label className="form-label fw-semibold">Image</label>
          <input
            type="file"
            className="form-control"
            accept="image/*"
            onChange={(e) => setImageFile(e.target.files?.[0] ?? null)}
          />
          <small className="text-muted">Optional. JPG, PNG, GIF or WEBP up to 5 MB.</small>
        </div>

        <div className="d-flex gap-2">
          <button type="submit" className="btn btn-primary rounded-pill px-4 fw-semibold" disabled={submitting}>
            {submitting && <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>}
            Create Event
          </button>
          <button type="button" className="btn btn-outline-secondary rounded-pill px-4" onClick={() => navigate('/')}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
};

export default CreateEvent;
