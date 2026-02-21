function getToken() {
  return new Promise((resolve) => {
    chrome.storage.local.get(["token"], (result) => resolve(result.token || null));
  });
}

function setAuth(token, email) {
  return new Promise((resolve) => {
    chrome.storage.local.set({ token, email }, resolve);
  });
}

function clearAuth() {
  return new Promise((resolve) => {
    chrome.storage.local.remove(["token", "email"], resolve);
  });
}

function getStoredEmail() {
  return new Promise((resolve) => {
    chrome.storage.local.get(["email"], (result) => resolve(result.email || null));
  });
}

function request(method, path, body) {
  return new Promise((resolve, reject) => {
    chrome.runtime.sendMessage(
      { type: "API_REQUEST", method, path, body },
      (response) => {
        if (chrome.runtime.lastError) {
          reject(new Error(chrome.runtime.lastError.message));
          return;
        }
        if (!response || response.error) {
          reject(new Error(response?.error || "Request failed"));
          return;
        }
        resolve(response.data);
      }
    );
  });
}

export const api = {
  register: (email, password) =>
    request("POST", "/auth/register", { email, password }),
  login: (email, password) =>
    request("POST", "/auth/login", { email, password }),
  getMe: () => request("GET", "/user/me"),
  getJobs: () => request("GET", "/jobs"),
  createJob: (title, description) =>
    request("POST", "/jobs", { title, description }),
  scan: (jobId, profileUrl, profileText, computedExperienceYears) =>
    request("POST", "/analysis/scan", {
      job_id: jobId,
      profile_url: profileUrl,
      profile_text: profileText,
      computed_experience_years: computedExperienceYears,
    }),
  shortlist: (jobId, profileUrl, candidateName, matchScore, summary, outreachMessage) =>
    request("POST", "/shortlist", {
      job_id: jobId,
      profile_url: profileUrl,
      candidate_name: candidateName,
      match_score: matchScore,
      summary,
      outreach_message: outreachMessage,
    }),
  getShortlist: (jobId) => request("GET", `/shortlist/${jobId}`),
};

export { getToken, setAuth, clearAuth, getStoredEmail };
