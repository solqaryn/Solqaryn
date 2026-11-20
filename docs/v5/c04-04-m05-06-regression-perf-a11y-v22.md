# C04-04.M05.06 — regresión de la vista desconectada de cobro (V22)

- PLAN D715: regresión, rendimiento y accesibilidad proporcionales del estado actual del alcance. PLAN E constituye meta de fase acumulativa, NO certifica pagos/PSP reales ni toda M05.
- R2 test-only. Nueva suite **4 casos Playwright sobre Angular 22 y Chromium real GitHub-hosted**: anónimo redirigido, 403 sin fuga de secreto en 320/1280, autorización sintética de preview solo lectura con zoom 200% y 4 tamaños, Tab real con foco visible y objetivo mínimo 44px/reduced motion. Asserts de cero POST/PUT/PATCH/DELETE, cero forms, mínimo de headers, ausencia de overflow y DOM bajo 160 nodos. Loguea tiempos de carga locales solo como métricas diagnósticas, NO_MEDIDO Web Vitals reales ni dispositivos físicos.
- Seguridad: /plataforma/sesion interceptada con fixture sintético; NO JWT firmado, SQL de membresía, webhook, provider, pago, refund, ni tenant real. El backend auténtico y el pago no se modifican, la UI no concede permisos.
- A0/A4: Scope Lock + Skill de HEAD, npm ci, lint, build de Angular, Chromium y `4 passed` sin fallos/skips con el SHA exacto en GitHub Actions. No certificar PASS hasta la conclusión; mantener VALIDANDO para CI. A5 exige otra Task Javier independiente de DEV03.
- Rollback: `git revert` aditivo de la suite, el workflow y este documento. No modificar datos, secrets, QA/main/PROD.
