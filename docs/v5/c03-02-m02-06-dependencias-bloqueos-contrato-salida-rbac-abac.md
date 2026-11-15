# SOLQARYN V5 — C03-02.M02.06: dependencias y contrato de salida de RBAC/ABAC

**Responsable:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Rama:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK:** STRICT. **Fecha:** 2026-11-15. **Tipo:** documentación de salida M02; no desarrollo de permisos ejecutables.

## Alcance y exclusiones absolutas

Cerrar el contrato documental preparatorio de autorización backend RBAC/ABAC para identidad, empresa, sucursal, pertenencia, operaciones, BOLA/IDOR, exportaciones, medios, soporte y revocación. Esta entrega **no crea** grants, controllers, DTO compilados, esquemas EF/MySQL, JWT, middleware, webhooks, workers, eventos, endpoints, pruebas de runtime ni configuración activa. Sin secretos, reescritura de historia, QA, main, PROD, DNS, pagos ni cuentas adicionales. El cumplimiento de reglas escritas NO certifica aislamiento real, pen-test ni ausencia de vulnerabilidades P0/P1.

Fuentes rectoras: STANDARD V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA), Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, arquitectura vigente .NET 10/Angular 22.2.1/MySQL 8.4/EF 10 y los contratos M01/M02 existentes.

## Prerrequisitos DAG y procedencia de evidencia

El DAG dinámico prescribe: `C03-02.M01.01–06+A5`, `C03-02.M02.01–05+A5`, `C01-02.M01.06+A5` y `C02-01.M01.06+A5`. Cada requisito es LISTO y requiere A5 independiente **acreditada**, no una mención histórica ni un gate falseado. El preflight de esta unidad comprobó PLAN PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y reclamación CAS propia; las sucesoras no reciben licencia técnica por VALIDANDO.

| Prerrequisito | Artefacto y condición a preservar |
| --- | --- |
| C03-02.M01.01–06 | Alcance, mapa de contratos, inventario de riesgos, criterios y documento terminal con LISTO+A5 |
| C03-02.M02.01 | 16 entidades conceptuales, ownership y reglas; no EF ni servicios nuevos |
| C03-02.M02.02 | 12 operaciones candidatas, 8 DTO, 14 errores, 16 invariantes de diseño |
| C03-02.M02.03 | 10 entidades, 8 índices, 6 migraciones **futuras**, 12 fixtures no ejecutados |
| C03-02.M02.04 | 16 claves tipadas, 12 flags por defecto OFF, 12 pares P/N de diseño |
| C03-02.M02.05 | 8 señales eventuales, 15 campos y 14 reglas de idempotencia futuras, sin dispatcher |
| C01-02.M01.06 | Barrera terminal de identidad y membresía, con A5 externa |
| C02-01.M01.06 | Barrera terminal de autenticación, autorización y tenant, con A5 externa |

**Responsables de M02.01/.02/.03/.05:** PRIMARY DEV 06, revisiones A5 por Task diferente. **M02.04:** autor PRIMARY DEV 01, revisión realizada por otra Task Javier independiente. Esta unidad no reutiliza A5 de su propia autoría ni la de un autor para certificarse. Source SHA, doc blob, diff y CI `completed/success` pertinente deben poder rastrearse en PLAN/BITACORA. Una compilación `CANCELLED`, `SKIPPED`, `IN_PROGRESS` o `FAIL` no se renombra PASS.

## Dieciséis controles terminales RBA01–RBA16

| Código | Condición de aceptación futura | Negativa bloqueante |
| --- | --- | --- |
| RBA01 | JWT backend firmado, issuer, audience y expiración verificados | Token ajeno, caducado o manipulado denegado |
| RBA02 | Membresía tenant vigente, comprobada en servidor | Revocación prevalece sobre JWT firmado |
| RBA03 | RBAC relacional específico de módulo, acción y recurso | Entitlement o rol visual no otorga grant |
| RBA04 | ABAC por ownership y condición del recurso | BOLA/IDOR por ID conocido no accede datos de otro |
| RBA05 | Tenant A/B en consulta, servicio y serialización | Header/DTO/route nunca elevan alcance tenant |
| RBA06 | Sucursal relacionada y autorizada, sin fallback | Sucursal externa se rechaza antes de la consulta |
| RBA07 | Recurso heredado con owner NULL es ambiguo | Nunca asignar tenant automáticamente |
| RBA08 | Políticas versionadas y deny-by-default para unknown | Flag/UI no transforma DENY en ALLOW |
| RBA09 | Exportación por fila y campo, con permiso explícito | CSV/PDF/XLSX no mezcla PII o filas de otro tenant |
| RBA10 | Medio propio y URL temporal solo tras validación backend | No generar enlaces firmados del tenant B |
| RBA11 | Soporte GET allowlisted, ámbito, TTL y auditoría | Soporte no escribe ni consulta tenant arbitrario |
| RBA12 | Revocación de grant/membresía invalida caché efectiva | Datos cacheados no restituyen permisos previos |
| RBA13 | Concurrencia y versiones de políticas/grants verificadas | Retry con contextos mezclados se rechaza |
| RBA14 | Logs/errores saneados, sin JWT, PII ni secretos | No exponer existencia de recursos ajenos ni URI sensible |
| RBA15 | CI pertinente del SHA exacto, Scope Lock y BITACORA | Queued/cancelled/skipped/failed no es PASS |
| RBA16 | A5 independiente, autorización de ambiente separada | LISTO_DEV no habilita QA/main/PROD ni IAM live |

Los 16 controles son **criterios de aceptación futuros**, no hallazgos de pruebas runtime ejecutadas durante M02.06. Ningún campo de un navegador, claim no verificado, cookie o flag sustituye la autoridad del servidor.

## Doce pares de prueba futura POS/NEG — NO EJECUTADOS

| ID | Caso positivo a instrumentar | Negativo que debe demostrar denegación |
| --- | --- | --- |
| PN01 | Usuario A con permiso propio lee recurso A | Usuario B pide id A, denegación BOLA/IDOR |
| PN02 | JWT y membresía activos permiten acción A | JWT expirado o miembro revocado denegado |
| PN03 | Rol A explícito permite operación R | Plan premium o guard Angular intenta permiso extra |
| PN04 | Sucursal A1 del mismo tenant responde | Sucursal B o sucursal ajena se rechaza |
| PN05 | Contexto de política válido y versionado funciona | Política desconocida, desactualizada o ausente = deny |
| PN06 | Owner no nulo y verificable permite acceso | Owner NULL o legado ambiguo no hereda tenant |
| PN07 | Export A devuelve solo filas/campos autorizados | Export con columna PII o registros B bloqueado |
| PN08 | Medio A autoriza URL efímera para miembro A | URL o metadata de objeto B no se revela |
| PN09 | Soporte autorizado GET limitado por plazo | Soporte intenta POST/DELETE o scope general |
| PN10 | Revocación A se aplica en próxima request | Token/caché anterior mantiene grant revocado |
| PN11 | Dos tenants concurrentes conservan sus contextos | Singleton/caché intercambia grants entre A y B |
| PN12 | CI completada con éxito exact-SHA y A5 externo real | Check cancelled o A5 ficticio abre sucesora |

No se corrió ningún HTTP, xUnit nuevo, migration SQL, Playwright o backend de autorización mediante este documento. Las pruebas de implementación posterior deben incluir roles, recursos, dos tenants sintéticos, sucursales, grants vigentes/revocados, datos saneados, logs, SHA, job CI y rollback reproducible.

## Recuperación y rollback R01–R08

1. **R01:** CI falla en código: leer job exacto, aislar cambio, reparar sin desactivar analizadores.
2. **R02:** `queued/in_progress`: no acreditar PASS; mantener VALIDANDO y evitar escrituras dobles.
3. **R03:** `cancelled` por nuevo commit: comparar blobs y exigir CI causal en HEAD aplicable.
4. **R04:** Scope Lock falla: corregir regla o contenido legítimamente, verificar nuevo SHA, no hacer bypass.
5. **R05:** Lease/owner incierto: no transferir ni modificar; readback y CAS atómico si se acredita abandono.
6. **R06:** A5_FAIL: no borrar BITACORA; reparar y obtener revisión independiente diferente.
7. **R07:** Negativa A/B o revocación falla en runtime: NO_GO sobre ese scope y prueba de regresión.
8. **R08:** Reversión documental: `git revert` sin force-push; NO restaura tablas, pagos ni identidad real.

## A0–A5 y condición de salida

**A0:** dos Markdown (este y CHANGELOG_AI), diff limitado, sin secretos/código/DDL. **A1:** dieciséis gates RBA, doce pares PN, ocho rutas R; positivos/negativos son diseño FUTUROS_NO_EJECUTADOS, no PASS funcional. **A2:** revisar contratos fail-closed de JWT/RBAC/ABAC, tenant A/B, sucursal, propiedad, exports, medios, soporte, PII y revocación, sin confundir cumplimiento textual con test real. **A3:** verificar dependencias DAG exigidas, M01.01–06/M02.01–05 LISTO+A5 y dos barreras externas C01-02/C02-01, más stack vigente; API, MySQL y Angular no cambian en diff documental. **A4:** Git fast-forward/CAS y comprobación de HEAD/diff/blobs, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success en SHA aplicable, readback PLAN/COLA/DAG/BITACORA. **A5:** otra Task Javier distinta de la autora revisa fuente, SHA y resultado; nunca autoaprobación.

**Salida válida de C03-02.M02.06:** únicamente contrato documental LISTO_DEV con A5_PENDIENTE; la implementación M03 requiere nuevas autorizaciones DAG, código real, tests dirigidos y ninguna promoción implícita a QA/main/PROD.
