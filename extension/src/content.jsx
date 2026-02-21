import React from "react";
import { createRoot } from "react-dom/client";
import Panel from "./panel.jsx";

function init() {
  if (!window.location.pathname.startsWith("/in/")) return;
  if (document.getElementById("sourceflow-root")) return;

  const container = document.createElement("div");
  container.id = "sourceflow-root";
  document.body.appendChild(container);

  const root = createRoot(container);
  root.render(<Panel />);
}

init();

let lastUrl = location.href;
new MutationObserver(() => {
  if (location.href !== lastUrl) {
    lastUrl = location.href;
    const existing = document.getElementById("sourceflow-root");
    if (existing) existing.remove();
    init();
  }
}).observe(document.body, { subtree: true, childList: true });
