# SOLQARYN V5 — C09-01.M01.05: criterios de aceptación observables para CRM universal

**Microtarea:** C09-01.M01.05 · PLAN_MAESTRO fila 1302 · **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a` · **Fecha:** 2026-11-14 · **Repositorio:** `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Salida del lote:** CRM universal con ficha/historial, segmentos, oportunidades, actividades y seguimiento; timeline aislado y permisos RBAC. **Esta unidad define una especificación verificable, no prueba o implementación CRM.** Todos los AC, N y NFR enumerados aquí están marcados **FUTUROS_NO_EJECUTADOS**.

## Procedencia y contrato de compatibilidad

- `docs/v5/c09-01-m01-01-alcance-crm-universal.md` (blob `4d35d8113cf1a1718dd7f487c50a06f51c1848b5`): objetivos y no-regresión NR01–NR14.
- `docs/v5/c09-01-m01-02-actores-interfaces-crm-universal.md` (blob `02a78bad5debea4678651624280a4353c54af24a`): actores A01–A14, interfaces I01–I12 y fronteras F01–F09 propuestas.
- `docs/v5/c09-01-m01-03-inventario-codigo-brechas-crm.md` (blob `34e0a8a652a9e92900644c3e7c8fc59ada58b5f8`): inventario estático de 16 fuentes y G01–G12 brechas aún no demostradas.
- `docs/v5/c09-01-m01-04-riesgos-supuestos-decisiones-crm.md` (blob `8a700705ea49da7d729e4f1010496751575db790`): R01–R16 **potenciales**, S01–S08 **sin verificar**, D01–D08 **pendientes**.
- `backend/src/Domain/Entities/Cliente.cs` (blob `8c6d4c4453e44f8c0be9475340465e1aadb3f787`) y `backend/src/Domain/Entities/UsuarioEmpresa.cs` (blob `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c`): límites existentes de cliente/empresa, no prueba de un CRM completo.

**Limitaciones:** una entidad Cliente existente, una búsqueda UI o un controller protegido no certifican timeline CRM, oportunidades, consentimiento, exportación segura ni autorización transversal. No se inspeccionaron datos ni ejecutaron pruebas runtime/HTTP/EF/Playwright con esta modificación de Markdown.

## AC01–AC14 — aceptación funcional POSITIVA FUTURA y negativa por caso

| AC | Observable positivo exigido al implementar | Negativo que debe rechazarse | Evidencia causal futura |
| --- | --- | --- | --- |
| AC01 | Usuario A autorizado obtiene ficha Cliente A con empresa resuelta por sesión | N01: JWT de A consulta ID Cliente B y no recupera PII | xUnit + API 401/403/404 y MySQL A/B |
| AC02 | Timeline A retorna actividades ordenadas con actor, fecha y correlación | N02: acceso a actividad de B no revela contenido | Repository/EF y API A/B |
| AC03 | Crear actividad válida una vez responde con ID estable | N03: repetir key/payload o carrera no duplica actividad | xUnit concurrente + MySQL efímero |
| AC04 | Oportunidad A cambia entre etapas permitidas y queda auditada | N04: rol lector, etapa ilegal o CAS obsoleto se rechazan | Domain/Application + API |
| AC05 | Asignación de oportunidad preserva empresa y responsable autorizados | N05: usuario de B o sucursal sin permiso no recibe asignación | RBAC server-side y fixtures cross-tenant |
| AC06 | Segmento A admite criterios allowlisted y paginados | N06: SQL, JS, columna ajena, filtro masivo o B se rechazan | DTO/FluentValidation + consulta parametrizada |
| AC07 | Búsqueda y listado CRM A mantienen filtros por tenant con paginación | N07: host/slug/X-Empresa-Id falsos no seleccionan B | API directa sin depender de Angular |
| AC08 | Export A de columnas mínimas se registra con actor y motivo | N08: rol sin permiso, export B o campo PII prohibido falla cerrado | API + contenido export + auditoría |
| AC09 | Contacto con consentimiento vigente recibe sólo canal/finalidad permitidos | N09: opt-out, falta de consentimiento o finalidad distinta impiden emisión | Contract tests y no-envío, sin proveedor live |
| AC10 | Revocar membresía/rol bloquea lecturas y acciones posteriores | N10: sesión revocada, JWT vencido o caché obsoleta no conserva privilegio | JWT/RBAC, cache invalidation, tests |
| AC11 | Cliente legado con owner reconciliado se presenta únicamente al tenant acreditado | N11: `EmpresaId=NULL` no se convierte en lectura global | Fixtures legacy/EF y negativa A/B |
| AC12 | Historial CRM y auditoría son distinguibles sin duplicar registros | N12: retry/evento repetido no corrompe auditoría ni mezcla semántica | Tests transaccionales, idempotencia y hashes |
| AC13 | UI Angular carga listas accesibles a 320px con foco y feedback claro | N13: manipular guard Angular no evita autorización API | Playwright/Chromium + API |
| AC14 | Desactivar capacidades CRM preserva clientes, ventas y trazas históricas | N14: rollback no borra ni altera registros de operaciones existentes | Migración ensayada, backup y revert |

Todos los casos requieren datos de prueba sintéticos de dos empresas aisladas A/B, sin secrets ni datos reales. El criterio de un caso es **aceptable sólo con resultados positivos y negativos observados** en el SHA de implementación exacto, con captura de evidencias, no por su mera presencia en el plan.

## NFR01–NFR10 — requisitos no funcionales medibles en fase técnica

| NFR | Umbral/medida que deberá fijarse con baseline y decisión de la fase | Condición de rechazo |
| --- | --- | --- |
| NFR01 | P95 de búsqueda/lista/timeline bajo volumen y tamaño de página representativos | Afirmar rendimiento sin dataset y medición |
| NFR02 | Límites de tamaño por request, filtros, páginas y exports configurados server-side | Solicitudes sin límite o carga de PII no acotada |
| NFR03 | Idempotencia de writes y respuesta estable a retries concurrentes | Dos efectos durables para misma operación lógica |
| NFR04 | Transacciones coherentes entre oportunidad, auditoría e historial | Registro parcial sin reconciliación/rollback |
| NFR05 | Trazas con request/correlation ID sin tokens ni PII innecesaria | Logging de secretos, notas o contactos completos |
| NFR06 | Resolución tenant/RBAC en backend incluso con cache miss y timeout | Fail-open al fallar caché o autorización |
| NFR07 | Contratos API/DTO versionados y compatibilidad del CRUD Cliente actual | Ruptura silenciosa de consumidores existentes |
| NFR08 | Accesibilidad teclado/foco, contraste y reduced motion en desktop/móvil | Componentes inaccesibles o cargas pesadas no justificadas |
| NFR09 | Plan de índice y volumen de MySQL 8.4 medido con EXPLAIN/queries sintéticas | Scans masivos no acotados sobre PII |
| NFR10 | Backup/restore y rollback ensayados con integridad referencial y auditoría | Pérdida de historial al revertir CRM |

Los umbrales numéricos de latencia, tamaño o retención **NO SE INVENTAN**: deben acordarse y establecerse sobre baseline antes de certificar pruebas de performance, indexación y retención.

## Matriz de ejecución futura y gates

| Tipo de prueba | Herramienta/ámbito previsto | Evidencia imprescindible |
| --- | --- | --- |
| Dominio y aplicación | .NET 10/xUnit | AC03/04/12, transiciones inválidas, idempotencia |
| API/JWT/RBAC/tenancy | ASP.NET 10 + solicitudes sin UI | AC01/05/07/08/10 con 401/403/404 y tenant A/B |
| Persistencia y migraciones | EF Core 10/MySql.EntityFrameworkCore + MySQL 8.4 efímero | AC02/03/06/11/14, rollback, integridad, índices |
| Frontend UI | Angular 22.2.1, Material/CDK, Playwright | AC13 y estados accesibles, sin autoridad cliente |
| Seguridad y resiliencia | CI GitHub Actions exact-SHA | token revocado, PII, replay, fallos y ausencia de secretos |
| Observabilidad | logs sintéticos y métricas medibles | NFR01–10 con valores medidos, sin prometer baseline |

**Estatus general: TODO_FUTURO_NO_EJECUTADO.** Ningún AC/NFR anterior representa una prueba ejecutada, un estado PASS real, ni autorización para activar rutas, workers, eventos, migraciones, servicios pagados o QA/main/PROD.

## Cierre documental A0–A5 y rollback

- **Impacto:** sólo el documento nuevo `docs/v5/c09-01-m01-05-criterios-aceptacion-crm.md` y una sección indivisible de `CHANGELOG_AI.md`; sin modificar código .NET, Angular, MySQL/EF, JWT/RBAC, API, secretos, PII real, jobs o contratos ejecutables.
- **A0:** Skill local cotejada con canónica, Scope Lock STRICT, permiso GitHub/Drive, PLAN H1302/I, COLA G/O y DAG F/G, CAS exclusivo con readback; revisar dependencias y archivo libre.
- **A1:** coherencia estática 14 positivos/14 negativos y 10 NFR con fuentes M01.01–04, oráculos reproducibles y estado futuro.
- **A2:** análisis de denegación para tenant A/B, JWT, RBAC, PII, NULL legacy, replay, consentimiento, permisos y timeout. No se atribuyen pruebas runtime.
- **A3:** matriz explícita de integración prevista .NET/API/EF/MySQL/Angular/Playwright; `NO_APLICA` la ejecución de esas suites por un diff sólo Markdown.
- **A4:** publicar en dev vía fast-forward + expected SHA, revisar blobs/diff, comprobar Scope Lock y check V5 del CHANGELOG `completed/success` en SHA exacto, PLAN/COLA/BITACORA y readback.
- **A5:** revisión independiente `A5_PENDIENTE` por otra Task del allowlist Javier, nunca autoría propia; no promoción automática.
- **Rollback:** git revert normal del commit documental en dev, sin reescritura histórica ni cambios de datos/producción.

**Decisiones D01–D08 siguen pendientes** antes de implementación con efectos; M01.06/M02–M06 requieren sus gates individuales y pruebas reales pertinentes. No crear endpoint CRM, worker, webhook ni integraciones de mensajería por este documento.
