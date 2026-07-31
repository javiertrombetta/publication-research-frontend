# Research Publication Management System — Frontend

The web application for Auckland Institute of Studies' research publication process: students
submit research proposals, work through ethics approval, and submit a research paper, which
coordinators, supervisors and an evaluation committee review before it can reach a public
catalogue.

This repository holds the frontend only. It is an ASP.NET Core MVC application that renders
Razor views and talks to the REST API in
[publication-research-backend](https://github.com/javiertrombetta/publication-research-backend).
It has no database of its own.

## Architecture

The frontend keeps no persistent state. Every piece of data comes from the API through a typed
client, and the only thing it stores in the browser is an encrypted authentication cookie.

Authentication bridges two models. The API issues JWT access and refresh tokens; the browser
never sees them. On a successful sign-in the tokens are stored as claims inside an encrypted
cookie, along with the user's roles, so `[Authorize(Roles = ...)]` works directly against the
role claims. A `DelegatingHandler` attaches the bearer token to every outgoing API call,
refreshes it shortly before it expires, and retries once on a `401`.

Authorisation is deny-by-default: a global fallback policy requires an authenticated user, and
anything reachable without an account — the public catalogue, sign-in, sign-up, password
recovery, the privacy policy — says so explicitly with `[AllowAnonymous]`.

```
Browser ──cookie──▶ ASP.NET Core MVC (this repo) ──Bearer JWT──▶ REST API ──▶ MySQL
```

## Requirements

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) or newer
- A running instance of the backend API

## Configuration

`appsettings.json` holds the defaults; `appsettings.Development.json` points at a local API.

| Key | Purpose |
| --- | --- |
| `Api:BaseUrl` | Where the backend API lives. |
| `Institution:ItSupportEmail` | Address behind the footer's "Contact IT". Empty until decided, and while empty the footer shows plain text rather than a link that goes nowhere. |
| `Institution:ResearchEnquiriesEmail` | Address a reader writes to for the full text of a published paper. Same behaviour when empty. |
| `Institution:PrivacyPolicyUrl` | The institution's authoritative privacy policy. |

## Running it

Start the backend first, then:

```bash
dotnet run --launch-profile http
```

The application listens on `http://localhost:5090` (or `https://localhost:7178` with the
`https` profile) and opens on the public catalogue.

To point it at a different API without editing files:

```bash
dotnet run --launch-profile http -- --Api:BaseUrl=https://your-api-host
```

## What is wired up

Each role's screens exist, but only some are connected to the API. The rest are laid-out views
still waiting to be wired.

| Area | State |
| --- | --- |
| Sign in, sign up, email verification, password recovery and reset | Connected |
| Public catalogue and publication detail | Connected |
| Student — publications, proposals, ethics, research paper, publication decision | Connected |
| Profile and profile photo (all roles) | Connected |
| Coordinator, Supervisor, Head of Department, External Committee Member, Admin | Views only |

### The student's route through the system

A student may run several publications at once, each with its own proposals, ethics workflow and
paper. Every pipeline route carries the publication's id and is guarded by both ownership and
stage, so a URL cannot be edited into someone else's work or into a stage that has not opened.

1. **Research proposals** — up to three, submitted together. A coordinator sends them to
   supervisors, a supervisor picks one, and the coordinator assigns them.
2. **Ethics approval** — a screening questionnaire followed by a declaration, then a supervisor
   and a coordinator decide whether documentation is required.
3. **Research paper** — drafted, uploaded and submitted, then reviewed by the supervisor and an
   evaluation committee before the coordinator accepts it.
4. **Publication decision** — once accepted, the author alone decides whether the paper appears
   in the public catalogue.

Each publication carries an activity history: every action taken on it, by whom, in what
capacity, and the comment that justified it.

### The public catalogue

The catalogue is the site's front door and needs no account. It lists published papers with
their abstracts and offers search by title or abstract, author, keyword and year, along with an
APA 7th citation for each.

The full text is deliberately not downloadable from it: a reader asks the institution for a copy,
and the API's download endpoint requires a signed-in user.

## Project layout

```
Controllers/          One per role, plus Auth, Profile, Public and Home
Models/               View models, grouped by area
Infrastructure/
  Api/                Typed API clients, DTOs and the shared response envelope
  Http/               Bearer-token attach, refresh and retry
  Options/            Strongly-typed configuration
Services/             Authentication cookie handling
Common/               Role landing, role names, status display
Views/                Razor views, grouped by controller
wwwroot/              Site CSS and JavaScript, and vendored Tabler and Bootstrap
```

## Conventions

- **British English** throughout the interface copy, and `en-GB` as the application's culture, so
  dates read `30 Jul 2026` rather than `Jul 30, 2026`.
- **Statuses** are humanised at the point of display (`InProgress` becomes `In Progress`) and
  coloured from a single mapping in `Common/DisplayText.cs`, so the same status is the same
  colour on every screen.
- **Messages** — flash messages and validation errors alike — are rendered as toasts from one
  shared template. A controller sets `TempData["SuccessMessage"]` or `TempData["ErrorMessage"]`,
  or leaves `ModelState` invalid, and needs no markup of its own.
- **Search and filter state** lives in the query string, so a filtered view can be linked to and
  reloaded, and keeps working without JavaScript.

## Technology

ASP.NET Core MVC (.NET 8) with Razor views, Tabler UI on Bootstrap 5, and jQuery Validation for
client-side form validation. No ORM, no database driver and no Identity: persistence and identity
belong to the backend.
