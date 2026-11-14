# SOLQARYN V5 — C08-04.M01.05: aceptación funcional y técnica observable de extensiones sectoriales

**Unidad:** C08-04.M01.05 · PLAN_MAESTRO fila 1266 · **Responsable:** PRIMARY DEV 04 / JAVIER_CHATGPT / TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09 · **Fecha:** 2026-11-14 · **Rama:** solqaryn/Solqaryn@dev · PROJECT_SCOPE_LOCK=STRICT.

**Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. M01.05: definir aceptación funcional y técnica observable de capacidades por rubro. **Contrato documental**, no implementación, no habilitación de módulos, endpoints, RBAC, migraciones, workers, webhooks ni promoción.

## Fuentes y límites del alcance

| Referencia original de dev | Blob verificado | Alcance |
| --- | --- | --- |
| docs/v5/c08-04-m01-01-alcance-extensiones-por-rubro.md | 41dd2a10d667124c021791278d43ceab3dc02c35 | Doce invariantes de tenancy y no regresión |
| docs/v5/c08-04-m01-02-actores-entradas-salidas-extensiones.md | 606e34598c50a3419d535115679631b705d46f84 | Actores A01–A14 e interfaces I01–I12 futuras |
| docs/v5/c08-04-m01-03-inventario-codigo-contratos-extensiones.md | f34c6b59328d9d5a9e106e55eedeb250d50fb900 | Once fuentes concretas y diez brechas por demostrar |
| backend/src/Domain/Entities/Empresa.cs | c44940df7c30983a4c23b93338e0cb9367962069 | Identidad de empresa y estado activo/inactivo |
| frontend/src/app/app.routes.ts | 2f560c14693455b79ac4546e9333fb74586ef234 | Rutas públicas y guards privados, no autoridad backend |
| frontend/src/app/core/guards/permiso.guard.ts | 1052bd38041055cc81e84a14d5fe0364e169b1fe | Control UI que debe coexistir con RBAC backend |

El catálogo, agenda, membresía comercial, catálogo y sitios futuros comparten Empresa, UsuarioEmpresa y permisos existentes; una capability no concede permisos. La pertenencia tenant se prueba en backend y nunca se infiere de host, slug, payload o X-Empresa-Id. Criterios definidos abajo son **FUTUROS_NO_EJECUTADOS**, no resultados de CI ni evidencia de rutas ejecutables.

## Catorce criterios positivos y negativos de aceptación (FUTUROS_NO_EJECUTADOS)

| AC | Positivo futuro observable | Negativo obligatorio (fail-closed) | Prueba causal cuando se implemente |
| --- | --- | --- | --- |
| AC01 | A activa se identifica con contexto tenant verificable | N01: A usa EmpresaId ajeno B o empresa inactiva | API .NET y MySQL A/B |
| AC02 | Operador A con RBAC y capability accede sólo al recurso autorizado | N02: flag UI sin permiso backend o rol revocado rechaza | API JWT/RBAC |
| AC03 | Capability allowlisted de A se activa con versión, actor y motivo | N03: clave desconocida, campo arbitrario o JS/SQL no se acepta | API y persistencia |
| AC04 | Desactivación impide nuevas altas de A sin perder historial | N04: no borra reservas, cobros ni recursos de B | MySQL transaccional |
| AC05 | Reserva A consulta horario, sucursal y recurso A | N05: ID de reserva, recurso o sede B no revela contenido | API A/B |
| AC06 | Membresía comercial vigente se evalúa sin confundirla con UsuarioEmpresa | N06: vencida/suspendida no otorga rol ni entitlement SaaS | Integración membresías |
| AC07 | Producto y catálogo A respetan ownership verificado | N07: EmpresaId legacy NULL o registro B no es global | Repositorio/MySQL |
| AC08 | Host público normalizado resuelve binding activo y contenido público mínimo | N08: host, forwarded host o slug manipulado no otorga acceso privado | API / proxy / E2E |
| AC09 | Angular muestra acciones accesibles según proyección y permisos | N09: alterar ruta, header o estado UI no evita backend RBAC | Angular + API |
| AC10 | Operación repetida con key idéntica es idempotente y auditada | N10: replay o doble envío no repite efecto ni auditoría | Concurrencia/DB |
| AC11 | Timeout/caché no verificable produce decisión segura | N11: error o caché vieja no reactiva capacidad revocada | Fallos controlados |
| AC12 | Soporte autorizado ve trazas mínimas de su ámbito | N12: sin PII, secretos, tokens ni identificadores de B en logs | Pruebas seguridad |
| AC13 | Vista ligera cumple teclado, foco, 320px y reduced motion | N13: ocultar botón no autoriza endpoint y no bloquea teclado | Playwright/a11y |
| AC14 | Rollback retorna versión válida de A sin alterar B | N14: reversión parcial o estado inconsistente no es éxito | Integración/rollback |

Cada aceptación deberá vincular escenario, fixtures A/B, respuesta/status, resultado persistente cuando corresponda, SHA y logs saneados. NO afirmar PASS de xUnit, MySQL o Playwright por definir esta tabla. Ningún check SKIPPED/CANCELLED/IN_PROGRESS/FAIL satisface una prueba obligatoria.

## Requisitos no funcionales NFR01–NFR10

- **NFR01–02:** JWT autenticado, RBAC server-side por recurso/acción y revocación.
- **NFR03:** denegación sobre IDs ajenos entre tenants A/B para lecturas, mutaciones y exportación.
- **NFR04:** ownership de entidades históricas nullable resuelto con trazabilidad, no por inferencia.
- **NFR05:** caché partitionada por tenant/versión y invalidación fail-closed.
- **NFR06:** idempotencia, auditoría saneada, correlación, rollback consistente.
- **NFR07:** reuso de catálogo, storefront, reserva y membresía sin duplicar identidad ni entidad Empresa.
- **NFR08:** teclado, contraste, foco visible, 320px y preferencia reduced motion.
- **NFR09:** medición de latencia y peso real sólo al disponer de una implementación con fixtures; ningún SLO inventado.
- **NFR10:** ausencia de permisos de QA/main/PROD por completar M01 o A5 en dev.

## Matriz de pruebas futuras y fases

| Fase | Entrega pertinente | Gate exigible |
| --- | --- | --- |
| M02 | Entidades, ownership, DTO, API contract y persistencia | Validación de invariantes, versiones y errores seguros |
| M03 | Código .NET 10/Angular 22.2.1 y MySQL cuando aplique | CI exact-SHA, positivos/negativos, tenancy A/B, JWT/RBAC, integración efímera |
| M04 | UI sectorial ligera y accesible | Playwright real cuando haya superficies y navegación |
| M05 | Suites/regresión, seguridad, integración y fallos | completed/success de checks causales exact-SHA |
| M06 | Reversión, dependencias, seguridad y evidencia | A0–A4 y A5 independiente; QA/main/PROD requieren otra autorización |

Esta M01.05 sólo define esos requisitos: no crea nuevo módulo, base de datos, endpoint, feature flag vivo, cuenta, Task, webhook, trigger, infraestructura o servicio de pago.

## Impacto A0–A5 y rollback

**Diff permitido:** nuevo Markdown y sección autocontenida de CHANGELOG_AI.md. Sin código ejecutable, migración, permiso, cambio de tenant, MySQL, Cloudinary/R2, DNS, QA/main/PROD o datos reales.

**A0:** Skill canónica, Scope Lock STRICT, fuente, DAG F VALIDADO/G SI, COLA G/O SI, CAS owner y archivos excluidos.
**A1:** coherencia documental de 14 criterios y negativos con M01.01–03; pruebas futuras no ejecutadas.
**A2:** riesgos documentados JWT/RBAC/tenant A/B, PII, replays, host confiable y fail-closed; ninguna certificación runtime.
**A3:** trazabilidad documental Domain/Application/Infrastructure/API/Angular; compilación, MySQL y browser NO_APLICA para diff Markdown.
**A4:** publicar fast-forward/CAS en dev, verificar diffs/blobs y checks Scope Lock y procedencia V5 completed/success del SHA exacto, con BITACORA/PLAN/COLA readback.
**A5:** PENDIENTE, por otra Task Javier distinta de autora; no autopromoción.
**Rollback:** git revert normal con revalidación de HEAD, sin force-push ni reescritura histórica.
