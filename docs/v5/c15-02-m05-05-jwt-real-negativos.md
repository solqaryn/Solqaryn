# C15-02.M05.05 — casos negativos JWT/RBAC y frontera A/B reales

**PLAN D2226:** ejecutar prueba negativa de seguridad, aislamiento o abuso, salida atómica de esta microtarea; E2226 es meta M05 acumulativa y no habilita ni certifica integraciones reales de proveedores ajenos a la pantalla. Riesgo **R2**, solo dev, PROJECT_SCOPE_LOCK=STRICT, sin QA/main/PROD.

A0: se añaden E2E, workflow GitHub-hosted y evidencia. No se modifica runtime, API, contratos DTO de producción, Cloudflare/R2/MySQL externos ni secretos reales.

A1: cinco pruebas de aceptación adversarial Playwright: control positivo de rol plataforma, 403 de empresa A y B por separado, JWT con firma falsificada rechazado y navegación bloqueada, anónimo 401. Los asserts fallan ante fuga de cinco referencias; el control positivo evita falsos PASS de rutas rotas.

A2: frontera real navegador Angular -> API ASP.NET Core .NET 10 -> MySQL 8.4 descartable, JWT firmado efímero, membresías distintas A/B y grant exclusivo de plataforma. Impide elevar permisos empresariales mediante JWT propio, token manipulado o falta de token. No simula HTTP con page.route ni secretos persistentes. No acredita BOLA de archivos/jobs, porque este D2226 trata la pantalla privada de arquitectura.

A3: servicio mysql:8.4, bootstrap real efímero y servidor ASP.NET + Angular en GitHub-hosted. Reutiliza plantilla **de CI** autorizada C15-04.M05.04, sin modificar sus archivos, fixtures de base y cuentas locales `c1504_admin_a`, `c1504_admin_b`, `c1504_platform_only` aisladas por job; firma generada por openssl y enmascarada. Runner distinto por job, sin producción.

A4: exige test job y sus 5 casos completed/success en SHA, Scope Lock V5 y checks pertinentes. Si CI falla/queda en progreso, conservar VALIDANDO y reparar causalmente con nuevo SHA, sin falsear PASS. A5 revisión por una Task Javier distinta del autor DEV03 y de cualquier reparador.

Rollback: `git revert` ordinario del commit en dev bajo CAS; no force-push. Separar caso de error de infraestructura de error del código.

Referencias oficiales leídas por pin: SKREF-10 `angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9` y SKREF-11 `openai/skills@49f948faa9258a0c61caceaf225e179651397431`. Se aplican fronteras de confianza y pruebas adversariales concretas; no se instala ninguna referencia.
