# C02-04.M06.03 — limitaciones, riesgos y decisiones verificables (V24)

**Alcance:** SOLQARYN, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. Microtarea institucional `C02-04.M06.03`, fila 430 de `PLAN_MAESTRO`. La acción de la columna D es «Documentar limitaciones, riesgos abiertos y decisiones tomadas». Esta entrega R0 es documentación de evidencia existente: no altera Angular, ASP.NET Core, JWT, RBAC, migraciones, MySQL, CI, secretos, despliegues ni autorizaciones.

**Corte de evidencia:** 2026-11-23 01:44 UTC. El SHA de referencia para la reparación de laboratorio es `d64b19a40983ea3e59d21c5de00652a8635e5daf`; el HEAD observado al reclamar esta microtarea es `b15c9b7faffc3a8354dd556ad5a6f2d52a11cf77`. Las evidencias se verifican en su SHA causal, no se atribuyen por proximidad temporal.

## 1. Hechos acreditados y alcance de cada prueba

| Evidencia | Resultado observado | Qué prueba | Qué **no** prueba |
| --- | --- | --- | --- |
| [C02-04.M05.06 · Chromium](https://github.com/solqaryn/Solqaryn/actions/runs/45091176575/job/137420827336) | GitHub-hosted, 8/8 PASS, 0 FAIL, 0 SKIP | Navegación Angular de 15 rutas de catálogo, focus/Tab/Enter real, contraste computado del contenedor >=4.5:1, anchos responsive, estados locales de carga/error/reintento, anónimo y 403 en una sesión interceptada | JWT firmado entregado al navegador, denegación HTTP real del backend, MFA, rendimiento de campo o accesibilidad WCAG integral |
| [C02-04.M05.06 · laboratorio JWT/MySQL](https://github.com/solqaryn/Solqaryn/actions/runs/45091176575/job/137420827601) | .NET 10, MySQL 8.4.13 efímero, TRX 9/9 PASS, 0 FAIL, 0 OMITIDOS, artefacto `14780789691` | TestServer HTTP con JWT HMAC firmado de CI, grant relacional persistido, revocación y cuatro rechazos 403 de dos actores de empresas A/B ante selección propia/cruzada | Browser conectado a esa API real, MFA, proveedor GitHub externo en vivo o acceso productivo |
| [SHA exacto](https://github.com/solqaryn/Solqaryn/commit/d64b19a40983ea3e59d21c5de00652a8635e5daf) | Scope Lock, V5, backend Release, seguridad, autorización, frontend y ambos trabajos dirigidos concluyeron SUCCESS | Código/runner de los casos señalados y configuración CI del SHA | Que un job `skipped`, `cancelled` o `in_progress` haya pasado; tres jobs opcionales de retry `SKIPPED` **no** se cuentan |
| `PLAN_MAESTRO!K427` y `BITACORA` | A5 correctiva independiente del alcance M05.06, revisor DEV05 distinto de autor DEV04 y reparador DEV06, con `A5_FAIL` histórico preservado | Resolución limitada de la brecha anterior: faltaba un laboratorio QA ejecutable y evidencia TRX real | Aprobación de la fase M05 completa, M06 completa ni publicación a QA/main/PROD |

El artefacto GitHub `c02-04-m05-06-real-qa-mysql84-jwt-trx` contiene los resultados TRX del laboratorio aislado. Las pruebas Angular usan `frontend/e2e/c02-04-m05-06-regression.spec.ts`; el laboratorio usa `backend/tests/Solqaryn.Tests/API/Controllers/C0204M0502QaHttpContractTests.cs` y `.github/workflows/v19-c02-04-m05-06-regression.yml`. Las rutas del catálogo se leen de `docs/v5/c02-04-m05-01-routes.json`.

## 2. Brechas abiertas y decisión por alcance

1. **Integración navegador → API con JWT real (R2): NO_MEDIDO.** Las ocho pruebas Chromium usan una sesión HTTP sintética declarada; las nueve pruebas backend sí ejercen JWT firmado y MySQL real aislado, pero en otro proceso/test. No fusionar ambos conjuntos como si acreditaran una sesión E2E integrada. Requiere nuevo escenario Playwright autenticado por backend efímero y negativos A/B antes de afirmar ese resultado; no es una regresión de código introducida por esta microtarea documental.
2. **MFA, reautenticación, motivo y acceso excepcional temporal (R2): NO_CERTIFICADO.** Son el resultado acumulativo de fase indicado en `PLAN_MAESTRO!E`, no prueba positiva de cada unidad M05 ni de `M06.03`. Requieren sus microtareas, contratos, seguridad y evidencias correspondientes. No declarar un MFA real a partir de un token de CI.
3. **IDOR/BOLA transversal y revocación sobre todos los módulos: NO_MEDIDO.** Los negativos del laboratorio verifican el controlador de evidencias QA, empresas A/B y la revocación probada; no autorizan extensiones a archivos, empleos, caché, backup o toda la plataforma.
4. **Rendimiento productivo y Core Web Vitals: NO_MEDIDO.** La espera artificial de 225 ms y la comprobación de ausencia de overflow son pruebas funcionales bajo condiciones controladas, no latencia real de red, tráfico, p95 o CWV de usuarios.
5. **Accesibilidad integral: NO_CERTIFICADA.** Existen pruebas de foco natural, contraste puntual y responsive en Chromium. El resultado no demuestra todas las exigencias WCAG ni zoom nativo de navegador al 200%; tampoco certifica dispositivos físicos.
6. **Origen externo de CI: aislado.** El backend utiliza un handler de GitHub simulado durante el laboratorio. Comprobar contratos reales de origen/datos, autenticación y errores de proveedor requiere otro alcance explícito. Nunca conectar pruebas al entorno productivo ni reutilizar credenciales reales.
7. **Historial de calidad:** los `A5_FAIL` anteriores de `C02-04.M05.06` y otros IDs no se borran. La nueva A5 se asocia únicamente al diff y SHA correctivo y registra el cambio de evidencia, no una reescritura del pasado.

## 3. Decisiones operativas

- Preservar `PROJECT_SCOPE_LOCK=STRICT`, rama `dev`, seis Tasks Javier, V17 namedRange/CAS, autoría histórica y `BITACORA` de 26 columnas. Cada nuevo `CLAIM` requiere PLAN H=PENDIENTE/I vacío, `COLA` G/O=SI, `DAG_TECNICO` F=VALIDADO/G=SI y exclusividad por ID.
- Para `M06.03` aplicar **R0 documental**: A0 inspeccionar fuentes, riesgos y diff de este archivo; A1 comprobar que cada positivo tenga SHA/run y que cada limitación negativa esté expresamente documentada; A2 `NO_APLICA` a cambios JWT/RBAC/tenant porque esta entrega no modifica runtime; A3 `NO_APLICA` a ejecución nueva MySQL/HTTP/Chromium por la misma razón; A4 exigir GitHub Scope Lock/V5 pertinentes en el nuevo SHA, PLAN/COLA/BITACORA readback y ausencia de sobrescritura. Una A5 independiente por otra Task Javier sigue pendiente tras LISTO.
- La capacidad de UI existente se describe sólo como evidencia de otra microtarea, no se modifica aquí. `SKREF-03` Impeccable y `SKREF-10` Angular son **NO_APLICA** para el diff documental actual, igual que motion `SKREF-04` y rendimiento .NET `SKREF-12` sin mediciones. Se consultó `SKREF-06` Humanizer, origen `blader/humanizer`, pin `9862685f575c65a8247f90369951df1b3416e3d6`, ruta `SKILL.md`, para redacción precisa; no se instalaron skills externas.
- Mantener las sucesoras calculadas por fórmulas, no escribir `SI` ni eludir `A5_FAIL`. `LISTO` en dev es diferente de autorización para QA, main, PROD o pagos.
- Ante nueva prueba fallida no maquillar `SKIPPED` como PASS: localizar SHA, job, archivos y cambio causal; registrar `QA_DEFECT_FOUND` o `A5_FAIL` con nueva hipótesis y reparación real. Dos revisiones sin evidencia nueva no justifican una tercera lectura repetida.

## 4. Reproducción y rollback

- Abrir el [commit de laboratorio](https://github.com/solqaryn/Solqaryn/commit/d64b19a40983ea3e59d21c5de00652a8635e5daf) y verificar los cuatro archivos modificados y el SHA del trabajo.
- Inspeccionar los dos jobs del [run 45091176575](https://github.com/solqaryn/Solqaryn/actions/runs/45091176575): ocho casos Chromium y nueve xUnit, TRX, 0 fallos/omitidos, MySQL 8.4 efímero; separar rigurosamente los resultados browser-sintéticos de los HTTP/SQL/JWT reales.
- Comprobar `PLAN_MAESTRO`, `COLA`, `DAG_TECNICO` y el EVENT_ID idempotente en `BITACORA`. Si una sucesora permanece en NO, leer sus requisitos sin editar fórmulas.
- El rollback de **este documento** consiste en `git revert` normal del commit que lo añade (tras revisar commits posteriores), conservando los eventos institucionales y la A5 correctiva registrada. No usar `reset`, force-push, QA, main, PROD ni migraciones reales.

**Estado propio:** documento para `C02-04.M06.03`. No declara `LISTO`, CI o A5 del nuevo SHA hasta comprobación posterior y readback institucional.
