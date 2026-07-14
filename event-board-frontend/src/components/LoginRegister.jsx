import { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const LoginRegister = () => {
  const [isLogin, setIsLogin] = useState(true);
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [role, setRole] = useState('User');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  const { login, register, isAuthenticated, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  // Redirect to previously requested route, or home page
  const from = location.state?.from?.pathname || '/';

  // If already authenticated, redirect to appropriate dashboard
  useEffect(() => {
    if (isAuthenticated && user) {
      // If "from" was "/" or another page that's not login/admin/dashboard, go to "from"
      // otherwise go to role-specific dashboard
      const target = (from === '/' || from === '/dashboard' || from === '/admin' || from === '/login') 
        ? (user.role === 'Admin' ? '/admin' : '/dashboard')
        : from;
      navigate(target, { replace: true });
    }
  }, [isAuthenticated, user, navigate, from]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);

    if (isLogin) {
      try {
        const data = await login(email, password);
        const role = data.role;
        
        let target = from;
        if (from === '/' || from === '/dashboard' || from === '/admin') {
          target = role === 'Admin' ? '/admin' : '/dashboard';
        }
        
        navigate(target, { replace: true });
      } catch (err) {
        console.error(err);
        const backendErrorMsg = err.response?.data?.message || err.response?.data?.Message || (typeof err.response?.data === 'string' ? err.response.data : null);
        setError(backendErrorMsg || 'Invalid email or password. Please try again.');
      } finally {
        setLoading(false);
      }
    } else {
      if (password !== confirmPassword) {
        setError('Passwords do not match.');
        setLoading(false);
        return;
      }
      if (password.length < 6) {
        setError('Password must be at least 6 characters.');
        setLoading(false);
        return;
      }

      try {
        await register(username, email, password, role);
        setSuccess(`Registration successful as ${role}! You can now log in.`);
        setIsLogin(true);
        // Clean fields
        setUsername('');
        setPassword('');
        setConfirmPassword('');
        setRole('User');
      } catch (err) {
        console.error(err);
        const backendErrorMsg = err.response?.data?.message || err.response?.data?.Message || (typeof err.response?.data === 'string' ? err.response.data : null);
        setError(backendErrorMsg || 'Registration failed. Email might already be taken.');
      } finally {
        setLoading(false);
      }
    }
  };

  return (
    <div className="container py-5">
      <div className="row justify-content-center">
        <div className="col-md-6 col-lg-5">
          <div className="card shadow-lg border-0 rounded-4 overflow-hidden">
            <div className="bg-gradient-primary-dark p-4 text-center text-white position-relative">
              <h2 className="fw-bold mb-1">{isLogin ? 'Welcome Back' : 'Create Account'}</h2>
              <p className="text-white-50 mb-0">
                {isLogin ? 'Sign in to access your dashboard' : 'Join EventBoard to book events'}
              </p>
              <div className="gradient-overlay"></div>
            </div>

            <div className="card-body p-4 p-sm-5">
              {/* Tabs */}
              <div className="d-flex bg-light p-1 rounded-pill mb-4">
                <button
                  type="button"
                  className={`btn flex-fill rounded-pill py-2 fw-semibold transition-all ${
                    isLogin ? 'btn-primary text-white' : 'btn-link text-muted text-decoration-none'
                  }`}
                  onClick={() => {
                    setIsLogin(true);
                    setError('');
                    setSuccess('');
                  }}
                >
                  Sign In
                </button>
                <button
                  type="button"
                  className={`btn flex-fill rounded-pill py-2 fw-semibold transition-all ${
                    !isLogin ? 'btn-primary text-white' : 'btn-link text-muted text-decoration-none'
                  }`}
                  onClick={() => {
                    setIsLogin(false);
                    setError('');
                    setSuccess('');
                  }}
                >
                  Sign Up
                </button>
              </div>

              {error && (
                <div className="alert alert-danger py-2.5 px-3 rounded-3 small border-0 mb-3" role="alert">
                  <strong>Error:</strong> {error}
                </div>
              )}

              {success && (
                <div className="alert alert-success py-2.5 px-3 rounded-3 small border-0 mb-3" role="alert">
                  {success}
                </div>
              )}

              <form onSubmit={handleSubmit}>
                {!isLogin && (
                  <div className="form-floating mb-3">
                    <input
                      type="text"
                      className="form-control rounded-3"
                      id="usernameInput"
                      placeholder="Username"
                      value={username}
                      onChange={(e) => setUsername(e.target.value)}
                      required
                    />
                    <label htmlFor="usernameInput">Username</label>
                  </div>
                )}

                <div className="form-floating mb-3">
                  <input
                    type="email"
                    className="form-control rounded-3"
                    id="emailInput"
                    placeholder=" "
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                  />
                  <label htmlFor="emailInput">Email address</label>
                </div>

                <div className="form-floating mb-3">
                  <input
                    type="password"
                    className="form-control rounded-3"
                    id="passwordInput"
                    placeholder="Password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                  />
                  <label htmlFor="passwordInput">Password</label>
                </div>

                {!isLogin && (
                  <div className="form-floating mb-4">
                    <input
                      type="password"
                      className="form-control rounded-3"
                      id="confirmPasswordInput"
                      placeholder="Confirm Password"
                      value={confirmPassword}
                      onChange={(e) => setConfirmPassword(e.target.value)}
                      required
                    />
                    <label htmlFor="confirmPasswordInput">Confirm Password</label>
                  </div>
                )}

                {!isLogin && (
                  <div className="form-floating mb-4">
                    <select
                      className="form-select rounded-3"
                      id="roleInput"
                      value={role}
                      onChange={(e) => setRole(e.target.value)}
                      required
                    >
                      <option value="User">User</option>
                      <option value="Admin">Admin</option>
                    </select>
                    <label htmlFor="roleInput">Register As</label>
                  </div>
                )}

                <button
                  type="submit"
                  disabled={loading}
                  className="btn btn-primary w-100 rounded-pill py-2.5 fw-semibold shadow-xs btn-hover-scale mt-2"
                >
                  {loading ? (
                    <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                  ) : null}
                  {isLogin ? 'Sign In' : 'Sign Up'}
                </button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginRegister;
