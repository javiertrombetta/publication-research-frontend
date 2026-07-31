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
recovery, invitation acceptance, the privacy policy — says so explicitly with `[AllowAnonymous]`.

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
| `Api:BaseUrl` | Where the backend API lives. The only setting that has to be right for the application to work at all. |
| `Institution:*` | Fallback only. The institution's name, contact addresses and privacy policy are administrator-editable settings held by the API; these values are used solely while the API is unreachable, so the footer degrades rather than disappearing. |

Everything else an administrator would want to change — committee sizes, ethics documents,
password rules, deadlines, upload limits, registration policy, SMTP — lives in the API and is
edited from **System settings** in the interface, not from a configuration file.

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

All six operational roles are connected to the API end to end.

| Area | State |
| --- | --- |
| Sign in, sign up, email verification, password recovery and reset | Connected |
| Change password (every role), with the account lockout the API enforces | Connected |
| Public catalogue and publication detail | Connected |
| Student — publications, proposals, ethics, research paper, publication decision | Connected |
| Coordinator — proposal dispatch, supervisor selections, both ethics reviews, paper decision | Connected |
| Supervisor — proposals, ethics decision and document checks, paper review | Connected |
| Head of Department — department oversight and ethics review | Connected |
| Committee members, internal and external — assignments and evaluation | Connected |
| Admin — dashboard, users, committee assignment, audit log | Connected |
| Admin — system settings and invitations | Connected |
| Notifications — the top bar's bell, the list, and marking as read | Connected |
| Profile and profile photo, all roles | Connected |
| Categories | Scaffold only — see below |

### Still scaffold

Three areas remain as laid-out views with hardcoded sample data:

- **Categories** (`Controllers/CategoriesController.cs`) has no backend at all. Publication
  categories exist as a table in the API's schema, but no endpoint exposes them, so there is
  nothing to connect this to yet.
- **`ProposalsController` and `PublicationsController`** are the original cross-role listing
  screens. Each role now reaches its own proposals and publications through its own dashboard,
  which is where the real data lives; these two are redundant, and are candidates for removal
  rather than wiring.
- **`committee_review` on Coordinator, Supervisor and Head of Department.** Evaluating a paper
  belongs to committee members, and that screen is wired under `ExternalSupervisor`, which serves
  both committee roles. The three copies do nothing.

### The student's route through the system

A student may run several publications at once, each with its own proposals, ethics workflow and
paper. Every pipeline route carries the publication's id and is guarded by both ownership and
stage, so a URL cannot be edited into someone else's work, or into a stage that has not opened.

1. **Research proposals** — up to three, submitted together. A coordinator sends them to
   supervisors, a supervisor picks one, and the coordinator assigns them.
2. **Ethics approval** — a screening questionnaire followed by a declaration, then a supervisor
   and a coordinator decide whether documentation is required. Which documents a student is asked
   for is configured by an administrator, and each publication keeps the list it was given.
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

### Administration

**System settings** covers eight groups, each saved and validated on its own so a rejected mail
server cannot discard an unrelated edit: committees, ethics documents, deadlines, uploads,
passwords, access, notifications and institution details.

Two of them change what the system asks of people, and both apply to work started afterwards
only. Committee composition and the ethics document list are recorded on a publication when it
is created, so tightening a rule never moves the goalposts for research already under way.

**Invitations** is how someone gets an account when self-registration is closed, which is every
deployment that is not a development one. An administrator invites any address to any role,
choosing the role as they send it. It is also the only route that ever existed for external
committee members: they are outside the institution, so no email domain could say what they are.

## Project layout

```
Controllers/          One per role, plus Auth, Profile, Public, Notifications,
                      Invitations, SystemSettings and Home
Models/               View models, grouped by area
Infrastructure/
  Api/                Typed API clients, DTOs and the shared response envelope
  Http/               Bearer-token attach, refresh and retry
  Options/            Strongly-typed configuration
Services/             Authentication cookie handling, institution details
ViewComponents/       The top bar's notification bell
Common/               Role landing, role names, status display
Views/                Razor views, grouped by controller
wwwroot/              Site CSS and JavaScript, and vendored Tabler and Bootstrap
```

## Conventions

- **British English** throughout the interface copy, and `en-GB` as the application's culture, so
  dates read `30 Jul 2026` rather than `Jul 30, 2026`.
- **Statuses** are humanised at the point of display — `InProgress` becomes `In Progress` — and
  coloured from a single mapping in `Common/DisplayText.cs`, so the same status is the same
  colour on every screen.
- **Messages**, both flash messages and validation errors, are rendered as toasts from one shared
  template in three kinds: success, error and information. A controller sets
  `TempData["SuccessMessage"]`, `["ErrorMessage"]` or `["InfoMessage"]`, or leaves `ModelState`
  invalid, and needs no markup of its own.
- **Search, filter and tab state** lives in the query string, so a filtered or tabbed view can be
  linked to and reloaded, and keeps working without JavaScript.
- **No Bootstrap JavaScript.** Tabler's `tabler.min.js` does not bundle it, so `data-bs-toggle`
  does nothing here. Tabs are server-rendered links, and confirmations are inline panels toggled
  by a few lines of plain JavaScript. Anything relying on Bootstrap's modal or tab components
  fails silently — reach for the existing patterns instead.
- **Rules an administrator controls are not restated here.** Password length and complexity,
  upload size and permitted file types all come from the API, so a form that duplicated them
  would go stale the first time they changed, and would reject input the server would have
  accepted.

## Technology

ASP.NET Core MVC (.NET 8) with Razor views, Tabler UI on Bootstrap 5, and jQuery Validation for
client-side form validation. No ORM, no database driver and no Identity: persistence and identity
belong to the backend.
