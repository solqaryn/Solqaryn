# SOLQARYN V5 — C08-04.M02.05: eventos, idempotencia, correlación y límites futuros de capacidades sectoriales

**2026-11-15. Autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`). **Autoridad:** STANDARD V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN/COLA/DAG institucional Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, Skill canónica y PROJECT_SCOPE_LOCK STRICT en `solqaryn/Solqaryn@dev`.

**Frontera:** contrato de M02 para extensiones por rubro; no implementa schemas MySQL, migraciones, API, Angular, flags vivos, JWT/RBAC, eventos, workers, colas, webhooks ni despliegues. Criterios son `FUTUROS_NO_EJECUTADOS`, no resultados de runtime. A5 sólo otra Task Javier.

**Antecedentes reales:** `docs/v5/c08-04-m01-01-alcance-extensiones-por-rubro.md` blob `41dd2a10d667124c021791278d43ceab3dc02c35` NR01–NR12; `...m01-03-inventario-codigo-contratos-extensiones.md` blob `f34c6b59328d9d5a9e106e55eedeb250d50fb900` (11 fuentes y 10 brechas); `...m01-05-criterios-aceptacion-extensiones-sectoriales.md` blob `9ad425e66e7839d38a2416cf97615b20e0e129a2` (AC01–AC14, N01–N14 futuros); `...m01-06-bloqueos-rollback-modulos-sectoriales.md` blob `009a4190314f89ec1516d0a8c4ddc7fe08cb4a96` (BL01–BL16). La existencia de fuentes y blobs no acredita una capability operacional.

## 1. Contrato futuro, cero dispatcher operativo

Este documento no instala ni invoca eventos reales: **no implementar eventos todavía**. Una arquitectura futura de transiciones de capabilities exigiría autorización de proyecto, analizador de permisos, idempotencia durable, cuotas y rollback. Las 6 Tasks Javier no son un bus, y registrar una transición PLAN no despierta otro ejecutor fuera de su programación.

| Tipo de evento propuesto (no emitido) | Emisor futuro | Consumidor autorizado hipotético | Invariante |
| --- | --- | --- | --- |
| `sector.capability.requested.v1` | backend autenticado | validador de configuración | Solicitud no equivale a grant |
| `sector.capability.enabled.v1` | transacción aprobada | proyección de capacidades | Verificar RBAC/tenant antes de nueva acción |
| `sector.capability.disabled.v1` | transacción aprobada | UI y control de nuevas altas | Preservar historia y revocar caché |
| `sector.capability.failed.v1` | control transaccional futuro | auditoría saneada | Error no activa capacidad |
| `sector.capability.reviewed.v1` | auditoría independiente futura | evidencia de cumplimiento | No sustituye A5 de la Task autora |

Los nombres son conceptos, no contratos C#/TypeScript registrados, colas reales ni permisos API. Ningún evento se dispara desde este documento.

## 2. Envelope, scope y consistencia futura

| Campo | Tipo conceptual | Prohibición/validación |
| --- | --- | --- |
| `schemaVersion` | entero fijo versionado | Rechazar desconocido |
| `eventId` | UUID único | Sin suplantar RUN_ID nativo |
| `idempotencyKey` | hash de tenant+capability+request+versión | Duplicado mismo body no repite efecto |
| `correlationId` | UUID sintético | Nunca actúa como credencial |
| `empresaId` | ID empresa verificado backend | A no puede emitir evento de B |
| `actorId` | identidad aprobada | Rol/revocación verificados antes de actuar |
| `capabilityKey` | enum allowlisted | No SQL/JS ni sectores desconocidos |
| `expectedVersion` | entero para CAS | Conflicto revierte/deniega con error seguro |
| `occurredAt` | UTC | No falsificar tiempo de proveedor |
| `result` | success, rejected, pending | Sólo confirmación transaccional acredita success |
| `evidenceSha` | commit hash verificado | No mezclar evidencia de HEAD distintos |
| `payload` | datos mínimos sanitizados | Sin JWT, PII, secrets ni datos B |

## 3. Controles de idempotencia y límites futuros

1. Activación idéntica produce un único efecto y un evento trazable, no múltiples grants.
2. Misma clave con payload distinto rechaza conflicto y conserva auditoría previa.
3. Actor revocado, tenant B, sede externa o clave no allowlisted resultan en denegación sin emitir éxito.
4. Desactivación bloquea nuevas altas y conserva datos históricos, sin borrar estados legítimos.
5. Orden fuera de secuencia y versión vieja no reactivan capacidad retirada.
6. Timeout, caché inválida o reintento agotado dejan estado pendiente/denegado, nunca activación silenciosa.
7. Cuota y tamaño máximo requerirán medición real y límite de proveedor; no inventar valores operativos.
8. Errores y correlación no pueden filtrar token, cliente, membresía privada o data de B.
9. Sólo backend sería autoridad para publicar, no Angular ni un aviso de Sheets.
10. No se crea dispatcher, webhook, broker, retry worker, automations nuevas ni tarea programada.
11. Prueba futura deberá incluir duplicados, replay, orden inverso, A/B, cancelación y rollback.
12. Registro BITACORA histórico sigue append-only, nunca se reemplazan eventos con dedupe posterior.

## 4. Validación, rollback y puertas posteriores

A0: alcance y diff documental con fuentes C08 M01. A1: doce reglas y diez pares hipotéticos definibles, **no ejecutados**. A2: revisión conceptual tenant/RBAC, no prueba runtime ni de negocio. A3: integración real NO_APLICA (ninguna instalada). A4: diff, Scope Lock y V5 por SHA exacto `completed/success`; si falta, VALIDANDO. A5 exclusivamente otro Javier Task ID. Rollback sólo `git revert` documental. M02.06 y M03 futuras dependen del DAG vivo con LISTO+A5 de todos los predecesores y no se habilitan por este texto.
