# SOLQARYN V5 FINAL — C01-01.M01.03: inventario causal de contratos, código y evidencia

**Alcance:** únicamente lectura, inventario y contraste documental de identidad universal: usuario, membresía, empresa, sucursal, rol, recursos y contexto tenant. Fuente ejecutable: Google Doc V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` y `solqaryn/Solqaryn@dev`. La evidencia de este documento **no certifica implementación C01-01, seguridad de todo el SaaS ni comportamiento HTTP/DB real**.

## Matriz de fuentes de dominio y límites

| Fuente exacta (Git blob leído) | Evidencia observable | Límite / prueba futura |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` — `c44940df7c30983a4c23b93338e0cb9367962069` | Raíz `Empresa` con identidad y ciclo activa/inactiva | La clase por sí sola no garantiza filtros tenant en repositorios; verificar operaciones y SQL en fase ejecutable |
| `backend/src/Domain/Entities/Sucursal.cs` — `8746b11fa30cdac0b97241b6221100bedd813b35` | `EmpresaId` nullable por compatibilidad; `ObtenerEmpresaIdTenant` y asignación/reasignación de ownership | Rechazar sucursal sin empresa positiva y cruce A→B no autorizado; revisar migración y consumidores reales |
| `backend/src/Domain/Entities/Usuario.cs` — `cfe319bb61e405379fa632ed18d3183cd2df1a68` | Identidad, PasswordHash, RolId legacy, Activo/Bloqueado/Eliminado | RolId legacy no representa autorización multiempresa; no usar dato de cliente como contexto autenticado |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` — `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía `UsuarioId`/`EmpresaId`/`RolId`, `Activa` y resolución de rol efectivo | Exigir membresía A y B separadas; verificar índice físico y migración sin inferirlos del contrato de dominio |
| `backend/src/Domain/Entities/Rol.cs` — `a4f913d66f2c6bd2351155d66a93f9d12dc18a09` | `AmbitoAutorizacion.Empresa/Plataforma`, `EmpresaId`, `EsSistema`, `Activo`, `Eliminado` | Ni coincidencia de nombre ni rol administrador empresarial habilitan privilegios globales |
| `backend/src/Domain/Security/ContextoTenantActual.cs` — `7377d2d752985cc9baf31d08280f38ad94a6a114` | Fábrica `DesdeMembresia` exige relación activa; `ExigirEmpresa` rechaza tenant distinto | Verificar consumidores API/servicios y controles sobre recurso/sucursal; existencia de fábrica no certifica aplicación transversal |
| `backend/src/Application/Services/AuthService.cs` — `88d18217cd214cc00fca383983d775113fba0c87` | Login BCrypt, estado de cuenta y rol relacional; emisión delegada a `IJwtService` | No inferir selección tenant, revocación de token o validación per-request sin inspeccionar middleware/servicios consumidores |

## Contratos y consumidores pendientes de certificación

- **Identidad / pertenencia:** la relación `UsuarioEmpresa` es la candidata a autoridad por empresa; `Usuario.RolId` de compatibilidad no habilita operaciones en otro tenant. Se requiere mapa de lecturas/escrituras de `IUsuarioRepository`, EF y contexto real para afirmar aplicación de filtros.
- **Sucursal y recurso:** la entidad define owner posible y cambios explícitos, pero deben validarse ACL, auditoría y transacciones de reasignación antes de implementar o aprobar pruebas integrales.
- **JWT/RBAC:** una sesión identificada no basta para conceder un recurso; la autorización efectiva debe combinar membresía activa, rol correcto, ámbito y permisos del servidor. La UI no reemplaza el control de backend.
- **Capa de datos:** no consta certificación nueva de indexación, migration, integridad referencial ni ejecución MySQL 8.4 para esta microtarea documental. Esas pruebas quedan para una tarea que modifique o valide infraestructura/persistencia.
- **Fuentes institucionales:** `PLAN_MAESTRO!H` es única autoridad de estado; `COLA!E` deriva por fórmula; `DAG_TECNICO` de este ID está `VALIDADO` con once prerequisitos (C00-01.M02.01–06 y C00-03.M01.01–05) LISTO. El adelanto autoriza únicamente M01.01–05 documental.

## Criterios dirigidos para revisión independiente

| Caso | Aceptación documental | Exclusión de afirmaciones |
| --- | --- | --- |
| P1 positivo | Las siete rutas y blobs anteriores se resuelven en `dev` y el documento asocia cada contrato con su frontera | No declarar pruebas de integración ya ejecutadas |
| P2 positivo | La resolución de `ContextoTenantActual.DesdeMembresia` y `UsuarioEmpresa.ObtenerRolEfectivo` aparece en código fuente con identidad y empresa diferenciadas | No inferir que todos los controladores usan la fábrica |
| N1 cross-tenant | Una cuenta A no debe acceder a recursos B sin membresía B verificada; caso futuro HTTP/DB obligatorio | No tratar `EmpresaId` del request como autorización |
| N2 compatibilidad | `Usuario.RolId` legacy, nombre de rol o `EsAdministrador` no implican autoridad global ni nuevo tenant | No crear bypass para roles viejos |
| N3 tenant indefinido | Sucursal sin `EmpresaId` positivo debe negar ownership; no usar primer tenant como fallback | No considerar datos sin owner como públicos |
| N4 cuenta revocada | Bloqueo, eliminación, membresía inactiva y token revocado deben fallar cerrado en la operación futura | No inferir revocación JWT runtime desde clase de entidad |
| N5 auditoría | Evento nuevo exige `BITACORA!A:Z` (26 campos), `EVENT_ID` e `IDEMPOTENCY_KEY` únicos | No generar esquema de 21 campos ni autodeclarar A5_PASS |
| N6 seguridad | Fallo en Scope Lock, prueba requerida o SHA incorrecto impide certificación posterior | Un job técnico SKIPPED/cancelled no es PASS |
| N7 concurrencia | HEAD cambiante requiere reconciliar changelog y evitar escritura sobre ID ajeno | No hacer force-push, reset ni crear PR/ramas |
| N8 alcance | El resultado exige solo lectura y evidencia con limitaciones; no afirmar API/EF/MySQL/Angular implementados | No liberar M01.06/M02 ni QA/main/PROD |

## Verificación A0–A5 proporcional y reversibilidad

**TIPO 1 documental:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI` por JWT/RBAC/tenant, `requiere_scope_lock=SI` para el SHA de publicación. A0 comprueba Markdown/diff/no secretos/changelog aditivo. A1 verifica P1/P2 y N1–N8 documentalmente. A2 revisa denegación por defecto, límites de cuenta/tenant y no promoción. A3 contrasta siete blobs, arquitectura, V5, DAG, PLAN, COLA y BITACORA A:Z. A4 únicamente tras commit fast-forward: SHA exacto, Scope Lock `completed/success`, diff/blobs, rollback y readback. A5_PENDIENTE exige segunda Task para A5_PASS.

**NO_APLICA:** builds .NET/Angular, Docker, MySQL y E2E, ya que esta unidad no introduce cambios ejecutables. NO se reportan pruebas live, tenant A/B HTTP o migraciones. **Rollback:** `git revert` normal del commit documental después de revalidar HEAD; preservar BITACORA histórica y añadir reconciliación idempotente. Prohibidas QA/main/PROD, servidores, secretos, nuevas Tasks, ramas o PR.
