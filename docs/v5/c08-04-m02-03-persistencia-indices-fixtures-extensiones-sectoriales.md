# SOLQARYN V5 — C08-04.M02.03: persistencia, índices, migraciones reversibles y fixtures conceptuales para rubros

**2026-11-15. Autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`). **Autoridad:** STANDARD V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN/COLA/DAG institucional Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, Skill canónica y PROJECT_SCOPE_LOCK STRICT en `solqaryn/Solqaryn@dev`.

**Frontera:** contrato de M02 para extensiones por rubro; no implementa schemas MySQL, migraciones, API, Angular, flags vivos, JWT/RBAC, eventos, workers, colas, webhooks ni despliegues. Criterios son `FUTUROS_NO_EJECUTADOS`, no resultados de runtime. A5 sólo otra Task Javier.

**Antecedentes reales:** `docs/v5/c08-04-m01-01-alcance-extensiones-por-rubro.md` blob `41dd2a10d667124c021791278d43ceab3dc02c35` NR01–NR12; `...m01-03-inventario-codigo-contratos-extensiones.md` blob `f34c6b59328d9d5a9e106e55eedeb250d50fb900` (11 fuentes y 10 brechas); `...m01-05-criterios-aceptacion-extensiones-sectoriales.md` blob `9ad425e66e7839d38a2416cf97615b20e0e129a2` (AC01–AC14, N01–N14 futuros); `...m01-06-bloqueos-rollback-modulos-sectoriales.md` blob `009a4190314f89ec1516d0a8c4ddc7fe08cb4a96` (BL01–BL16). La existencia de fuentes y blobs no acredita una capability operacional.

## 1. Modelo conceptual de propiedad sin duplicación

| Elemento futuro | Propiedad y relación | Regla innegociable |
| --- | --- | --- |
| `CapabilityDefinition` | Clave versionada global y allowlist de rubros | Nunca duplicar Empresa, Usuario, Cliente o Producto |
| `TenantCapabilityGrant` | Empresa existente + capability + versión + estado | Activación requiere autorización backend; flag no es RBAC |
| `CapabilityConfiguration` | Empresa + clave allowlisted + versión de schema | No SQL, JavaScript, HTML ni permisos arbitrarios |
| `CapabilityAuditRecord` | Empresa, actor autorizado, efecto, SHA y correlación | Append-only, sin JWT o PII en log |
| `SectorResourceBinding` | Recurso existente + Empresa/Sucursal verificadas | Prohibido convertir `EmpresaId=NULL` heredado en acceso global |
| `CapabilityTransition` | Versión previa/nueva y control de concurrencia | Desactivar preserva historia; fallos deniegan nuevas operaciones |

Estos son **nombres de diseño**, no clases EF Core, tablas, consultas o indices existentes. `Empresa.cs`, `UsuarioEmpresa.cs`, `Producto.cs`, `Sucursal.cs`, `PublicStoreBinding.cs` son entidades reales descritas en M01.03 y deben ser reutilizadas después de verificar sus contratos actuales. Las membresías comerciales no deben confundirse con `UsuarioEmpresa` ni los entitlements del SaaS.

## 2. Restricciones y consultas futuras

| Consulta/superficie | Índice compuesto conceptual | Comportamiento de denegación |
| --- | --- | --- |
| Grant por empresa | `EmpresaId,CapabilityKey,Version` | Sin membresía y rol no hay acceso aunque exista grant |
| Estado operativo | `EmpresaId,CapabilityKey,State` | Incógnito, suspendido o caché inválida deniega |
| Enlace de recurso | `EmpresaId,ResourceKind,ResourceId` | ID de tenant B o sucursal ajena no se revela |
| Auditoría depurada | `EmpresaId,CorrelationId,OccurredAt` | Sin tokens, datos personales ni logging intertenant |
| Reintento | `EmpresaId,OperationKey,RequestHash` | Mismo key con cuerpo distinto rechaza conflicto |

Ningún índice fue creado. Una futura migración deberá ser aditiva, validada por EF Core 10/MySQL 8.4 en CI efímera y con rollback probado; requerirá autorización propia M03.04, sin modificar fuentes productivas. Un esquema SQL no se acepta hasta conocer cardinalidad y restricciones reales. Reutilizar la historia comercial y evitar borrados en cascada de recursos existentes.

## 3. Diez fixtures de validación futura, no ejecutada

| Fixture | Permiso observable futuro | Negativo obligatorio |
| --- | --- | --- |
| F01 A activo | Capability allowlisted, RBAC y Empresa vigente | Empresa B devuelve acceso denegado |
| F02 A revocado | Sin nuevas operaciones tras revocación | Token válido antiguo no reabre permiso |
| F03 B activo | B sólo ve datos de B | A no infiere IDs por listado o error |
| F04 sede externa | Usuario A opera sucursal de A | Recurso de otra sede sin permiso se rechaza |
| F05 legado NULL | Propiedad histórica se resuelve por evidencia | NULL no se trata como global |
| F06 apagado | Histórico sigue disponible según RBAC | No se crean nuevos recursos ni se borran datos |
| F07 replay | Mismo request es idempotente | Payload diferente con misma clave no duplica |
| F08 host | Binding activo normalizado permite sitio público | Host manipulado nunca concede permiso privado |
| F09 configuración | Sólo allowlist tipada y versionada | SQL/JS/campo desconocido falla |
| F10 concurrencia | Una transición con versión esperada gana | Dos activaciones simultáneas no duplican grants |

A0: diffs, fuentes y matriz de relación. A1: 10 oráculos documentales, no xUnit ejecutado. A2: límites tenant A/B, RBAC, sucursal, privacidad revisados como diseño, NO como pruebas de integración. A3: MySQL/EF/API inexistentes en el cambio, NO_APLICA. A4: exigir Scope Lock y V5 exact SHA `completed/success`, documento y CHANGELOG en dev. Reversión: revert Git normal, sin migración de datos. A5 por revisor Javier distinto. Sucesora M02.06 sigue bloqueada por LISTO+A5 M02.01–05 según DAG vigente.
