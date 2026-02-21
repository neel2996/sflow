const API_BASE = "http://localhost:5000";

chrome.runtime.onInstalled.addListener(() => {
  console.log("SourceFlow extension installed");
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === "API_REQUEST") {
    handleApiRequest(message).then(sendResponse);
    return true;
  }
});

async function handleApiRequest({ method, path, body }) {
  const { token } = await chrome.storage.local.get(["token"]);
  const headers = { "Content-Type": "application/json" };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  try {
    const res = await fetch(`${API_BASE}${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    const text = await res.text();
    let data;
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }

    if (!res.ok) {
      return { error: (data && data.error) || res.statusText };
    }
    return { data };
  } catch (err) {
    return { error: err.message };
  }
}
