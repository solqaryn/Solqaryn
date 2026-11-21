# SOLQARYN V22 - C04-01.M05.06 real Chromium regression, performance and accessibility

**Owner:** DEV06 Javier. **Repo:** solqaryn/Solqaryn@dev. **Scope Lock:** STRICT. **Risk:** R2, test-only. PLAN_MAESTRO D607 defines "Ejecutar regresión, rendimiento y accesibilidad proporcionales". PLAN E607 is a cumulative M05 outcome, not a requirement that this test-only unit certify server-side billing, quotas, plan prices, payments or the entire phase.

## A0 impact and architecture

Add a focused Playwright regression, GitHub-hosted exact-SHA workflow and this documentation. Existing Angular 22.2.1 private plan reference, platform guard, API backend, schemas, JWT and commercial services remain untouched. No production runtime, migration or provider is activated. Safe rollback is a normal additive git revert; no force push, QA/main/PROD, secrets or payments.

## A1 functional, negative, boundary and performance tests

Five browser cases: (1) authenticated fixture sees exactly three reference sections, NO_CERTIFICADO, no forms/pay buttons or HTTP writes; (2) widths 320/375/768/1280 px, headings, aria-labelledby and no overflow; (3) real Tab and Enter, >=44px return link, >=3px visible focus, reduced motion without programmatic .focus(); (4) real Chromium navigation and Angular paint with <=15s CI bound, PerformanceNavigationTiming DOMContentLoaded, DOM <4000 elements; (5) anonymous, denied 403 and unavailable 503 hide private content and do not echo internal errors. These are real browser measurements and adversarial assertions, not fabricated Lighthouse or field Core Web Vitals. No claim of exhaustive WCAG color-contrast testing.

## A2 security

Platform session uses a **synthetic test fixture**, explicitly NOT a signed JWT or live backend. Real Angular route guard is exercised; negative states are fail-closed and no POST/PUT/PATCH/DELETE occurs. The real signed-JWT HTTP/MySQL tenant negatives live in the separate C04-01.M05.05 scope; this UI test does not claim their outcomes.

## A3 and A4 GitHub Actions

Ubuntu GitHub-hosted uses locked npm/Angular, TypeScript + production build, actual Chromium, Playwright 5/5 required (JUnit 0 failed/errors/skipped). Strict Scope Lock and applicable V5 provenance must be SUCCESS on exact published commit SHA. FAIL/SKIPPED/CANCELLED/IN_PROGRESS required checks do NOT pass. Publish via direct dev fast-forward/CAS with change provenance and append-only institutional BITACORA. Once code is complete, EN_PROGRESO -> VALIDANDO; if external CI is queued/in progress, keep VALIDANDO and hand off exact SHA/job. A0-A4 scope-only before LISTO. A5 by another Javier Task, not DEV06.

## LIMITACION_FASE

No live authenticated JWT/role contract, actual billing/upgrades, server-side pricing/quota enforcement, payment processor, MySQL browser integration, provider uptime SLA or production field CWV certified. The overarching M05 remains cumulative. CHANGELOG_AI.md is kept byte-identical because the read connector returned no full content for the large existing file; this avoids history truncation or interleaved headings. This document and append-only BITACORA record the change.
