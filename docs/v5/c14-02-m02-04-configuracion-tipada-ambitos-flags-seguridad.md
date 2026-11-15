# SOLQARYN V5 — C14-02.M02.04: configuración tipada, ámbitos y flags seguros para aceptación de seguridad

**Microtarea:** `C14-02.M02.04` · **Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT (`6aeacb7820ac8191b7d768a6f33dfa09`) · **Fecha:** 2026-11-14, America/Tegucigalpa · **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Naturaleza:** definición técnica **DOCUMENTAL** para la aceptación de seguridad futura C14-02. Las claves, flags, enums y oráculos siguientes son `PROPUESTOS_NO_IMPLEMENTADOS` y sus pruebas `FUTUROS_NO_EJECUTADOS`. No se modifica configuración real, API, Angular, JWT, RBAC, MySQL, tenant, Cloudflare, workers, secretos, alertas, ni QA/main/PROD. Una bandera nunca concede autorización ni prueba ausencia de P0/P1.

## 1. Autoridad, fuentes y límites

Autoridad ejecutable: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y `docs/AUTOMATION_STATUS.md` del HEAD dev. El preflight acreditó PLAN fila 2063 H=PENDIENTE/I vacía, COLA G/O=SI, y `DAG_TECNICO` por ID exacto F=VALIDADO/G=SI; el requisito dinámico es M01.01–06 con revisión A5 independiente. El claim y cualquier cierre requieren readback institucional, propietario único y controles A0–A4 exact-SHA.

| Fuente Git en dev | Blob de referencia | Contrato relacionado |
| --- | --- | --- |
| `docs/v5/c14-02-m01-06-gates-bloqueo-rollback-liberacion-seguridad.md` | `6cb629af7993a6f4eb7cb8518f538f7ed196326f` | B01–B12 y prohibición de liberación automática |
| `docs/v5/c14-02-m02-01-modelo-ownership-invariantes-aceptacion-seguridad.md` | `1bf7ca3f8c549212a24e10ed1afefb2fdedea972` | SE01–SE14, SI01–SI16 y SA01–SA12 |
| `docs/v5/c14-02-m02-02-api-dto-errores-validaciones-compatibilidad-seguridad.md` | `9de9d398cc2d238ce2ba66f27890c54b3317d782` | API01–API12 y errores hipotéticos |
| `docs/v5/c14-02-m02-03-persistencia-indices-fixtures-seguridad.md` | `f8eeec67ea3c7165c967d9a9d6c28a6970d8909a` | PD01–PD12, IX01–IX08, fixtures sin SQL |

La arquitectura actual conserva API .NET 10, Domain/Application/Infrastructure, EF Core 10/MySQL 8.4, JWT/BCrypt/RBAC y Angular 22.2.1. La configuración de aceptación descrita aquí es una propuesta desacoplada: **no sustituye autenticación, decisiones de pertenencia ni políticas de autorización del backend**. La proyección UI no decide acceso.

## 2. Contrato de claves tipadas CFG01–CFG16 (propuestas, no claves implementadas)

En una futura implementación, cada entrada debe tener tipo estricto, versión, procedencia, propietario de decisión, validación y política de rechazo. Valores ausentes o mal tipados para un gate de seguridad equivalen a `DENY`/`NOT_EVALUATED`, nunca a PASS.

| ID | Clave conceptual / tipo | Ámbito autorizado y valor seguro inicial |
| --- | --- | --- |
| CFG01 | `securityAcceptance.enabled: boolean` | Plataforma/dev, `false`; no afecta autorización de recursos |
| CFG02 | `securityAcceptance.allowedScopeIds: string[]` | Gobierno institucional, conjunto vacío; IDs exactos no comodines |
| CFG03 | `securityAcceptance.requireIndependentReview: true` | Global y no debilitante por tenant/cliente |
| CFG04 | `securityAcceptance.requireExactShaCi: true` | Global; `queued/skipped/cancelled` no son PASS |
| CFG05 | `securityAcceptance.allowedEnvironments: enum[]` | Sólo `dev` bajo esta orden; QA/PROD requieren permiso separado |
| CFG06 | `securityAcceptance.tenantIsolation: enum` | `ENFORCED`; configuración no puede anular comprobación del backend |
| CFG07 | `securityAcceptance.membershipRefresh: enum` | `SERVER_VERIFIED`; no confiar en claim aportado por UI |
| CFG08 | `securityAcceptance.rbacMode: enum` | `DENY_BY_DEFAULT`; el plan no concede grants |
| CFG09 | `securityAcceptance.hostMatch: enum` | `EXACT_TRUSTED_BINDING`; sin substring ni Host falsificable |
| CFG10 | `securityAcceptance.mediaAccess: enum` | `SIGNED_OWNER_CHECKED`; TTL y tenant efectivos sólo en implementación futura |
| CFG11 | `securityAcceptance.asyncEffectPolicy: enum` | `DISABLED_UNTIL_AUTHORIZED`; nada de dispatcher por documento |
| CFG12 | `securityAcceptance.evidenceState: enum` | `NOT_EVALUATED`; sólo runner comprobado podría emitir PASS |
| CFG13 | `securityAcceptance.maxEvidenceBytes: int` | Límite positivo explícito y finito, valor pendiente de presupuesto técnico |
| CFG14 | `securityAcceptance.redactionPolicy: enum` | `MINIMIZE_PII_AND_SECRETS`; nunca tokens/JWT en log |
| CFG15 | `securityAcceptance.expectedVersion: opaque` | CAS real en futuro datastore con versión, conflicto = rechazo |
| CFG16 | `securityAcceptance.releaseGate: enum` | `BLOCKED`; no transformar LISTO_DEV/A5 en habilitación productiva |

Estas claves **no son variables de entorno actuales ni nombres reales de feature flags**. No se propone almacenar secretos ni permisos en flags. Un valor `true` no puede sustituir prueba JWT, RBAC, ownership/tenant, CI, A5 o permiso comercial; tampoco alterar `PROJECT_SCOPE_LOCK`.

## 3. Orden de resolución, aislamiento y negativa segura

1. **Procedencia:** configuración futura sólo de fuente servidor/institucional autorizada, nunca URL, LocalStorage, header Host arbitrario, cuerpo HTTP, identidad visual ni cliente Angular.
2. **Precedencia:** restricciones globales de seguridad y allowlist de scope se intersectan con contexto de ejecución y tenant real; ninguna excepción por tenant o feature flag puede ampliar un gate estricto.
3. **Tipos y compatibilidad:** rechazar clave desconocida, tipo incompatible, enum fuera de contrato, formato no canónico, duración negativa, versión obsoleta y cambio entre readback y acción.
4. **Tenancy A/B:** toda búsqueda se acota al owner/membresía verificados por backend y recursos efectivamente vinculados; una configuración de B nunca se puede leer o inferir desde A.
5. **Separación de autoría:** la Task autora conserva su scope de dev; `A5_PASS` sólo por otro TASK_ID entre las seis Tasks Javier, con SHA, diff, pruebas causales y evento inmutable.
6. **Observabilidad:** distinguir resultado `PASS`, `FAIL`, `NOT_EVALUATED`, `IN_PROGRESS`; ocultar PII/secretos; correlación opaca y artefactos de acceso controlado.
7. **Rollback:** revert documental normal de dev. Una futura desactivación del feature gate falla cerrada y no revierte controles JWT/RBAC ni modifica datos reales.

La respuesta frente a datos ausentes, inconsistencia de scope o falla del proveedor de configuración es **rechazar la acción sensible sin filtrar otra empresa**. `NOT_EVALUATED` no acredita ausencia de P0/P1. Los controles no permiten usar código, documentos o flags para activar QA, main, PROD, DNS ni exportaciones.

## 4. Oráculos VAL01–VAL12 positivos/negativos — FUTUROS_NO_EJECUTADOS

| Caso | Aceptación positiva futura | Contraprueba negativa obligatoria |
| --- | --- | --- |
| VAL01 | Scope dev allowlisted y versión vigente permiten evaluar | Scope no registrado o comodín intenta abrir evaluación |
| VAL02 | Actor A autorizado opera únicamente sobre A | Tenant A solicita recurso/configuración de B |
| VAL03 | JWT válido y membresía activa se contrastan en servidor | Token expirado o membresía revocada obtiene permiso |
| VAL04 | RBAC/grant exacto permite operación prevista | Flag habilitado sin grant intenta saltar autorización |
| VAL05 | Host exacto con binding confiable resuelve propietario | Host similar, spoof o StoreKey ajeno redirige acceso |
| VAL06 | Documento/medio autorizado respeta owner y TTL | Archivo de B, URL expirada o replay entrega bytes |
| VAL07 | Evidencia CI exact-SHA completada y exitosa puede acreditarse | Job cancelado/skipped/queued presentado como PASS |
| VAL08 | Reviewer independiente verifica diff/artefacto original | AuthorTaskId igual a reviewerTaskId intenta A5 |
| VAL09 | Schema tipado y versión aceptada no pierden información | Clave desconocida, tipo incorrecto o CAS obsoleto se acepta |
| VAL10 | Correlación segura ayuda a auditar sin secretos | Log revela JWT, email ajeno, datos sensibles o acceso cruzado |
| VAL11 | Evaluación futura identifica P0/P1 abiertos y bloquea release | Ausencia de registro se declara vulnerabilidades cero |
| VAL12 | Revocación de flag sensible cae en DENY sin otros efectos | Flag/client input habilita producción, migración o backend bypass |

Son **24 condiciones a probar en una fase posterior**, no ejecuciones ni suites certificadas. Para runtime real harían falta fixtures sintéticos tenant A/B, permisos, logs redactados, comprobación backend/Angular y CI dirigida en SHA, además de revisión independiente. No hay nueva cobertura HTTP, MySQL, Playwright ni pentest en esta microtarea.

## 5. Matriz A0–A5 y reversión

- **A0, estática:** validar que el artefacto se limita a un Markdown nuevo y una sección de `CHANGELOG_AI.md`, coherencia CFG01–16/VAL01–12, procedencia y no secretos.
- **A1, positiva/negativa documental:** verificar tipos y oráculos contradictorios o falsificables; todos los casos de ejecución están etiquetados `FUTUROS_NO_EJECUTADOS`.
- **A2, seguridad:** revisar conceptualmente JWT, RBAC, tenant A/B, hosts, archivos, jobs, PII, SI01–SI16 y prohibición de habilitación por flag. No afirmar prueba de penetración ni P0/P1 cero.
- **A3, integración documental:** comparar M01.06, M02.01–03 y DAG exacto. Compilación .NET/Angular, MySQL y Playwright son `NO_APLICA_DOCUMENTAL` a dos Markdown; no se atribuyen como PASS.
- **A4, publicación:** fast-forward/CAS de `dev`, diff y SHA/blobs comprobados, checks `verify-solqaryn-project-scope` y procedencia V5 `completed/success` en SHA aplicable, BITACORA de 26 columnas y readbacks institucionales.
- **A5, independiente:** `A5_PENDIENTE` hasta revisión de otra Task Javier sobre el commit y artefacto publicados. No abre QA/main/PROD.

**Rollback:** `git revert` normal del commit documental y evento compensatorio aditivo si se revierte; nunca force-push ni borrar historia. Ningún contrato aquí autoriza nuevas tablas, endpoints, flags runtime, dispatcher de eventos o promoción. M02.05 y M02.06 deben comprobar su propia elegibilidad dinámica y predecesores A5.
