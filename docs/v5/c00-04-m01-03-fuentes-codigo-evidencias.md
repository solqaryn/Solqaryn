# C00-04.M01.03 — Inventario de contratos, código y evidencias sin certificación presunta

**Alcance:** SOLQARYN V5 FINAL, C00-04 Gobierno de cambios y decisiones, M01.03. `RESULTADO_EXIGIDO`: fijar qué requiere autorización, qué es reversible y qué no se ejecuta automáticamente. La salida de esta microtarea es un inventario documental verificable; no modifica API, componentes UI, persistencia, workflows, secretos ni decisiones de producción.

**Autoridades:** Documento `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`; `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_AUTHORITY.md`, `docs/AUTOMATION_V13.md` y única Skill canónica `.agents/skills/solqaryn-project-governance/SKILL.md`.

## Inventario de fuentes comprobadas

| Fuente o archivo `dev` | Evidencia de lectura | Qué prueba realmente | Lo que NO certifica |
| --- | --- | --- | --- |
| `AGENTS.md` | blob `54146df5e8eea5217e21958f946d9d0ebfc30571` | Gobernanza V5, Scope Lock estricto, control DEV y separación de cuentas | Permiso sobre `qa`, `main` o PROD |
| `docs/PROJECT_SCOPE_LOCK.md` | blob `a92ae64d46a82b5255919012b474f7ece430ed6b` | Identidad de repositorio/branch, restricciones de CI y no force-push | Que cualquier job nuevo haya aprobado |
| `docs/AUTOMATION_V13.md` | blob `a6b82da5fde00e3662dfe6c4465a2dec57d61152` | Protocolo A0–A5, DAG y orden de estados | A5_PASS de un revisor distinto |
| `docs/AUTOMATION_AUTHORITY.md` | fuente autenticada por cuenta Javier, versión vigente en `dev` | Roles autorizados y cuenta de cada Task | Que Javier controle Tasks de Alex |
| `.agents/skills/solqaryn-project-governance/SKILL.md` | blob `304e400d5c0f89fcfdcc65caece86c34276734b5`, cuerpo comparado con Skill instalada | Coincidencia de autoridad de gobierno | Un test funcional runtime |
| `backend/src/API/Solqaryn.API.csproj` | blob `2a7b33a0974d1ebe13676593d84b458b1243e037` | Declaración `TargetFramework=net10.0`, referencias .NET | Compilación o llamadas HTTP de este candidato |
| `frontend/package.json` | blob `e6d030f643421cb343cc12617e742469e88223c4` | Scripts `npm`, `lint`, `build:prod` | E2E y accesibilidad ejecutados |
| `.github/workflows/solqaryn-core-ci.yml` | blob `bcb173e96d2a202caa622a4e8e2972ab88d31526` | Configuración declarada para runner, `setup-dotnet` y `setup-node` | Resultado verde del SHA candidato sin consultar check-runs |
| `.github/workflows/dev-ci.yml` | blob `3f6912458d17a2a049d7b6a804472a5e1cd1b746` | Ruta de pruebas sujeta a triggers, paths e impacto | Ejecución automática de MySQL cuando job está SKIPPED |
| `PLAN_MAESTRO` / `COLA` | Fuente viva en Sheet, H estado, I responsable, E espejo, G/O elegibilidad | Lectura institucional actual y ausencia de claim duplicado por ID | Que todos los 2.340 estén autorizados a ejecutarse |
| `DAG_TECNICO` | C00-04.M01.03 con revisión `VALIDADO` y gate `SI` | Excepción documental de 11 dependencias LISTO | Cierre global de C00, nueva arquitectura o cambios de runtime |
| `BITACORA` A:Z | 26 encabezados reales, `EVENT_ID` A, `IDEMPOTENCY_KEY` R | Esquema de trazabilidad para eventos futuros | Validez de un evento truncado de 21–25 campos |

Los blobs anteriores identifican archivos inspeccionados y sujetos a cambio concurrente. Su SHA documenta la versión muestreada, **no** sustituye la comprobación del HEAD y del `head_sha` real antes de operar.

## Contrato de evidencia y no-certificación

1. **Selección y claim:** `PENDIENTE` con I libre; `COLA!G=SI`, `COLA!O=SI`; DAG `VALIDADO` con todos los prerequisitos realmente LISTO; verificar conexión GitHub `jmejia31` y Drive JAVIER. Si la identidad no es positiva, aplicar fail-closed. Una Task de Alex sólo se administra desde su cuenta.
2. **Candidato:** documentar un cambio acotado, diff, riesgos, consumidores, interfaces y rollback; pasar a `VALIDANDO` antes de A0–A3. No reutilizar resultados de otro SHA.
3. **Auditorías:** A0 calidad estática/documental; A1 aceptación observable positiva y negativa; A2 JWT/RBAC/tenant, PII y autoridad sin bypass; A3 contratos e integración relevantes. Para un documento sin código runtime, Node/Angular/.NET/MySQL/Docker/E2E quedan `NO_APLICA` motivados, no `PASS` de herramienta.
4. **Publicación:** solo fast-forward directo a `dev` con HEAD revalidado. A4 exige SHA y blobs publicados, check-runs de `verify-solqaryn-project-scope` en `completed/success`, diff final, PLAN/COLA/BITACORA y rollback normal.
5. **Cierre:** A0–A4 aplicables `PASS`, evento `A5_PENDIENTE` para otro TASK_ID, `PLAN_MAESTRO!H=LISTO` y readback `COLA!E=LISTO`; no autopromoción a QA/main/PROD.

## Matriz de autorización, riesgos y reversibilidad

| Riesgo / decisión | Evidencia exigida | Acción ante incumplimiento |
| --- | --- | --- |
| Lectura de documentación y metadatos | Fuentes oficiales y versiones de archivo, sin publicación de secretos | Ajustar inventario, no declarar certificación |
| Cambios de código y CI | Diff candidato y jobs técnicos pertinentes de `head_sha` exacto | Mantener VALIDANDO o corrección dirigida; `SKIPPED` no es PASS |
| IAM, secretos, JWT/RBAC y tenancy | Negativas de aislamiento y permisos cuando cambia backend | NO_GO del ID inseguro; no aceptar autorización implícita |
| Datos MySQL/EF y migraciones | Prueba MySQL 8.4 descartable y rollback validado, sin PROD | No ejecutar operación destructiva |
| Estado y BITACORA | Única H mutable, espejo E, 26 campos A:Z, claves únicas y readback | No añadir evento inválido ni inventar LISTO |
| Infraestructura y despliegue | A5_PASS independiente + orden adicional expresa | No modificar `qa`, `main`, PROD, DNS, R2, Cloudflare, Hostinger |
| Concurrencia de otros ejecutores | Responsable por ID, archivos sin colisión, HEAD antes de escribir | Reconciliar aditivamente o abandonar scope en su estado real |

## Pruebas documentales positivas y negativas

**Positivo:** recuperar un documento del HEAD `dev` y verificar su blob, dependencia DAG y fuente oficial, con `PLAN_MAESTRO!H/I` coherentes; luego escribir un cambio documental de dos archivos en `dev`, Scope Lock del SHA exacto y evento de 26 campos preservando autoría. Esto demuestra trazabilidad de documentación, no funcionalidad de API.

**Negativo A:** un resultado de CI anterior o un job `SKIPPED` no prueba el SHA nuevo. No certificar un backend con una compilación Angular sin test pertinente.

**Negativo B:** si `COLA!G/O` indican `SI` pero otra Task tiene I ocupado o falta prerrequisito, no reclamar. La reserva objetivo N=8 no libera filas.

**Negativo C:** una BITACORA con 21–25 campos o claves duplicadas no cumple A:Z; no truncar ni sobrescribir el historial.

**Negativo D:** `A5_PASS` del autor sobre su propia microtarea está prohibido; `LISTO_DEV` no equivale a permiso de QA/main/PROD.

**Negativo E:** una arquitectura objetivo futura no activa proveedores, DNS, pagos ni migraciones; son operaciones sensibles con autorización separada.

## No-alcance, consumidores y rollback

Este inventario afecta solo a `docs/v5/c00-04-m01-03-fuentes-codigo-evidencias.md`, `CHANGELOG_AI.md` y registros institucionales pertinentes. Sin cambios a API/DTO, rutas Angular, MySQL, migraciones, transacciones, workers, JWT, secretos, versiones, workflows o proveedores. Consumidores: ejecutores autorizados, revisores independientes y gobierno de cambios.

**Prerrequisitos de DAG:** `C00-01.M02.01–06` y `C00-03.M01.01–05` LISTO; `C00-04.M01.03` revisada VALIDADO. Revalidar si cambian dependencias y antes de cualquier trabajo ejecutable. **Rollback:** revertir solo commit documental mediante `git revert` normal, registrar reconciliaciones mediante eventos aditivos sin borrar históricos y nunca force-push.
