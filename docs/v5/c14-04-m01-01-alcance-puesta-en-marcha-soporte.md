# SOLQARYN V5 FINAL — C14-04.M01.01: objetivo, exclusiones y no-regresión de puesta en marcha

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** contrato preparatorio M01.01 exclusivamente documental; no despliegue ni certificación de producción.

**Fuentes rectoras:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, [C14-03.M01.01](./c14-03-m01-01-alcance-promocion-controlada.md) y [C14-03.M01.05](./c14-03-m01-05-criterios-promocion-controlada.md). Prefiltro inicial de esta unidad: PLAN fila 2126 PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI; el gate sólo autoriza esta preparación, no libera M01.06/M06 ni infraestructura.

## 1. Objetivo de C14-04 y resultado futuro comprobable

Definir el alcance de una **puesta en marcha controlada, soporte y continuidad** para un SaaS multitenant: landing y dominios, almacenamiento y medios condicionados, flujos de atención, telemetría y alertas, copias/restore, respuesta a incidentes y período de observación. El resultado futuro del bloque deberá enlazar un smoke real por versión y entorno, responsables de soporte, criterios de severidad, salud y rollback, evidencias de backup/restauración ensayadas y una ventana de observación con decisión autorizada. Este documento **sólo delimita** el trabajo; no constituye ese smoke, medición de SLO, ensayo de restore, aprovisionamiento de R2, ni aceptación de producción.

La topología **actual** sigue siendo la del repositorio: Angular 22.2.1 en frontend, ASP.NET Core net10.0, EF Core 10/MySQL, JWT/RBAC/tenant A/B, Render/Vercel/Aiven y Cloudinary donde corresponda. Hostinger y Cloudflare/R2 son **objetivos futuros sujetos a permisos, gates y planes individuales**. El documento no sustituye contratos actuales ni valida cuentas/proveedores. C14-04 prepara el marco que consumirán M01.02 actores, M01.03 fuentes, M01.04 riesgos y M01.05 aceptación; ninguna de ellas anticipa un deploy.

## 2. No-alcance vinculante

No modificar ahora DNS, TLS, dominios, `main`/`qa`/PROD, compras, cuentas de pago, servicios Hostinger/Cloudflare/R2, secretos, configuraciones de producción, MySQL real, migraciones, backups reales, datos personales, políticas de JWT/RBAC, controles de tenant, jobs, colas, Cloudinary, endpoints, Angular, seguridad de infraestructura ni infraestructura de monitorización. Tampoco ejecutar un webhook/event dispatcher, crear Tasks, PR o ramas ni reinterpretar `LISTO_DEV`/A5 como autorización para otro entorno. Ninguna futura imagen, archivo o medio se migra por inferencia; requiere inventario y autorización explícita.

No afirmar evidencias operativas no existentes: un enlace a workflow, un plan, un test escrito o un mock no demuestran que un servicio estuvo disponible, que una restauración fue viable o que se cumplió un objetivo de disponibilidad. Cualquier prueba futura deberá usar datos sintéticos/servicios efímeros o permiso específico, y conservar su `head_sha`, resultado y fecha.

## 3. Invariantes NR01–NR16 de no-regresión

| ID | Invariante preservada | Condición bloqueante futura |
| --- | --- | --- |
| NR01 | Separación DEV/QA/main/PROD y autorización expresa por ambiente | LISTO_DEV o A5 se usa como permiso de despliegue |
| NR02 | Identidad de release verificable por SHA y digest | Artefacto no coincide con el commit probado |
| NR03 | Scope Lock obligatorio `completed/success` exact-SHA | Check requerido pendiente, omitido o fallido contado PASS |
| NR04 | CI funcional y de seguridad causal al cambio | Build o suite requerida cancelada/skipped usada como aceptación |
| NR05 | Autenticación y revocación JWT siguen gobernadas por backend | Token caducado/revocado conserva acceso |
| NR06 | RBAC y pertenencia multiempresa/sucursal operan en API | Tenant A obtiene recurso de B por slug, host, ID o cabecera |
| NR07 | Landing y dominios públicos no permiten suplantación de tenant | Host arbitrario resuelve configuración privilegiada |
| NR08 | Archivos/medios privados conservan ownership y política de acceso | R2/Cloudinary entrega objeto de otra empresa |
| NR09 | MySQL/EF preserva integridad, auditabilidad e historial comercial | Restore/migración pierde pedidos, kardex o saldos |
| NR10 | Backup y restore requieren evidencia ensayada no destructiva | Se declara recuperable sin prueba de restauración |
| NR11 | Alertas/observabilidad no filtran secretos ni PII | Logs guardan JWT, credenciales o datos de B |
| NR12 | Incidentes tienen dueño, severidad, escalación y reversión | Fallo crítico permanece sin responsable o stop condition |
| NR13 | UI continúa ligera, responsive, accesible y con estados claros | Smoke visual omite navegación, teclado o flujo de error |
| NR14 | Proveedores/transiciones se controlan por flags y rollback | Cambio de DNS/storage carece de reversión autorizada |
| NR15 | Auditoría institucional conserva historial append-only | Se sobrescribe BITACORA o se duplica owner/ID |
| NR16 | A5 independiente y permiso operativo separados | Task aprueba su propio trabajo o habilita PROD automáticamente |

## 4. Ocho pares de aceptación futura, definidos y NO ejecutados

| Caso | Positivo a demostrar en entorno expresamente autorizado | Negativo que debe bloquear |
| --- | --- | --- |
| P01/N01 | Smoke landing/API con SHA y digest auditados | Respuesta de artefacto diferente al aprobado |
| P02/N02 | Host y rutas autorizadas resuelven tenant correcto | Host spoofing/redirección revela tenant ajeno |
| P03/N03 | A y B tienen JWT/RBAC y recursos efectivamente aislados | Token/URL/ID de A obtiene dato de B |
| P04/N04 | Archivos previstos tienen inventario, owner y ACL | Archivo privado servido sin comprobación de propiedad |
| P05/N05 | Backup aislado se restaura y reconcilia sin datos productivos | Restore no probado o destruye integridad |
| P06/N06 | Health, trazas y alertas se correlacionan por release | Error 5xx o fallo de job sin diagnóstico ni escalación |
| P07/N07 | Canal de soporte clasifica incidente y ejecuta rollback ensayado | Incidente severo sin on-call, mitigación o criterio stop |
| P08/N08 | UI/E2E sintéticos verifican acceso, vacío, error y teclado | Un spec presente, sin ejecución Playwright, se llama PASS |

**Estado real de P01–P08 y N01–N08:** `FUTURO_NO_EJECUTADO`. No hay medición de latencia/disponibilidad, visita a proveedores ni acceso a producción por este commit.

## 5. Evidencia esperada y límites de autoridad

Una futura ficha operacional deberá incluir: `MICROTAREA_ID`, actor/rol, ambiente permitido, `SOURCE_SHA`, `ARTIFACT_DIGEST`, versión frontend/API/MySQL, `job_id/status/conclusion/head_sha`, correlación de pruebas positivas y negativas, tenant de fixture, dashboard/health, responsable de incidentes y escalación, ventana de observación, resultado de restore (si corresponde), autorización humana explícita, evaluación P0/P1, revisión A5 con Task distinta y reversión. El dato fuente del plan sigue siendo PLAN_MAESTRO/COLA/DAG, no la propia ficha.

**Impacto de esta microtarea:** un Markdown en `docs/v5` y sección autocontenida en `CHANGELOG_AI.md`; no cambian API/DTO, clientes, Domain/Application/Infrastructure, persistencia, JWT/RBAC, archivos, operaciones, alarmas, secretos ni interfaces. Consumidores futuros: M01.02–05 y gates específicos M02–M06. Observabilidad y rollback quedan como requisitos, no acciones ejecutadas.

## 6. A0–A5 y rollback

- **A0 documental:** ruta nueva única, 16 invariantes, 8 pares P/N, alcance y exclusiones coherentes.
- **A1 documental:** aceptación positiva/negativa descrita y falsable; no se simula ejecución real.
- **A2 documental:** límites JWT, RBAC, tenant A/B, privacidad, integridad comercial, archivos y secretos preservados.
- **A3 documental:** coherencia con arquitectura y contratos C14-03; compilaciones .NET, Angular, Playwright y MySQL son `NO_APLICA` para cambios Markdown únicamente, no `PASS`.
- **A4 publicación:** commit directo a dev mediante fast-forward y CAS del HEAD actual, diff preciso, validación de procedencia CHANGELOG y Scope Lock sobre SHA publicado, readbacks PLAN/COLA/BITACORA.
- **A5:** `A5_PENDIENTE` de otra de las cinco Tasks Javier, sin autoaprobación; A5 no autoriza QA/main/PROD.

**Rollback:** `git revert` normal del changeset documental tras inspeccionar commits concurrentes; preservar historial de BITACORA y no recurrir a force-push, reset, migración o reversión de datos productivos.
