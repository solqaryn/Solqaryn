# SOLQARYN V5 — C11-01.M01.02: actores, entradas, salidas y fronteras del modelo relacional

**Autoría:** PRIMARY DEV 06 / JAVIER_CHATGPT / Task 6af16bbad26881918c283abe06e1f32a. **Unidad:** C11-01 Modelo relacional normalizado, fase documental M01.02, PLAN_MAESTRO fila 1587. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); DAG VALIDADO/SI. **Repo:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Resultado exacto y límites

Este documento identifica los actores, entradas/salidas conceptuales y superficies de confianza que deberán considerarse **en la preparación del diseño relacional C11-01**: claves, FK, índices, propiedad empresarial, historial nullable, transacciones y compatibilidad EF Core/MySQL. Se apoya en el objetivo M01.01 ya publicado; no declara implementada ninguna tabla, API, migración, consulta, permiso o test. Especificar una entrada o salida significa definir un contrato futuro, **no afirmar que existe un endpoint o funcionalidad en producción**.

La arquitectura vigente es ASP.NET Core 10 (API como exposición y composición; Application/Domain/Infrastructure separados), EF Core 10.0.12, Oracle MySql.EntityFrameworkCore 10.0.9, MySQL, Angular 22.2.1. JWT/RBAC y aislamiento tenant deben imponerse en backend. Aplicaciones de Hostinger, Cloudflare o cambios al almacenamiento no forman parte de esta unidad.

## Actores A01–A12 y sus límites propuestos

| ID | Actor | Responsabilidad y frontera |
| --- | --- | --- |
| A01 | Propietario de empresa autenticado | Gestiona recursos de su propia empresa; no elige tenant arbitrario |
| A02 | Administrador de sucursal | Opera registros con permiso y sucursal validada en backend |
| A03 | Operador de caja/venta | Cambios comerciales transaccionales sólo con permisos efectivos |
| A04 | Personal de inventario | Movimientos y relaciones producto/almacén dentro del ámbito autorizado |
| A05 | Contabilidad y cobranza | Conserva FK y trazabilidad de factura/pago sin acceso ajeno |
| A06 | Cliente/consumidor de consulta pública | Capability acotada; no transforma URL en autorización administrativa |
| A07 | Soporte de plataforma temporal | Lectura expresamente limitada; no puede persistir ni alterar pertenencia |
| A08 | API y capa Application | Verifican identidad, roles, empresa/sucursal e invariantes antes de persistir |
| A09 | Domain | Define identidad e invariantes de agregados, sin depender de EF/HTTP |
| A10 | Infrastructure/AppDbContext | Aplica mappings, controles de contexto, transacciones y persistencia |
| A11 | Migrador en CI efímero futuro | Ensaya esquema/rollback aislados, nunca apunta a Aiven real sin autorización |
| A12 | Auditor de datos/seguridad | Revisa evidencia de FK, índices, históricos y aislamiento con datos no sensibles |

Los actores A01–A12 son roles de análisis; no constituyen una tabla RBAC nueva ni conceden privilegios.

## Contratos de entradas y salidas propuestos I01/O01–I12/O12

| Par | Entrada conceptual futura | Salida/garantía esperada |
| --- | --- | --- |
| I01/O01 | Id de entidad en operación autenticada | Fila autorizada o rechazo sin filtrar existencia ajena |
| I02/O02 | Contexto empresarial server-side verificado | Filtro de propiedad coherente de EmpresaId, sin confiar en cliente |
| I03/O03 | Sucursal y recurso comercial solicitado | Asociación a empresa y sucursal válidas o denegación |
| I04/O04 | Alta/edición de cliente con historial nullable | Transición documentada sin reasignación silenciosa |
| I05/O05 | Venta, detalle, factura y cobro relacionados | Commit transaccional completo o rollback sin parcialidad |
| I06/O06 | Movimiento inventario/almacén | FK, stock y empresa consistentes tras operación |
| I07/O07 | Búsqueda paginada por índice | Query autorizada, sin fuga cross-tenant, plan EXPLAIN medible |
| I08/O08 | Propuesta de nueva FK/índice por migración | Informe de impacto, compatibilidad, rollback; sin ejecutar DDL en M01 |
| I09/O09 | Muestra histórica con propiedad indeterminada | Registro de excepción, no autorización ni hard-delete |
| I10/O10 | Contexto de soporte sólo lectura | Proyección sin capacidad de SaveChanges |
| I11/O11 | Lectura de storefront/capability pública | DTO acotado sin exponer datos administrativos/inventario privado |
| I12/O12 | Fallo de escritura o conexión en transacción | Error seguro, auditoría mínima, reversión y sin PII/secretos |

Son pares contractuales para M02/M03, **no endpoints implementados ni nuevos DTO/SQL**. Parámetros tenantId, EmpresaId, slug, cookie o encabezado de navegador nunca constituyen autoridad por sí solos. No se presumen firmas API, cardinalidades ni nombres de tablas: M01.03 deberá corroborarlos.

## Fronteras F01–F09 y escenarios de negación

**F01 navegador → API:** identidad/claims de servidor, permiso específico; no confiar en ID empresa arbitrario.
**F02 API → Application:** validar rol, empresa, sucursal, alcance de operación y estados comerciales.
**F03 Application → Domain:** invariantes de agregados y reglas referenciales sin dependencia de infraestructura.
**F04 Application → Infrastructure/DbContext:** contexto tenant comprobado, consultas y comandos con capacidades diferenciadas.
**F05 DbContext → MySQL:** transacción y FK con índice adecuado; mensajes de error no revelan SQL/PII.
**F06 soporte → lectura:** contextos de soporte temporal no ejecutan persistencia.
**F07 storefront público → lectura comercial:** capability pública no habilita inventario privado ni RBAC.
**F08 migración efímera → datos de prueba:** copia ficticia, nunca credenciales/DB productiva.
**F09 documento M01 → ejecución:** un gate documental o A5 de preparación no es licencia para ejecutar DDL, trabajos, QA o PROD.

## Evidencia consultada y huecos reales

- backend/src/Domain/Entities/Empresa.cs, blob c44940df7c30983a4c23b93338e0cb9367962069: raíz empresarial activa/inactiva sin soft-delete persistente.
- backend/src/Domain/Entities/Cliente.cs, blob 8c6d4c4453e44f8c0be9475340465e1aadb3f787: EmpresaId nullable por históricos sin asignación; prohibido usarlo como autorización sin resolver ownership.
- backend/src/Infrastructure/Persistence/AppDbContext.cs, blob c4b32f6637f1b4426655bb0cd12af8dd9d229866: VerifiedCommercialTenant/VerifiedPublicStoreTenant/VerifiedPlatformSupportTenant, y soporte sólo lectura; inspección inicial de superficie, no ejecución de un test de aislamiento.
- backend/src/Infrastructure/Solqaryn.Infrastructure.csproj, blob 6210d8b12abd77171d95d17e2292c1a319b89263: versiones de persistencia; no implica disponibilidad de MySQL real.
- docs/v5/c11-01-m01-01-objetivo-no-alcance-modelo-relacional.md: objetivos e invariantes M01.01 previos, no certificación de modelo.

M01.03 debe ampliar inventario con mapa de DbSet, IEntityTypeConfiguration, índices, migraciones y pruebas por ruta exacta. **Sin acceso a datos productivos no se afirma completitud, exclusividad de FK ni inexistencia de registros huérfanos.**

## Oráculos de revisión FUTUROS — no ejecutados

**Positivos P01–P06:** P01 obtener fila propia con tenant y rol; P02 transacción comercial consistente; P03 mantener histórico nullable sin elevación; P04 Query con índice bajo MySQL efímero; P05 usuario soporte consulta sin cambiar datos; P06 rollback de migración autorizada en DB efímera.

**Negativos N01–N10:** N01 sesión A intenta recurso B; N02 cliente sustituye EmpresaId; N03 sucursal ajena en FK; N04 cliente histórico sin propiedad recibe permiso erróneo; N05 soporte intenta SaveChanges; N06 storefront obtiene objeto privado; N07 falla FK a mitad de transacción; N08 migración destructiva sin copia y rollback; N09 logs/errores exponen PII o secreto; N10 documento se toma por autorización PROD.

**Ninguno de P01–P06/N01–N10 ha sido ejecutado** por esta entrega Markdown; son reglas para una CI causal futura, no PASS.

## Auditorías y rollback

**A0:** contraste con fuente V5, plan, DAG, Skill, identidad, diseño y archivos limitados. **A1:** inspección manual de 12 actores, 12 I/O y 9 fronteras; distinguir propuesta de API vigente. **A2:** revisión tenant A/B, JWT/RBAC, PII, transacciones y soporte read-only sin tocar permisos. **A3:** correspondencia Domain/Application/Infrastructure/API, MySQL/EF y M01.01, sin afirmar pruebas runtime. **A4:** fast-forward/CAS exact HEAD de dev, diff sólo Markdown y CHANGELOG, checks Scope Lock y V5 provenance completed/success en SHA y readback PLAN/COLA/BITACORA. Suites backend/frontend/seguridad en curso que no validan este texto se clasifican NO_APLICA causal, no PASS.

**A5:** revisión independiente de otra Task Javier (nunca del autor). M01.02 no desbloquea M01.06, M02, migraciones ni QA/main/PROD. **Rollback:** revert ordinario sólo del commit documental, preservando historial y sin modificar datos.
