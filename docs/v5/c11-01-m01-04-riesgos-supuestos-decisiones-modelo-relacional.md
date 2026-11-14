# SOLQARYN V5 — C11-01.M01.04: riesgos, supuestos y decisiones de normalización relacional

**Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT / Task 6af16bbad26881918c283abe06e1f32a. **Ámbito:** C11-01 Modelo relacional normalizado, M01.04, PLAN_MAESTRO fila 1589. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; PLAN/COLA/DAG VALIDADO/SI; repositorio solqaryn/Solqaryn/dev y PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Regla de evidencia y no-alcance

Esta tarea establece riesgos potenciales, supuestos por refutar y decisiones aún pendientes para el futuro modelo relacional normalizado. Las unidades C11-01.M01.01–03 anteriores definieron objetivo, actores/superficies y un inventario de archivos con blobs exactos; aquí se conservan esos límites. Ningún riesgo R se declara vulnerabilidad confirmada, ningún supuesto S se reporta validado, ninguna decisión D se presenta como aprobada por el propietario. Este documento **no ejecuta** cambios SQL, migraciones EF/MySQL, consultas Aiven, backfills, logs ni pruebas runtime.

Las fuentes relevantes incluyen Empresa.cs (c44940df7c30983a4c23b93338e0cb9367962069), Cliente.cs (8c6d4c4453e44f8c0be9475340465e1aadb3f787), Sucursal.cs (8746b11fa30cdac0b97241b6221100bedd813b35), UsuarioEmpresa.cs (f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c), AppDbContext.cs (c4b32f6637f1b4426655bb0cd12af8dd9d229866), CommercialTenantReadFilterModel.cs (f2422beec8f2d054c20184d073bad668513fa53b), CommercialTenantOwnershipGraphPhysicalConstraints.cs (1d33d4d0ad321ac6f891e85064e2f2900a980f1b), CanonicalMySqlPhysicalContract.cs (f590f85fa5e372051ef3d2a3fab9552f6e7c8cbb) y la migración M1EndurecerIntegridadRelacional (7480fa634101ab9740f0183f8f937ee3e11d9fbb). Un blob de código es evidencia de texto versionado, **no de despliegue ni ejecución exitosa**.

## Riesgos R01–R16 — POTENCIALES, NO_CONFIRMADOS

| Riesgo | Hipótesis de fallo futuro | Mitigación o prueba de salida |
| --- | --- | --- |
| R01 | EmpresaId manipulable en cliente produce lectura ajena | Contexto backend firmado y tests tenant A/B |
| R02 | Cliente.EmpresaId NULL histórico se vuelve accesible sin propietario | Inventario, cuarentena y fail-closed |
| R03 | Sucursal legacy sin EmpresaId acaba reasignada tácitamente | Resolver owner con transición explícita y auditable |
| R04 | Rol heredado de Usuario suplanta UsuarioEmpresa.RolId | Matriz de membresía y autorización negativa |
| R05 | FK simple permite asociar hijo de tenant A al padre B | FK compuesta EmpresaId + Id y prueba física |
| R06 | Índice global rompe unicidad válida por empresa | Revisar índice, ámbito, colisones y SQL EXPLAIN |
| R07 | FK/índice declarados en código no existen en MySQL efectivo | Auditoría information_schema por entorno permitido |
| R08 | Migración expansiva rompe consultas durante transición | Expand/contract y compatibility tests efímeros |
| R09 | Backfill irreversible destruye referencia comercial histórica | Backup, tabla de reconciliación y revert ensayado |
| R10 | Transacciones parciales desalinean inventario, ventas y factura | Integración real de commit/rollback |
| R11 | Filtro EF no cubre SQL raw, jobs o tienda pública | Contratos de autorización por cada vía |
| R12 | Soporte temporal ejecuta SaveChanges | Prueba negativa y guard server-side |
| R13 | DDL de comprobación corre contra Aiven productivo | Restricción a servicio MySQL efímero en CI |
| R14 | Logs de SQL o excepción filtran PII/secretos | Redacción y pruebas de no divulgación |
| R15 | Reintento o concurrencia introduce duplicados en claves naturales | Índice compuesto + idempotencia comprobada |
| R16 | Documentar requisitos se trata como PASS técnico o A5 | Readback de audit trail y checks exact-SHA |

Estos son escenarios que justifican pruebas, no bugs que se hayan reproducido.

## Supuestos S01–S10 — NO_VERIFICADOS

- **S01:** cada tabla comercial tendrá propiedad empresarial detectable; verificar matriz por entidad/relación.
- **S02:** los históricos NULL son finitos y reconciliables sin pérdida; verificar conteos reales bajo autorización.
- **S03:** toda consulta autorizada atraviesa el guard EF; comprobar rutas que usan SQL raw, jobs y tiendas públicas.
- **S04:** constraints físicas compuestas se aplicaron en cada DB vigente; comprobar migration history + information_schema.
- **S05:** el rol relacional efectivo de UsuarioEmpresa sustituye todas las lecturas legacy; auditar consumidores.
- **S06:** unicidad global debe migrar a unicidad empresarial para todas las claves naturales; revisar contrato por tabla.
- **S07:** MySQL objetivo y proveedor Oracle soportan cada operación DDL propuesta; demostrar en CI efímera.
- **S08:** una reversión DB conservará registros y constraints antiguas; probar Up/Down y checksums.
- **S09:** la escala de datos real justifica los índices propuestos; medir antes de imponer límites.
- **S10:** cualquier requerimiento de Cloudflare/Hostinger es futuro y sin autorización productiva; distinguir roadmap de permiso.

La prueba de cada supuesto se reserva a un ID técnico y ambiente autorizado; S01–S10 no son afirmaciones de estado físico.

## Decisiones D01–D10 — PENDIENTES, no autorización ejecutable

| Decisión | Alternativas a resolver | Evidencia previa obligatoria |
| --- | --- | --- |
| D01 | Nullable legacy temporal vs obligatoriedad gradual | Censo de filas y consumidores |
| D02 | FK compuesta en DB vs guard transaccional complementario | Prueba cross-tenant y plan de migración |
| D03 | Índice tenant-scoped específico por entidad | EXPLAIN, colisiones, volumen representativo |
| D04 | Expansión de columnas por etapa vs backfill por batches | Lectores/escritores activos y rollback |
| D05 | Tratamiento de huérfanos y errores de referencia | Reconciliación auditable sin hard-delete automático |
| D06 | Fuente de rol efectivo y transición de consumidores legacy | Matriz RBAC y test de denegación |
| D07 | Estrategia de transacción para ventas, inventario, pagos | Invariantes contables y fallos inyectados |
| D08 | Aislamiento de soporte y lectura pública fuera de EF | Matriz de endpoints, jobs y capabilities |
| D09 | Forma de reportar errores y trazas SQL seguros | Pruebas de redacción PII/secretos |
| D10 | Gates para ejecutar migración/deploy posterior | Permiso explícito, CI exact-SHA, A5 y rollback probado |

**D01–D10 no están resueltas** en esta fase; son decisiones técnicas/operativas que exigen evidencia específica y, si procede, autorización nueva antes de tocar datos o producción.

## Escenarios observables futuros (NO_EJECUTADOS)

Positivos: **P01** dos tenants ven solo sus filas; **P02** una fila legacy válida permanece durante expansión; **P03** FK empresa+ID acepta padre/hijo mismo owner; **P04** compatibilidad con el cliente Angular y API anterior; **P05** venta+inventario+factura se confirman de forma atómica; **P06** migration Up/Down en MySQL efímero conserva conteos; **P07** query indexada cumple presupuesto definido; **P08** soporte consulta sin permitir SaveChanges.

Negativos: **N01** cliente usa EmpresaId de otro tenant; **N02** descendiente con FK padre ajeno; **N03** históricos NULL reciben permiso sin reconciliación; **N04** rol legacy prevalece sobre membresía; **N05** soporte escribe; **N06** SQL raw salta filtro; **N07** reintento duplica clave natural; **N08** falla transacción dejando stock parcial; **N09** migración sin backup/rollback; **N10** logs revelan PII/token; **N11** CI queued/cancelled se contabiliza PASS; **N12** documentación V5 se usa como autorización QA/main/PROD.

Ningún P01–P08/N01–N12 se ha ejecutado con esta entrega; clasificar como FUTURO_NO_EJECUTADO, jamás prueba pasada.

## Dependencias, auditorías y rollback

**Componente:** M01 documental de modelo relacional C11-01. **Capas consumidoras:** Domain, Application, Infrastructure, API, Angular, pero no se alteran. **Datos/migraciones/SQL:** ninguno. **Seguridad:** solo análisis de JWT/RBAC/tenant/PII. **Jobs, servicios, eventos:** ninguno creado. **Observabilidad:** análisis de error/PII sin introducir logs. **Cambio canónico de arquitectura:** ninguno.

**A0:** verificar fuente de plan, Skill/HEAD, identidad propia y DAG PREPARACIÓN VALIDADO/SI, CAS exclusivo, diff dos Markdown. **A1:** validar trazabilidad R01–R16/S01–S10/D01–D10, estados de evidencia y escenarios; **A2:** revisar fail-closed tenant A/B, históricos y acceso read-only; **A3:** arquitectura Domain/Application/Infrastructure y contratos SQL/EF sin ejecución; **A4:** publicación fast-forward CAS a dev, checks exact-SHA verify-solqaryn-project-scope y V5 provenance completed/success y readback PLAN/COLA/BITACORA. Las suites runtime ajenas al Markdown se justifican NO_APLICA, no PASS. **Rollback:** git revert del commit documental conservando la historia y sin tocar la base.

**A5_PENDIENTE:** otra Task Javier debe hacer la revisión independiente. Ni esta microtarea ni una A5 documental permiten alterar migraciones, JWT/RBAC, datos Aiven, Cloudflare/Hostinger, main, QA o PROD.
