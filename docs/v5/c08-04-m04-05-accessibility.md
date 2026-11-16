# C08-04.M04.05 — Responsive, keyboard, focus and contrast

- Risk: **R1** — targeted Angular CSS and Playwright browser checks, no API, persistence, roles or tenancy change.
- Scope: protected, read-only `/extensiones-sectoriales` preview; `sector-modules-readonly.component.scss`, new Playwright spec and SHA-isolated GitHub-hosted workflow.
- Issue: contrast preset's `.sector-card` inherits nearly white foreground while `.sector-tag` retained light background; dark preset tag contrast fails WCAG 2.2 AA normal text threshold of 4.5:1.
- Minimal fix: dark preset tag receives opaque `#334155` background and `#f8fafc` foreground; institutional preset remains unchanged.
- A1 checks: anonymous and synthetic tenant 403 denial, keyboard Tab from initial focus and visible outline (no programmatic focus), responsive overflow at 320/375/768/1280, and calculated RGB foreground/background contrast in BOTH visual presets. Assertions fail if the contrast fix is removed.
- A2: UI guards only, synthetic 403 and redacted response; not a live JWT/RBAC/tenant A-B certificate. No HTTP mutation or permission grant implemented.
- A3: real Chromium with local Angular server through GitHub Actions, plus existing Angular unit regressions and build. No MySQL, EF, backend or external provider changed; those integrations are **NO_APLICA** for this scope.
- A4: only exact-SHA completed SUCCESS checks and institutional PLAN/COLA/DAG/BITACORA readback justify LISTO; QUEUED, IN_PROGRESS, CANCELLED, SKIPPED are not PASS of required checks.
- A5: separate Javier task must inspect code, adversarial tests and exact SHA; never self-approve.
- Rollback: normal fast-forward `git revert` of this scope commit on dev; no force push. No QA/main/PROD or secrets.
- SKREF-10: angular/angular pin `6808966993bf2b2d455d9b6245e960a59a01b9a9`, Angular 22 maintain existing project and verify build.
