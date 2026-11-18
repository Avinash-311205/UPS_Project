# ResilienceX frontend demo

Responsive React + TypeScript frontend prototype for logistics cyber-resilience operations.

## Run locally

```sh
npm install
npm run dev
```

## Demo sign-in accounts

Use an employee ID or email and the matching demo password:

| Role | Employee ID | Email | Password |
| --- | --- | --- | --- |
| Operations Manager | `RX-1042` | `manager@resiliencex.com` | `Password123!` |
| IT / Security Administrator | `RX-2086` | `security@resiliencex.com` | `SecurePass456!` |
| Hub Operator | `RX-3195` | `operator@resiliencex.com` | `HubOps789!` |

The login screen demo-account buttons sign directly into that role's mock dashboard. Signing in saves a mock user ID in `sessionStorage` for the current browser tab. Sign out clears that session.

When the login screen loads, `public/promo_video.mp4` plays as a full-viewport intro and the login form appears when it ends. The original MP4 is served without transcoding and does not loop. Browsers commonly block autoplay with sound until the user interacts with the page; when blocked, use the on-screen **Play introduction with sound** prompt. **Skip intro** is also available.

## Demo permissions

- **Operations Manager:** reviews and approves or rejects continuity activation.
- **IT / Security Administrator:** views system health, isolates the affected integration, and handles recovery validation and reconciliation.
- **Hub Operator:** sees only the two mock shipments assigned to the hub and can record shipment activity after continuity is approved; system and recovery controls are not available.

Continuity activation remains unavailable until an Operations Manager explicitly approves it. Mock incident and continuity state is retained in `sessionStorage` across sign-outs in the same browser tab so the role handoff can be demonstrated.

## Backend integration note

Authentication and role checks are simulated in the browser using `src/auth/mockAuth.ts`. This is demonstration behavior only: it is not secure authentication, and client-side permission checks can be bypassed. Do not use real credentials or rely on this frontend to protect data. Replace the mock auth module with the backend API client when the ASP.NET Core Identity/JWT service is ready, and enforce every role and action on the API as well.
