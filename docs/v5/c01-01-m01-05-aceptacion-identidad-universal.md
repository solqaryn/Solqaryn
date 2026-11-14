# C01-01.M01.05 — Aceptación funcional y técnica observable de identidad universal

**Fuente única:** V5 FINAL, unidad `C01-01.M01.05`, [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Autor PRIMARY DEV 04 — JAVIER, `TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09`, repo exclusivo `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido de fase pendiente de implementación:** definir tenant, sucursal, propiedad, recurso, usuario, membresía y contexto, mediante contratos de dominio y matriz de pertenencia. **Esta M01.05 solo define criterios de aceptación, no implementa el contrato de toda la fase.** DAG permite preparación documental independiente M01.01–05 tras 11 predecesoras C00; M01.06 y M02+ requieren revalidar cierre C00, identidad, seguridad y dependencias. No usar este documento como prueba de runtime o privilegios efectivos.

## Lecturas concretas de contratos existentes

| Fuente actual GitHub DEV | Blob verificado | Frontera observable y límite |
| --- | --- | --- |
| `backend/src/Domain/Entities/Empresa.cs` | `c44940df7c30983a4c23b93338e0cb9367962069` | Empresa raíz tenant, estado Activa y nombre no vacío; no equivale a membresía |
| `backend/src/Domain/Entities/Sucursal.cs` | `8746b11fa30cdac0b97241b6221100bedd813b35` | `EmpresaId?` legacy, `ObtenerEmpresaIdTenant` debe rechazar ausencia/valor no positivo; no asumir sucursal sin dueño como global |
| `backend/src/Domain/Entities/Usuario.cs` | `cfe319bb61e405379fa632ed18d3183cd2df1a68` | Identidad, `PasswordHash`, `RolId` legacy; estos datos no autorizan sin contexto empresarial |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | Membresía usuario↔empresa activa, rol efectivo propio, IDs positivos y coincidencia |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | Fábrica `DesdeMembresia`, `ExigirEmpresa`, fail-closed ante empresa ajena |
| `backend/src/Application/Interfaces/IUsuarioScopeService.cs` | `5345ee2de1b1e942bff8eded658f5dc27c0a6745` | Scope tenant desde membresía verificada, fallback legacy sin autorización |
| `backend/src/Application/Services/PermisoService.cs` | `278a8fa90dec43609d20157208a143f5794dcc35` | Grants relacionales con restricciones de rol/empresa, sin superpermiso por metadato admin |

**Límite:** estos archivos se leyeron como texto fuente/blobs; no se ejecutaron tests HTTP, .NET, MySQL ni navegación para esta unidad. Las pruebas futuras se ejecutarán en GitHub Actions sobre SHA exacto cuando exista cambio ejecutable.

## Matriz de aceptación para implementar en M02/M03

| ID | Resultado observable exigible | Verificación positiva y negativa futura | Criterio de rechazo |
| --- | --- | --- | --- |
| ID01 | Empresa activa identificable por ID válido, lifecycle estable | Crear/consultar empresa autorizada; rechazar nombre vacío y empresa inactiva según caso | Identidad de empresa ambigua |
| ID02 | Usuario autenticado vinculado a membresía activa | Resolver empresa propia; rechazar usuario sin membresía y membresía suspendida | Autorizar solo por JWT sin membresía |
| ID03 | Rol efectivo ligado a usuario/empresa, no rol global legacy | Cambiar empresa cambia rol efectivo; impedir reuse de rol de A en B | Permitir privilegios cross-tenant |
| ID04 | Contexto tenant construido únicamente desde `UsuarioEmpresa` validada | Positivo `DesdeMembresia`; negativos usuario/empresa ajenos | Aceptar `EmpresaId` del cliente |
| ID05 | Ownership de sucursal exige `EmpresaId` positivo o cuarentena | Sucursal propia accesible; sucursal sin dueño o de B rechazada | Tratar `EmpresaId=null` como recurso público |
| ID06 | Recurso y sus relaciones sucursal/almacén/ubicación no cruzan tenant | A→A con ID propio; A→B, B→A y recurso sin dueño DENY | Filtración de inventario entre empresas |
| ID07 | Acciones empresariales respetan RBAC y propiedad | Grant explícito autoriza; rol inactivo/sistema/ajeno DENY | Privilegios por UI o `EsAdministrador` |
| ID08 | Selección de empresa muestra solo membresías válidas | Lista de empresas del usuario; no enumerar empresas globales | Divulgación de otras empresas |
| ID09 | Sesiones y claims inválidos/expirados nunca resuelven tenant | JWT válido + membresía; token inválido/revocado 401/403 | Bypass de autenticación |
| ID10 | Errores de acceso no exponen PII, hashes ni JWT | Respuesta segura sin datos; revisar logging y auditoría | Fuga de secretos en error/log |
| ID11 | Roles y dueños se preservan en migraciones y alta | Pruebas EF/MySQL de FK, constraints y rollback cuando se implemente | Migración no comprobada/huérfanos |
| ID12 | Capabilities comerciales futuras no sustituyen membresía ni permisos | Plan habilitado sin membership/grant = DENY | Escalada por plan/rubro |
| ID13 | Soporte DEV mantiene allowlist exacta y no se expande por esta fase | GET permitido bajo política; escritura ajena DENY | Convertir soporte en superadmin |
| ID14 | Cambios concurrentes de identidad/membresía conservan consistencia | Revocación y transacción deterministas con pruebas causales | Sesión conserva privilegio revocado |
| ID15 | Integración API/UI/DB demuestra denegación server-side | Pruebas positivas/negativas reales y E2E si UI cambia | Test mock como sustituto de backend |
| ID16 | Publicación y cierre rastreables | SHA exacto, A0–A4 PASS, BITACORA 26 columnas y A5 otro TASK_ID | SKIPPED/PENDING de gate requerido como PASS |

## Invariantes y controles transversales

- Una identidad global de `Usuario` no implica autoridad sobre cualquier `Empresa`: se requiere `UsuarioEmpresa` activa y rol tenant adecuado. `RolId` legacy no se convierte en autorización adicional.
- `Sucursal.EmpresaId=null` y recursos sin propietario explícito fallan cerrado hasta migración autorizada; no asumir datos de otra empresa por pertenencia a una misma cuenta.
- La empresa A nunca puede consultar/mutar recursos de B, aunque UI, ruta, cabecera o identificador suministrado sugieran el contrario.
- No exponer `PasswordHash`, JWT, secretos, direcciones, PII ni información de pertenencia empresarial a un actor no autorizado.
- No alterar API/DTO, EF Core, RBAC, CORS, rate limiting, Cloudinary, infraestructura ni reglas de ramas en el presente documento.

## Validaciones de este documento, separadas de pruebas futuras

**P1 (documental):** ID01–ID16 cubren empresa, sucursal, recurso, usuario, membresía, rol, contexto, seguridad e integración, cada uno con evidencia positiva y negativa futura. **P2 (documental):** distinguir los contratos existentes de decisiones/migraciones pendientes sin prometer implementación.

**N1–N6 (documentales):** rechazo de sesión sin membresía, A→B, sucursal sin owner, escalada por `EsAdministrador`, `EmpresaId` enviado por frontend como autoridad, y certificación de runtime con solo documentación. **N7–N8:** rechazar jobs `SKIPPED` como prueba exigida; rechazar A5_PASS de la Task autora o promoción QA/main/PROD. No se declaran tests HTTP/DB ejecutados.

## Clasificación, auditorías A0–A5 y reversión

`requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` para este documento sin código ejecutable. `requiere_security=SI` (revisión documental JWT/RBAC/tenant/PII), `requiere_scope_lock=SI` (commit de documentación en dev).

A0: inspección del diff y secretos, Markdown y CHANGELOG preservado; A1: matriz ID01–ID16 y P1/P2/N1–N8 documentales; A2: amenazas cross-tenant y denegación sin fuga; A3: trazabilidad Plan V5, DAG con once predecesoras LISTO y blobs de fuente; A4: commit directo fast-forward, diff/árbol/SHA exacto, check Scope Lock requerido y readback PLAN/COLA/BITACORA; A5_PENDIENTE para Task diferente. **Rollback:** `git revert` no destructivo y rectificaciones institucionales aditivas; no cambios a QA/main/PROD ni datos o despliegues.
