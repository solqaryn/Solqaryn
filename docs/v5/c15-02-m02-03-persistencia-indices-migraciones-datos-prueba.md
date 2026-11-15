# SOLQARYN V5 FINAL — C15-02.M02.03: persistencia, índices, migraciones y datos de prueba de auditoría arquitectónica

**Alcance:** SOLQARYN dev, 14 de noviembre de 2026 (America/Tegucigalpa). **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **PROJECT_SCOPE_LOCK=STRICT**.

## 1. Autoridad, dependencias y límites

Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; fila C15-02.M02.03 de PLAN_MAESTRO/COLA y DAG_TECNICO. Prerrequisito técnico explícito: C15-02.M01.01–06 LISTO con A5 independiente, DAG F=VALIDADO y G=SI. M02.01 aporta entidades lógicas ENT01–ENT16 e invariantes INV01–INV16; M02.02 sigue su propio cierre y no se declara automáticamente certificado por este documento.

**Resultado documental, no implementación.** Se definen contratos futuros de persistencia, índices, pruebas con datos ficticios y estrategia reversible. No se crean tablas, columnas, entidades EF, migraciones, semillas, usuarios, tokens, recursos cloud, workers ni endpoints. MySQL real y servicios externos no se consultan ni modifican. Los escenarios siguientes son **FUTUROS_NO_EJECUTADOS** y no acreditan restauraciones, aislamientos ni rendimiento medidos.

Arquitectura actual: API ASP.NET Core 10, capas Domain/Application/Infrastructure/API, EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9 declarado en Infrastructure, MySQL, JWT/RBAC y controles tenant backend. Hostinger, Cloudflare, R2, DNS y corte de proveedor siguen siendo objetivos futuros con autorización separada.

## 2. Contrato lógico de registros y propiedad (DAT01–DAT12)

| ID | Registro conceptual | Identidad/relación | Regla de aislamiento |
| --- | --- | --- | --- |
| DAT01 | TenantVerificado | EmpresaId de membresía autenticada | Nunca confiar en cabecera/slug cliente |
| DAT02 | OperadorAutorizado | UserId, rol, vigencia, DAT01 | Denegar permisos revocados |
| DAT03 | RecursoExterno | providerRef opaca, DAT01, clase, entorno | NO_ACREDITADO hasta inventario real |
| DAT04 | DependenciaServicio | DAT03 origen/destino, contrato y versión | No cruzar límites de tenant |
| DAT05 | ArtefactoAuditable | commit SHA, tree/digest, entorno | Exact-SHA o NO_GO |
| DAT06 | EsquemaParticionado | DAT01, tipo, modelo y versión | FK compuesta con tenant |
| DAT07 | RespaldoManifestado | DAT06, checksum, custodia, fecha UTC | No equivale a restore probado |
| DAT08 | PruebaRestauración | DAT07, entorno efímero, resultado | Sólo PASS con comparación observada |
| DAT09 | ObjetoArchivable | DAT01, objeto, hash, TTL y owner | No presumir existencia ni acceso |
| DAT10 | DominioAutorizado | DAT01, host normalizado, estado TLS | Binding no nace del navegador |
| DAT11 | DecisiónReversible | autorización diferenciada, DAT07, DAT08 | Sin paso de corte por documento |
| DAT12 | PruebaYRevisión | source SHA, A0–A4, A5 autor/revisor | A5 ajena y EVENT_ID único |

Todos los identificadores anteriores son nombres de diseño, no tablas existentes ni cambios del esquema comercial. EmpresaId NULL, registros huérfanos y pertenencia no verificable permanecen en cuarentena; no se reasignan por heurística.

## 3. Índices y constraints propuestos para revisión futura (IDX01–IDX12)

| ID | Clave o restricción conceptual | Prueba futura |
| --- | --- | --- |
| IDX01 | DAT01: clave de tenant única y estable | Duplicidad rechazada |
| IDX02 | DAT02: (EmpresaId, UserId, vigencia) | Mismo usuario B no concede A |
| IDX03 | DAT03: (EmpresaId, providerRef, entorno) único | Recurso B invisible a A |
| IDX04 | DAT04: FK compuesta por EmpresaId de ambos extremos | Unión entre tenants rechazada |
| IDX05 | DAT05: (entorno, commitSHA, digest) | SHA inconsistente rechazado |
| IDX06 | DAT06: (EmpresaId, esquemaVersion, tipo) | NULL legacy bloqueado |
| IDX07 | DAT07: (EmpresaId, checksum, timestampUTC) | Backup sin hash inválido |
| IDX08 | DAT08: (EmpresaId, respaldoId, intentoId) | Restore ajeno rechazado |
| IDX09 | DAT09: (EmpresaId, objetoId, versión) | Enlace vencido no reutilizable |
| IDX10 | DAT10: host normalizado único con ownership acreditado | Host duplicado no se asigna |
| IDX11 | DAT11: (entorno, cambioId, autorizaciónVersion) | Sin grant no hay corte |
| IDX12 | DAT12: EVENT_ID e IDEMPOTENCY_KEY únicos | Doble evento rechazado |

No se garantiza compatibilidad DDL ni selectividad real. Antes de materializar cualquier índice: inspeccionar esquema existente, proveedor Oracle EF, collation MySQL 8.4, cardinalidad, coste, uso de índices, transacciones y ventanas de mantenimiento; ejecutar únicamente en servicio efímero autorizado. Evitar almacenar JWT, secretos, credenciales, PII no necesaria o bytes de archivos en evidencia.

## 4. Plan de migración futuro condicionado (MIG01–MIG08)

1. **MIG01:** inventario real, propietario y autorización explícita por proveedor y entorno; ausencia de evidencia implica NO_GO.
2. **MIG02:** comparar modelo conceptual con `AppDbContext` y migraciones existentes sin mutar esquema ni datos.
3. **MIG03:** clasificar datos con tenant ambiguo/NULL en cuarentena; no adjudicar automáticamente.
4. **MIG04:** diseñar migración aditiva forward + rollback compensatorio; verificar compatibilidad con versión publicada.
5. **MIG05:** ejecutar ensayo MySQL 8.4 **efímero** sólo bajo scope técnico posterior y permiso separado; incluir FK A/B y constraints.
6. **MIG06:** generar backup verificable, checksum y prueba de restore independiente con conteos y hash.
7. **MIG07:** comprobar autorización/observabilidad, impacto y regresiones exact-SHA, incluida CI no cancelada.
8. **MIG08:** decisión GO/NO_GO con A5 externa, rollback acreditado y autorización de cambio de entorno; el presente M02 nunca permite cutover.

La documentación no satisface por sí misma ninguna de estas etapas; si falta evidencia, conservar NO_EJECUTADO en vez de PASS.

## 5. Oráculos de datos ficticios y negativos (FX01–FX12; NO_EJECUTADOS)

| ID | Positivo a probar en futuro CI | Negativo bloqueante |
| --- | --- | --- |
| FX01 | Tenant A lee sólo registros de A | A solicita PK de B: 403/404 sin PII B |
| FX02 | Mismo tenant padre-hijo válido | FK A→B se rechaza |
| FX03 | Rol vigente y membresía verificada | Token con permiso revocado se deniega |
| FX04 | Registro tenant conocido se procesa | EmpresaId NULL o huérfano queda aislado |
| FX05 | Índice de recurso con owner correcto | providerRef de B no es enumerable |
| FX06 | Checksum del respaldo coincide | Manifiesto sin digest es NO_GO |
| FX07 | Restore efímero preserva conteos | Cambio de hash bloquea avance |
| FX08 | URL propia, firmada y vigente funciona | URL expirada o tenant B obtiene cero bytes |
| FX09 | Host autorizado y certificado válido | Host spoofing o TLS inválido no selecciona tenant |
| FX10 | Evento con idempotencia por tenant | Replay del mismo ID o de B se rechaza |
| FX11 | CI completed/success en SHA preciso | CANCELLED/SKIPPED/in_progress no son PASS |
| FX12 | A5 de Task distinta y evento único | Autoaprobación/duplicado impide dependencia |

Fixtures sintéticos mínimos: tenants ficticios A y B, roles vigentes/revocados, registro sin tenant, pares padre/hijo A/B, manifiestos con hash válido/inválido, URL caducada, host no autorizado, evento repetido y snapshots con sumas de control discordantes. No incluir correos, nombres, tokens, secretos o datos comerciales reales.

## 6. Alcance de pruebas, riesgo y reversión

**A0 estática:** comparar STANDARD V5, M01.06, ENT/INV de M02.01, DAG/PLAN/COLA y arquitectura vigente. **A1 documental:** comprobar DAT01–12, IDX01–12, MIG01–08 y FX01–12 con pares positivo/negativo definidos; estas pruebas se han **diseñado**, no ejecutado en un motor. **A2 seguridad:** sólo diseño fail-closed de tenant A/B, JWT/RBAC, FK, NULL y secretos; no se modifica runtime ni se atribuye pentest. **A3 integración:** coherencia de contratos con capas Domain/Application/Infrastructure, autoría, dependencias y rollback, sin conexión MySQL o proveedores. **A4 publicación:** diff limitado a este documento y CHANGELOG, fast-forward con control de SHA, checks Scope Lock y V5 completed/success sobre SHA exacto, readback institucional. **A5:** revisión por otra Task Javier distinta de este autor.

Riesgos principales: garantías DB no probadas, índices sin estadísticas, tenancy por filtros opcionales, datos legacy NULL, restore sólo supuesto, corte de DNS/archivos sin rollback real y accidental habilitación de DAG por un texto. Todos mantienen **NO_ACREDITADO** hasta pruebas técnicas futuras.

**Rollback de esta entrega:** git revert normal de los dos archivos Markdown y evento compensatorio append-only; no force-push, reset, cambios de fórmulas, QA/main/PROD ni edición de historia BITACORA.
