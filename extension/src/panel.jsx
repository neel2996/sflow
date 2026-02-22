import React, { useState, useEffect } from "react";
import { api, getToken } from "./api.js";

const COLORS = {
  primary: "#0073b1",
  success: "#057642",
  warning: "#b24020",
  bg: "#ffffff",
  border: "#e0e0e0",
  text: "#333333",
  textLight: "#666666",
  panelBg: "#f8f9fa",
};

const styles = {
  container: {
    position: "fixed",
    top: "80px",
    right: "16px",
    width: "360px",
    maxHeight: "calc(100vh - 100px)",
    overflowY: "auto",
    backgroundColor: COLORS.bg,
    borderRadius: "12px",
    boxShadow: "0 4px 24px rgba(0,0,0,0.15)",
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
    fontSize: "14px",
    color: COLORS.text,
    zIndex: 9999,
    border: `1px solid ${COLORS.border}`,
  },
  header: {
    padding: "16px 20px",
    borderBottom: `1px solid ${COLORS.border}`,
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
  },
  logo: {
    fontSize: "16px",
    fontWeight: "700",
    color: COLORS.primary,
    margin: 0,
  },
  credits: {
    fontSize: "12px",
    color: COLORS.textLight,
    backgroundColor: COLORS.panelBg,
    padding: "4px 10px",
    borderRadius: "12px",
  },
  body: { padding: "16px 20px" },
  select: {
    width: "100%",
    padding: "10px 12px",
    border: `1px solid ${COLORS.border}`,
    borderRadius: "8px",
    fontSize: "14px",
    backgroundColor: COLORS.bg,
    marginBottom: "12px",
    outline: "none",
    boxSizing: "border-box",
  },
  button: {
    width: "100%",
    padding: "12px",
    backgroundColor: COLORS.primary,
    color: "#fff",
    border: "none",
    borderRadius: "8px",
    fontSize: "14px",
    fontWeight: "600",
    cursor: "pointer",
    boxSizing: "border-box",
  },
  buttonDisabled: { opacity: 0.6, cursor: "not-allowed" },
  resultCard: {
    marginTop: "16px",
    padding: "16px",
    backgroundColor: COLORS.panelBg,
    borderRadius: "8px",
    border: `1px solid ${COLORS.border}`,
  },
  scoreBar: {
    height: "8px",
    borderRadius: "4px",
    backgroundColor: "#e0e0e0",
    marginTop: "8px",
    marginBottom: "16px",
    overflow: "hidden",
  },
  label: {
    fontSize: "12px",
    fontWeight: "600",
    color: COLORS.textLight,
    textTransform: "uppercase",
    letterSpacing: "0.5px",
    marginBottom: "6px",
    marginTop: "12px",
  },
  tag: {
    display: "inline-block",
    padding: "3px 10px",
    borderRadius: "12px",
    fontSize: "12px",
    marginRight: "6px",
    marginBottom: "6px",
  },
  message: {
    padding: "12px",
    backgroundColor: "#fff",
    borderRadius: "8px",
    border: `1px solid ${COLORS.border}`,
    fontSize: "13px",
    lineHeight: "1.5",
    whiteSpace: "pre-wrap",
  },
  error: {
    color: COLORS.warning,
    padding: "12px",
    backgroundColor: "#fef2f0",
    borderRadius: "8px",
    fontSize: "13px",
    marginTop: "12px",
  },
  notAuth: {
    padding: "24px 20px",
    textAlign: "center",
    color: COLORS.textLight,
  },
  toggleBtn: {
    position: "fixed",
    top: "80px",
    right: "16px",
    width: "48px",
    height: "48px",
    borderRadius: "50%",
    backgroundColor: COLORS.primary,
    color: "#fff",
    border: "none",
    cursor: "pointer",
    fontSize: "14px",
    fontWeight: "bold",
    boxShadow: "0 2px 12px rgba(0,0,0,0.2)",
    zIndex: 10000,
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
  },
};

function scoreColor(score) {
  if (score >= 70) return COLORS.success;
  if (score >= 50) return "#cc8800";
  return COLORS.warning;
}

const MONTH_MAP = { jan:0,feb:1,mar:2,apr:3,may:4,jun:5,jul:6,aug:7,sep:8,oct:9,nov:10,dec:11 };

function parseMonthYear(str) {
  str = str.trim().toLowerCase();
  if (/present/i.test(str)) return new Date();
  const m = str.match(/^([a-z]+)\s+(\d{4})$/);
  if (!m) return null;
  const mo = MONTH_MAP[m[1].substring(0, 3)];
  if (mo === undefined) return null;
  return new Date(parseInt(m[2]), mo, 1);
}

function computeTotalExperience() {
  const pageText = (document.querySelector("main") || document.body).innerText || "";

  // Match "Month Year - Month Year" or "Month Year - Present"
  const rangeRegex = /(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\s+(\d{4})\s*[-–]+\s*(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|Present)[a-z]*\s*(\d{0,4})/gi;

  const ranges = [];
  let m;
  while ((m = rangeRegex.exec(pageText)) !== null) {
    const startStr = `${m[1]} ${m[2]}`;
    const endStr = /present/i.test(m[3]) ? "present" : `${m[3]} ${m[4]}`;
    const start = parseMonthYear(startStr);
    const end = parseMonthYear(endStr);
    if (!start || !end || end < start) continue;
    const months = (end.getFullYear() - start.getFullYear()) * 12 + (end.getMonth() - start.getMonth());
    if (months <= 0 || months > 480) continue;
    ranges.push({ start, end, months });
  }

  console.log("[SF] Raw ranges found:", ranges.map(r => `${r.start.toDateString()} → ${r.end.toDateString()} (${r.months}mo)`));
  if (ranges.length === 0) {
    console.log("[SF] No date ranges found in page text!");
    return { totalMonths: 0, totalYears: 0, roleCount: 0, roles: [] };
  }

  // Sort by start date
  ranges.sort((a, b) => a.start - b.start);

  // Merge overlapping ranges — handles multi-role companies without double counting
  const merged = [{ ...ranges[0] }];
  for (let i = 1; i < ranges.length; i++) {
    const last = merged[merged.length - 1];
    if (ranges[i].start <= last.end) {
      if (ranges[i].end > last.end) last.end = ranges[i].end;
    } else {
      merged.push({ ...ranges[i] });
    }
  }

  let totalMonths = 0;
  const roles = [];
  for (const r of merged) {
    const mo = (r.end.getFullYear() - r.start.getFullYear()) * 12 + (r.end.getMonth() - r.start.getMonth());
    totalMonths += mo;
    roles.push({ text: `${mo} mos`, months: mo });
  }

  const years = Math.round((totalMonths / 12) * 10) / 10;
  console.log("[SF] Merged periods:", merged.length, "| Total:", totalMonths, "months =", years, "years");
  return { totalMonths, totalYears: years, roleCount: merged.length, roles };
}

function extractProfileName() {
  const selectors = [
    "h1",
    "main h1",
    ".text-heading-xlarge",
    ".pv-text-details__left-panel h1",
    "[data-anonymize='person-name']",
  ];
  for (const sel of selectors) {
    const el = document.querySelector(sel);
    const text = el?.innerText?.trim();
    if (text && text.length > 1 && text.length < 100) return text;
  }
  const title = document.title || "";
  const match = title.match(/^([^|\-–—]+?)\s*[|\-–—]\s*LinkedIn/i);
  if (match) return match[1].trim();
  const urlMatch = window.location.pathname.match(/\/in\/([^/]+)/);
  if (urlMatch) return urlMatch[1];
  return "";
}

function extractProfileText() {
  const name = extractProfileName();
  const headline = document.querySelector(".text-body-medium")?.innerText?.trim() || "";
  const experience = computeTotalExperience();

  let fullText = `Name: ${name}\nHeadline: ${headline}\n\n`;
  fullText += `--- COMPUTED TOTAL EXPERIENCE ---\n`;
  fullText += `Total: ${experience.totalYears} years (${experience.totalMonths} months, ${experience.roleCount} roles)\n`;
  fullText += `Roles: ${experience.roles.map((r) => r.text).join(", ")}\n`;
  fullText += `IMPORTANT: Use exactly ${experience.totalYears} years as total_experience_years. Do NOT recalculate.\n\n`;

  const sectionIds = ["about", "experience", "education", "skills", "certifications", "languages"];
  for (const id of sectionIds) {
    const el = document.getElementById(id);
    if (el) {
      const sec = el.closest("section");
      if (sec) {
        fullText += `--- ${id.toUpperCase()} ---\n${sec.innerText}\n\n`;
      }
    }
  }

  const main = document.querySelector("main");
  if (main) fullText += `--- FULL PAGE ---\n${main.innerText}\n`;

  return {
    name,
    profileUrl: window.location.href.split("?")[0],
    profileText: fullText.substring(0, 15000),
    computedExperience: experience,
  };
}

export default function Panel() {
  const [open, setOpen] = useState(true);
  const [authed, setAuthed] = useState(false);
  const [credits, setCredits] = useState(0);
  const [jobs, setJobs] = useState([]);
  const [selectedJob, setSelectedJob] = useState("");
  const [scanning, setScanning] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [expDebug, setExpDebug] = useState("");
  const [scannedProfile, setScannedProfile] = useState(null);
  const [shortlisted, setShortlisted] = useState(false);
  const [shortlisting, setShortlisting] = useState(false);
  const [country, setCountry] = useState("IN");

  useEffect(() => {
    checkAuth();
    const listener = (changes) => {
      if (changes.token) checkAuth();
    };
    chrome.storage.onChanged.addListener(listener);
    return () => chrome.storage.onChanged.removeListener(listener);
  }, []);

  async function checkAuth() {
    const token = await getToken();
    if (!token) { setAuthed(false); return; }
    try {
      const me = await api.getMe();
      setAuthed(true);
      setCredits(me.credits);
      setCountry(me.country || "IN");
      const jobList = await api.getJobs();
      setJobs(jobList);
      if (jobList.length > 0) setSelectedJob(jobList[0].id.toString());
    } catch {
      setAuthed(false);
    }
  }

  async function autoScrollPage() {
    const delay = (ms) => new Promise((r) => setTimeout(r, ms));
    const startY = window.scrollY;
    let y = 0;
    while (y < document.body.scrollHeight) {
      window.scrollTo(0, y);
      await delay(150);
      y += 700;
    }
    window.scrollTo(0, document.body.scrollHeight);
    await delay(400);
    window.scrollTo(0, startY);
    await delay(200);
  }

  async function handleScan() {
    setError("");
    setResult(null);
    setExpDebug("");
    setShortlisted(false);
    setScannedProfile(null);
    setScanning(true);

    try {
      await autoScrollPage();
      const { name, profileUrl, profileText, computedExperience } = extractProfileText();
      setExpDebug(`${computedExperience.totalYears} yrs detected (${computedExperience.totalMonths} months)`);
      const cleanUrl = profileUrl.replace(/\/details\/experience\/?$/, "").replace(/\/$/, "");
      const data = await api.scan(parseInt(selectedJob), cleanUrl, profileText, computedExperience.totalYears);
      setResult(data);
      setScannedProfile({ name, profileUrl: cleanUrl });
      const me = await api.getMe();
      setCredits(me.credits);
    } catch (err) {
      setError(err.message);
      if (err.statusCode === 403 || err.code === "PAYWALL") {
        chrome.runtime.sendMessage({ type: "OPEN_PAYWALL", country });
      }
    } finally {
      setScanning(false);
    }
  }

  async function handleShortlist() {
    if (!result || !scannedProfile || shortlisted) return;
    setShortlisting(true);
    try {
      const res = await api.shortlist(
        parseInt(selectedJob),
        scannedProfile.profileUrl,
        scannedProfile.name,
        result.match_score,
        result.summary,
        result.outreach_message
      );
      setShortlisted(true);
      if (res.already_shortlisted) {
        setShortlisted(true);
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setShortlisting(false);
    }
  }

  if (!open) {
    return (
      <button style={styles.toggleBtn} onClick={() => setOpen(true)}>SF</button>
    );
  }

  function openPaywall() {
    chrome.runtime.sendMessage({ type: "OPEN_PAYWALL", country });
  }

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <h3 style={styles.logo}>SourceFlow</h3>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <span
            style={{ fontSize: "11px", color: COLORS.primary, cursor: "pointer", textDecoration: "none" }}
            onClick={() => chrome.tabs.create({ url: chrome.runtime.getURL("legal.html") })}
          >
            Legal
          </span>
          {authed && <span style={styles.credits}>{credits} credits</span>}
          <button
            onClick={() => setOpen(false)}
            style={{ background: "none", border: "none", fontSize: "18px", cursor: "pointer", color: COLORS.textLight, padding: "0 4px" }}
          >
            &times;
          </button>
        </div>
      </div>

      {!authed ? (
        <div style={styles.notAuth}>
          <p style={{ marginBottom: "8px", fontWeight: "600" }}>Not logged in</p>
          <p style={{ fontSize: "13px" }}>Click the SourceFlow icon to log in, then refresh this page.</p>
        </div>
      ) : (
        <div style={styles.body}>
          {jobs.length === 0 ? (
            <div style={{ textAlign: "center", color: COLORS.textLight, padding: "12px 0" }}>
              <p>No jobs yet.</p>
              <p style={{ fontSize: "13px" }}>Add a job description in the extension popup first.</p>
            </div>
          ) : (
            <>
              <label style={styles.label}>Select Job</label>
              <select style={styles.select} value={selectedJob} onChange={(e) => setSelectedJob(e.target.value)}>
                {jobs.map((j) => (
                  <option key={j.id} value={j.id}>{j.title}</option>
                ))}
              </select>

              <button
                style={{ ...styles.button, ...(scanning || credits <= 0 ? styles.buttonDisabled : {}) }}
                onClick={handleScan}
                disabled={scanning || credits <= 0}
              >
                {scanning ? "Scanning..." : "Scan Profile"}
              </button>

              <button
                style={{ ...styles.button, marginTop: "8px", backgroundColor: COLORS.success, fontSize: "12px", padding: "8px" }}
                onClick={openPaywall}
              >
                Get Credits
              </button>

              {expDebug && (
                <div style={{ marginTop: "8px", padding: "8px", background: "#f0f4ff", borderRadius: "6px", fontSize: "11px", color: "#333", border: "1px solid #c5d0f0" }}>
                  <strong>Experience detected:</strong> {expDebug}
                </div>
              )}
            </>
          )}

          {error && <div style={styles.error}>{error}</div>}

          {result && (
            <div style={styles.resultCard}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={styles.label}>Match Score</span>
                <span style={{ fontSize: "24px", fontWeight: "700", color: scoreColor(result.match_score) }}>
                  {result.match_score}%
                </span>
              </div>
              <div style={styles.scoreBar}>
                <div style={{
                  width: `${result.match_score}%`,
                  height: "100%",
                  backgroundColor: scoreColor(result.match_score),
                  borderRadius: "4px",
                  transition: "width 0.5s ease",
                }} />
              </div>

              <div style={{ display: "flex", gap: "12px", marginBottom: "8px" }}>
                <div style={{ flex: 1, padding: "8px", backgroundColor: "#fff", borderRadius: "6px", border: `1px solid ${COLORS.border}`, textAlign: "center" }}>
                  <div style={{ fontSize: "18px", fontWeight: "700", color: COLORS.primary }}>
                    {result.total_experience_years != null ? result.total_experience_years : "N/A"}
                  </div>
                  <div style={{ fontSize: "11px", color: COLORS.textLight }}>Years Exp.</div>
                </div>
                <div style={{ flex: 1, padding: "8px", backgroundColor: "#fff", borderRadius: "6px", border: `1px solid ${COLORS.border}`, textAlign: "center" }}>
                  <div style={{ fontSize: "14px", fontWeight: "700", color: COLORS.primary }}>
                    {result.seniority_level || "N/A"}
                  </div>
                  <div style={{ fontSize: "11px", color: COLORS.textLight }}>Seniority</div>
                </div>
              </div>

              <div style={styles.label}>Strengths</div>
              <div>
                {result.strengths?.map((s, i) => (
                  <span key={i} style={{ ...styles.tag, backgroundColor: "#e6f4ea", color: COLORS.success }}>{s}</span>
                ))}
              </div>

              <div style={styles.label}>Missing Skills</div>
              <div>
                {result.missing_skills?.map((s, i) => (
                  <span key={i} style={{ ...styles.tag, backgroundColor: "#fef2f0", color: COLORS.warning }}>{s}</span>
                ))}
              </div>

              <div style={styles.label}>Summary</div>
              <p style={{ fontSize: "13px", lineHeight: "1.5", margin: "0 0 12px" }}>{result.summary}</p>

              <div style={styles.label}>Outreach Message</div>
              <div style={styles.message}>{result.outreach_message}</div>
              <button
                style={{ ...styles.button, marginTop: "8px", backgroundColor: "transparent", color: COLORS.primary, border: `1px solid ${COLORS.primary}`, fontSize: "12px", padding: "8px" }}
                onClick={() => navigator.clipboard.writeText(result.outreach_message)}
              >
                Copy Outreach Message
              </button>

              <button
                style={{
                  ...styles.button,
                  marginTop: "8px",
                  backgroundColor: shortlisted ? COLORS.success : "#f5a623",
                  fontSize: "13px",
                  padding: "10px",
                  opacity: shortlisted || shortlisting ? 0.8 : 1,
                  cursor: shortlisted || shortlisting ? "not-allowed" : "pointer",
                }}
                onClick={handleShortlist}
                disabled={shortlisted || shortlisting}
              >
                {shortlisted ? "✓ Shortlisted" : shortlisting ? "Saving..." : "⭐ Shortlist Candidate"}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
