# C01-02.M01.02 — Actores, entradas, salidas y superficies de capabilities/RBAC

**Ámbito:** preparación documental, V5 FINAL `C01-02.M01.02`; ejecución atribuida a PRIMARY DEV 04 — JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. Fuente ejecutable: [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Sólo `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado de fase objetivo (NO ejecutado por M01.02):** modelar capacidades por rubro, plan, rol, tenant, sucursal y recurso, con denegación por defecto. Este inventario determina posibles entradas, salidas, actores y superficies para su posterior contrato; no crea endpoints, claims, migraciones, roles, permisos o entitlements. Requiere revalidación con C01-01 y C00-01 final antes de M01.06 y cualquier ejecución M02–M06.

## Evidencia verificada y fronteras

| Fuente real | Rol observable | No inferir |
| --- | --- | --- |
| `backend/src/Domain/Security/ContextoTenantActual.cs` (blob `7377d2d752985cc9baf31d08280f38ad94a6a114`) | Tenant, usuario y rol efectivo desde membresía; `ExigirEmpresa` rechaza discrepancia | Que un `EmpresaId` del cliente autorice |
| `backend/src/Application/Services/PermisoService.cs` (blob `278a8fa90dec43609d20157208a143f5794dcc35`) | Matriz de grants por rol; rechazos para rol inactivo, sistema o sin propietario | Acceso por solo atributo `EsAdministrador` |
| `backend/src/Application/Common/CatalogoPermisosBase.cs` (blob `6060b4c54c0428902603f2157984b693a324e8f3`) | Identificadores técnicos de módulo/acción, no autorización automática | Que catálogo implique grant |
| `backend/src/Application/Security/PlatformSupportPolicy.cs` (blob `b103f0b0ec7b9169c060661f6a4456f8863152cf`) | Soporte restringido DEV con allowlist explícita de lecturas | Acceso global de soporte por pertenencia al equipo |
| `backend/tests/Solqaryn.Tests/N04CatalogoPermisosRuntimeTests.cs` (blob `00a9044cd8d400cde58b7f4bce63ff98a7432e94`) | Prueba existente contra desalineación atributos API/catálogo | Haberla ejecutado en este trabajo |
| `docs/ERP_N0_4_RBAC_RELACIONAL.md` | Contrato documental de grants relacionales | Certificación actual de nuevas capabilities |

## Matriz preliminar de actores y superficies

| Actor / consumidor | Entrada prevista | Salida observable | Límite de autorización y prueba futura |
| --- | --- | --- | --- |
| Usuario con membresía empresarial activa | Sesión JWT autenticada, empresa seleccionada y recurso solicitado | Acción permitida o 403 sin datos de terceros | Resolver contexto desde membresía verificada y comprobar rol/permiso efectivo |
| Administrador de empresa | Rol empresarial con grants explícitos y operación sobre roles propios | Matriz empresarial editable sólo dentro de su empresa | Rechazar rol sistema, otro tenant o grants ajenos |
| Usuario sin membresía o suspendido | Identidad válida pero sin relación activa con empresa | 403/denegación sin revelar recursos | Negativo A→B y sin membresía |
| Usuario con plan/rubro habilitado pero sin grant | Entitlement de plan/rubro (fuente futura a definir) | Denegación; no se eleva RBAC por plan | Negativo: plan no sustituye rol/permiso |
| Usuario con grant pero plan/rubro restringido | Grant persistido, condición comercial no satisfecha | Denegación conservadora, política comercial por definir | Revalidar intersección entitlement+RBAC (decisión M02) |
| Usuario en sucursal distinta de recurso | Empresa correcta pero sucursal/recurso ajenos | Denegar cuando ownership lo exija; no cambiar datos | Negativo entre sucursales; sin suponer un modelo aún no aprobado |
| Soporte plataforma en DEV | Solicitud GET sujeta a allowlist exacta y contexto verificado | Sólo lectura limitada a rutas permitidas | `PlatformSupportPolicy` no habilita rutas generales ni operaciones de escritura |
| Cliente Angular/UI | Estado de sesión, menú, indicadores de capability | Mostrar/ocultar experiencia; nunca autoridad final | Forzar petición manual y comprobar rechazo de la API |
| API/Application | Identidad validada y operación de dominio/DTO | 2xx autorizado, 4xx seguro, auditabilidad | JWT/RBAC/tenant server-side; no confiar en headers o IDs suministrados |
| Repositorio/EF/MySQL | Consulta filtrada por propietario/tenant y contexto | Filas del ámbito o resultado vacío/error | Prueba integración real cuando exista implementación; no realizada aquí |
| Auditoría y jobs | Actor técnico, correlación y alcance autorizado | Registro mínimo sin secretos ni PII; acción reproducible | Jobs no heredan privilegios de usuario externo por defecto |
| Invitado/público | Solicitud sin JWT ni membresía | Sólo rutas públicas explícitas, sin datos empresariales privados | Denegación para acciones RBAC y tenant-owned |

## Contratos propuestos, pendientes de diseño M02

**Entradas a definir:** `ActorId`, membresía activa, `EmpresaId` verificado, `SucursalId` si aplica, recurso con dueño real, `Modulo/Accion` del catálogo, rol efectivo, rubro/plan y vigencia de la capability. Los claims o valores recibidos NO constituyen por sí solos autorización.

**Salidas a definir:** decisión ALLOW/DENY respaldada por reglas trazables; error HTTP seguro, auditabilidad y explicación de rechazo sin divulgar PII. La salida de UI puede diferir visualmente, pero no alterar la decisión backend.

**Interfaz futura:** matriz `rubro × plan × rol × tenant × sucursal × recurso × acción`, con precedencia explícita, default DENY, revocación y negativos cross-tenant; no existe endpoint de capability declarado ni se autoriza crear uno en M01.02.

## Supuestos y riesgos pendientes

S1: el RBAC relacional de dominio sigue siendo autoridad; S2: rubro/plan añaden restricciones, no grants; S3: sucursal/recurso poseen ownership comprobable; S4: C01-01 definirá contrato final de identidad/membresía; S5: registros legacy sin `EmpresaId` no son públicos por defecto. Cada supuesto debe invalidarse si el código o plan final contradice su premisa.

R1: privilege escalation por entitlement comercial; R2: confusión de tenant de sesión con identificador del cliente; R3: bypass de rol administrador; R4: falta de filtro por sucursal; R5: catálogo y atributos runtime divergentes; R6: secretos/PII expuestos en errores/auditoría; R7: autoría duplicada o evidencia de SHA anterior. Mitigación transversal: fail-closed, pruebas positivas/negativas proporcionales, owner/claims de Sheets únicos y A5 externo.

## Positivos y negativos documentales

**P1:** el flujo usuario miembro con grant y capability permitida conduce a una ruta de autorización backend explícita. **P2:** una decisión DENY produce salida segura sin mutar datos y sin permisos UI como autoridad.

**N1:** plan sin grant no autoriza; **N2:** empresa A no accede a B; **N3:** administrador sin grants no obtiene bypass; **N4:** soporte intenta escritura u otra ruta fuera de allowlist y debe ser denegado; **N5:** rol inactivo/sistema/ajeno no se modifica; **N6:** permiso frontend sin RBAC backend no autoriza. Estos seis son **casos de aceptación de diseño**, no tests funcionales/HTTP ejecutados.

## Clasificación A0–A5, restricciones y rollback

`requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` porque los únicos archivos modificados son documentación y CHANGELOG. `requiere_security=SI` (inspección documental JWT/RBAC/tenant y límites de datos), `requiere_scope_lock=SI` por commit dev.

A0: fuentes, diff, sintaxis, secretos, preservación CHANGELOG. A1: P1/P2 y N1–N6 documentales. A2: frontera auth/tenancy y rechazo explícito. A3: Plan V5, DAG VALIDADO y once prerrequisitos LISTO, código/arquitectura. A4: commit directo fast-forward, SHA/árbol/diff, Scope Lock exact-SHA y readback de 26 columnas. A5: revisión por otra Task; no autoaprobación. **Rollback:** revert Git normal no destructivo del commit, reconciliación aditiva en Sheets/BITACORA. No QA/main/PROD, migraciones, licencias, cobros, despliegue ni autoridad nueva.
