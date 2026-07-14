import { BrowserRouter as Router, Routes, Route, Link, Navigate, useLocation } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import EventList from './components/EventList';
import EventDetail from './components/EventDetail';
import LoginRegister from './components/LoginRegister';
import UserDashboard from './components/UserDashboard';
import AdminDashboard from './components/AdminDashboard';
import CreateEvent from './components/CreateEvent';
import './App.css';

// Protected Route Component
const ProtectedRoute = ({ children, allowedRoles }) => {
  const { isAuthenticated, user, loading } = useAuth();
  const location = useLocation();

  if (loading) {
    return (
      <div className="d-flex justify-content-center align-items-center min-vh-50 py-5">
        <div className="spinner-border text-primary" role="status">
          <span className="visually-hidden">Loading...</span>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user?.role)) {
    return <Navigate to="/" replace />;
  }

  return children;
};

// Navbar Component with authentication logic
const NavigationBar = () => {
  const { isAuthenticated, user, logout } = useAuth();
  const location = useLocation();
  const isAdmin = user?.role === 'Admin';
  const onLoginPage = location.pathname === '/login';

  return (
    <nav className="navbar navbar-expand-lg navbar-dark bg-dark-glass sticky-top shadow-sm py-3">
      <div className="container">
        <Link to="/" className="navbar-brand d-flex align-items-center gap-2 fw-bold text-gradient-nav">
          <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="currentColor" className="bi bi-grid-3x3-gap-fill text-primary" viewBox="0 0 16 16">
            <path d="M1 2a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1zm5 0a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1zm5 0a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1zM1 7a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1zm5 0a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1zm5 0a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1zM1 12a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1zm5 0a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1zm5 0a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v2a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1z"/>
          </svg>
          <span>EventBoard</span>
        </Link>
        <button 
          className="navbar-toggler" 
          type="button" 
          data-bs-toggle="collapse" 
          data-bs-target="#navbarNav" 
          aria-controls="navbarNav" 
          aria-expanded="false" 
          aria-label="Toggle navigation"
        >
          <span className="navbar-toggler-icon"></span>
        </button>
        <div className="collapse navbar-collapse justify-content-end" id="navbarNav">
          <ul className="navbar-nav gap-2 align-items-center">
            {/* Events browsing isn't relevant on the login page */}
            {!onLoginPage && (
              <li className="nav-item">
                <Link to="/" className="nav-link px-3 rounded-pill">Events</Link>
              </li>
            )}
            {isAuthenticated && (
              <>
                <li className="nav-item">
                  <Link to="/create" className="nav-link px-3 rounded-pill">Create Event</Link>
                </li>
                {/* Admins moderate all RSVPs in the Admin Panel, so a personal dashboard doesn't apply */}
                {!isAdmin && (
                  <li className="nav-item">
                    <Link to="/dashboard" className="nav-link px-3 rounded-pill">My Dashboard</Link>
                  </li>
                )}
                {isAdmin && (
                  <li className="nav-item">
                    <Link to="/admin" className="nav-link px-3 rounded-pill text-warning fw-semibold">Admin Panel</Link>
                  </li>
                )}
                <li className="nav-item ms-lg-2">
                  <span className="text-white-50 small me-2">Hi, {user?.userName}</span>
                  <button onClick={logout} className="btn btn-outline-light btn-sm rounded-pill px-3">
                    Sign Out
                  </button>
                </li>
              </>
            )}
            {!isAuthenticated && (
              <li className="nav-item">
                <Link to="/login" className="btn btn-primary rounded-pill px-4 ms-2 shadow-xs fw-semibold">
                  Sign In
                </Link>
              </li>
            )}
          </ul>
        </div>
      </div>
    </nav>
  );
};

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="d-flex flex-column min-vh-100 bg-light-gray">
          <NavigationBar />

          {/* Main Content Area */}
          <main className="flex-grow-1">
            <Routes>
              {/* Browsing is public — visitors don't need to sign in */}
              <Route path="/" element={<EventList />} />
              <Route path="/event/:id" element={<EventDetail />} />
              <Route path="/login" element={<LoginRegister />} />
              <Route
                path="/create"
                element={
                  <ProtectedRoute>
                    <CreateEvent />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/dashboard"
                element={
                  <ProtectedRoute>
                    <UserDashboard />
                  </ProtectedRoute>
                }
              />
              <Route 
                path="/admin" 
                element={
                  <ProtectedRoute allowedRoles={['Admin']}>
                    <AdminDashboard />
                  </ProtectedRoute>
                } 
              />
            </Routes>
          </main>

          {/* Footer */}
          <footer className="bg-dark text-white-50 py-4 mt-auto border-top border-secondary">
            <div className="container text-center">
              <p className="mb-0 small">&copy; {new Date().getFullYear()} EventBoard. All rights reserved.</p>
            </div>
          </footer>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
