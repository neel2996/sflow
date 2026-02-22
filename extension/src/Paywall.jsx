import React, { useState, useEffect } from "react";
import { api } from "./api.js";

const COLORS = {
  primary: "#0073b1",
  success: "#057642",
  border: "#e0e0e0",
  text: "#333333",
  textLight: "#666666",
};

export default function Paywall({ country = "IN", onClose }) {
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [processing, setProcessing] = useState(null);
  const [razorpayKey, setRazorpayKey] = useState("");

  const isIndia = String(country).toUpperCase() === "IN";

  useEffect(() => {
    load();
  }, [country]);

  async function load() {
    setLoading(true);
    setError("");
    try {
      const [plansData, config] = await Promise.all([
        api.getPlans(country),
        api.getClientConfig(),
      ]);
      setPlans(plansData);
      setRazorpayKey(config?.razorpay_key_id || "");
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  async function handleRazorpay(plan) {
    if (!razorpayKey) {
      setError("Razorpay not configured");
      return;
    }
    setProcessing(plan.id);
    setError("");
    try {
      const res = await api.createOrder(plan.id);
      const orderId = res?.order_id ?? res?.orderId;
      if (!orderId) throw new Error("Failed to create order");
      await loadRazorpayScript();
      const rzp = new window.Razorpay({
        key: (res?.key || razorpayKey),
        order_id: orderId,
        name: "SourceFlow",
        description: `${plan.name} - ${plan.credits} credits`,
        handler: () => {
          setProcessing(null);
          onClose?.();
        },
      });
      rzp.open();
    } catch (err) {
      setError(err.message);
      setProcessing(null);
    }
  }

  function loadRazorpayScript() {
    return new Promise((resolve) => {
      if (window.Razorpay) {
        resolve();
        return;
      }
      const s = document.createElement("script");
      s.src = "https://checkout.razorpay.com/v1/checkout.js";
      s.onload = resolve;
      document.head.appendChild(s);
    });
  }

  function handlePlan(plan) {
    handleRazorpay(plan);
  }

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        backgroundColor: "rgba(0,0,0,0.5)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 10001,
        padding: "16px",
      }}
    >
      <div
        style={{
          backgroundColor: "#fff",
          borderRadius: "12px",
          maxWidth: "400px",
          width: "100%",
          maxHeight: "90vh",
          overflowY: "auto",
          padding: "20px",
          fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
        }}
      >
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
          <h3 style={{ margin: 0, color: COLORS.primary, fontSize: "18px" }}>
            Get Credits
          </h3>
          {onClose && (
            <button
              onClick={onClose}
              style={{ background: "none", border: "none", fontSize: "20px", cursor: "pointer", color: COLORS.textLight }}
            >
              ×
            </button>
          )}
        </div>

        <p style={{ fontSize: "13px", color: COLORS.textLight, marginBottom: "16px" }}>
          {isIndia ? "One-time credit packs (INR)" : "Subscription plans (USD)"}
        </p>

        {loading && <p style={{ textAlign: "center", color: COLORS.textLight }}>Loading plans...</p>}
        {error && <p style={{ color: "#b24020", fontSize: "13px", marginBottom: "12px" }}>{error}</p>}

        {!loading && plans.length > 0 && (
          <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
            {plans.map((p) => (
              <div
                key={p.id}
                style={{
                  padding: "14px",
                  border: `1px solid ${COLORS.border}`,
                  borderRadius: "8px",
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                }}
              >
                <div>
                  <div style={{ fontWeight: "600", fontSize: "14px" }}>{p.name}</div>
                  <div style={{ fontSize: "13px", color: COLORS.textLight }}>
                    {p.credits} credits
                    {(p.billing_type === "subscription" || p.billing_type === "Monthly") && " / month"}
                  </div>
                </div>
                <button
                  onClick={() => handlePlan(p)}
                  disabled={processing !== null}
                  style={{
                    padding: "8px 16px",
                    backgroundColor: COLORS.primary,
                    color: "#fff",
                    border: "none",
                    borderRadius: "6px",
                    fontSize: "13px",
                    fontWeight: "600",
                    cursor: processing ? "wait" : "pointer",
                  }}
                >
                  {processing === p.id ? "..." : isIndia ? "Buy" : "Subscribe"}
                </button>
              </div>
            ))}
          </div>
        )}

        <p style={{ fontSize: "11px", color: COLORS.textLight, marginTop: "16px" }}>
          1 credit = 1 LinkedIn profile scan
        </p>
      </div>
    </div>
  );
}
