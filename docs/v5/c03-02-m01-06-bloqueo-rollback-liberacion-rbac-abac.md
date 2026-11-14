# SOLQARYN V5 FINAL — C03-02.M01.06: bloqueo, rollback y liberación documental RBAC/ABAC

**Alcance de esta microtarea:** cerrar la preparación M01 de `C03-02 | Autorización granular y privacidad` mediante reglas verificables de bloqueo y salida; **no implementar políticas, endpoints, JWT, EF, workers ni pruebas live**. Autor: PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`), repositorio `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridades: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

## 1. Predecesores, procedencia y cierre limitado

La preparación M01.01–05 está documentada con archivos versionados e identificadores de blob; sus estados LISTO con A5 independiente se leyeron en PLAN_MAESTRO antes de reclamar este ID. El preflight individual exigió `PLAN H475=PENDIENTE/I475 vacío`, `COLA G/O=SI`, `DAG_TECNICO!F446=VALIDADO/G446=SI` y Scope Lock SUCCESS en HEAD exacto. Los valores son una **fotografía de autorización de esta unidad**, no capacidad de implementar M02–M06.

| Predecesor | Documento de dev | Blob Git exacto | Salida |
|---|---|---|---|
| `C03-02.M01.01` | `docs/v5/c03-02-m01-01-objetivo-no-regresion-rbac-abac.md` | `c3317ea0c9569ef6d88ca61ce8d3afd04f0c855d` | objetivos y 16 límites de no-regresión |
| `C03-02.M01.02` | `docs/v5/c03-02-m01-02-actores-entradas-salidas-superficies-rbac-abac.md` | `7b9a4e58c179cafd5fc9116501d2052cb5bdc7a4` | actores, superficies, correlación y rutas de confianza |
| `C03-02.M01.03` | `docs/v5/c03-02-m01-03-inventario-autorizacion-privacidad.md` | `51b5a01a4770977aadcc780145a17aaed8a3fb41` | inventario de código, tests existentes y brechas |
| `C03-02.M01.04` | `docs/v5/c03-02-m01-04-riesgos-supuestos-decisiones.md` | `abc47c8b61d290ca5d8e14e43ca6f105a244773d` | 12 riesgos, 6 supuestos, 6 decisiones abiertas |
| `C03-02.M01.05` | `docs/v5/c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md` | `a5f551f1bd2cde2c42e6daf9e6e488f247e36602` | matriz AC positiva y negativa FUTURA |

**Fuentes de código existentes inspeccionadas indirectamente desde M01.03:** `TenantPermissionContext.cs` (`df7d49f3e185c538f3b3dafabb36c3a971a7257d`) separa selector `X-Empresa-Id` de autorización; `RequierePermisoAttribute.cs` (`b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4`) requiere grant; `VerifiedCommercialTenant.cs` (`2da140ce9d7f47329d39d292d3271596467910cc`) marca scope tras validación; `PlatformSupportPolicy.cs` (`b103f0b0ec7b9169c060661f6a4456f8863152cf`) limita soporte a lectura específica. Estas comprobaciones de fuente **no prueban** que todos los endpoints, exports, archivos, DTO o rutas futuras sean seguros. Las suites `N65FTenantPermissionIsolationTests`, `P1001ARolesHttpJwtIsolationTests`, `P1007ATenantSwitchAuthorizationTests` y `CommercialTenantMySqlIsolationIntegrationTests` se identificaron en el inventario; **no fueron ejecutadas por este changeset**.

## 2. Matriz de gates, bloqueos y remediación por causa

| Área | Gate de salida M01 / futura M02 | Condición NO_GO y respuesta |
|---|---|---|
| Identidad y JWT | Identidad autenticada, vigente, firma y audiencias válidas; configuración mínima de issuer y rechazo por expiración | Claim, rol o token cliente como autoridad única; token revocado que sigue dando acceso; denegar y probar por request |
| Membresía/RBAC | Membresía empresa activa, rol empresarial, `RolPermiso` por módulo/acción, permiso actual validado en backend | `Usuario.RolId` legado o `EsAdministrador` sin grants como bypass; denegar |
| ABAC tenant y sucursal | Comparar recurso, empresa, sucursal, owner y contexto de aplicación autorizado de servidor | `X-Empresa-Id` de A no da autoridad sobre B; BOLA/IDOR, sucursal cruzada, recurso huérfano = DENY |
| Archivos, URLs, export | Revalidar antes de firmar/emitir URL, leer bytes o exportar CSV/XLSX/PDF; aplicar filtro de empresa/sucursal/PII antes de serializar | Retornar URL de B, filas B o detalles de objeto inexistente/no autorizado = NO_GO, sin revelar existencia |
| Soporte plataforma | Solo GET allowlisted, auditoría y duración/ámbito exactos, sin efectos | Soporte POST/PUT/DELETE, lectura de módulo extra o tenant arbitrario = denegación |
| Revocación y caché | Cada operación protegida revalida membresía y grants; `HttpContext`/tenant por solicitud, sin ALLOW global persistente | Token stale o caché conserva privilegios, mezcla de A/B en concurrencia, fallback permisivo = NO_GO |
| Errores y privacidad | 401/403/404 uniformes según contrato, logs con correlación opaca y sin JWT, PII, secretos o existencia B | Logs y stacktraces cruzados, conteos B o tokens presentes en DTO/errors = NO_GO |
| Persistencia y atomicidad | SQL/EF/MySQL 8.4 efímero con fixtures A/B para cambios que afecten datos; rollback probado | Esquema no comprobado, nulidad/índices ambiguos, timeout causa escritura parcial = mantener VALIDANDO |
| Gobierno/CI | Scope Lock y pruebas causales exact-SHA; A5 independiente de otra Javier Task | SKIPPED/CANCELLED/FAIL/QUEUED/IN_PROGRESS requerido jamás PASS; A5 de autor prohibida |
| Entornos y procesos | Solo dev y archivos autorizados; sin scripts externos, compras, secretos o deploy | No generar ramas/PR, workers, event dispatcher, llamadas entre Tasks o QA/main/PROD por este documento |

Las barreras de alcance no son una excepción dinámica: ante JWT/RBAC/ABAC incierto, se deniega la operación afectada. Un README, test almacenado o flag comercial no certifica API/backend real.

## 3. Dependencias posteriores y criterios verificables

**M02 (contratos):** definir reglas RBAC/ABAC, DTOs/versiones, compatibilidad con las rutas de permisos, ownership de recursos, políticas de revocación y correlación. Cada `C03-02.M02.*` exige un nuevo preflight `PLAN/COLA/DAG`, sin heredar `O=SI` de M01.06. Requisitos comerciales de plan/rubro son filtros adicionales, nunca grants.

**M03 (implementación real):** usar ASP.NET Core 10 `Domain <- Application <- Infrastructure`, API en borde; Angular 22.2.1 solo muestra UX. Implementar guards y filtros únicamente en microtareas autorizadas. Probar integración HTTP/JWT y negativas BOLA/IDOR; cuando toque EF/DB, MySQL 8.4 efímero, casos A/B, rollback y auditoría. No tocar DB productiva, permisos globales ni auth cross-tenant sin pruebas. El diseño no habilita M04/M05/M06 terminal ni despliegue.

**M04–M05 (pruebas):** caso positivo con grant real y recursos A, negativos de recurso B, sucursal B, export B, soporte fuera de allowlist, revocación con JWT todavía vigente, errores y concurrencia A/B. A5 exige TASK_ID diferente del autor y check-run exact-SHA `completed/success`; `skipped/cancelled/in_progress/failed` no es PASS.

## 4. Oráculos de aceptación documental, NO_EJECUTADOS_RUNTIME

| Caso | Tipo | Estímulo futuro | Resultado requerido |
|---|---|---|---|
| P01 | Positivo | JWT legítimo + membresía A + grant A + recurso/sucursal A | Acción autorizada limitada a A y auditable |
| P02 | Positivo | Exportar datos A con permiso específico de export | Solo filas A y campos permitidos, sin PII B |
| P03 | Positivo | Consultar/grabar recurso de A tras rotación de tenant autorizada | Autoridad reevaluada y resultado del tenant correcto |
| P04 | Positivo | Soporte autorizado ejecuta GET estrictamente allowlisted | Lectura mínima registrada sin acceso transversal |
| N01 | Negativo | JWT A solicita ID de recurso B (BOLA/IDOR) | 403/404 seguro; 0 filas, bytes o metadatos B |
| N02 | Negativo | Cabecera `X-Empresa-Id=B` o DTO/claim suplantado | No atribuir empresa/rol B; DENY |
| N03 | Negativo | Rol legacy/`EsAdministrador` sin `RolPermiso` | DENY sin acceso por conveniencia |
| N04 | Negativo | Usuario de sucursal A1 accede recurso de sucursal A2 sin grant | DENY y cero mutación |
| N05 | Negativo | CSV/XLSX/PDF incluye datos B por filtro manipulado | No generar artefacto filtrado incorrectamente |
| N06 | Negativo | URL firmada o metadata media de B pedida desde A | No crear/entregar URL ni existencia B |
| N07 | Negativo | Miembro A pierde rol/grant entre dos solicitudes con JWT válido | Segunda solicitud DENY |
| N08 | Negativo | Dos requests A/B simultáneos comparten caché/scope de request | A/B aislados, sin PII o permisos cruzados |
| N09 | Negativo | Soporte solicita PUT/DELETE o GET módulo no allowlisted | DENY; ningún side effect |
| N10 | Negativo | Fallo de DB / timeout / auditoría en operación protegida | Fail-closed; sin mutación parcial o respuesta con secreto |
| N11 | Negativo | Error devuelve stacktrace/JWT o cuenta de objetos B | Mensaje saneado y correlación opaca |
| N12 | Negativo | Intentar promover M01.06 como certificación runtime o QA/PROD | NO_GO; requiere M02–M06, A5 independiente y autorización explícita |

**Los P01–P04 y N01–N12 están especificados, no ejecutados.** La existencia de código xUnit, archivos de integración MySQL o checks generales no significa cobertura positiva de esta nueva documentación.

## 5. A0–A5 proporcional, control de cambios y rollback

- **A0 documental:** inspeccionar blobs M01.01–05, diff de exactamente este documento + `CHANGELOG_AI.md`, ausencia de secretos y secciones `##` vacías/intercaladas.
- **A1 documental:** comprobar consistencia de cuatro positivos y doce negativos con matriz AC, riesgos y superficie; `NO_APLICA_RUNTIME` por diff solo Markdown.
- **A2 documental:** conservar seguridad JWT/RBAC/tenant A/B, sucursal/ownership, export, media, revocación, soporte y PII fail-closed, sin afirmar tests ejecutados.
- **A3 documental:** cotejar cinco predecesores, rutas actuales y stack; no añadir endpoint, DTO, worker, migración, política dinámica ejecutable ni configuración de producción.
- **A4:** publicación fast-forward con CAS HEAD y `verify-solqaryn-project-scope` + `V5 - Sin encabezados vacios ni atribucion cruzada` completados SUCCESS **exact-SHA**; PLAN/COLA/BITACORA readback. Compilación .NET/Angular/MySQL/Playwright no es prueba causal para Markdown y se clasifica `NO_APLICA` sin contabilizar PASS.
- **A5:** debe emitirlo otra Task Javier diferente de `6adbb9fff1548191a1bd5b30d9e9984a`. LISTO_DEV documental no autoriza QA/main/PROD.
- **Rollback de esta unidad:** `git revert` normal y evento compensatorio aditivo tras verificar HEAD y colisiones; nunca reescribir BITACORA. Para cambios de datos futuros, rollback requiere backup, integridad y ensayo real.

