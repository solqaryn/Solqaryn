# C00-01.M01.03 - Inventario verificable de contratos, codigo y evidencia

Proyecto: `solqaryn/Solqaryn`; rama `dev`; `PROJECT_SCOPE_LOCK=STRICT`.
Responsable: Javier :30, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`.
Corte de lectura: `2026-11-13T06:29:37Z`; HEAD observado `a9ccc7959ed591e3b5b1e4c961d3f11b4de6e126`.
Alcance: inventario documental V5, no certificacion de funcionalidades, datos o proveedores.
La autoria registrada y el commit Git son trazabilidad; NO constituyen firma criptografica del propietario.

## Fuentes y huellas comprobadas

Los 40 caracteres hexadecimales siguientes son IDs de blob Git para archivos leidos en `dev`.
No son hashes SHA-256 del Google Doc, del Google Sheet ni atestaciones independientes.
La identidad de Drive y la fecha de modificacion son referencias verificables, no pruebas de contenido desplegado.

| Clase | Fuente exacta | Huella o referencia | Alcance demostrado |
| --- | --- | --- | --- |
| Gobierno | `.agents/skills/solqaryn-project-governance/SKILL.md` | `304e400d5c0f89fcfdcc65caece86c34276734b5` | Skill canónica consultada |
| Arquitectura | `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Limites declarados |
| Alcance | `docs/PROJECT_SCOPE_LOCK.md` | `5bfc90f2f1460efcb7d7b42bd11cf6f8ca37c4d8` | `STRICT` y autorizacion dev |
| Agentes | `AGENTS.md` | `0645966cfdf03abad550918b41e1ef4f061fc3b8` | Operacion del repositorio |
| Estado | `docs/AUTOMATION_STATUS.md` | `30beddbd544d555e0c3800a093fef8aa338a7017` | Censo y fuentes vigentes |
| Autoridad | `docs/AUTOMATION_AUTHORITY.md` | `61b5d184edee27e69d296764e5029f6cff755a04` | Identidad y limites |
| Protocolo | `docs/AUTOMATION_V13.md` | `d2a5cadda943299a6b3a80a2d126fb6f6a819857` | A0-A5, directo dev |
| UI | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Angular `22.2.1` declarado |
| API | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | `net10.0` declarado |
| Persistencia | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF Core `10.0.12`, MySQL Oracle `10.0.9` declarados |
| Plan ejecutable | Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` | V5 FINAL, modificado `2026-11-13T05:37:26.179Z` | 2.340 IDs, fuente de requisitos; SIN hash de contenido |
| Ledger | Google Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` | 2.340 IDs `PLAN_MAESTRO`; `COLA`; `BITACORA` | PLAN!H estado; COLA derivada por formulas |

## Matriz de contradicciones, evidencia y decision

| Riesgo de inferencia | Evidencia leida | Decision fail-closed |
| --- | --- | --- |
| V5 objetivo Hostinger/Cloudflare versus infraestructura actual | Arquitectura declara Vercel/Render/Aiven/Cloudinary; V5 describe topologia futura | NO declarar migracion, despliegue ni DNS realizado |
| Pausa historica versus orden posterior | Scope Lock registra suspension 2026-11-12 20:56 y posterior reanudacion V5 condicionada; Task nativa requiere readback | No inferir habilitacion de un texto archivado; verificar estado de cada Task |
| V3/V13 comercial versus V5 FINAL | Plan V5 y nuevo Sheet 2.340 IDs; V13 conserva A0-A5 | NO reclamar IDs del backlog historico ni abrir PR/ramas |
| Formulas versus estado mutable | `PLAN_MAESTRO!H` gobierna; `COLA!E/G/O` se derivan | NO editar COLA manualmente ni certificar por un `SI` aislado |
| CI verde versus aceptacion | Scope Lock del HEAD observado SUCCESS; otros jobs pueden continuar | No equiparar check con A0-A4, A5_PASS, HTTP/MySQL/browser reales |
| Imagenes historicas inexistentes | Inventario V5 indica cero imagenes a migrar | Migracion historica NO_APLICA; futuro R2 requiere controles independientes |

## Criterio de salida de esta microtarea

- Caso positivo: las fuentes Git tienen ruta e ID de blob, y Drive/Sheet tienen IDs exactos y limitaciones explicitas.
- Casos negativos: cambio a otro repo, eliminar STRICT, convertir Hostinger en migrado, usar V3 como backlog, alterar formulas o afirmar A5_PASS debe invalidar la prueba dirigida.
- A0 calidad estatica y A1 documental; A2 fronteras JWT/BCrypt/RBAC, tenant A/B, secretos sin cambios en runtime; A3 Node dirigido. No se certifica ejecucion HTTP, SQL/MySQL, Angular en navegador ni integracion productiva.
- A4 requiere SHA publicado fast-forward, diff final y readback PLAN/COLA/BITACORA; A5_PENDIENTE independiente antes de QA/main/PROD.
- Rollback del artefacto: `git revert` normal del commit del inventario, despues de revisar cambios concurrentes; no reset, no force, no borrado de BITACORA.

Este inventario no completa por si solo la microfase M01 ni certifica otro ID.
