# SOLQARYN V5 — C15-03.M01.01: alcance y no-regresión de auditoría de seguridad y tenancy

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 02 JAVIER (TASK_ID `6af16bb9492c8191b8e8c7d606f11507`). **Repositorio/rama:** `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT. **Clase:** preparación documental M01, no implementación ni certificación de seguridad en runtime.

## Propósito, evidencia y límites

Preparar una auditoría repetible de **aislamiento tenant A/B, sucursal, propiedad de recursos, archivos, jobs y control plane** antes de aceptar nuevas rutas de plataforma universal. La salida de esta unidad es **el contrato de alcance**, no un resultado de pentest, un despliegue ni un pase A5. El resultado esperado del plan V5 exige pruebas negativas verificables y cero cruces *cuando esas pruebas se ejecuten*, lo cual NO se demuestra al escribir este documento.

Autoridad: STANDARD V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO fila 2234, COLA G/O=SI y DAG_TECNICO fila 2195, F=VALIDADO/G=SI en el preflight. Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y `docs/AUTOMATION_STATUS.md` del HEAD dev. El DAG documental NO habilita cambios IAM, JWT, RBAC, migraciones, integraciones de pago, envíos, DNS, QA/main/PROD ni desbloquea por sí solo M01.06.

## Fronteras actuales comprobadas en el árbol fuente

| Referencia exacta del árbol de entrada | Evidencia verificable por lectura estática | Límite que NO se infiere |
| --- | --- | --- |
| `backend/src/API/Filters/TenantPermissionContext.cs` — blob `df7d49f3e185c538f3b3dafabb36c3a971a7257d` | `X-Empresa-Id` es selector; rechaza encabezado ambiguo o inválido y exige marca de autorización server-side | Un header válido no prueba membresía ni RBAC |
| `backend/src/Application/Security/IdentidadUniversalAuthorizationGuard.cs` — blob `5c7a256ea9709f6bf4279f2ff89addf599e3a311` | Exige empresa del contexto, snapshot de membresía/rol vigente y sucursal verificada | No certifica todas las rutas que deban invocarlo |
| `backend/src/Application/Security/VerifiedCommercialTenant.cs` — blob `2da140ce9d7f47329d39d292d3271596467910cc` | La marca server-side de empresa autorizada se resuelve de `HttpContext.Items` | No convierte parámetros de cliente en autorización |
| `backend/src/API/Middleware/UnscopedCommercialTenantGuardMiddleware.cs` — blob `558dda1eb5be729b2bd9a6d9fefbccd5db94addc` | Declara cuarentena de API privada con 2+ empresas y excepciones delimitadas | Excepciones y rutas requieren prueba de regresión, no permiso general |
| `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` — blob `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | Filtra por `AuthorizedReadEmpresaId` cuando hay marca positiva | Sin esa marca retorna query: depender de cuarentena externa requiere prueba end-to-end |
| `backend/src/Infrastructure/Persistence/CommercialCrossTenantWriteGuard.cs` — blob `1989e290a855a991782c59b4ee4f147be817f347` | Comprueba asociaciones cross-tenant y preserva `EmpresaId NULL` legacy sin atribución implícita | No sustituye filtros de lectura ni resuelve datos heredados |
| `backend/src/Application/AutomationV5/V5WorkItemTenantAuthorizationGuard.cs` — blob `d14aaf38094d11994130b3e6bf2db3116d576d8f` | Modelo fail-closed de actor/scope/tenant, separado del CAS institucional | No hay dispatcher server-side autorizado por esta unidad |
| `backend/tests/Solqaryn.Tests/API/Controllers/UsuarioEmpresaTenantBoundaryTests.cs` — blob `52acc04044441a0203139b8be404fd5ed2d6ce72` | Contratos negativos de asignación de empresa B desde A y restricción de membresías | Test existente en Git no significa ejecutado aquí |
| `backend/tests/Solqaryn.Tests/API/SecurityBoundaryContractTests.cs` — blob `efd7f8111bdbabf0b63b5fbc9f89384dd3316fdc` | Contratos estáticos de `[Authorize]`, upload size y rate limiting | No equivale a ataque HTTP real ni auditoría integral |

Fuente de cada blob: API GitHub `contents/{path}?ref=8708950383c5a6c118a7f4913785753a3529a0fc`. Su existencia y lectura estática **no** acreditan cobertura funcional.

## Dieciséis invariantes de no-regresión exigibles en una fase de pruebas posterior

| ID | Condición que deberá preservarse y probarse |
| --- | --- |
| NR01 | Todo acceso privado exige identidad y permiso vigente resueltos en backend, no por Angular/cliente |
| NR02 | Un JWT de tenant A nunca autoriza recurso de B aunque cambie header, query, body o ruta |
| NR03 | `X-Empresa-Id` múltiple, inválido o cero se rechaza sin fallback que amplíe autoridad |
| NR04 | Una membresía o rol revocado deja de autorizar al refrescar snapshot server-side |
| NR05 | Una sucursal inexistente o ajena no puede usarse para leer/escribir datos |
| NR06 | El dueño de cada recurso se verifica en servidor y no se reasigna mediante DTO de cliente |
| NR07 | Rutas de listado, detalle, export y búsqueda no enumeran IDs de otra empresa |
| NR08 | Consultas con `IgnoreQueryFilters` históricas mantienen ámbito autorizado o se bloquean |
| NR09 | Relaciones venta–cliente, compra–proveedor y detalle–producto no cruzan tenant |
| NR10 | Filas legacy con `EmpresaId=NULL` siguen sin propietario atribuido artificialmente |
| NR11 | Host público sólo resuelve el tenant conforme a binding confiable; un Host falsificado no confiere derechos |
| NR12 | Herramientas de soporte y control plane validan autoridad propia y alcance limitado |
| NR13 | Archivos, imágenes, descargas y URLs temporales validan dueño/tenant y expiración |
| NR14 | Jobs, colas y reintentos futuros no heredan tenant del payload no confiable |
| NR15 | Eventos y auditoría preservan actor, correlación y resultado sin divulgar PII ni secretos |
| NR16 | Fallos, timeouts y estados ambiguos fallan cerrados; ninguna excepción libera producción/QA |

**NR01–NR16 son obligaciones contractuales, NO resultados observados.** Esta unidad no ejecutó requests HTTP ni fixtures MySQL; no declara ausencia de IDOR, escalada de privilegios o filtraciones.

## Matriz positiva/negativa futura — requisitos no ejecutados

| Caso | Positivo esperado | Negativo bloqueante |
| --- | --- | --- |
| P01/N01 | Tenant A con permiso vigente consulta sólo sus recursos | Token A + ID B devuelve rechazo sin filtrar dato |
| P02/N02 | Miembro autorizado cambia su rol dentro de A según RBAC | Header B o rol revocado no confiere asignación |
| P03/N03 | Sucursal perteneciente a A acepta operación propia | Sucursal de B bajo contexto A es rechazada |
| P04/N04 | Consulta histórica autorizada filtra `EmpresaId=A` | Bypass sin scope no devuelve filas de B |
| P05/N05 | Upload permitido registra owner A y tamaño permitido | MIME/tamaño/scope inválidos no persisten archivo |
| P06/N06 | Job autorizado conserva idempotencia y tenant verificado | Reintento con clave replay o tenant B no duplica efectos |
| P07/N07 | Soporte de plataforma con grant actual sólo ve datos permitidos | Token o encabezado de soporte falsos no amplían alcance |
| P08/N08 | Registros y logs de una operación exitosa son auditables | Error nunca expone secretos, PII o contenido de B |

Evidencia mínima futura por caso: commit SHA causal, tenant A/B fixture aislada, rol y pertenencia reales, endpoint/job, request/expected/observed, resultado HTTP o aserción, timestamps/log redacted, control de rollback y enlace a CI. Un job `queued/in_progress/skipped/cancelled/failed` nunca es PASS obligatorio.

## Alcance técnico, peligros y rollback

- **Cambios de esta microtarea:** este archivo Markdown y sección autocontenida de `CHANGELOG_AI.md`. Sin cambio de API/DTO/DB/frontend/security config ni credenciales.
- **Dependencias:** C15-03.M01 preparación y gates dinámicos de PLAN/COLA/DAG; no se suponen A5 de otros lotes por texto.
- **Riesgo que debe inspeccionarse después:** `TenantQueryBypassGuard` conserva el query sin `AuthorizedReadEmpresaId`, con garantía dependiente de la cuarentena middleware; requiere test negativo de borde real, no es vulnerabilidad demostrada.
- **Pruebas de esta entrega:** auditoría textual de fuentes y clasificación de escenarios como FUTUROS_NO_EJECUTADOS; Scope Lock y procedencia V5 por SHA exacto tras publicación; builds ajenos al diff Markdown no acreditan pruebas de negocio.
- **Rollback:** `git revert` del commit documental, revisión de HEAD/CI y reconciliación PLAN/COLA/BITACORA con evento aditivo; jamás force-push ni reescritura histórica.
- **Gate de salida:** A0–A4 documentales con readback, A5_PENDIENTE por distinta Task Javier. No promover ni liberar M01.06/M02 por el solo documento.
