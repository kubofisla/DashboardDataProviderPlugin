# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2025-12-03

### Added

- Initial **Dashboard Data Provider** SimHub plugin exposing telemetry over HTTP at `http://localhost:8080/dashboarddata/`.
- JSON API endpoints:
  - `GET /dashboarddata/` – returns current telemetry and target lap time.
  - `POST /dashboarddata/settarget` – sets target lap time directly.
  - `POST /dashboarddata/adjust` – adjusts target lap time by a delta.
  - `POST /dashboarddata/resettofast` – resets target lap time to fastest lap.
  - `POST /dashboarddata/resettolast` – resets target lap time to last lap.
- Persistence of target lap time between sessions.

### Tooling & Documentation

- `SETUP_GUIDE.md` – developer-focused manual for building from source, deploying to SimHub, and testing.
- `deploy-simhub-plugin.ps1` – helper script to deploy the built DLL to the SimHub installation directory and restart SimHub.
- `test_endpoints.ps1` – script to exercise all HTTP endpoints and print responses.
- `monitor-dashboard.ps1` – script to continuously poll `GET /dashboarddata/` and display live JSON output.
- Updated `README.md` with installation instructions (GitHub releases or build from scratch) and a pointer to the Loupedeck integration plugin at `https://github.com/kubofisla/SimHubIntegrationPlugin`.
