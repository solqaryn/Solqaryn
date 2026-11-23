# C08-03.M03.01 — reglas de dominio de membresías de gimnasio (V24)

PROJECT_SCOPE_LOCK=STRICT · `solqaryn/Solqaryn@dev` · Task autora DEV05 `6adc2e55f47c81919f6374e561687076`.

## Salida atómica y límites de alcance

Implementación **real compilable** de `C0803GymMembershipDomainRules` en `backend/src/Domain/AutomationV5` y xUnit adversariales en `backend/tests/Solqaryn.Tests/Application`. Resuelve sólo `PLAN_MAESTRO!D1238`: reglas de dominio y validaciones server-side para un *candidato* de acceso de membresía. No pretende acreditar la meta **acumulativa** de `E1238` ni completar M03.02–06.

La nueva política evalúa snapshots **que un adaptador confiable futuro deberá obtener del backend**: tenant/actor/rol, sucursal, titular, plan y versión; estado Active, vigencia estricta UTC `[inicio, fin)`, suspensión, credencial revocada, conciliación ERP real y denegación de dispositivo offline o declaración de pago del cliente. Todos los resultados, incluido el exitoso, tienen `AuthorizesDoorOpening=false`, `AuthorizesJwtOrRbac=false`, `AuthorizesPayment=false` y `CertifiesHttpMysqlOrHardware=false`. El candidato sólo permite que una futura Application vuelva a verificar JWT, permisos relacionales, membresía, facturación y transacción/versión antes de cualquier operación; **no** se instala un endpoint, lector, QR, DDL, migración, pago, gate o permiso.

El módulo permanece deliberadamente desconectado del runtime de ingreso físico, porque el repositorio no dispone todavía de la integración M03.02–06, esquema de gimnasios, consentimiento comercial D01–D08 o hardware autorizado. Por tanto, un test xUnit **no** demuestra apertura de puertas, JWT firmado, HTTP real ni MySQL.

## Evidencia A0–A5 proporcional

- **A0:** diff Domain + xUnit + este documento, ramas `dev`/Scope Lock, sin cambios transversales de arquitectura, secretos, configuración o datos. Consumidor futuro: capa Application con adapter de snapshots server-side, sin grants implícitos.
- **A1:** positivos de precondiciones candidatas y fronteras UTC inclusiva/exclusiva; negativos por null, tenant A/B, sucursal/plan/persona ajena, estado no activo/desconocido, versión obsoleta, reloj ambiguo, suspensión, revocación, ERP no conciliado y dispositivo offline.
- **A2:** comprobación estática y dinámica de reglas puras de aislamiento A/B, JWT/RBAC declarados como precondiciones que *NO* autentican por sí solas; se requiere revalidación real de grants en M03.02+.
- **A3:** NO_APLICA a nuevo MySQL/EF/HTTP/Chromium/hardware porque el diff sólo agrega motor de reglas Domain aislado, tests de dominio y documentación; NO significa que estas integraciones estén certificadas.
- **A4:** verificar xUnit real .NET 10 GitHub-hosted del SHA publicado, `verify-solqaryn-project-scope`, backend, seguridad pertinentes y readback PLAN/COLA/DAG/BITACORA. Un check required SKIPPED/QUEUED/IN_PROGRESS/FAIL no es PASS.
- **A5:** revisión independiente genuina por otra Task Javier, distinta de DEV05, sobre código y casos negativos, posterior a A0–A4 y sin promoción de entornos.

**Rollback:** `git revert` ordinario sobre el commit exacto de `dev`, sin fuerza ni cambios de datos; preservar BITACORA. El dominio debe permanecer de modo fail-closed mientras no exista adaptador de infraestructura y HTTP autenticado que pruebe tenant/JWT/EF/MySQL 8.4 con negativos reales.
