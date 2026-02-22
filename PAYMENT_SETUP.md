# SourceFlow Payment Wall – Setup Instructions

## Overview

- **India (IN)**: Razorpay one-time credit packs (₹99 / ₹199 / ₹999)
- **Global (US/EU/Other)**: Razorpay subscription plans (USD $9 / $19 / $49)
- **Credits**: 1 credit = 1 LinkedIn profile scan
- **New users**: 0 credits on registration
- **Provider**: Razorpay only (Stripe removed)

---

## 1. Environment Variables

Add to `appsettings.json` or environment:

```bash
# Razorpay (India + Global)
Razorpay__KeyId=rzp_live_xxx
Razorpay__KeySecret=xxx
Razorpay__WebhookSecret=xxx
Razorpay__EnableMockPayments=false

# PostgreSQL (production)
DATABASE_URL=postgresql://user:pass@host:5432/dbname
```

---

## 2. Razorpay Setup

1. Create a Razorpay account and get Key ID + Key Secret.
2. Enable international payments in Razorpay dashboard for USD/EUR/GBP.
3. Create a webhook: `https://your-api.com/payments/razorpay-webhook`
4. Subscribe to **order.paid** and **payment.captured**.
5. Copy the webhook secret to `Razorpay__WebhookSecret`.
6. Add `Razorpay__KeyId` to config – returned to client for checkout.

---

## 3. API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/payments/plans?country=IN` | No | Plans for India (INR) |
| GET | `/payments/plans?country=US` | No | Plans for Global (USD) |
| GET | `/payments/client-config` | No | Razorpay Key ID for frontend |
| POST | `/payments/create-order` | Yes | Create Razorpay order (orderId, amount, currency, key) |
| POST | `/payments/create-razorpay-order` | Yes | Legacy – returns order_id only |
| POST | `/payments/razorpay-webhook` | No | Razorpay webhook (signature validated) |
| POST | `/payments/mock-razorpay-success` | Yes | Mock payment (dev only) |
| POST | `/payments/simulate-razorpay-webhook` | Yes | Simulate webhook (dev only) |

---

## 4. Extension Behavior

- **403 on scan** → Paywall modal opens.
- **0 credits** → "Get Credits" button shown in panel and popup.
- **India (country=IN)** → INR one-time packs.
- **Other countries** → USD subscription plans.

---

## 5. Security

- Credits are added only via webhooks (or mock/simulate in dev).
- Razorpay: HMAC SHA256 validates `X-Razorpay-Signature`.
- Idempotent webhook: duplicate orders are ignored (RazorpayOrderId check).
- Frontend success callbacks are not trusted; fulfillment is webhook-only.

---

## 6. Seeded Plans

| Region | Plan | Price | Credits | Type |
|--------|------|-------|---------|------|
| India | Starter | ₹99 | 50 | one_time |
| India | Growth | ₹199 | 150 | one_time |
| India | Pro | ₹999 | 1000 | one_time |
| Global | Starter | $9 | 200 | subscription |
| Global | Growth | $19 | 600 | subscription |
| Global | Pro | $49 | 2000 | subscription |

---

## 7. Run Locally

```bash
# Backend
cd backend/SourceFlow.Api
dotnet run

# Extension
cd extension
npm run build
# Load extension in chrome://extensions
```

Update `extension/background.js` to use your API URL (e.g. `http://localhost:8080`).
