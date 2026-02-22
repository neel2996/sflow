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

  const [jobs, setJobs] = useState([]);

  const [activeTab, setActiveTab] = useState("jobs");
  const [shortlistJobId, setShortlistJobId] = useState("");
  const [shortlistCandidates, setShortlistCandidates] = useState([]);
  const [shortlistLoading, setShortlistLoading] = useState(false);
  const [country, setCountry] = useState("IN");

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
      setUserEmail(me.email);
      setCredits(me.credits);
      setCountry(me.country || "IN");
      const jobList = await api.getJobs();
      setJobs(jobList);
      if (jobList.length > 0) setShortlistJobId(jobList[0].id.toString());
      setView("dashboard");
    } catch {
      await clearAuth();
      setView("auth");
    }
  }

  async function handleAuth(e) {
    e.preventDefault();
    setError("");
    try {
      const data = isRegister
        ? await api.register(email, password)
        : await api.login(email, password);
      await setAuth(data.token, data.email);
      setUserEmail(data.email);
      setCredits(data.credits);
      setCountry(data.country || "IN");
      const jobList = await api.getJobs();
      setJobs(jobList);
      setView("dashboard");
    } catch (err) {
      setError(err.message);
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
      setError(err.message);
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

  if (view === "loading") {
    return <div style={s.container}><p style={{ textAlign: "center", padding: "20px" }}>Loading...</p></div>;
  }

  if (view === "auth") {
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
          <button type="submit" style={s.button}>
            {isRegister ? "Register" : "Log In"}
          </button>
        </form>
        {error && <p style={s.error}>{error}</p>}
        <p style={s.toggle} onClick={() => { setIsRegister(!isRegister); setError(""); }}>
          {isRegister ? "Already have an account? Log in" : "New here? Register"}
        </p>
        <p style={{ fontSize: "11px", color: COLORS.textLight, textAlign: "center", marginTop: "12px" }}>
          By continuing, you agree to our{" "}
          <span style={{ color: COLORS.primary, cursor: "pointer", textDecoration: "underline" }} onClick={() => chrome.tabs.create({ url: chrome.runtime.getURL("legal.html") })}>
            Terms & Privacy
          </span>
        </p>
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
        <span style={{ fontSize: "13px", color: COLORS.textLight }}>{userEmail}</span>
        <span style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <span style={s.creditBadge}>{credits} credits</span>
          <button
            onClick={() => chrome.runtime.sendMessage({ type: "OPEN_PAYWALL", country })}
            style={{ fontSize: "11px", padding: "2px 8px", backgroundColor: COLORS.primary, color: "#fff", border: "none", borderRadius: "6px", cursor: "pointer", fontWeight: "600" }}
          >
            Get Credits
          </button>
        </span>
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
    justifyContent: "space-between",
    alignItems: "center",
    padding: "8px 12px",
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
};

const root = createRoot(document.getElementById("root"));
root.render(<App />);
