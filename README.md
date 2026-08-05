# Research Publication Management System: Frontend

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
anything reachable without an account, whether the public catalogue, sign-in, sign-up, password
recovery, invitation acceptance or the privacy policy, says so explicitly with `[AllowAnonymous]`.

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

Everything else an administrator would want to change, from committee sizes and ethics documents to
password rules, deadlines, upload limits, registration policy and SMTP, lives in the API and is
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

### Signing in to try it

A development or shared testing API comes up with a demonstration dataset already in it: twenty-two
accounts across two departments and thirty publications, parked at every point in the three
pipelines where somebody has to act. Every account uses the password `DevTest123!`: `student.test@aisstudent.ac.nz`,
`supervisor.test@ais.ac.nz`, `coordinator.test@ais.ac.nz`, `hod.test@ais.ac.nz`,
`reviewer.test@ais.ac.nz`, `external.test@ais.ac.nz`, `admin.test@ais.ac.nz`. The API's README lists
the rest and says what each one has waiting.

Sign in as any of them and the notification bell shows what that role owes, rather than a system with
nothing in it.

## What is wired up

All six operational roles are connected to the API end to end: Admin, Head of Department,
Coordinator, Supervisor, Reviewer and external committee member. Students and Staff are the two
that are never assigned work, so neither is counted among them.

| Area | State |
| --- | --- |
| Sign in, sign up, email verification, password recovery and reset | Connected |
| Change password (every role), with the account lockout the API enforces | Connected |
| Public catalogue and publication detail | Connected |
| Student: publications, proposals, ethics, research paper, publication decision | Connected |
| Coordinator: proposal dispatch, supervisor selections, both ethics reviews, paper decision | Connected |
| Supervisor: proposals, ethics decision and document checks, paper review | Connected |
| Head of Department: department oversight and ethics review | Connected |
| Reviewers and external committee members: assignments and evaluation | Connected |
| Admin: dashboard, users, departments, committee assignment, supervisor groups, audit log | Connected |
| Admin: system settings and invitations | Connected |
| Notifications: the top bar's bell, the list, and marking as read | Connected |
| Profile and profile photo, all roles | Connected |

Nothing is left as a laid-out screen with sample data in it. Every controller here reaches the API,
and the three that could not were removed rather than left to look finished:

- **`ProposalsController` and `PublicationsController`** were cross-role listings from before each
  role had a dashboard of its own. Every one of their screens now exists, with real data, in the
  place its role actually starts from.
- **`CategoriesController`** had nothing behind it. Publication categories were a table no endpoint
  exposed and nothing ever wrote to, doing a job `ResearchArea` already does end to end, on a
  student's profile, on a paper's metadata, and as a filter in the public catalogue. The table has
  been dropped as well, so the question does not outlive the screen.
- **`committee_review` on Coordinator, Supervisor and Head of Department** were three copies of a
  screen that belongs to committee members, and none of them had a view. Reaching one produced an
  error, not an empty page. The real one is under `ExternalSupervisor`, which serves both committee
  roles.

### The student's route through the system

A student may run several publications at once, each with its own proposals, ethics workflow and
paper. Every pipeline route carries the publication's id and is guarded by both ownership and
stage, so a URL cannot be edited into someone else's work, or into a stage that has not opened.

1. **Research proposals**: submitted together, as many as the institution asks for. A coordinator
   sends them to supervisors, the supervisors say which they would take on, and the coordinator
   allocates one. A round where nobody is willing goes back, and the coordinator either sends it to
   different supervisors or asks the student to write new ones.
2. **Ethics approval**: a screening questionnaire followed by a declaration, then a supervisor
   and a coordinator decide whether documentation is required. Which documents a student is asked
   for is configured by an administrator, and each publication keeps the list it was given.
3. **Research paper**: drafted, uploaded and submitted, then reviewed by the supervisor and an
   evaluation committee before the coordinator accepts it.
4. **Publication decision**: once accepted, the author alone decides whether the paper appears
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

**System settings** is twelve tabs, each saved and validated on its own so a rejected mail server
cannot discard an unrelated edit: committees, ethics documents, the steps of each pipeline, which
decisions must carry a comment, deadlines, uploads, passwords, access, notifications, where files
are stored, departments and institution details.

Two of them change what the system asks of people, and both apply to work started afterwards
only. Committee composition and the ethics document list are recorded on a publication when it
is created, so tightening a rule never moves the goalposts for research already under way.

**Invitations** is how someone gets an account when self-registration is closed, which is every
deployment that is not a development one. An administrator invites any address to any role,
choosing the role as they send it. It is also the only route that ever existed for external
committee members: they are outside the institution, so no email domain could say what they are.

## Project layout

```
Controllers/          One per role (Student, Supervisor, Coordinator, HeadOfDepartment,
                      ExternalSupervisor for both committee roles, Admin), plus Auth,
                      Profile, Public, Notifications, Invitations, Users, AuditLogs,
                      SystemSettings, Downloads, Theme, Sidebar and Home
Models/               View models, grouped by area
Infrastructure/
  Api/                Typed API clients, DTOs and the shared response envelope
  Http/               Bearer-token attach, refresh and retry
  Options/            Strongly-typed configuration
Services/             Authentication cookie handling, institution details
ViewComponents/       The top bar's notification bell
Common/               Role landing, role names, status display, paging helpers
Views/                Razor views, grouped by controller
  Shared/             The layout, the sortable column heading, the pager, the toast
wwwroot/              Site CSS and JavaScript, and vendored Tabler and Bootstrap
```

`ExternalSupervisor` is named for the role it was written for and now serves both committee roles,
Reviewer and external. Renaming it would mean renaming its routes, which the API's audit trail and
the team's bookmarks both point at, so it keeps the name and this paragraph explains it.

## Conventions

- **Statuses** are humanised at the point of display, so `InProgress` becomes `In Progress`, and
  coloured from a single mapping in `Common/DisplayText.cs`, so the same status is the same
  colour on every screen.
- **Messages**, both flash messages and validation errors, are rendered as toasts from one shared
  template in three kinds: success, error and information. A controller sets
  `TempData["SuccessMessage"]`, `["ErrorMessage"]` or `["InfoMessage"]`, or leaves `ModelState`
  invalid, and needs no markup of its own.
- **Search, filter and tab state** lives in the query string, so a filtered or tabbed view can be
  linked to and reloaded, and keeps working without JavaScript.
- **Paging, sorting and searching happen in the API, never in the browser.** Sorting the ten rows a
  page happens to hold is not sorting the list: the oldest proposal in a department is on the last
  page, and somebody who asks for oldest first expects to see it. Column headings are links that
  carry the sort in the query string; `Views/Shared/_SortableHeader.cshtml` draws one and
  `_Pager.cshtml` draws the controls under it.
- **Whatever a listing can be ordered by, it shows.** A heading that sorts by a date the rows do not
  display looks broken, because the order changes and nothing visible explains why.
- **Bootstrap's JavaScript is there, under another name.** Tabler's `tabler.min.js` bundles it and
  exposes it as `window.tabler` rather than `window.bootstrap`, so `data-bs-toggle` works for
  collapse, dropdown, modal and tab, and code that reaches for `bootstrap.Modal` by name does not.
  Anything more than showing and hiding is plain JavaScript in `wwwroot/js/site.js`, driven by
  `data-rpms-*` attributes in the markup, so a view says what it wants rather than carrying a
  script of its own.
- **Tabs and filters are server-rendered where the state should survive a reload.** System settings
  puts its tab in the query string for that reason: a rejected mail server should not throw you
  back to the first tab.
- **Read the page without seeing it.** Five rules, checked against the rendered HTML of 156 pages
  across every role rather than against the views: one `<main>` per page and a skip link ahead of
  it, so a keyboard is not made to walk the sidebar again on each screen; one `<h1>` and no level
  skipped, since the heading levels are how a screen reader offers to jump about; every header
  cell says `scope`; and every field carries a name, whether from a `<label>` that points at it,
  from sitting inside one, or from `aria-label` where the design gives a row of controls a single
  visible label. Section titles are written `<h2 class="h3">`: the level is what it is, the size is
  what it looks like. A placeholder is not a label.
- **The site names its own restrictions to the browser.** A content security policy, `nosniff`,
  `X-Frame-Options: DENY` and `Referrer-Policy: no-referrer` are set for every response in every
  environment, not left to whatever a host adds: the `SAMEORIGIN` header visible while developing
  comes from the development pipeline and is gone once the application is published. The policy
  names `'self'` and nothing else because the site loads nothing else, everything down to Tabler
  being served from here. Inline script and style are permitted, since the views use both. What
  the policy is really buying is that no script may be fetched from another origin, no form of
  ours may be posted somewhere else, and no page anywhere may put this site in a frame under a
  layer of its own.
- **Rules an administrator controls are not restated here.** Password length and complexity,
  upload size and permitted file types all come from the API, so a form that duplicated them
  would go stale the first time they changed, and would reject input the server would have
  accepted.

### Things people can set for themselves

Three preferences belong to the person rather than to the institution, so they are saved against the
account and follow them to another browser:

- **A light or dark theme**, switched from the user menu.
- **The order of the sidebar**, rearranged by dragging or from the keyboard, and saved after a
  pause rather than on every nudge, so moving one item three places does not become three requests
  racing each other.
- **Whether they are taking new work on**, which governs what the system offers them next and leaves
  everything already assigned exactly where it is. That is deliberately not the same as an
  administrator disabling the account, and it is only shown to the roles that are ever chosen.

### On a phone

Every screen works at 320 pixels. The rule is that the page scrolls down and never sideways: a
listing wider than the screen scrolls inside its own container rather than dragging the whole layout
with it. The sidebar becomes a drawer under the top bar, and the column headings that stand over a
grid of cards are hidden where the cards stop being a grid, since a list of links describing columns
that are no longer side by side is worse than none.

## Technology

ASP.NET Core MVC (.NET 8) with Razor views, Tabler UI on Bootstrap 5, and jQuery Validation for
client-side form validation. No ORM, no database driver and no Identity: persistence and identity
belong to the backend.
