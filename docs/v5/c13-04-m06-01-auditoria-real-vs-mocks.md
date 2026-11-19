# C13-04.M06.01 — Auditoría de evidencia real frente a mocks del lote visual (V22)

PLATFORM=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SCOPE_LOCK=STRICT
MICROTAREA_ID=C13-04.M06.01
AUTHOR_TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09
RISK=R0_DOCUMENTAL_SIN_RUNTIME
A5=REVISION_INDEPENDIENTE_PENDIENTE

## Alcance atómico, autoridad y consumidores

La acción exacta de PLAN_MAESTRO!D2012 es **revisar que las pruebas correspondan al alcance real y no a mocks insuficientes**. PLAN E2012 (“Completa M06”, una matriz global de pantallas/responsive) es el objetivo acumulativo del lote, no un gate de certificación global imputable a este documento R0. Se revisaron las seis entregas existentes C13-04.M05.01–06, sus SHA y los tipos de prueba, sin reejecutarlas ni cambiar el runtime. La tabla procede de los cambios de GitHub y de PLAN_MAESTRO/BITACORA; una referencia histórica no confiere permisos presentes ni convierte fallos en PASS.

El changeset únicamente agrega este inventario y una sección autocontenida a CHANGELOG_AI.md: no modifica frontend, backend, rutas, API, dominio, base de datos, auth, políticas, CI ni infraestructura. Consumidores: M06.02 (reproducibilidad), M06.03 (riesgos), M06.04 (rollback), M06.05 (revisor independiente) y M06.06 (cierre), que mantienen gates propios calculados por DAG/COLA.

## Evidencia causal acotada y límites

| ID | SHA de fuente inspeccionada | Prueba real o doble | Límite de certificación |
| --- | --- | --- | --- |
| M05.01 | `cc82b73c1391d28c8bad0455c19a404c708c2f98` | Vitest 5/5 sobre invariantes del inventario Angular: rutas, guards heredados, lazy y negativos | No HTTP firmado/servidor ni medición física WCAG |
| M05.02 | `f762c6a4f73a31790996f7b4d7487d1fde6dbd3d` | Angular TestBed 8/8; `HttpTestingController` **simula backend**, con negativos 401/403/500/503 | No valida emisión de JWT, autorización API/SQL, revocación real |
| M05.03 | `22c007d866a7fd84f3e1e941d8eedd5c6ded8b06` | xUnit Category=Integration, MySQL 8.4.13 **real efímero** 1/1 y regresión MySQL 52/52 | Sólo fixtures de sesión/tenant A/B, no JWT HTTP firmado, Aiven ni datos reales |
| M05.04 | `d37e117bab2cc500d9e65d594887dbb1a7f418aa` | Playwright Chromium, ASP.NET API y JWT firmado/MySQL 8.4 en el runner: 13/13, rechazos de mutación ERP | Prueba negativa dirigida; no toda matriz de rutas ni todas las superficies operativas |
| M05.05 | `bf97c9a252fa4d4b495786b28160434c39d43dc4` | Chromium/JWT/MySQL 8.4 reales, 8/8 negativos BOLA, revocación y ámbito A/B | No pentest global de archivos, jobs, identidad, CORS y caché |
| M05.06 | `acbc5e6dbf1d85cc21c88f18f2b264ce6f382c5d` | Chromium real con sitio Angular local, cinco rutas públicas, navegación/overflow/heading/reduced-motion, A5 posterior independiente | No campo Web Vitals, dispositivos físicos, accesibilidad integral, JWT/DB de producción |

Los SHA y sus archivos modificados se contrastaron con GitHub. Resultados específicos arriba provienen de la evidencia PLAN/BITACORA y de los workflows dirigidos registrados allí; **no son una ejecución nueva de estas seis suites**. El estado de checks completos debe verificarse por SHA causal cuando cada fuente se utilice como gate.

## Hallazgos adversariales / matriz de suficiencia

- **PASS acotado**: existen casos positivos/negativos reales de navegación, persistencia sintética MySQL y rutas de seguridad con JWT firmado. Se prohíbe equiparar un TestBed con servidor HTTP o equiparar MySQL descartable con Aiven.
- **LIMITACION P1 de fase**: la cobertura de las rutas ERP/tenant y permisos granulares no es exhaustiva; los tests M05.04/M05.05 no demuestran por sí solos ausencia de IDOR/BOLA en todas las rutas y archivos.
- **LIMITACION P2 de experiencia**: sin mediciones de LCP/CLS/INP de usuarios reales ni dispositivos físicos; smoke de Chromium no equivale a auditoría WCAG integral ni todos los tamaños de pantalla.
- **FAIL histórico preservado**: el SHA de M05.06 de origen tuvo el check amplio «Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo» en `completed/failure`. Su suite dirigida verde y la reparación posterior no autorizan cambiar ese hecho histórico a PASS; la causalidad debe demostrarse por trabajo posterior y por alcance.
- **Condición de error**: cualquier job requerido `failed`, `cancelled`, `skipped`, `queued` o `in_progress` sobre el SHA de aceptación no acredita PASS. Los tests/unitarios del diff documental NO pueden sustituir integración de otra entrega.
- **Frontera de responsabilidad**: no afirmar global M06, restauración, pruebas en producción, permisos R3 ni liberar M06.06 por completar esta auditoría; DAG y COLA sólo cambian mediante fórmulas después de eventos genuinos.

## A0–A5, reversibilidad y siguiente paso

- **A0**: revisar requisito D2012, seis SHA fuente, clasificación real/doble y consumidores; diff sólo este archivo y CHANGELOG aditivo.
- **A1**: esta auditoría de evidencia prueba falsabilidad de cada aseveración con código/CI fuente; no sustituye ejecución de test. Negativos documentados: JWT falso, host/tenant cruzado, BOLA, fallo de módulo, error API y límites de browser.
- **A2**: NO_APLICA ejecutar JWT/RBAC/tenant nuevo en este diff R0; los tests genuinos de M05.04/05 se identifican pero no se elevan a certificación global.
- **A3**: NO_APLICA nuevas integraciones HTTP/MySQL/Playwright porque no cambia runtime ni archivos de test; M05.03/04/05 se reconocen sólo como evidencia histórica causal de sus propios scopes.
- **A4**: commit fast-forward/CAS en `dev`, V17 lock para ID de PLAN y readback de H/I/J/K/L/M, BITACORA/COLA/DAG. En SHA exacto del changeset comprobar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`; ninguna prueba requerida omitida.
- **A5**: otra de las cinco Tasks Javier, distinta de DEV04, inspecciona este documento, los seis SHA y la historia de fallo y puede registrar PASS acotado o FAIL causal; nunca autocertificación.
- **Rollback de este cambioset**: `git revert` normal del commit documental, con HEAD actualizado y sin force-push, sin borrar BITACORA. No se deshabilitan Tasks ni se cambia producción, políticas o datos.

M06.02 debe fijar versiones/blobs y jobs reproducibles; M06.03 documenta riesgos pendientes; M06.04 ensaya reversión sin datos reales; M06.05 solicita A5; M06.06 sólo es candidato si el DAG acredita todos los gates. Ninguna de estas acciones se simula en M06.01.
