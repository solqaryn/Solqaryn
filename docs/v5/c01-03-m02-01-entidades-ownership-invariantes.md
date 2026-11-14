# SOLQARYN V5 FINAL — C01-03.M02.01: entidades, propiedad e invariantes de configuración dinámica

**Clase:** M02 / contrato documental, no código ejecutable. **Autor:** PRIMARY DEV 04, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Origen:** Standard de Implementación de Automatizaciones V5 FINAL, ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; estado de `PLAN_MAESTRO`, `COLA` y `DAG_TECNICO`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Resultado y alcance delimitado

Este contrato define las **entidades conceptuales, su ownership, las relaciones y los invariantes verificables** para separar los valores configurables del código ejecutable de SOLQARYN. El alcance exacto de M02.01 es especificación: no instala subsistema de configuración, API, flags, tablas, permisos, jobs, workers ni cambios de datos. Los contratos DTO/API, índices/migraciones, flags, eventos y cierre de M02 son tareas diferentes (M02.02–06) y requieren preflight propio. Las pruebas de API/MySQL/Angular/tenancy reales siguen pendientes de implementación y no se certifican con Markdown.

Se parte de la **autoridad servidor**: identidad de actor autenticado + membresía activa + empresa/sucursal/recurso propio + permiso RBAC vigente + entitlement cuando proceda. El `EmpresaId`, `tenant_id`, header, slug, URL, cookie, DTO o flag del cliente sólo pueden expresar una selección solicitada; no otorgarán acceso.

## 2. Inventario concreto de código existente, NO un nuevo sistema desplegado

| Archivo actual de dev (blob inspeccionado) | Realidad observada | Límite de este contrato |
| --- | --- | --- |
| `backend/src/Domain/Entities/ConfigEmpresa.cs` (`664547d28b513d3187622b02c57a7db1f812f7ec`) | `ConfigEmpresa` con `EmpresaId` positivo, `Moneda`, `ZonaHoraria`, JSON operativos, referencia **opaca** de secreto SMTP y `Version` monotónica; `PlantillaCorreoEmpresa` por empresa | No convertir JSON en código ejecutable; la referencia de secreto jamás debe exponerse |
| `backend/src/Domain/Entities/EmpresaConfiguracion.cs` (`d3d6e59a15c59bc04fa769f8bcefcfe5f50794e3`) | Configuración de marca y datos visibles, con `Activa`, `FechaActualizacion`, múltiples campos editables | Modelo legacy global/activo, sin `EmpresaId` visible: **no asumir aislamiento tenant** de este recurso |
| `backend/src/Infrastructure/Persistence/Configurations/EmpresaConfiguracionConfiguration.cs` (`f238687672981621506175db2cc9d6ec6d02d323`) | Índice `UX_EmpresaConfiguraciones_Activa` y columna computada global `ActivaUnica` | Prohibido presentar este índice como unique por empresa; migración separada y reversible si se decide evolucionar |
| `backend/src/Domain/Entities/FeatureFlagsSaaS.cs` (`ec0062f4d76fd106365c20f3ba50c214369a3de5`) | `PlanModulo` y política pura de entitlement fail-closed que exige plan, suscripción, tenant y regla explícita | Entitlement no sustituye RBAC, membresía, ownership ni autorización de modificar configuración |
| `backend/src/Application/Services/EmpresaConfiguracionService.cs` (`bfe65f93aa00e2ce7cc805b553c7f1b34014b98b`) | Servicio existente con constructor histórico y otro tenant-first, DTOs de configuración y auditoría | No acreditar cobertura tenant completa de todos sus consumidores desde la firma; revisar rutas/API y pruebas en M03 |

**Decisión D01:** distinguir `ConfigEmpresa` operativa tenant-scoped del legado `EmpresaConfiguracion` de presentación/global. No fusionarlas ni asumir aislamiento implícito. **D02:** mantener `PlanModulo` como restricción adicional de módulo, nunca como otorgamiento de permisos. **D03:** el nuevo catálogo tipado, si se aprueba en M02/M03, debe coexistir de forma aditiva y con migración/rollback verificables, sin reescribir tablas existentes por este documento.

## 3. Entidades conceptuales y ownership futuro (sin nuevas tablas)

| Contrato conceptual | Clave / identidad | Owner efectivo | Relación y exclusión |
| --- | --- | --- | --- |
| `CatalogoClaveConfiguracion` | `ClaveCanonica` en allowlist + versión de esquema | Gobernanza de plataforma, editable sólo mediante cambio de código aprobado | Define tipo, límites, ámbitos admisibles, valor seguro y condición de sensibilidad; ningún cliente crea claves |
| `ValorConfiguracionAmbito` | (`ClaveCanonica`, `Ambito`, `OwnerId`, `Version`) conceptual | Plataforma/global o empresa verificada desde servidor; sucursal/recurso sólo si el esquema explícitamente lo autoriza | El mismo nombre de clave en empresas A y B **no comparte valor ni fila** |
| `ActorCambioConfiguracion` | Usuario autenticado + membresía + permiso + contexto servidor | Empresa/scope al momento de la operación | Rol legacy, tenant solicitado o plan solo no dan permiso; cambios requieren reautenticación cuando sea sensible |
| `RevisionConfiguracion` | Secuencia monotónica / compare-and-swap de valor | Misma empresa y misma clave/scope | Rechaza revisión obsoleta y evita pérdida silenciosa por doble escritor |
| `AuditoriaConfiguracion` | Evento idempotente + actor + clave + scope + revisión + resultado | Registro controlado por servidor sin datos sensibles | No copia JWT, secretos, credenciales, PII ajena o valores privados a logs |
| `BanderaFuncionalPermitida` | Clave declarada en allowlist y ámbito permitido | Dueño validado + policy de plan/módulo independiente | Puede condicionar UX, **jamás** deshabilita JWT, RBAC, Scope Lock, rate limiting ni auditoría |

No se dictan nombres de tablas, PK/FK, rutas HTTP ni la representación física de estas entidades; M02.02–03 debe contrastarlos con EF Core 10.0.12, el proveedor MySQL `MySql.EntityFrameworkCore` 10.0.9 y el contrato C01-01. Cualquier valor global queda reservado a un administrador de plataforma con autorización explícita, no a un usuario que se autodenomine global.

## 4. Invariantes y casos falsables de futura implementación

| ID | Regla de diseño | Contraejemplo que debe fallar |
| --- | --- | --- |
| INV-01 | Sólo claves explícitamente allowlisted y normalizadas se consideran editables | Clave desconocida, anidada o Unicode confusable |
| INV-02 | El catálogo fija `bool`, entero acotado, enum o texto validado; valor y default cumplen el tipo | Número fuera de rango, null prohibido, JSON libre o estructura no tipada |
| INV-03 | El backend calcula y valida empresa/ámbito desde membresía activa | A solicita `EmpresaId` de B por body, header, cookie, URL o slug |
| INV-04 | Scope global, empresa, sucursal y recurso tienen owners y precedencia **explícitos** | Empresa B hereda un valor privado de A o un recurso huérfano |
| INV-05 | Lectura/escritura exige permiso RBAC específico y entitlement independiente si aplica | Usuario sin rol vigente con flag o plan habilitado |
| INV-06 | Revocación de membresía/empresa/rol invalida autorización en la próxima operación | Token/caché obsoletos conservan privilegio |
| INV-07 | `Version` y CAS rechazan actualización obsoleta, con no-mutación y respuesta segura | Dos escritores modifican la misma revisión y ambos sobrescriben |
| INV-08 | Secretos, rutas internas, credenciales y claves de infraestructura no son valores editables | Solicitud de revelar `CorreoSecretoReferencia` o cadena de conexión |
| INV-09 | Ningún texto configurable se evalúa como SQL/JS/HTML/CSS arbitrario | `<script>`, `javascript:`, `DROP TABLE`, plantillas con código |
| INV-10 | Un error de validación o proveedor no deja persistencia parcial | Se actualiza valor sin auditoría o se filtra dato B por excepción |
| INV-11 | Flag nunca debilita autenticación, permisos, aislamiento, auditoría ni límites | `disable_rbac=true` solicitado por usuario |
| INV-12 | La especificación no acredita runtime, DB, integración o despliegue | Marcar PASS xUnit/API/MySQL/Playwright sin ejecución exact-SHA |

Matriz mínima P/N: **P01** admin A, miembro activo con capacidad de configuración, cambia clave permitida A y lee versión A; **P02** lector autorizado A recibe fallback seguro de clave inexistente de ámbito permitido; **P03** dos revisiones válidas seriales mantienen historial. **N01** B intenta leer A; **N02** A solicita B en payload; **N03** membresía revocada; **N04** rol o módulo inexistente; **N05** clave no admitida / valor fuera de tipo; **N06** inyección; **N07** revisión obsoleta; **N08** secreto; **N09** error parcial; **N10** flag de seguridad. Se trata de **oráculos documentales**, no de tests ejecutados.

## 5. Dependencias, impacto, verificaciones y reversión

- **Predecesoras:** `C01-03.M01.01–06` LISTO y A5 independiente Javier. `DAG_TECNICO!A232:J232` identifica `C01-03.M02.01` con F=VALIDADO/G=SI; `PLAN_MAESTRO!H224/I224` y `COLA!G224/O224` fueron leídos antes de reclamar. Sólo autorización **para evaluar** este documento, no para cambios IAM/DB.
- **Impacto:** un Markdown nuevo y sección inseparable de `CHANGELOG_AI.md`; Domain, Application, Infrastructure, API, Angular, MySQL, JWT/RBAC/tenant, providers y workflows **sin modificación**. No hay migración, secretos, cambios de endpoint ni consumidores runtime nuevos.
- **A0:** inspección de cinco fuentes por blob, diff limitado, provenance del CHANGELOG sin encabezados vacíos y búsqueda de secretos.
- **A1:** verificar INV-01–12 y P01–03/N01–10 como consistencia del contrato; los tests runtime quedan para fases M03–M05.
- **A2:** amenaza de cross-tenant A/B, membresía revocada, inyección, datos privados y denegación por defecto descrita; no inferir ejecución de pruebas de seguridad live.
- **A3:** compatibilidad documental con C01-01, `ConfigEmpresa`, `EmpresaConfiguracion` y `FeatureFlagsSaaS`; documenta explícitamente la separación de modelado global/tenant y el riesgo de migración.
- **A4:** comprobar publicación fast-forward dev, SHA exacto, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`, y readback de PLAN/COLA/BITACORA **antes de LISTO_DEV**; nunca acreditar `queued`, `skipped` o `failed` cuando sea requerido.
- **A5:** `A5_PENDIENTE` por otra de las cinco Tasks Javier, ID distinto de la autora. Ninguna promoción QA/main/PROD autorizada.
- **CI proporcional a este diff:** `requiere_dotnet=NO`, `requiere_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_playwright=NO`, `requiere_scope_lock=SI`; seguridad revisada documentalmente, no pruebas live fingidas.
- **Rollback:** `git revert` normal del commit documental con HEAD revalidado, preservando modificaciones concurrentes y BITACORA histórica; no force-push/reset, no tocar datos comerciales.

**Condición de salida:** modelo de propiedad y reglas de invariantes especificadas con fuentes concretas, limitaciones visibles y revisión independiente pendiente. `M02.02–06` y todas las fases ejecutables tienen gates propios; esta microtarea no libera ninguna en cascada.
