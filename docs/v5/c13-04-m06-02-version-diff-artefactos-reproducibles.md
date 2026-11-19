# C13-04.M06.02 — manifiesto reproducible de evidencia visual (V22)

**Origen:** SOLQARYN / `solqaryn/Solqaryn@dev` / `PROJECT_SCOPE_LOCK=STRICT`. **Autor de esta microtarea:** `6adc2e55f47c81919f6374e561687076` (DEV05). **Riesgo del cambio:** R0, registro documental sin runtime. El objetivo preciso de `PLAN_MAESTRO!D2013` es **registrar versión, diff, artefactos y resultado reproducible**. La descripción de `E2013` es meta acumulativa M06: este manifiesto **no** certifica por sí mismo la matriz visual completa ni su cierre.

## A0 — stack, alcance y trazabilidad de versiones

- Frontend del HEAD base: Angular 22.2.1 standalone/Material, Node 24 y npm 11.19.0 (revalidar `frontend/package.json` en el SHA que se quiera reproducir). Enrutamiento, accesibilidad y guards existentes: **sin cambios de código por este manifiesto**.
- Backend: ASP.NET Core `net10.0`; EF Core 10.0.12 y proveedor Oracle `MySql.EntityFrameworkCore` 10.0.9 según referencias canónicas. MySQL 8.4.13 citado abajo es un **servicio efímero de GitHub-hosted**, no Aiven/producción.
- Archivos de esta unidad: este manifiesto y el bloque autocontenido en `CHANGELOG_AI.md`. Ningún DTO, esquema, API, build, JWT/RBAC, rol, migración, workflow, dispositivo ni contrato cliente se modifica. Dependencias: evidencia C13-04.M05.01–06, revisiones A5 del lote y gobernanza V17/V22. Rollback futuro: `git revert <SHA-publicado>` normal en `dev`, sin force-push ni borrar BITACORA.

## Versiones fuente, diffs, artefactos y resultados delimitados

Cada SHA siguiente identifica el commit fuente **histórico** de su microtarea. Para inspeccionar el diff usar `git show --stat --oneline <SHA>` y `git diff <SHA>^ <SHA> -- <rutas-del-scope>`; no sustituir el SHA por HEAD nuevo sin comprobar hashes de blobs, workflow y dependencias. Verificar checks en `GET /repos/solqaryn/Solqaryn/commits/<SHA>/check-runs?per_page=100` y abrir los jobs correspondientes para confirmar realmente el número de pruebas.

| Unidad | Commit fuente (GitHub) | Artefacto/probanza dirigida | Resultado exacto y límite |
| --- | --- | --- | --- |
| `M05.01` | [`cc82b73c`](https://github.com/solqaryn/Solqaryn/commit/cc82b73c1391d28c8bad0455c19a404c708c2f98) | Vitest de rutas/guardas y workflow `C13-04.M05.01 - Vitest reglas y Angular Release` | `completed/success`; 5/5 dirigidas según evidencia institucional. No HTTP/JWT/MySQL global |
| `M05.02` | [`f762c6a4`](https://github.com/solqaryn/Solqaryn/commit/f762c6a4f73a31790996f7b4d7487d1fde6dbd3d) | [Contrato Angular HTTP/DTO](c13-04-m05-02-contrato-api-dto-seguro.md), TestBed 8/8; job `C13-04.M05.02 - TestBed HTTP/DTO fail-closed` | `completed/success`; respuestas **mock** en pruebas; no backend autenticado real |
| `M05.03` | [`22c007d8`](https://github.com/solqaryn/Solqaryn/commit/22c007d866a7fd84f3e1e941d8eedd5c6ded8b06) | [MySQL sesión global aislada](c13-04-m05-03-mysql-sesion-global-aislado.md), Category=Integration 1/1, checks `C13-04.M05.03 - Oracle EF MySQL 8.4 sesion global A/B` | `completed/success`; MySQL 8.4.13 efímero; el test emplea identidad sintética, **no** JWT HTTP firmado |
| `M05.04` | [`d37e117b`](https://github.com/solqaryn/Solqaryn/commit/d37e117bab2cc500d9e65d594887dbb1a7f418aa) | Chromium 13/13 con API .NET 10, JWT firmado y MySQL efímero, check `Angular y API autenticas, JWT firmado, MySQL 8.4 y Chromium` | `completed/success` para recorridos concretos; no todos los roles, pantallas y terminales |
| `M05.05` | [`bf97c9a2`](https://github.com/solqaryn/Solqaryn/commit/bf97c9a252fa4d4b495786b28160434c39d43dc4) | Chromium 8/8 negativos JWT/tenant, check `C13-04.M05.05 - denegaciones JWT reales y Chromium` | Dirigido `success`, **pero** check global `Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo` en **`completed/failure`**; no PASS global |
| `M05.06` | [`acbc5e6d`](https://github.com/solqaryn/Solqaryn/commit/acbc5e6dbf1d85cc21c88f18f2b264ce6f382c5d) | Chromium 5/5 en superficies públicas, tamaños 320/375/768, `C13-04.M05.06 - five public surfaces real Chromium` | Dirigido `success`; check global de módulos/transferencias **`completed/failure`**, tres casos de inventario. No hardware físico ni Web Vitals de campo |

**Fronteras de reproducibilidad.** Los checks `SKIPPED` de migraciones opcionales o retry auxiliar no se cuentan como PASS obligatorio. Los `SUCCESS` dirigidos son evidencia del **scope específico**, no aceptación global sin negativa, cobertura y dependencia causal. Referencia de límites y riesgos [M06.03](c13-04-m06-03-limitaciones-riesgos-decisiones.md) — mantener `R09` (3 FAIL globales de transferencias) y `R02` (Chromium simulado ≠ dispositivo físico) intactos.

## Procedimiento reproducible de auditoría (solo lectura, sin secretos)

1. Posicionar el checkout únicamente sobre el SHA fuente a inspeccionar. En Github Actions abrir el check dirigido por nombre, comprobar `head_sha`, `status=completed`, `conclusion=success`, logs con ejecutados/aprobados/fallidos y versiones del runner/servicios.
2. Consultar `git show --name-status <SHA>` y `git diff <SHA>^ <SHA>`, contrastar paths, hashes y configuración de test del commit de origen. Si HEAD posterior difiere, demostrar identidad exacta de blobs y dependencias o ejecutar el job dirigido de nuevo; no reciclar aprobaciones.
3. Reproducir con herramientas soportadas de GitHub-hosted y workflows versionados del commit, no con fake endpoints. Para `M05.03` exigir `Category=Integration`, MySQL 8.4 efímero y TRX `executed >= 1, failed = 0`. Para `M05.04/.05/.06`, inspeccionar los logs reales Chromium/JWT/MySQL pertinentes. Tests de interfaz con mocks no acreditan HTTP/JWT backend.
4. Verificar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en el SHA de **esta** entrega antes de cambiar estado. Contrastarlos con PLAN H/I/J/K/L/M, COLA G/O, DAG F/G y BITACORA append/readback. Solo se admite A5 de una Task Javier distinta de autor y reparador.

## A1–A5 de esta salida atómica

- **A1:** cotejo positivo de seis commits y checks dirigidos existentes; negativo de las dos ejecuciones con check global `failure` y de `SKIPPED` opcional, que permanecen explicitadas como NO_PASS.
- **A2/A3:** `NO_APLICA` para nuevas ejecuciones HTTP, JWT/RBAC, MySQL o Chromium **en este changeset R0 documental**: no toca runtime ni lo exige `PLAN D2013`. Persisten las obligaciones específicas R2 en los componentes/casos del lote; no se simula cobertura.
- **A4:** exigir commit `dev` fast-forward con SHA exacto, check Scope Lock y V5 `completed/success` y readback PLAN/COLA/BITACORA; en espera de validación del SHA de publicación.
- **A5:** revisión independiente de otra Task Javier, con código/documento/diffs/comparación causal; pendiente hasta prueba real.

**No-acciones:** no modificar permisos empresariales, bypass tenant, validadores CI, fórmulas DAG/COLA, infra QA/main/PROD, secretos, pagos ni BITACORA histórica. Este manifiesto no declara la fase M06 completa, no libera M06.06 ni enmascara FAIL transversal.
