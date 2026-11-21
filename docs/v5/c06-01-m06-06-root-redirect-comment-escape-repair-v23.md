# V23 C06-01.M06.06: real Angular root redirect repair

## Root cause observed on the original exact SHA
- Source SHA: eb02b1ba76b62a19e15b74a245ab5c1bbd04bcb3; GitHub Actions run 44961917277 job 137022389640.
- Browser test at frontend/e2e/c06-01-m06-06-public-entry.spec.ts line 18 expected /conoce-solqaryn after navigating to /, but received / for 15 seconds. The private /dashboard->/login test succeeded; two root-dependent tests failed.
- In frontend/src/app/app.routes.ts, `// V23 ... available.\n // Private ... here.\n { path: '', redirectTo: ... }` contained literal backslash+n sequences; because it was one actual TypeScript line, the // comment swallowed the route. Angular compiler accepted the file while route was not registered.

## Narrow corrective change
- Convert the two literal escaped `\n` markers into real newline characters; restore root redirect registration without changing lazy landing, login route or private route guards.
- No API, JWT, RBAC, database, domain, billing, checkout or production behavior is added. No package install or external skill execution.
- V23 authorized references consulted at original pinned sources: SKREF-03 Impeccable 2149fcce39a90bb409df5f16515f316a76dc6199; SKREF-10 Angular Developer 6808966993bf2b2d455d9b6245e960a59a01b9a9; SKREF-05 Taste ccbc15639c97057cbfcf32ecebc38ef716e4bb37. SKREF-04 motion, SKREF-06 copy, SKREF-11 threat-model, SKREF-12 .NET perf: NO_APLICA.

## Acceptance and limitations
- Re-run exact-HEAD V23 C06-01.M06.06 GitHub-hosted Playwright Chromium: public root, private anonymous dashboard, viewport widths 320/390/768/1280/1440, no body overflow, real Tab and reduced-motion; inspect uploaded screenshots. Test results not pre-certified by this source change.
- Check Angular 22.2.1 typecheck/build, V5 changelog provenance, Scope Lock, and applicable security/authorization CI completed SUCCESS on actual tested SHA; skipped/queued/running not PASS.
- PLAN D901 is a specific technical action in M06; PLAN E901 is the cumulative landing release goal. Pricing, onboarding, domains, host, metadata and any Phase-wide P0/P1 remain bounded separately and must not be silently certified.
- Only V17 exclusive writer may touch PLAN; maintain historical owner DEV03. Follow-up independent A5 must be by a distinct Javier Task other than DEV03 author and DEV04 repairer.
- Safe rollback by regular git revert after rechecking HEAD and concurrent writers. Never change QA/main/PROD or live payments.

## Follow-up: causal C15-01 source-audit CI repair
- Original route repair SHA 31a24c91282ae9a975412dd82fbc0c9941654ab2 triggered GitHub Actions C15-01.M05.04 job 137025198961, which failed with `route hash drift frontend/src/app/app.routes.ts`.
- The audit is correct to fail: after restoring the root redirect, actual Angular declarations grew 120->121. The canonical non-authoritative route snapshot held the prior blob.
- Update only the recorded app.routes.ts blob hash to `94c093bec9ab6765b4a7696a4e99aaaf03f10673` and lexical declarations to 121; preserve 2340/2340/2301 PLAN/COLA/DAG historical snapshot rows and other three route hashes/counts.
- Re-run source-audit and V5/ScopeLock on new exact-SHA. No weakening of source audit and no fake pass. Source Angular and Playwright blobs unchanged; see SHA `31a24c91` for directed Chromium completion independently.
- UI work conditional references: SKREF-03 Impeccable, SKREF-05 Taste, SKREF-10 Angular original pinned sources were consulted on the previous fix; this snapshot repair changes no UI, so additional skills NO_APLICA.
