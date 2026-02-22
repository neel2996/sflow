import React, { useState, useEffect } from "react";
import { createRoot } from "react-dom/client";
import { api, getToken, clearAuth } from "./api.js";

const COLORS = {
  primary: "#0073b1",
  bg: "#ffffff",
  border: "#e0e0e0",
  text: "#333333",
  textLight: "#666666",
  error: "#b24020",
};

function JobsPage() {
  const [view, setView] = useState("loading");
  const [jobs, setJobs] = useState([]);
  const [jobTitle, setJobTitle] = useState("");
  const [jobDesc, setJobDesc] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    load();
  }, []);

  async function load() {
    const token = await getToken();
    if (!token) {
      setView("auth");
      return;
    }
    try {
      const list = await api.getJobs();
      setJobs(list);
      setView("main");
    } catch {
      await clearAuth();
      setView("auth");
    }
  }

  async function handleAddJob(e) {
    e.preventDefault();
    setError("");
    setSuccess("");
    setSubmitting(true);
    try {
      const job = await api.createJob(jobTitle, jobDesc);
      setJobs((prev) => [job, ...prev]);
      setJobTitle("");
      setJobDesc("");
      setSuccess("Job added! Add another below.");
    } catch (err) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  }

  function handleBack() {
    window.close();
  }

  if (view === "loading") {
    return (
      <div style={s.page}>
        <p style={{ textAlign: "center", padding: "40px" }}>Loading...</p>
      </div>
    );
  }

  if (view === "auth") {
    return (
      <div style={s.page}>
        <div style={s.card}>
          <h1 style={s.title}>SourceFlow</h1>
          <p style={{ color: COLORS.textLight, marginBottom: 24 }}>
            Please open the SourceFlow extension popup and log in first.
          </p>
          <button onClick={handleBack} style={s.button}>
            Close
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={s.page}>
      <div style={s.header}>
        <h1 style={s.title}>Manage Jobs</h1>
        <button onClick={handleBack} style={s.linkBtn}>
          ← Back to extension
        </button>
      </div>

      <div style={s.card}>
        <h2 style={s.sectionTitle}>Add New Job</h2>
        <form onSubmit={handleAddJob}>
          <input
            style={s.input}
            type="text"
            placeholder="Job title"
            value={jobTitle}
            onChange={(e) => setJobTitle(e.target.value)}
            required
          />
          <textarea
            style={{ ...s.input, minHeight: 120, resize: "vertical" }}
            placeholder="Paste full job description..."
            value={jobDesc}
            onChange={(e) => setJobDesc(e.target.value)}
            required
          />
          <button type="submit" style={s.button} disabled={submitting}>
            {submitting ? "Adding…" : "Add Job"}
          </button>
        </form>
        {success && <p style={s.success}>{success}</p>}
        {error && <p style={s.error}>{error}</p>}
      </div>

      <div style={s.card}>
        <h2 style={s.sectionTitle}>Your Jobs ({jobs.length})</h2>
        {jobs.length === 0 ? (
          <p style={{ color: COLORS.textLight }}>No jobs yet. Add one above.</p>
        ) : (
          <div style={s.jobList}>
            {jobs.map((j) => (
              <div key={j.id} style={s.jobItem}>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <span style={{ fontWeight: 600, fontSize: 15 }}>{j.title}</span>
                  <p style={{ fontSize: 13, color: COLORS.textLight, margin: "4px 0 0", lineHeight: 1.4 }}>
                    {j.description?.slice(0, 120)}
                    {j.description?.length > 120 ? "…" : ""}
                  </p>
                </div>
                <span style={{ fontSize: 12, color: COLORS.textLight, flexShrink: 0 }}>
                  {new Date(j.createdAt).toLocaleDateString()}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

const s = {
  page: {
    maxWidth: 640,
    margin: "0 auto",
    padding: 24,
    color: "#333",
  },
  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 24,
  },
  title: {
    color: COLORS.primary,
    fontSize: 24,
    fontWeight: 700,
    margin: 0,
  },
  linkBtn: {
    background: "none",
    border: "none",
    color: COLORS.primary,
    fontSize: 14,
    cursor: "pointer",
    padding: "8px 0",
  },
  card: {
    backgroundColor: "#fff",
    border: `1px solid ${COLORS.border}`,
    borderRadius: 12,
    padding: 24,
    marginBottom: 24,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: 600,
    margin: "0 0 16px",
  },
  input: {
    width: "100%",
    padding: "12px 14px",
    border: `1px solid ${COLORS.border}`,
    borderRadius: 8,
    fontSize: 14,
    marginBottom: 12,
    outline: "none",
    boxSizing: "border-box",
    fontFamily: "inherit",
  },
  button: {
    padding: "12px 24px",
    backgroundColor: COLORS.primary,
    color: "#fff",
    border: "none",
    borderRadius: 8,
    fontSize: 14,
    fontWeight: 600,
    cursor: "pointer",
  },
  success: {
    color: "#057642",
    fontSize: 14,
    marginTop: 12,
  },
  error: {
    color: COLORS.error,
    fontSize: 14,
    marginTop: 12,
  },
  jobList: {
    display: "flex",
    flexDirection: "column",
    gap: 0,
  },
  jobItem: {
    display: "flex",
    alignItems: "flex-start",
    gap: 16,
    padding: "16px 0",
    borderBottom: `1px solid ${COLORS.border}`,
  },
};

const root = createRoot(document.getElementById("root"));
root.render(<JobsPage />);
