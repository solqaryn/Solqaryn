# SOLQARYN V5 — C11-01.M01.05: criterios de aceptación observable del modelo relacional

**Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT / Task 6af16bbad26881918c283abe06e1f32a. **Unidad:** C11-01.M01.05, preparación documental del Modelo relacional normalizado, PLAN_MAESTRO fila 1590. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), DAG VALIDADO/SI y PROJECT_SCOPE_LOCK=STRICT; 2026-11-14, solqaryn/Solqaryn/dev.

## Entrega concreta, estado de pruebas y dependencias

Se definen **16 criterios de aceptación positivos y 16 negativos** medibles más 10 requisitos no funcionales para el futuro trabajo C11-01 de claves primarias/foráneas, índices, ownership por empresa/sucursal, nulabilidad histórica y compatibilidad MySQL/EF. Estos criterios no implementan una función, no ejecutan pruebas y **no constituyen resultados PASS**. Cada caso requiere ambiente, oráculo y evidencia de ejecución en SHA futuro, no solo nombre de test.

La base contractual es C11-01.M01.01 (objetivo/no regresión), M01.02 (12 actores, 12 I/O conceptuales, 9 fronteras) y M01.03 (15 fuentes versionadas S01–S15 y 12 brechas G01–G12) junto a M01.04 (16 riesgos R, 10 supuestos no verificados S y 10 decisiones pendientes D). El gate de DAG VALIDADO/SI autoriza **M01 documental**, no habilita M01.06 ni cualquier implementación de esquema, worker, proveedor, JWT/RBAC, SQL en Aiven, QA/main/PROD. El estudio conserva los criterios de ownership tenant A/B y capacidad de soporte estrictamente read-only.

## Matriz de aceptación positiva futura — P01–P16 (NO_EJECUTADOS)

| Caso | Preparación/acción futura | Oráculo verificable |
| --- | --- | --- |
| P01 | Tenant A consulta entidad A con JWT/rol permitido en API | 200 y fila A, sin datos de B ni exposición de EmpresaId ajeno |
| P02 | Tenant B consulta su propia entidad | Registro B accesible solo para B; referencia A ausente |
| P03 | Crear padre/hijo del mismo tenant en MySQL efímero | FK compuesta aceptada; identidad y owner coherentes |
| P04 | Consultar Sucursal ligada a Empresa autorizada | EmpresaId y sucursal resueltos server-side, con owner positivo |
| P05 | Cliente histórico con EmpresaId nullable existe en fixture | Conserva datos y queda cuarentena sin atribución implícita |
| P06 | Usuario con UsuarioEmpresa activa y RolId scoped correcto | Permiso efectivo corresponde únicamente a esa empresa |
| P07 | Alta de venta con detalle/inventario/factura en transacción | Commit atómico, referencias y saldos consistentes |
| P08 | Reversa de operación comercial con fallo inducido | Conteos/registros invariantes tras rollback |
| P09 | Índice tenant-scoped requerido por query real | information_schema + EXPLAIN y criterio de presupuesto medido |
| P10 | Dos tenants usan clave natural que es única por tenant | Ambos registros válidos, sin conflicto global indebido |
| P11 | Ejecutar VerifyAsync de contratos físicos en MySQL efímero | Evidencia de resultado del método, schema y SHA concreto |
| P12 | Aplicar migración versionada autorizada con datos efímeros | Paso Up reproducible con validación de esquema/compatibilidad |
| P13 | Revertir migración de prueba a snapshot previo | Down/compensación restauran registros e índices verificados |
| P14 | Operación pública limitada de storefront | Solo DTO público acotado, sin inventario o roles privados |
| P15 | Soporte autenticado consulta información en modo read-only | Lectura exitosa sin modificación registrada |
| P16 | Error referencial controlado en API | Error estable, correlación/auditoría sin PII/SQL sensible |

P11 no queda aprobado porque exista código VerifyAsync; requiere ejecutar ese método y conservar los logs legítimos. P12/P13 requieren autorización y MySQL efímero, no migraciones sobre Aiven real.

## Matriz de aceptación negativa futura — N01–N16 (NO_EJECUTADOS)

| Caso | Ataque o fallo a simular | Resultado exigido |
| --- | --- | --- |
| N01 | JWT tenant A intenta consultar fila B | Denegación sin filtrar existencia de B |
| N02 | Mutar EmpresaId, slug, header o cookie del cliente | No desplaza el contexto verificado backend |
| N03 | Hijo A referencia padre B usando FK simple | Guard transaccional/FK compuesta rechaza |
| N04 | Sucursal sin owner histórico se usa en operación nueva | Fail-closed, sin asignación silenciosa |
| N05 | Cliente legacy EmpresaId=NULL tratado como autorizado | Denegación/cuarentena hasta reconciliación |
| N06 | Usuario.RolId legacy contradice UsuarioEmpresa.RolId | Prevalece autorización tenant scoped o deny |
| N07 | Soporte temporal intenta SaveChanges | Excepción/403 según capa y cero filas modificadas |
| N08 | Capability pública intenta leer inventario administrativo | Denegación sin fuga de atributos |
| N09 | Unicidad global rechaza clave repetida legítima de B | Prueba revela el defecto y bloquea migración |
| N10 | Dos escrituras concurrentes duplican clave natural | Índice/idempotencia impiden duplicado |
| N11 | SQL raw evita filtro EF o guard de tenant | Test falla si hay lectura/escritura cruzada |
| N12 | Transacción de venta falla a mitad de stock/factura | Rollback completo sin desbalance |
| N13 | Migración vuelve NOT NULL un histórico huérfano | Gate bloquea hasta censo/backfill autorizado |
| N14 | Deshacer migración borra datos comerciales o referencias | NO_GO, conservar snapshot y activar recuperación |
| N15 | Excepción registra token, PII, cadena o SQL privado | Falla control de seguridad, log redactado requerido |
| N16 | CI de SHA wrong/pending/failed o informe sin prueba | No pasa A4 ni atribuye PASS de MySQL/API/seguridad |

Los resultados esperados son **contratos de prueba no ejecutados**. Una CI cancelada, omitida, queued, in_progress o fallida nunca puede etiquetarse PASS cuando sea requerida por un cambio runtime posterior.

## Requisitos no funcionales RNF01–RNF10 y evidencia necesaria

- **RNF01 Aislamiento:** fixture tenant A/B reproducible, consulta negativa y auditoría por EmpresaId.
- **RNF02 Integridad:** FK y garantías atómicas con inyección de fallos sobre MySQL 8.4 efímero.
- **RNF03 Compatibilidad:** lectores/escritores soportan ventana expand/contract sin ruptura.
- **RNF04 Reversibilidad:** plan de backup, snapshot, Up/Down y checksum con datos sintéticos.
- **RNF05 Rendimiento:** EXPLAIN/latencia antes-después bajo carga y umbral aprobado (no inventar tiempos).
- **RNF06 Seguridad:** protección JWT/RBAC, soporte read-only y capability pública sin elevación.
- **RNF07 Privacidad:** no volcar PII, tokens ni SQL sensible en telemetría/logs.
- **RNF08 Idempotencia:** concurrencia/reintentos no generan duplicaciones comerciales.
- **RNF09 Trazabilidad:** evidencia exact-SHA de código, esquema/versiones, CI pertinente y rollback.
- **RNF10 Gobernanza:** autorizaciones de migración y A5 independiente, sin liberación implícita de ambientes.

Ninguna meta numérica RPO/RTO o p95 se declara satisfecha sin medición aprobada. Las versiones actuales de arquitectura (ASP.NET Core 10, EF Core 10.0.12 y proveedor MySql.EntityFrameworkCore 10.0.9, Angular 22.2.1) son baseline documental, no comprobación de producción.

## Trazabilidad y dependencias técnicas

**Código fuente ya identificado:** Empresa.cs blob c44940df7c30983a4c23b93338e0cb9367962069, Cliente.cs blob 8c6d4c4453e44f8c0be9475340465e1aadb3f787, Sucursal.cs blob 8746b11fa30cdac0b97241b6221100bedd813b35, UsuarioEmpresa.cs blob f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c, AppDbContext.cs blob c4b32f6637f1b4426655bb0cd12af8dd9d229866, CommercialTenantPhysicalConstraints.cs blob d021861d357de201049fece1b6c63b2c9c218250, CommercialTenantReadFilterModel.cs blob f2422beec8f2d054c20184d073bad668513fa53b, CommercialTenantNaturalKeysPhysicalContract.cs blob 7e93ab363c4e5f3e7639c4b796b4d96315ed1917, CanonicalMySqlPhysicalContract.cs blob f590f85fa5e372051ef3d2a3fab9552f6e7c8cbb y M1EndurecerIntegridadRelacional.cs blob 7480fa634101ab9740f0183f8f937ee3e11d9fbb. **La existencia de código no acredita aplicación ni prueba**.

Para implementación futura, M02/M03 deberán generar matrices caso -> test exacto -> entorno -> oracle -> SHA -> workflow/job -> evidencia de fallo y rollback. **Condición de NO_GO:** discrepancias de propiedad; históricos sin tratamiento; FK no verificadas; ausencia de pruebas tenant A/B; migración sin respaldo/reversión; CI requerida no PASS; self-review A5; datos productivos sin otra autorización.

## Auditorías, impacto y rollback de esta unidad documental

**Impacto:** solamente docs/v5 y CHANGELOG_AI.md. Backend/Frontend/API/DB/seguridad/rutas/jwt/roles/tenant no se cambian; no se implementan pruebas, endpoints, migraciones, SQL, eventos, webhooks, workers ni proveedores. La nueva especificación no actualiza las doce referencias externas ni la Skill canónica.

**A0:** identidad Task06, gobernanza, HEAD/dev, Scope Lock, PLAN/COLA/DAG y archivo exclusivo. **A1:** 16 criterios positivos, 16 negativos, 10 RNF con oráculos coherentes, todos NO_EJECUTADOS. **A2:** seguridad tenant A/B, JWT/RBAC, privacidad y soporte solo lectura sin cambios runtime. **A3:** coherencia Domain/Application/Infrastructure y MySQL, pruebas efímeras futuras, sin alegar certificación. **A4:** CAS fast-forward a dev, diff Markdown, verify-solqaryn-project-scope y control V5 de CHANGELOG completed/success del SHA exacto, readbacks BITACORA/PLAN/COLA. Compilaciones no causales para Markdown son NO_APLICA, nunca PASS.

**A5_PENDIENTE:** revisión formal independiente exclusiva de una de las otras cinco Tasks Javier; nunca autoaprobación. **Rollback:** revert ordinario del commit documental sin alterar historia, QA/main/PROD ni datos.
