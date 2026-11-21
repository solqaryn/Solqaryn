# C14-01.M06.01 — Auditoría V22 de pruebas reales frente a dobles

## Autoridad, criterio y alcance
PLAN_MAESTRO D2048 exige **revisar que las pruebas correspondan al alcance real y no a mocks insuficientes**. Este entregable R0 es un examen documental reproducible de M05.01–M05.06 del lote C14-01: no introduce nueva autorización, endpoint, cambio runtime, migración, dato productivo ni CI falsa. PLAN E («Completa M06») es un resultado acumulativo y **no** una certificación otorgada por esta auditoría. Se preserva `PROJECT_SCOPE_LOCK=STRICT`, rama `dev`, A0–A5 y A5 independiente.

## Matriz de evidencia causal (no convertir mock en integración)
| ID / PLAN D | Prueba revisada | Evidencia GitHub-hosted por SHA | Conclusión de alcance |
| --- | --- | --- | --- |
| M05.01 D2042: regla unitaria | Angular `empresa-identidad.c14-01-m04-03.spec.ts`: ids inválidos, tenant A/B, carrera y nombre hostil; proveedor **simulado** | `72b817271eb2119460fc27c141886c16a6749bf6` · job `136725882650` · 16/16 declarados; pasos CI sin fallo | Válida como unidad, **no** JWT/MySQL/HTTP real |
| M05.02 D2043: contrato DTO/errores | Angular `empresa-configuracion.c14-01-m05-02.spec.ts`: `HttpTestingController`, 401/403/404/503, DTO cruzado y carrera; transporte **falso** | `6dd70f002c4b08bcae0c71fca6d1255e27d597ec` · job `136726543828` · 6/6 declarados; CI completada | Prueba contrato consumidor, **no** prueba API de red/JWT |
| M05.03 D2044: integración | `frontend/e2e/c14-01-m04-03-real-branding.spec.ts`: HTTP localhost a ASP.NET Core 10, JWT firmado, Oracle EF/MySQL 8.4 efímero, Chromium; lectura A/B y reread A | `0c0daf0b470708c3c726dc9375f9a7fcdf46aea8` · job `136727934733` · 6/6 documentados; pasos de navegador SUCCESS | Integración **real de lectura**; no escritura comercial ni proveedor externo |
| M05.04 D2045: positivo | Navegador real público→perfil privado A→panel global neutral, persistencia A tras reload, navegador aislado | `211e7e2ab96a264bcfaafdb4b27809348bfb6ec7` · job `136729230818` · 8/8 documentados; pasos SUCCESS | Aceptación positiva acotada, no módulos sectoriales completos |
| M05.05 D2046: negativo | HTTP backend real: anónimo, JWT adulterado, A→B, header cruzado, enumeración y público 503; no `route.fulfill` | `e6b4bd4ae007e6f85df73933cbe42ab4e87baad0` · job `136728582305` · 7/7 documentados; pasos SUCCESS | Denegaciones de identidad privadas, **no** RBAC de cada archivo/job |
| M05.06 D2047: regresión/a11y | Chromium con API y MySQL efímeros, dos sesiones JWT, 320/375/768/1280, Tab/Enter natural, errores y recarga. Primer SHA falló 2/4; reparación cambió test | `fc611e676aa1faa93101419f951baf701262956d` · job `136735799511` · 4/4 documentados; pasos SUCCESS | Navegador real, pero viewport emulado; sin teléfonos físicos, LCP/INP/CLS de campo ni carga real |

Se contrastaron los seis documentos `docs/v5/c14-01-m05-0*-*.md`, las rutas reales de tests y las listas de archivos cambiados en los seis commits. Los jobs mencionados se consultaron mediante GitHub `fetch_workflow_job_steps`: todos los pasos requeridos listados estaban `completed/success` en esa lectura; los conteos de tests proceden de la evidencia causal por ID del Sheet y sus jobs, no de simularlos aquí. No convertir un paso SKIPPED opcional en PASS. Para cierre por nuevo SHA, corroborar de nuevo Scope Lock y procedencia V5 del SHA exacto.

## A0–A4 de esta microtarea R0
- **A0:** matriz de correspondencia D2042–D2047, diffs por commit, separando dobles de integración de red y pruebas físicas. Requisito D2048 atendido sin interpretar cada M05 como certificación de toda M05/M06.
- **A1:** negativos documentados: DTO tenant cruzado, 401/403/404/503, JWT adulterado, ID inválido, carrera y focus/Tab real. Confirmar que las pruebas que usan mocks no acreditan backend. Ninguna prueba nueva se inventa en este archivo.
- **A2/A3:** nuevos JWT/HTTP/MySQL/Chromium **NO_APLICA** a la edición documental; la matriz sí señala qué pruebas previas fueron reales y cuáles no. Los riesgos residuales sectoriales y de dispositivos no quedan certificados.
- **A4:** publicar este documento más una sección autocontenida en `CHANGELOG_AI.md` por fast-forward CAS; exigir `verify-solqaryn-project-scope` y prueba V5 de procedencia `completed/success` en el SHA publicado. Los demás checks solo si son causalmente exigibles al diff. Registrar PLAN/COLA/BITACORA con readback.
- **A5:** revisión genuina de una Task Javier distinta de DEV01 autora de este informe; no autoaprobar ni liberar QA/main/PROD.

## Limitaciones, acción y reversión
La cobertura M05 permite justificar las **pruebas concretas**; no equivale a certificar toda la superficie sectorial, privilegios de archivos/jobs, teléfonos físicos, CWV reales, P0 global o permisos productivos. Una aprobación M05.06 A5 independiente es diferente de este documento y debe tener su propio evento verificable. Si esta auditoría omite un escenario causal, agregar corrección trazable sin modificar historia. Rollback mediante `git revert` normal en `dev`, sin force-push ni alterar BITACORA histórica.
