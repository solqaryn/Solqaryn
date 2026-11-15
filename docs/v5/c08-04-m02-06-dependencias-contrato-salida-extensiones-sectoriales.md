# SOLQARYN V5 — C08-04.M02.06: dependencias técnicas y contrato de salida de módulos sectoriales

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER (`6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-15. **Repositorio/rama:** `solqaryn/Solqaryn@dev`. **Alcance:** `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** cierre documental de contratos M02, no implementación de un sistema de activación de capabilities.

## 1. Propósito exacto y autorización institucional

El documento maestro V5 FINAL autorizado es `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. `PLAN_MAESTRO!A1273` identifica `C08-04.M02.06`: «Fijar dependencias técnicas y el contrato de salida del lote», dentro de extensiones sectoriales sin duplicar plataforma ni entidades. El entregable de fase pretende contratos de módulos sectoriales; este archivo sólo fija condiciones para implementaciones futuras.

**Preflight comprobado antes del claim:** `PLAN_MAESTRO H1273=PENDIENTE`, `I1273` vacío, `COLA G1273=SI/O1273=SI` y `DAG_TECNICO A1234=C08-04.M02.06, F1234=VALIDADO/G1234=SI`. Se confirmó `C08-04.M01.06` LISTO con A5 independiente y `C08-04.M02.01–.05` LISTO con A5 independiente distinta del autor en cada ID. Para M02.01/.02, la revisión de otra Task Javier se acreditó en esta corrida sobre `cd74fc5482905404978e706e928368bd6a1f136c`, blobs `21e3483972d42c82bf1e2b47e281a21b2b8e9411` y `43371ec2720570932b1bc5de4dac7ddb23aab62e`. Ninguna de estas revisiones modifica permiso de producción o habilita directamente M03 sin gate propio.

## 2. Matriz causal de las cinco preparaciones M02 y M01 terminal

| Unidad | Artefacto fuente vigente por blob Git | Contrato adquirido; lo que NO acredita |
| --- | --- | --- |
| M01.06 | `009a4190314f89ec1516d0a8c4ddc7fe08cb4a96` | Bloqueos BL01–BL16 y rollback para módulos; no implementación |
| M02.01 | `21e3483972d42c82bf1e2b47e281a21b2b8e9411` | Seis objetos conceptuales, INV01–INV14 sobre Empresa, vínculo, RBAC y auditoría; no tablas ni Domain C# |
| M02.02 | `43371ec2720570932b1bc5de4dac7ddb23aab62e` | OP01–OP08, seis DTO y E01–E12 de denegación hipotética; no API HTTP |
| M02.03 | `3133ba320ed164c82239afbd05b74383670d8246` | Claves/índices/fixtures y migraciones **propuestas**; cero MySQL ejecutado |
| M02.04 | `974740a26bfd1e2a9d18b44f6e337504dedb01d0` | Flags/config tipada fail-closed, ámbitos y precedencia; cero flags activados |
| M02.05 | `5f2bc7e71b50fbb3284cf258dbdd683221beda7f` | Eventos, idempotencia y correlación **futuros**; cero dispatcher, webhook, bus o emisión |

Los documentos de M02.03–.05 tienen A5 distinto de su autor y fueron publicados por Task Javier legítima. Se debe revalidar blob y HEAD de cada consumidor en la implementación y repetir checks causales si sus dependencias cambian. Los casos P/N escritos son `FUTUROS_NO_EJECUTADOS`.

## 3. Interfaz de capacidades: quién conserva la autoridad

```text
Angular 22.2.1 UI / Signals / guards
  -> (consulta/solicitud, jamás grant de RBAC)
ASP.NET Core 10 Controller autorizado
  -> Application service: identidad + Empresa efectiva + permiso + versionado
  -> Domain: política tipada allowlisted y transiciones denegadas por defecto
  -> Infrastructure EF Core 10 + Oracle MySQL 8.4 (únicamente tras M03/CI)
  -> Auditoría redactada y rollback aditivo
```

`Empresa`, `UsuarioEmpresa`, `Sucursal`, `Producto`, `Cliente` y `PublicStoreBinding` actuales son **fuentes que se deben reutilizar**, no entidades que este contrato duplique o migre. La pertenencia comercial de un club/gimnasio no equivale a membresía `UsuarioEmpresa`; los flags `enabled` o `allowedActions` de Angular no confieren permisos backend. `EmpresaId=NULL` histórico nunca significa multiempresa global. Host/slug/cabecera del navegador tampoco son autoridad.

## 4. Gates técnicos T01–T16 para futuras acciones reales

| Gate | Evidencia exigida (ninguna sustituida por este documento) |
| --- | --- |
| T01 | Contexto tenant resuelto en backend antes de I/O, con membresía vigente y denegación cross-tenant A/B |
| T02 | JWT válido y RBAC de recurso/acción, no inferido de un entitlement, UI, flag ni host |
| T03 | Allowlist versionada de capabilityKeys y schema, estados desconocidos denegados sin fallback |
| T04 | Identidad de Empresa/Sucursal/objeto verificada, incluyendo registros legacy nullable |
| T05 | Políticas de activación/pausa/retirada aprobadas; ninguna transición se ejecuta por leer un contrato |
| T06 | Separar catálogo global técnico, asignación tenant, configuración y permisos, con identidad única |
| T07 | CAS/expectedVersion + clave idempotente por tenant, acción y payload, con pruebas de concurrencia |
| T08 | Desactivación conserva historia y no borra registros de otras capacidades ni empresas |
| T09 | Auditoría append-only con actor y correlación, sin JWT/PII/credenciales ni datos de B |
| T10 | EF/MySQL con esquema e índices probados mediante DB **efímera** Oracle/8.4, migración aditiva y rollback comprobado |
| T11 | API 401/403/404/409 bien delimitada, conflictos de versión, revocación y no enumeración de B |
| T12 | Angular accesible y ligera, guards sólo de experiencia, tests de teclado/foco/routing proporcionales |
| T13 | Flags tipados con default false, precedencia y aislamiento por Empresa, versión y sucursal |
| T14 | Prohibido activar eventos, dispatcher, worker, webhook, bus, R2, DNS o proveedor por un texto M02 |
| T15 | Compilar/probar .NET/Angular/MySQL y seguridad reales para cada microtarea M03/M04 sobre SHA causal; CI FAILED/CANCELLED/SKIPPED/QUEUED nunca PASS |
| T16 | Scope Lock STRICT, PLAN/COLA/DAG G/O/F, BITACORA, A0–A4, A5 revisión por otra Task Javier y autorización separada antes de QA/main/PROD |

**Resultado de estas puertas:** `NO_GO` sólo al efecto no acreditado. El documento M02.06 puede ser LISTO_DEV con A0–A4 **documentales** reales, sin afirmar que T01–T16 fueron superadas en runtime. Su propio A5 exige Task distinta y un nuevo recálculo DAG para cualquier M03.

## 5. Pruebas futuras de activación y desactivación: casos positivos/negativos

| Caso futuro | Oráculo positivo | Rechazo o prueba negativa |
| --- | --- | --- |
| P01/N01 tenant | A con rol/capability obtiene sólo registros A | A con `resourceId` de B o EmpresaId cliente de B se deniega |
| P02/N02 permiso | Permiso backend existente y asignación tipada permite acción limitada | Flag/entitlement/host no elude RBAC revocado |
| P03/N03 activación | Clave allowlisted/versionada y expectedVersion vigente se registra una sola vez | Clave libre, versión vencida o replay diferente genera conflicto |
| P04/N04 desactivación | Disabled impide nuevas operaciones sin borrar histórico | Caché/UI antigua no reactiva capacidad ni borra B |
| P05/N05 recurso | Vínculo a sucursal/objeto A con propiedad probada | NULL legacy o sucursal B nunca se convierten en permiso global |
| P06/N06 auditoría | Registro redactado de actor/tenant/operación/versión y UTC | JWT, PII, token o datos B en eventos/logs son fallo |
| P07/N07 persistencia | MySQL efímero verifica transacción, índice, idempotencia y rollback aditivo | Carrera, migración destructiva o FK entre tenants no pasa |
| P08/N08 API | JWT/RBAC válido produce DTO tipado con error seguro cuando corresponda | Sin credenciales, no membership o permiso revocado nunca retorna éxito |
| P09/N09 Angular | Estado disabled, loading/error y foco son accesibles | UI nunca infiere grants ni revela catálogo privado en ruta pública |
| P10/N10 caída | Falla de proveedor/CI mantiene estado negado y recuperable | Timeout no habilita capability por fallback |
| P11/N11 correlación | Operación legítima se correlaciona sin datos sensibles | Duplicados de entrega no ejecutan 2 mutaciones |
| P12/N12 seguridad/CI | A0–A4 exact-SHA y A5 ajena cerrados antes de sucesora pertinente | `queued/in_progress/skipped/cancelled/failed` no libera sucesora técnica |

**Estos doce pares son futuros NO_EJECUTADOS**. No se instalan pruebas, esquemas, endpoints ni mecanismos de activación en esta entrega.

## 6. Ocho acciones de retorno y cierre escalonado

| Causa observable | Acción de recuperación, nunca automática por este documento |
| --- | --- |
| R01 — aislamiento A/B falla | Detener activación, denegar y preservar evidencia, rollback del cambio mínimo |
| R02 — RBAC revocado aún concede | Deny-by-default backend, revertir grant inseguro, revisar autorización |
| R03 — CAS/idempotencia falla | No repetir transición, conciliar ledger y verificar carrera |
| R04 — DB/índice/migración falla | Restauración de ensayo efímero previamente versionada; no SQL destructivo improvisado |
| R05 — flag/config o versionado fallan | Desactivar capability sólo tras control de permisos y preservar historia |
| R06 — auditoría/PII expuesta | Detener envío, redactar logs, corregir filtros, no borrar hechos históricos |
| R07 — Angular/cliente queda inconsistente | Respuesta backend autoritativa, invalidar caché y revalidar E2E |
| R08 — Scope Lock, CI o A5 fallan | Conservar VALIDANDO/owner, investigar logs exact SHA y reparar; no forzar DAG |

**Rollback de este propio changeset documental:** `git revert` ordinario del commit con este Markdown y sección completa de `CHANGELOG_AI.md` en dev, sin force push, sin tocar BITACORA histórica ni modificar otro autor. Las reversas hipotéticas de runtime no se realizan aquí.

## 7. Evidencia A0–A5 y condición de salida M02

**A0:** ID exacto y predecesores, blobs/documentos, Skill, Scope Lock y árbol Git inspeccionados; sólo dos Markdown cambiados. **A1:** verificar consistencia de los 16 gates, doce pares positivos/negativos y ocho rollback, como especificación de pruebas futuras; nunca declarar pruebas runtime aprobadas. **A2:** límites tenant/JWT/RBAC/PII/host/ownership/flags denegados por defecto documentados; cero cambios a seguridad ejecutable. **A3:** contrato transversal entre Domain/Application/Infrastructure/API/Angular y M01/M02 sin nueva integración MySQL/HTTP/UI; builds generales `NO_APLICA` a un diff Markdown, nunca PASS obligatorio. **A4:** publicación dev fast-forward/CAS, diff/blobs y checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` ambos `completed/success` sobre SHA causal, BITACORA A:Z/PLAN/COLA/DAG readback. **A5_PENDIENTE** por Task Javier diferente de la autora, aun con A0–A4 aprobados.

**No confundir fase M02 cerrada documentalmente con funcionalidad:** M03 demandará implementación y CI real .NET 10/Angular 22.2.1/MySQL 8.4, aislamiento A/B, autorizaciones y gates exact-SHA. M03 sólo podrá reclamarse si PLAN H=PENDIENTE/I vacío, DAG F=VALIDADO/G=SI, COLA G/O=SI y el Scope Lock actual acredita permiso. Ninguna promoción a QA/main/PROD, cambio de secretos, R2/DNS, pagos, nuevas Tasks o eventos ejecutables se autoriza aquí.
