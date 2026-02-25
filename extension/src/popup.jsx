import React, { useState, useEffect } from "react";
import { createRoot } from "react-dom/client";
import { api, setAuth, clearAuth, getToken, getStoredEmail } from "./api.js";

const COLORS = {
  primary: "#0073b1",
  bg: "#ffffff",
  border: "#e0e0e0",
  text: "#333333",
  textLight: "#666666",
  error: "#b24020",
};

function App() {
  const [view, setView] = useState("loading");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [credits, setCredits] = useState(0);
  const [userEmail, setUserEmail] = useState("");
  const [error, setError] = useState("");
  const [isRegister, setIsRegister] = useState(false);
  const [showForgotPassword, setShowForgotPassword] = useState(false);
  const [forgotEmail, setForgotEmail] = useState("");
  const [forgotSent, setForgotSent] = useState(false);

  const [jobs, setJobs] = useState([]);

  const [activeTab, setActiveTab] = useState("jobs");
  const [shortlistJobId, setShortlistJobId] = useState("");
  const [shortlistCandidates, setShortlistCandidates] = useState([]);
  const [shortlistLoading, setShortlistLoading] = useState(false);
  const [country, setCountry] = useState("IN");
  const [registerCountry, setRegisterCountry] = useState("IN");

  const [feedbackOpen, setFeedbackOpen] = useState(false);
  const [feedbackType, setFeedbackType] = useState("feedback");
  const [feedbackMessage, setFeedbackMessage] = useState("");
  const [feedbackEmail, setFeedbackEmail] = useState("");
  const [feedbackSubmitting, setFeedbackSubmitting] = useState(false);
  const [feedbackToast, setFeedbackToast] = useState(false);

  useEffect(() => {
    checkAuth();
  }, []);

  async function checkAuth() {
    const token = await getToken();
    if (!token) {
      setView("auth");
      return;
    }
    try {
      const me = await api.getMe();
      const storedEmail = await getStoredEmail();
      setUserEmail(me.email || storedEmail || "");
      setCredits(me.credits ?? 0);
      setCountry(me.country || "IN");
      const jobList = await api.getJobs();
      setJobs(jobList);
      if (jobList.length > 0) setShortlistJobId(jobList[0].id.toString());
      setView("dashboard");
    } catch (err) {
      await clearAuth();
      setView("auth");
      setError(err?.statusCode === 401 ? "Your session has expired. Please log in again." : "");
    }
  }

  async function handleAuth(e) {
    e.preventDefault();
    setError("");
    try {
      const data = isRegister
        ? await api.register(email, password, registerCountry)
        : await api.login(email, password);
      await setAuth(data.token, data.email);
      setUserEmail(data.email);
      setCredits(data.credits);
      setCountry(data.country || "IN");
      const jobList = await api.getJobs();
      setJobs(jobList);
      setView("dashboard");
    } catch (err) {
      if (err?.statusCode === 401) {
        setError("Your session has expired. Please log in again.");
      } else {
        setError(err.message);
      }
    }
  }

  async function handleLogout() {
    await clearAuth();
    setView("auth");
    setEmail("");
    setPassword("");
  }

  function openJobsPage() {
    chrome.tabs.create({ url: chrome.runtime.getURL("jobs.html") });
  }

  async function handleLoadShortlist(jobId) {
    setShortlistJobId(jobId);
    setShortlistLoading(true);
    setShortlistCandidates([]);
    try {
      const candidates = await api.getShortlist(parseInt(jobId));
      setShortlistCandidates(candidates);
    } catch (err) {
      if (err?.statusCode === 401) {
        await clearAuth();
        setView("auth");
        setError("Your session has expired. Please log in again.");
      } else {
        setError(err.message);
      }
    } finally {
      setShortlistLoading(false);
    }
  }

  async function handleTabChange(tab) {
    setActiveTab(tab);
    setError("");
    if (tab === "shortlisted" && shortlistJobId) {
      await handleLoadShortlist(shortlistJobId);
    }
  }

  async function handleFeedbackSubmit(e) {
    e.preventDefault();
    if (!feedbackMessage.trim()) return;
    setFeedbackSubmitting(true);
    setError("");
    try {
      const emailToSend = feedbackEmail.trim() || (userEmail || "");
      await api.submitFeedback(emailToSend || undefined, feedbackMessage.trim(), feedbackType);
      setFeedbackOpen(false);
      setFeedbackMessage("");
      setFeedbackEmail("");
      setFeedbackType("feedback");
      setFeedbackToast(true);
      setTimeout(() => setFeedbackToast(false), 4000);
    } catch (err) {
      if (err?.statusCode === 401) {
        await clearAuth();
        setView("auth");
        setError("Your session has expired. Please log in again.");
      } else {
        setError(err.message);
      }
    } finally {
      setFeedbackSubmitting(false);
    }
  }

  if (view === "loading") {
    return <div style={s.container}><p style={{ textAlign: "center", padding: "20px" }}>Loading...</p></div>;
  }

  async function handleForgotPassword(e) {
    e.preventDefault();
    if (!forgotEmail.trim()) return;
    setError("");
    try {
      await api.forgotPassword(forgotEmail.trim());
      setForgotSent(true);
    } catch (err) {
      setError(err.message);
    }
  }

  if (view === "auth") {
    if (showForgotPassword) {
      return (
        <div style={s.container}>
          <h2 style={s.logo}>Forgot Password</h2>
          {forgotSent ? (
            <>
              <p style={{ fontSize: "13px", color: COLORS.textLight, textAlign: "center" }}>
                If that email exists, we've sent a reset link. Check your inbox.
              </p>
              <p style={{ ...s.toggle, marginTop: "16px" }} onClick={() => { setShowForgotPassword(false); setForgotSent(false); setError(""); }}>
                ← Back to login
              </p>
            </>
          ) : (
            <form onSubmit={handleForgotPassword}>
              <input
                style={s.input}
                type="email"
                placeholder="Your email"
                value={forgotEmail}
                onChange={(e) => setForgotEmail(e.target.value)}
                required
              />
              <button type="submit" style={s.button}>Send reset link</button>
              {error && <p style={s.error}>{error}</p>}
              <p style={{ ...s.toggle, marginTop: "12px" }} onClick={() => { setShowForgotPassword(false); setError(""); }}>
                ← Back to login
              </p>
            </form>
          )}
        </div>
      );
    }
    return (
      <div style={s.container}>
        <h2 style={s.logo}>SourceFlow</h2>
        <form onSubmit={handleAuth}>
          <input
            style={s.input}
            type="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <input
            style={s.input}
            type="password"
            placeholder="Password (min 6 chars)"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={6}
          />
          {isRegister && (
            <select
              style={s.input}
              value={registerCountry}
              onChange={(e) => setRegisterCountry(e.target.value)}
              title="Country affects payment options (India: INR/Razorpay, Other: USD/Paddle)"
            >
              <option value="IN">India (INR)</option>
              <option value="US">United States (USD)</option>
              <option value="OTHER">Other (USD)</option>
            </select>
          )}
          <button type="submit" style={s.button}>
            {isRegister ? "Register" : "Log In"}
          </button>
        </form>
        {error && <p style={s.error}>{error}</p>}
        <p style={s.toggle} onClick={() => { setIsRegister(!isRegister); setError(""); }}>
          {isRegister ? "Already have an account? Log in" : "New here? Register"}
        </p>
        {!isRegister && (
          <p style={{ ...s.toggle, marginTop: "4px", fontSize: "11px" }} onClick={() => setShowForgotPassword(true)}>
            Forgot password?
          </p>
        )}
        <p style={{ fontSize: "11px", color: COLORS.textLight, textAlign: "center", marginTop: "8px" }}>
          <span style={{ color: COLORS.primary, cursor: "pointer", textDecoration: "underline" }} onClick={() => { setFeedbackEmail(""); setFeedbackOpen(true); }}>
            Send Feedback
          </span>
        </p>
        <p style={{ fontSize: "11px", color: COLORS.textLight, textAlign: "center", marginTop: "12px" }}>
          By continuing, you agree to our{" "}
          <span style={{ color: COLORS.primary, cursor: "pointer", textDecoration: "underline" }} onClick={() => chrome.tabs.create({ url: chrome.runtime.getURL("legal.html") })}>
            Terms & Privacy
          </span>
        </p>
        {feedbackToast && (
          <div style={s.toast}>Thanks! Your feedback helps us improve SourceFlow 🚀</div>
        )}
        {feedbackOpen && (
          <div style={s.modalOverlay} onClick={() => !feedbackSubmitting && setFeedbackOpen(false)}>
            <div style={s.modal} onClick={(e) => e.stopPropagation()}>
              <h3 style={{ margin: "0 0 12px", fontSize: "15px", color: COLORS.text }}>Send Feedback</h3>
              <form onSubmit={handleFeedbackSubmit}>
                <select style={s.input} value={feedbackType} onChange={(e) => setFeedbackType(e.target.value)}>
                  <option value="feedback">Feedback</option>
                  <option value="bug">Bug</option>
                  <option value="feature">Feature Request</option>
                </select>
                <textarea
                  style={{ ...s.input, minHeight: "80px", resize: "vertical" }}
                  placeholder="Your message..."
                  value={feedbackMessage}
                  onChange={(e) => setFeedbackMessage(e.target.value)}
                  required
                />
                <input
                  style={s.input}
                  type="email"
                  placeholder="Email (optional)"
                  value={feedbackEmail}
                  onChange={(e) => setFeedbackEmail(e.target.value)}
                />
                <div style={{ display: "flex", gap: "8px", marginTop: "8px" }}>
                  <button type="button" style={s.linkBtn} onClick={() => setFeedbackOpen(false)} disabled={feedbackSubmitting}>Cancel</button>
                  <button type="submit" style={s.button} disabled={feedbackSubmitting}>{feedbackSubmitting ? "Sending..." : "Submit"}</button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div style={s.container}>
      <div style={s.header}>
        <h2 style={{ ...s.logo, fontSize: "16px" }}>SourceFlow</h2>
        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          <span style={{ ...s.linkBtn, fontSize: "11px" }} onClick={() => chrome.tabs.create({ url: chrome.runtime.getURL("legal.html") })}>Legal</span>
          <button onClick={handleLogout} style={s.linkBtn}>Logout</button>
        </div>
      </div>

      <div style={s.infoRow}>
        <div style={{ fontSize: "13px", color: COLORS.textLight, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", marginBottom: "6px" }} title={userEmail}>
          {userEmail || "—"}
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <span style={s.creditBadge}>{credits ?? 0} credits</span>
          <button
            onClick={async () => {
              try {
                const me = await api.getMe();
                const c = me?.country || country || "IN";
                setCountry(c);
                chrome.runtime.sendMessage({ type: "OPEN_PAYWALL", country: c });
              } catch {
                chrome.runtime.sendMessage({ type: "OPEN_PAYWALL", country });
              }
            }}
            style={{ fontSize: "11px", padding: "2px 8px", backgroundColor: COLORS.primary, color: "#fff", border: "none", borderRadius: "6px", cursor: "pointer", fontWeight: "600" }}
          >
            Get Credits
          </button>
        </div>
      </div>
      <div style={{ marginBottom: "12px", display: "flex", alignItems: "center", gap: "8px", flexWrap: "wrap" }}>
        <span style={{ fontSize: "12px", color: COLORS.textLight }}>Region:</span>
        <select
          style={{ ...s.input, marginBottom: 0, padding: "6px 10px", fontSize: "12px", width: "auto" }}
          value={country === "OTHER" || country === "USA" ? "US" : country}
          onChange={async (e) => {
            const v = e.target.value;
            try {
              await api.updateCountry(v);
              setCountry(v);
            } catch (err) {
              if (err?.statusCode === 401) {
                await clearAuth();
                setView("auth");
                setError("Your session has expired. Please log in again.");
              } else {
                setError(err.message);
              }
            }
          }}
          title="Affects payment options (India: INR, Other: USD)"
        >
          <option value="IN">India (INR)</option>
          <option value="US">USA / Other (USD)</option>
        </select>
        <span style={{ ...s.linkBtn, fontSize: "11px", marginLeft: "auto" }} onClick={() => { setFeedbackEmail(userEmail || ""); setFeedbackOpen(true); }}>Send Feedback</span>
      </div>


      <div style={s.tabs}>
        <button
          style={{ ...s.tab, ...(activeTab === "jobs" ? s.tabActive : {}) }}
          onClick={() => handleTabChange("jobs")}
        >
          Jobs
        </button>
        <button
          style={{ ...s.tab, ...(activeTab === "shortlisted" ? s.tabActive : {}) }}
          onClick={() => handleTabChange("shortlisted")}
        >
          Shortlisted
        </button>
      </div>

      {activeTab === "jobs" && (
        <>
          <div style={s.section}>
            <h3 style={s.sectionTitle}>Jobs</h3>
            <button
              onClick={openJobsPage}
              style={{ ...s.button, fontSize: "13px", padding: "10px" }}
            >
              Add Jobs
            </button>
            <p style={{ fontSize: "11px", color: COLORS.textLight, marginTop: "8px" }}>
              Opens a page where you can add multiple job descriptions.
            </p>
          </div>

          {jobs.length > 0 && (
            <div style={s.section}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "8px" }}>
                <h3 style={s.sectionTitle}>Your Jobs ({jobs.length})</h3>
                <button
                  onClick={async () => { try { const list = await api.getJobs(); setJobs(list); } catch {} }}
                  style={s.linkBtn}
                >
                  Refresh
                </button>
              </div>
              {jobs.map((j) => (
                <div key={j.id} style={s.jobItem}>
                  <span style={{ fontWeight: "500" }}>{j.title}</span>
                  <span style={{ fontSize: "11px", color: COLORS.textLight }}>
                    {new Date(j.createdAt).toLocaleDateString()}
                  </span>
                </div>
              ))}
            </div>
          )}

          <p style={{ fontSize: "11px", color: COLORS.textLight, textAlign: "center", margin: "12px 0 0" }}>
            Open a LinkedIn profile to scan candidates.
          </p>
        </>
      )}

      {activeTab === "shortlisted" && (
        <div style={s.section}>
          <h3 style={s.sectionTitle}>Shortlisted Candidates</h3>
          {jobs.length === 0 ? (
            <p style={{ fontSize: "13px", color: COLORS.textLight }}>No jobs yet.</p>
          ) : (
            <>
              <select
                style={{ ...s.input, marginBottom: "12px" }}
                value={shortlistJobId}
                onChange={(e) => handleLoadShortlist(e.target.value)}
              >
                {jobs.map((j) => (
                  <option key={j.id} value={j.id}>{j.title}</option>
                ))}
              </select>

              {shortlistLoading ? (
                <p style={{ fontSize: "13px", color: COLORS.textLight }}>Loading...</p>
              ) : shortlistCandidates.length === 0 ? (
                <p style={{ fontSize: "13px", color: COLORS.textLight }}>No candidates shortlisted yet.</p>
              ) : (
                shortlistCandidates.map((c) => {
                  const displayName = c.candidate_name?.trim() || (c.profile_url?.match(/\/in\/([^/?#]+)/)?.[1] || "Unknown");
                  return (
                  <div key={c.id} style={s.candidateItem}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                      <span style={{ fontWeight: "600", fontSize: "13px" }}>{displayName}</span>
                      <span style={{
                        fontSize: "12px",
                        fontWeight: "700",
                        color: c.match_score >= 70 ? "#057642" : c.match_score >= 50 ? "#cc8800" : "#b24020",
                      }}>
                        {c.match_score}%
                      </span>
                    </div>
                    <a
                      href={c.profile_url}
                      target="_blank"
                      rel="noreferrer"
                      style={{ fontSize: "11px", color: COLORS.primary, textDecoration: "none" }}
                    >
                      Open Profile ↗
                    </a>
                  </div>
                ); })
              )}
            </>
          )}
        </div>
      )}

      {error && <p style={s.error}>{error}</p>}

      {feedbackToast && (
        <div style={s.toast}>
          Thanks! Your feedback helps us improve SourceFlow 🚀
        </div>
      )}

      {feedbackOpen && (
        <div style={s.modalOverlay} onClick={() => !feedbackSubmitting && setFeedbackOpen(false)}>
          <div style={s.modal} onClick={(e) => e.stopPropagation()}>
            <h3 style={{ margin: "0 0 12px", fontSize: "15px", color: COLORS.text }}>Send Feedback</h3>
            <form onSubmit={handleFeedbackSubmit}>
              <select
                style={s.input}
                value={feedbackType}
                onChange={(e) => setFeedbackType(e.target.value)}
              >
                <option value="feedback">Feedback</option>
                <option value="bug">Bug</option>
                <option value="feature">Feature Request</option>
              </select>
              <textarea
                style={{ ...s.input, minHeight: "80px", resize: "vertical" }}
                placeholder="Your message..."
                value={feedbackMessage}
                onChange={(e) => setFeedbackMessage(e.target.value)}
                required
              />
              <input
                style={s.input}
                type="email"
                placeholder="Email (optional)"
                value={feedbackEmail}
                onChange={(e) => setFeedbackEmail(e.target.value)}
              />
              <div style={{ display: "flex", gap: "8px", marginTop: "8px" }}>
                <button type="button" style={s.linkBtn} onClick={() => setFeedbackOpen(false)} disabled={feedbackSubmitting}>
                  Cancel
                </button>
                <button type="submit" style={s.button} disabled={feedbackSubmitting}>
                  {feedbackSubmitting ? "Sending..." : "Submit"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

const s = {
  container: {
    width: "340px",
    padding: "16px",
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    color: COLORS.text,
  },
  logo: {
    color: COLORS.primary,
    fontSize: "20px",
    fontWeight: "700",
    textAlign: "center",
    marginBottom: "16px",
  },
  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: "12px",
  },
  input: {
    width: "100%",
    padding: "10px 12px",
    border: `1px solid ${COLORS.border}`,
    borderRadius: "6px",
    fontSize: "13px",
    marginBottom: "8px",
    outline: "none",
    boxSizing: "border-box",
    fontFamily: "inherit",
  },
  button: {
    width: "100%",
    padding: "10px",
    backgroundColor: COLORS.primary,
    color: "#fff",
    border: "none",
    borderRadius: "6px",
    fontSize: "14px",
    fontWeight: "600",
    cursor: "pointer",
  },
  error: {
    color: COLORS.error,
    fontSize: "12px",
    marginTop: "8px",
    textAlign: "center",
  },
  toggle: {
    fontSize: "12px",
    color: COLORS.primary,
    textAlign: "center",
    cursor: "pointer",
    marginTop: "12px",
  },
  linkBtn: {
    background: "none",
    border: "none",
    color: COLORS.primary,
    fontSize: "12px",
    cursor: "pointer",
    padding: 0,
  },
  infoRow: {
    display: "flex",
    flexDirection: "column",
    padding: "10px 12px",
    backgroundColor: "#f8f9fa",
    borderRadius: "6px",
    marginBottom: "12px",
  },
  creditBadge: {
    fontSize: "12px",
    fontWeight: "600",
    color: COLORS.primary,
    backgroundColor: "#e8f0fe",
    padding: "2px 8px",
    borderRadius: "10px",
  },
  section: {
    marginBottom: "12px",
    padding: "12px",
    border: `1px solid ${COLORS.border}`,
    borderRadius: "8px",
  },
  sectionTitle: {
    fontSize: "13px",
    fontWeight: "600",
    marginBottom: "8px",
    color: COLORS.text,
  },
  jobItem: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "6px 0",
    borderBottom: `1px solid ${COLORS.border}`,
    fontSize: "13px",
  },
  tabs: {
    display: "flex",
    borderBottom: `1px solid ${COLORS.border}`,
    marginBottom: "12px",
  },
  tab: {
    flex: 1,
    padding: "8px",
    background: "none",
    border: "none",
    fontSize: "13px",
    fontWeight: "500",
    cursor: "pointer",
    color: COLORS.textLight,
    borderBottom: "2px solid transparent",
    marginBottom: "-1px",
  },
  tabActive: {
    color: COLORS.primary,
    borderBottom: `2px solid ${COLORS.primary}`,
  },
  candidateItem: {
    padding: "8px 0",
    borderBottom: `1px solid ${COLORS.border}`,
  },
  modalOverlay: {
    position: "fixed",
    inset: 0,
    backgroundColor: "rgba(0,0,0,0.4)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    zIndex: 1000,
  },
  modal: {
    backgroundColor: COLORS.bg,
    padding: "20px",
    borderRadius: "8px",
    width: "90%",
    maxWidth: "320px",
    boxShadow: "0 4px 20px rgba(0,0,0,0.15)",
  },
  toast: {
    position: "fixed",
    bottom: "16px",
    left: "50%",
    transform: "translateX(-50%)",
    backgroundColor: "#057642",
    color: "#fff",
    padding: "10px 16px",
    borderRadius: "8px",
    fontSize: "13px",
    fontWeight: "500",
    zIndex: 1001,
    boxShadow: "0 2px 12px rgba(0,0,0,0.2)",
  },
};

const root = createRoot(document.getElementById("root"));
root.render(<App />);
