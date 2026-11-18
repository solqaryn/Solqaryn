# SOLQARYN V5 FINAL — C05-03.M02.06: contrato de salida y dependencias del builder gobernado

**Microtarea:** C05-03.M02.06; PLAN_MAESTRO fila 805. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID \`6adc2e4bd8a48191ac7d8641ed95a1a3\`. **Ámbito:** \`solqaryn/Solqaryn@dev\`, PROJECT_SCOPE_LOCK=STRICT. **Riesgo:** V15 R0 documental, 18 de noviembre de 2026.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`); Sheet institucional \`1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU\` (PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA); \`AGENTS.md\`, \`docs/PROJECT_SCOPE_LOCK.md\` y Skill canónica. **Resultado:** fijar las dependencias técnicas y las condiciones de salida del lote documental M02; no implementar el builder ni otorgar permisos.

## 1. Precondiciones verificadas y contrato de alcance

La entrada V17 se habilitó únicamente después del A5 independiente corregido de \`C05-03.M02.05\`: PLAN H805=PENDIENTE/I805 vacío, COLA G805/O805=SI, DAG F766=VALIDADO/G766=SI. Se obtuvo \`SQV17_C05_03_M02_06\` para H:I fila 805, con namedRangeId \`2000000805\`, CAS H con \`occurrencesChanged=1\`, owner DEV03 y readback. Estas condiciones habilitan **documentar M02.06**, nunca migrar, desplegar o modificar JWT/RBAC.

Predecesores exigidos por el DAG vigente: \`C05-03.M01.01–06 + A5\`, \`C05-03.M02.01–05 + A5\`, \`C05-01.M01.06 + A5\`, \`C05-02.M01.06 + A5\`. Sus salidas PLAN eran LISTO y sus notas acreditaban revisiones A5 independientes. El A5_FAIL P2 histórico de M02.05 sigue en BITACORA; la reparación de EV08/IDEM08/T06 y el A5_PASS posterior no lo borran. Esta comprobación no reemplaza una consulta del DAG al momento de iniciar M03.

**Documentos base (solo contratos):**
- \`docs/v5/c05-03-m01-06-bloqueos-rollback-liberacion-builder-gobernado.md\`: BL01–BL16, RB01–RB10, LG01–LG12 y decisiones D01–D08 **pendientes**.
- \`docs/v5/c05-03-m02-01-entidades-ownership-invariantes-builder-gobernado.md\`: E01–E12 e INV01–INV16 **conceptuales**.
- \`docs/v5/c05-03-m02-02-api-dto-errores-validaciones-builder-gobernado.md\`: OP01–OP08, DTO D01–D08, ERR01–ERR12 y V01–V14 **no implementados**.
- \`docs/v5/c05-03-m02-03-persistencia-indices-migraciones-fixtures-builder-gobernado.md\`: P01–P12, IDX01–IDX10, MIG01–MIG06 y FX01–FX12 **no ejecutados**.
- \`docs/v5/c05-03-m02-04-configuracion-tipada-ambitos-feature-flags-builder-gobernado.md\`: CFG01–CFG16, FF01–FF12 **propuestos OFF**, PREC01–PREC08; sin cambios de configuración real.
- \`docs/v5/c05-03-m02-05-eventos-idempotencia-correlacion-limites-builder-gobernado.md\`: EV01–EV12, IDEM01–IDEM12, LIM01–LIM08, T01–T12 **futuros**; EV08/IDEM08/T06 exigen autor distinto del aprobador autenticado.

## 2. Dieciséis puertas terminales G01–G16

Estas puertas son una lista **contractual de verificación futura**, no un resultado de tests ejecutados. El lote M02 puede cerrar su especificación documental; la autorización técnica o productiva continúa cerrada.

| ID | Puerta de implementación futura: evidencia exigible |
| --- | --- |
| G01 | Tenant del recurso derivado de autoridad backend y membresía vigente; nunca de Host/header arbitrario. |
| G02 | JWT firmado, expiración y revocación comprobados con negativas reales por rol y sesión. |
| G03 | Ownership por tenant A/B y recurso validado en servidor; usuario B nunca lee ni muta A. |
| G04 | Componentes, secciones y campos solo desde allowlist tipada; negar HTML/JS/CSS ejecutable. |
| G05 | Rechazar URL \`javascript:\`, \`file:\`, localhost, redes internas y destinos SSRF; host público verificado. |
| G06 | DTO y errores opacos para denegación, ausencia, conflicto, cuota y validación: 403/404/409/422/429 sin filtraciones. |
| G07 | Revisión monotónica y CAS persistente comprobado; no lost update ni confianza en una lectura seguida de escritura. |
| G08 | Idempotencia transaccional tenant+actor+operación+recurso+versión; replay diferente => conflicto sin side effects. |
| G09 | Autor y aprobador autenticado diferentes sobre la **misma revisión**; autoaprobación, rol revocado o versión stale => denegar. |
| G10 | Publicación solo con aprobación, lease/rol y host vigentes, sin exponer drafts o PII de otros tenants. |
| G11 | Preview read-only; ni eventos de publicación, correo, worker, outbox o proveedor por mera visualización. |
| G12 | Límites medibles de tamaño, profundidad, intentos, TTL y concurrencia con rechazos seguros. |
| G13 | Migraciones EF/MySQL 8.4 bajo revisión explícita, transacción/reversa y Category=Integration realmente ejecutada. |
| G14 | Observabilidad correlacionada, redacción de JWT/PII/SQL y conservación de historia de auditoría. |
| G15 | UI Angular accesible/responsive y tests Playwright reales solo al cambiar navegación o componentes; backend sigue siendo autoridad. |
| G16 | Dependencias DAG, A0–A5, Scope Lock y SHA/checks exactos, rollback y autorización de ambiente acreditados antes de habilitar sucesores. |

**Estado de G01–G16:** se especifican sus oráculos; \`0/16\` puertas de implementación runtime certificadas por esta microtarea. BL/LG y decisiones D01–D08 permanecen pendientes. Ninguna de estas filas se escribe como fórmula SI o PASS en COLA/DAG.

## 3. Dependencias técnicas y consumidores M03+

- **Domain/Application (.NET 10):** un M03 futuro deberá materializar entidades tipadas, author/reviewer separados, invariantes, errores y revocación. No se crean clases, servicios, API ni DTO compilables aquí.
- **Infrastructure (EF Core 10 / Oracle MySql.EntityFrameworkCore / MySQL 8.4):** datos por empresa, constraints de idempotencia, CAS, índices y migraciones exigen CI con MySQL efímero real, rollback y negativa A/B. Ninguna tabla o migración se crea mediante M02.
- **Angular 22.2.1:** renderizado de schema permitido, no ejecutar HTML/JS arbitrario; guards de UI solo complementan autorización backend. No se crea pantalla ni se activan flags.
- **Integraciones/eventos:** EV01–EV12 son intenciones, no jobs ni dispatchers. No Cloudflare R2 real, Hostinger, DNS, secrets, proveedores de pago, scheduler o producción.
- **Seguridad transversal:** JWT/RBAC/tenant, rol revocado, actor malicioso, XSS, SSRF, replay y referencias cruzadas requieren tests adversariales antes de cualquier release.
- **Siguiente lote:** M03 debe superar **su propio** PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, revisión de riesgos y preflight de código; M02 LISTO o A5 por sí solos no conceden un permiso R3 ni validan A1–A3 de M03.

## 4. Diez oráculos positivo/negativo de aceptación FUTUROS_NO_EJECUTADOS

| Caso | Resultado positivo futuro | Negativa/borde/error obligatorios |
| --- | --- | --- |
| T01 | Crear draft con permiso de tenant A | Usuario B intenta editar A: denegado sin filtración. |
| T02 | Añadir sección allowlist tipada | HTML/JS/CSS arbitrario rechazado. |
| T03 | Validar destino público permitido | SSRF, javascript/file/localhost rechazados. |
| T04 | CAS con versión actual | Dos writers con versión stale: uno falla, sin pérdida. |
| T05 | Misma clave/payload devuelve resultado único | Mismo key con cuerpo diferente: conflicto, sin duplicar. |
| T06 | Aprobador autenticado diferente del autor aprueba revisión vigente | Autoaprobación, rol revocado, tenant B y versión stale bloqueados. |
| T07 | Preview autorizado devuelve mínima vista | Preview no dispara evento, worker, mail ni publicación. |
| T08 | Publicar con aprobación y host propio actual | Host falso, lease expirado o revocación niega acceso. |
| T09 | Payload dentro de presupuesto procesa una vez | Tamaño, profundidad o reintentos excesivos => 422/429 seguro. |
| T10 | Rollback autorizado registra historial aditivo | Replay/tenant B no borra historia ni divulga PII/JWT. |

**Resultado real:** diez pares especificados, **0/10 ejecutados**. No se presentan como pruebas funcionales, HTTP, navegador, JWT, MySQL ni proveedores aprobados. Las decisiones de negocio D01–D08 y los bloqueos BL/LG deberán resolverse mediante autorización/implementación separadas.

## 5. Auditorías A0–A5, evidencias y rollback

- **A0:** verificar este alcance M02.06, fuentes, ocho predecesores compuestos, G01–G16, T01–T10 y diff de **este Markdown más una sección autocontenida de CHANGELOG**.
- **A1 (R0):** revisar consistencia documental de positivos, negativos, límites, revocación, errores y oráculos; sin fingir ejecución de pruebas futuras.
- **A2 (R0):** tenant A/B, JWT/RBAC, autoría independiente, XSS/SSRF, idempotencia/CAS y minimización de PII definidos de modo fail-closed; no certificar seguridad runtime.
- **A3 (R0):** cotejar los cinco contratos de M02 y M01.06 con PLAN/COLA/DAG. xUnit, MySQL, Angular, Playwright y proveedor vivo **NO_APLICA** a este diff exclusivo Markdown.
- **A4:** crear commit fast-forward serializado en \`dev\` con CAS del HEAD; comprobar \`verify-solqaryn-project-scope\` y \`V5 - Sin encabezados vacios ni atribucion cruzada\` completed/success del SHA exacto, evidencia Git y readback de PLAN H/I, BITACORA A:Z, COLA y DAG. Si CI queda queued/in_progress, mantener VALIDANDO y no contar PASS.
- **A5:** revisión humana/automatizada independiente de otra Task Javier con TASK_ID distinto de DEV03, examinando requisitos, contratos, SHA y pruebas; LISTO_DEV no implica A5_PASS.

**Reversa:** git revert normal de este commit tras comprobar HEAD y no escritores concurrentes; BITACORA permanece aditiva. Sin cambios a QA, main, PROD, Aiven, dominio, secretos, ambientes ni configuraciones físicas de Tasks. Esta especificación no sustituye trabajo ejecutable de M03 ni licencia activaciones futuras.
