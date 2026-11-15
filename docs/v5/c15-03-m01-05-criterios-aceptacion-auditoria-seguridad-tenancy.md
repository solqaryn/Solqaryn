# SOLQARYN V5 — C15-03.M01.05: criterios observables de auditoría de seguridad y tenancy

**Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Alcance:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Tipo:** preparación documental M01.05; ningún test de penetración, migración, endpoint, despliegue ni acceso productivo se ejecuta por este artefacto.

## Autoridad, fuentes y dependencia

- Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO fila 2238 y COLA G/O=SI; DAG_TECNICO F=VALIDADO/G=SI. Estas condiciones autorizan exclusivamente evaluación y desarrollo en dev, no un PASS de seguridad.
- Antecedentes `C15-03.M01.01–04` ya publicados: alcance y NR01–NR16; actores, IO01–IO12 y fronteras T01–T10; quince fuentes y brechas G01–G12; riesgos R01–R14, supuestos S01–S08 y decisiones D01–D08.
- Fuentes de código de esos inventarios: `backend/src/API/Filters/TenantPermissionContext.cs`, `backend/src/Application/Security/IdentidadUniversalAuthorizationGuard.cs`, `backend/src/API/Middleware/UnscopedCommercialTenantGuardMiddleware.cs`, `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs`, `backend/src/Infrastructure/Persistence/CommercialCrossTenantWriteGuard.cs`, `backend/src/Application/AutomationV5/V5WorkItemTenantAuthorizationGuard.cs`, `backend/tests/Solqaryn.Tests/API/Controllers/UsuarioEmpresaTenantBoundaryTests.cs` y `backend/tests/Solqaryn.Tests/API/SecurityBoundaryContractTests.cs`. Sus existencias no certifican cobertura de runtime.
- Stack declarado y leído: Angular 22.2.1, ASP.NET Core net10.0, EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9; autorización JWT/RBAC en backend y aislamiento de tenant A/B. Cloudinary es transición vigente; Hostinger/R2 son objetivos, no activados.

## Contrato de evaluación positiva y negativa — TODO NO_EJECUTADO

Cada fila define entradas aisladas, oráculo y evidencia futura obligatoria. **Estado de todos los casos: NO_EJECUTADO; no equivale a PASS, FAIL ni hallazgo.** Ninguna cifra de vulnerabilidades resulta de esta tabla.

| Caso | Positivo esperado con contexto legítimo | Negativo bloqueante y oráculo |
| --- | --- | --- |
| AC01 | JWT de usuario A y rol vigente permite recurso propio autorizado | N01: JWT A sobre recurso B no devuelve datos B ni produce efecto |
| AC02 | Selector `X-Empresa-Id` único y autorizado conserva tenant A | N02: selector duplicado, inválido, nulo o B no amplía permisos |
| AC03 | Membresía activa y rol correcto autorizan acción propia | N03: rol revocado o membresía caducada deniega aunque el JWT siga válido |
| AC04 | Sucursal perteneciente a A permite operaciones limitadas a A | N04: sucursal B, inexistente o cambiada en body/ruta se rechaza |
| AC05 | Detalle, lista, búsqueda y export autorizados sólo incluyen recursos A | N05: ID de B o paginación no filtra existencia ni registros B |
| AC06 | Consulta con filtros EF y marca positiva de tenant retorna sólo A | N06: `IgnoreQueryFilters` sin contexto verificado no expone A/B; verificar middleware |
| AC07 | Escritura propia conserva FK de cliente, producto y documento de A | N07: FK de B o propiedad mutable de request se rechaza antes del commit |
| AC08 | Filas históricas con `EmpresaId=NULL` quedan sin propietario atribuido | N08: ningún backfill automático ni vista cross-tenant por un valor NULL |
| AC09 | Host público registrado resuelve sitio público correspondiente | N09: Host falso o manipulado no abre superficie privada ni recursos B |
| AC10 | Soporte/plataforma usa un grant explícito, acotado y auditable | N10: usuario comercial o soporte revocado no adquiere autoridad global |
| AC11 | Descarga de archivo propio con permiso y enlace vigente funciona | N11: archivo B, URL manipulada o vencida no descarga ni revela metadatos |
| AC12 | Operación diferida futura conserva tenant/actor firmados del servidor | N12: payload exterior no puede inyectar tenant, owner ni idempotency key |
| AC13 | Respuesta de error niega sin filtrar PII y mantiene correlación | N13: mensaje, log o traza no expone JWT, secreto ni dato de B |
| AC14 | Carga concurrente A/B conserva límites y partición de caché | N14: cache key compartida no entrega resultado B a sesión A |
| AC15 | Recuperación de error conserva auditoría y estado transaccional | N15: timeout o reintento no duplica cambios ni eleva permisos |
| AC16 | CI y readback instituyen A0–A4 exact-SHA y A5 por otra Task Javier | N16: `skipped`, `cancelled`, SHA distinto o autor propio no constituyen PASS |

## Requisitos de evidencia causal para una fase posterior

1. **Fixture mínimo:** dos empresas artificiales A/B, dos sucursales, usuarios con roles distintos, un usuario revocado, recursos con relaciones propias y ajenas; MySQL efímero y backend de CI, nunca Aiven operativo. Registrar cómo se crean y destruyen los datos; sin datos reales.
2. **Pruebas positivas:** para cada AC01–AC16 conservar configuración, credenciales sintéticas, comando, SHA, respuesta, entidad persistida y aserción; resultados HTTP esperados se fijan por contrato vigente, sin inventar un código 403/404 universal.
3. **Pruebas negativas:** ejecutar N01–N16 en CI dirigida. Validar simultáneamente no autorización, ausencia de filas/campos ajenos, cero efectos laterales y redacción en logs. No basta una prueba que sólo observe HTTP.
4. **Autorización y middleware:** inspeccionar orden efectivo de `TenantPermissionContext`, `IdentidadUniversalAuthorizationGuard`, cuarentena de rutas sin contexto, filtros EF y guard de escrituras; `IgnoreQueryFilters` sólo se considera seguro tras prueba integrada en pipeline real.
5. **Archivos, hosts, soporte y jobs:** si la funcionalidad todavía no existe, consignar `NO_APLICA_RUNTIME_NO_IMPLEMENTADO` con prueba de inexistencia; **nunca** marcar PASS por ausencia de implementación.
6. **Prueba de regresión:** compilación .NET 10, tests de seguridad/permisos y MySQL efímero para rutas afectadas; Angular/Playwright sólo cuando el cambio real involucre UI. Registrar resultado de GitHub Actions por nombre, `head_sha`, `completed/success` y URL de job.
7. **Evidencia institucional:** PLAN H/I/K/L, COLA derivada, DAG F/G y BITACORA A:Z deben permanecer concordantes; A5 por TASK_ID diferente del autor; ningún resultado dev autoriza QA/main/PROD.

## Requisitos no funcionales y puertas de aceptación

| ID | Requisito | Evidencia medible futura |
| --- | --- | --- |
| NF01 | Aislamiento A/B en lecturas y escrituras | Conteo 0 de recursos B retornados a A en fixtures y negativos ejecutados |
| NF02 | Rechazo uniforme de permisos revocados | Matriz de roles, grants, contexto y respuestas con sello de ejecución |
| NF03 | Fallos sin divulgación | Inspección automatizable de cuerpo/error/log bajo casos N01–N16 |
| NF04 | Integridad transaccional | Conteos y FKs antes/después del negativo con rollback |
| NF05 | Idempotencia | Identificador y replay probado sobre evento o worker real existente |
| NF06 | Trazabilidad de identidad y tenant | Correlation ID, actor y tenant en log redactado de fixtures |
| NF07 | Determinismo de CI | Misma prueba y mismo SHA con artefactos y logs rastreables |
| NF08 | Reversibilidad | Revert ordinario del changeset documental; sin reset ni force-push |

**Puerta documental M01.05:** aprobar sólo coherencia de criterios, fuentes, oráculos y límites en dev con A0–A4 documentales. **Puerta M01.06 y siguientes:** comprobar predecesores y revisiones A5 independientes y evidencia de P0/P1; no liberar sobre este texto por sí mismo. Si falta cualquier check pertinente en `completed/success`, mantener VALIDANDO y no declarar LISTO_DEV.

## Alcance del changeset y rollback

Únicamente este Markdown y una sección indivisible de `CHANGELOG_AI.md`. Ningún cambio de código, API, JWT/RBAC, datos, providers, CI workflow, secret, DNS o QA/main/PROD. Las condiciones AC01–AC16 y N01–N16 se redactaron como **criterios futuros NO_EJECUTADOS**, no como certificación de ausencia de vulnerabilidades. Reversión mediante `git revert` ordinario, manteniendo historia; no sobrescribir eventos BITACORA.
