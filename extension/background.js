const API_BASE = "http://localhost:8080";
//const API_BASE = "https://sflow-5diw.onrender.com";
chrome.runtime.onInstalled.addListener(() => {
  console.log("SourceFlow extension installed");
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === "API_REQUEST") {
    handleApiRequest(message).then(sendResponse);
    return true;
  }
  if (message.type === "OPEN_PAYWALL") {
    (async () => {
      const { token } = await chrome.storage.local.get(["token"]);
      const params = new URLSearchParams({ country: message.country || "IN" });
      if (token) params.set("token", token);
      const url = `${API_BASE}/paywall?${params}`;
      chrome.tabs.create({ url });
      sendResponse({ ok: true });
    })();
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
      return { error: (data && data.error) || res.statusText, statusCode: res.status, data };
    }
    return { data };
  } catch (err) {
    return { error: err.message };
  }
}
