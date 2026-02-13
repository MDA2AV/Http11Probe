# Changelog

All notable changes to Http11Probe are documented in this file.

## [Unreleased]

### Added
- **Server configuration pages** — per-server docs pages showing Dockerfile, source code, and config files for all 36 tested servers (`docs/content/servers/`)
- **Clickable server names** — server names in the probe results table and summary bar chart now link to their configuration page
- **Sticky first column** — server name column stays pinned to the left edge while scrolling horizontally through result tables
- **Collapsible sub-groups** — group headers in result tables are now clickable to collapse/expand, with a chevron indicator and a "Collapse All / Expand All" toggle button
- **Row-click detail popup** — clicking a server row opens a modal showing that server's results for the current table in a vertical layout (Test, Expected, Got, Description) with section and table name in the header
- **Truncation notice** — tooltip and modal now show a `[Truncated]` notice at the top when raw request/response data exceeds the 8,192-byte display limit
- **Filter box** — text input above result tables to filter by server name, language, or test name; supports multiple comma-separated keywords

### Changed
- **Horizontal column headers** — test name headers are now displayed horizontally instead of rotated at -55°, improving readability
- **Zebra striping** — alternating row backgrounds for easier scanning
- **Softer borders** — lighter table borders in both light and dark mode, plus vertical separators between test columns
- **Expected row styling** — CSS-only background with dark mode support (fixes light gray leak in dark mode), thicker bottom border for visual separation
- **Scored/unscored separator** — heavier vertical border line between scored and unscored test columns
- **Larger pills** — increased padding, min-width, and border-radius for result badges
- **Improved readability** — larger base font (13px), bigger column headers with heavier weight and letter-spacing, more cell padding throughout
- **Group header refinement** — added padding and bottom border to collapsible group headers
- **Toggle button polish** — reduced border-radius from pill to button shape
- **Scroll overflow hint** — "Scroll to see all tests" label and right-edge fade gradient appear when tables overflow horizontally
- **Language suffix dark mode** — improved contrast for language labels in dark mode
- **Mobile bottom-sheet modal** — modal slides up from bottom on small screens with full width and rounded top corners
- **Touch-friendly targets** — larger buttons and invisible hit-area expansion on pills for touch devices
- **Smooth momentum scroll** — added `-webkit-overflow-scrolling:touch` for iOS
- **Stronger sticky shadow on mobile** — increased shadow intensity for the pinned server name column
- **Scrollable tooltips** — hover tooltips are now interactive and scrollable for large payloads (removed `pointer-events:none`, increased `max-height` to `60vh`)
- **Larger click modal** — expanded from `max-width:700px` to `90vw` and `max-height` from `80vh` to `85vh` to better accommodate large request/response data
- Raw request capture now includes truncation metadata when payload exceeds 8,192 bytes (`TestRunner.cs`)
- Raw response capture now includes truncation metadata when response exceeds 8,192 bytes (`ResponseParser.cs`)

## [2026-02-12]

### Added
- **Request/response detail tooltips** — hover over a result pill to see the raw response; click to open a modal with both the raw request and response (#27)
- Repository cleanup — removed clutter files (probe-glyph.json, pycache, package-lock.json, DotSettings.user)

### Fixed
- BARE-LF tests (RFC 9112 §2.2) adjusted to warn on 2xx instead of fail, matching RFC SHOULD-level requirement (#21)

### Removed
- Proxy compliance tests removed from the suite (#20)

## [2026-02-11]

### Added
- POST endpoint for Kestrel (ASP.NET Minimal) server (#13)
- POST endpoint for Quarkus server (#14)
- POST endpoint for Spring Boot server (#16)
- POST endpoint for Express server (#17)

### Fixed
- H2O server now allows POST commands (#19)
- Flask server routing and default port (#11)
- SimpleW server POST handling and version update (#5)

## [2026-02-09]

### Added
- SimpleW server contributed by stratdev3 (#2)

### Fixed
- Glyph server — reset request state on each new connection (#3)
- In-development frameworks now filtered from results (#4)
- SimpleW removed from blacklisted servers

## [2026-02-08]

### Added
- **30 new tests** — body/content handling, chunked TE attack vectors, and additional compliance/smuggling tests (46 → 80 → 110+)
- **7 new servers** — Actix, Ntex, Bun, H2O, NetCoreServer, Sisk, Watson
- **6 more servers** — GenHTTP, SimpleW, EmbedIO, Puma, PHP, Deno, and others (total: 36)
- **Deep analysis docs** — verified RFC evidence and ABNF grammar added to all glossary pages
- **Exact HTTP request code blocks** in all glossary pages
- **Category filter** — filter probe results by Compliance, Smuggling, or Malformed Input
- **Language filter** — filter servers by programming language
- **Sub-tables** — result tables split into logical groups within each category
- **Unscored tests** — separate bucket for RFC-compliant reference tests, shown with reduced opacity and asterisk
- **CLI improvements** — `--test` filter, `--help`, docs links in output, selected test display
- **Summary bar chart** — ranked bars replacing summary badges, with pass/warn/fail/unscored segments
- **Scrollbar styling** — themed scrollbars for probe result tables
- **Custom favicon** — shield icon for browser tab
- **Docs logo** — minimal shield outline

### Fixed
- Summary fail count derivation so pass + warn + fail = total
- Unscored double-counting in summary statistics
- Sort order: rank by scored pass + scored warn only
- Puma Dockerfile: install build-essential for nio4r native extension
- Deno Dockerfile: use `latest` tag instead of nonexistent `:2`
- FRAGMENT-IN-TARGET re-scored as strict (implicit grammar prohibition)
- Nancy and Nginx failing to start in CI
- All servers bound to `0.0.0.0` for Docker reachability

### Removed
- Redundant SMUG-HEADER-INJECTION test (covered by other smuggling tests)
- Nancy server removed from probe (no probe.json)

## [2026-02-07]

### Added
- **Initial release** — extracted from Glyph11 into standalone Http11Probe repository
- 12 standalone test servers dockerized with Docker Compose
- Sequential probe workflow — one server at a time on port 8080
- CI probe workflow (`.github/workflows/probe.yml`) with STRICT expectations dictionary
- Hugo + Hextra documentation site with glossary, per-test docs, and probe results pages
- Separate pages for Compliance, Smuggling, Malformed Input categories
- Landing page with platform framing and contributor onboarding
- "Add a Framework" documentation page

### Fixed
- Docker image tags lowercased as required
- Git worktree/orphan branch creation for latest-results
- GlyphServer: replaced manual buffer with PipeReader, fixed closing without response on oversized requests
- Pingora build: added cmake and g++ to build stage
