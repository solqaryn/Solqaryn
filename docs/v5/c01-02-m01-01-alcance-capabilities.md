# C01-02.M01.01 — Objetivo, límites y no regresión de capabilities/RBAC

**Módulo autorizado:** V5 FINAL `C01-02.M01.01`, fase M01 de **Capabilities y permisos**, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Ejecutor PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. Fuentes únicas: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet V5 PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Resultado exigido de fase, AÚN NO IMPLEMENTADO:** modelar capacidades por rubro, plan, rol, tenant, sucursal y recurso, con denegación por defecto y matriz de capabilities. Esta microtarea solo **fija objetivo, no-alcance y criterio de no-regresión**. Su cierre certifica un documento, nunca autoriza una entidad de permisos nueva, la implementación de capabilities, acceso comercial, cambios de base de datos o release.

## Objetivo trazable de C01-02

Para una operación futura `(actor, tenant, sucursal, recurso, acción)`, la decisión de acceso será verificable mediante identidad y membresía server-side, grants RBAC persistidos y restricciones comerciales de rubro/plan cuando se definan. La política debe ser **fail-closed**: ausencia de cualquiera de las condiciones obligatorias implica denegación, sin elevar permisos por plan, rol de sistema o estado de UI. La semántica exacta y la precedencia de condiciones se diseñarán en M02, no en M01.01. La autoridad de identidad universal pertenece a C01-01 y deberá revalidarse antes de permitir contratos finales o ejecución.

## Contratos existentes consultados en GitHub

| Componente | Evidencia concreta | Contrato a preservar |
| --- | --- | --- |
| Membresía `UsuarioEmpresa` | `backend/src/Domain/Entities/UsuarioEmpresa.cs` — blob `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Rol efectivo solo dentro de membresía activa y empresa coincidente |
| Contexto tenant | `backend/src/Domain/Security/ContextoTenantActual.cs` — blob `7377d2d752985cc9baf31d08280f38ad94a6a114` | `DesdeMembresia`, `ExigirEmpresa`, no tomar tenant sin verificar |
| Scope Application | `backend/src/Application/Interfaces/IUsuarioScopeService.cs` — blob `5345ee2de1b1e942bff8eded658f5dc27c0a6745` | Consumidor legacy sin tenant falla cerrado; no extender autorización |
| Matriz RBAC | `backend/src/Application/Services/PermisoService.cs` — blob `278a8fa90dec43609d20157208a143f5794dcc35` | Roles de empresa y grants relacionales, rechazo de plantilla/sistema |
| Catálogo | `backend/src/Application/Common/CatalogoPermisosBase.cs` — blob `6060b4c54c0428902603f2157984b693a324e8f3` | Catálogo no es un permiso por sí solo |
| Soporte DEV | `backend/src/Application/Security/PlatformSupportPolicy.cs` — blob `b103f0b0ec7b9169c060661f6a4456f8863152cf` | Allowlist restringida GET; nunca mutaciones ni bypass general |
| Guarda de catálogo | `backend/tests/Solqaryn.Tests/N04CatalogoPermisosRuntimeTests.cs` — blob `00a9044cd8d400cde58b7f4bce63ff98a7432e94` | Atributos de API representables por catálogo; prueba existente, **no ejecutada aquí** |

La fuente documental `docs/ERP_N0_4_RBAC_RELACIONAL.md` explica que `EsAdministrador` no basta para autorizar sin grants explícitos. El documento histórico no sustituye evidencia de CI nueva.

## Dentro del alcance de M01.01

1. Formular el objetivo no ambiguo de la matriz y su delimitación por recurso/empresa.
2. Señalar roles y membresías como autoridad existente, y plan/rubro como posible filtro adicional, no grant automático.
3. Definir criterios de no-regresión testables que preserven aislamiento tenant, JWT, RBAC, ownership y seguridad por denegación.
4. Separar preparativos M01.01–05 de cierre M01.06, contratos M02, implementación M03, UI M04, pruebas M05 y liberación M06.
5. Mantener trazabilidad en 2.340 IDs V5, PLAN_MAESTRO estado H, COLA fórmulas G/O, DAG VALIDADO y BITACORA 26 columnas.

## Fuera de alcance y decisiones pendientes

- **No se implementan** API, DTO, grants, roles, migraciones EF/MySQL, worker, frontend Angular, feature flags comerciales, plan de cobro, nuevos servicios o cambios de infraestructura.
- **No se autorizan** QA, main, PROD, uso de credenciales Alex, ramas, PR, secrets, proveedores nuevos o despliegue; tampoco aperturas de permisos en producción.
- **No se certifica** que la matriz capabilities exista o haya pasado A0–A5 de runtime. A5 independiente es para promoción futura y no puede ejecutarla la Task autora.
- Pendientes para M02: tabla de verdad rubro/plan/rol/tenant/sucursal/recurso; precedencia y expiración; política de revocación, caché y logs; ownership de recursos; puntos de extensión con RBAC; pruebas API/DB negativas; rollback de cambios ejecutables.
- Los contratos preliminares de `C01-02.M01.02`, `M01.03`, `M01.04` y `M01.05` son observación y diseño inicial; no constituyen aceptación de implementación.

## Matriz de no-regresión para consumidores y contratos futuros

| NR | Garantía que no se puede degradar | Prueba/criterio futuro |
| --- | --- | --- |
| NR01 | Empresa A nunca lee ni muta entidad de B | Negativos API A→B y B→A sobre datos de test |
| NR02 | Usuario sin membresía válida no recibe permiso | 401/403 sin filtración de PII/recursos |
| NR03 | Rol sin grants explícitos no accede aunque `EsAdministrador` esté activo | Unit/service API de matriz, fail-closed |
| NR04 | Entitlement de rubro/plan no crea grants en RBAC | Plan válido sin grant → DENY |
| NR05 | Grant persistido no anula una restricción comercial obligatoria | Grant sin entitlement → DENY |
| NR06 | Sucursal/recurso de otro owner no se acepta por ID de cliente | Rechazo server-side e invariantes ownership |
| NR07 | La UI no sustituye la autorización backend | Petición forzada desde navegador sin grant → 403 |
| NR08 | Los roles de sistema/soporte no se convierten en superusuarios | Intento mutación fuera de allowlist → DENY |
| NR09 | Catálogo y atributos de permisos runtime siguen coherentes | Suite `N04CatalogoPermisosRuntimeTests` en CI del SHA afectado |
| NR10 | Recursos legacy sin propietario inequívoco no son públicos | `EmpresaId=null` se cuarentena, no autorización |
| NR11 | Revocación y errores mantienen atomicidad y no filtran datos | Tests de revocación, transacciones y logs seguros |
| NR12 | La trazabilidad no confunde A5 ni evidencia de SHA distinto | Check runs exact-SHA, 26 campos BITACORA, A5 de otra Task |

## Comprobaciones de aceptación y rechazo de M01.01 (solo documento)

**Positivo P1:** objetivo y no-alcance están diferenciados; todos NR01–NR12 incluyen garantía y prueba pertinente de implementación futura. **Positivo P2:** el alcance se limita a la preparación documental y queda sujeto a C01-01/C00 final antes de ejecutar.

**Negativos N1–N6:** no aceptar permiso de plan sin grant; no admitir tenant B desde A; no convertir `EsAdministrador` en bypass; no tratar UI como autoridad; no atribuir test de .NET/MySQL a documento no ejecutable; no cerrar M02/M03/QA/main/PROD ni A5_PASS por la Task autora. Estos son criterios documentales, no pruebas funcionales ya ejecutadas.

## Controles proporcionales, evidencias y rollback

**Clasificación:** `requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO` (diff solo Markdown y CHANGELOG). `requiere_security=SI` (invariantes JWT/RBAC/tenancy y PII revisadas documentalmente). `requiere_scope_lock=SI` por publicación en dev.

A0 inspección de Markdown, cambios acotados y ausencia de secretos; A1 escenarios P1/P2, N1–N6; A2 negativa tenant/rol/plan/PII en contrato, sin reclamar prueba HTTP; A3 V5 ↔ PLAN/COLA/DAG, arquitectura y siete blobs fuentes; A4 fast-forward dev, SHA, árbol, Scope Lock exact-SHA, readback PLAN/COLA/BITACORA. A5 independiente por Task distinta, **PENDIENTE** para cerrar dev. **Rollback:** revert normal no destructivo del commit documental y corrección aditiva de estados/evidencia; jamás force-push ni borrar historia. No cambios QA/main/PROD.
