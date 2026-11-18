# ResilienceX

React + TypeScript frontend and ASP.NET Core authentication backend for logistics resilience operations.

## Run locally

```sh
npm install
npm run dev
```

## Email authentication

Registration requires a username (up to 50 characters), email, and password (at least 8 characters). Sign-in requires the registered email and password. Passwords are hashed by the backend; the frontend does not contain demo-account fallbacks.

The Vite development server proxies `/api` to `http://localhost:5123`. Start the ASP.NET Core backend and configure its database connection string before registering users. Set the connection string locally with .NET user secrets (do not commit database credentials):

```sh
cd Backend/Backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=resilienceX;User=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;"
dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SECRET_KEY_AT_LEAST_32_CHARACTERS"
dotnet ef database update
dotnet run
```

The existing EF migration creates the `Users` table in the configured database. The backend stores password hashes, not plaintext passwords.

## Google sign-in

Create a Google OAuth client ID for the app's origin, then configure the same client ID for both the frontend and backend:

```sh
# Frontend .env.local
VITE_GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com

# Backend (from Backend/Backend)
dotnet user-secrets set "Google:ClientId" "your-client-id.apps.googleusercontent.com"
```

Google credentials are validated by the backend; a verified Google email can register or sign in. The Google button remains unavailable until the OAuth client ID is configured. Never share database passwords in chat or commit them to source control.
