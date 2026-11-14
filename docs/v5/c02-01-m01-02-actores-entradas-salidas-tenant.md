# SOLQARYN V5 FINAL — C02-01.M01.02: actores, entradas, salidas y superficies del contexto tenant

**Tipo:** documento preparatorio de M01 para `C02-01 — Contexto tenant server side`. **Resultado de la fase futura:** resolver host, identidad, membresía y ámbito sin confiar en parámetros del cliente. Esta entrega identifica contratos y superficies, **NO** acredita un nuevo resolver universal, integración HTTP, MySQL, JWT/RBAC live ni mecanismo de host autorizado.

**Fuente única:** Plan V5 FINAL (`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet (`1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`), Skill canónica, Scope Lock STRICT, `AGENTS.md` y código vivo de `solqaryn/Solqaryn@dev`. El DAG `C02-01.M01.02` está VALIDADO y permite sólo documentos M01.01–05 tras 16 prerequisitos LISTO: C00-01.M02.01–06, C00-03.M01.01–05 y C01-01.M01.01–05. **No habilita** M01.06/M02 ni ignora cierres y revisión A5 exigibles.

## 1. Actores y responsabilidades verificables en una implementación FUTURA

| Actor | Entrada esperada | Salida permitida tras comprobar autoridad en servidor | Denegación exigible |
| --- | --- | --- | --- |
| Visitante anónimo | Host público y recurso de catálogo | Sólo superficie pública expresamente habilitada | No obtener identidad interna, tenant privado ni metadatos protegidos |
| Usuario autenticado | JWT válido y selección empresarial | Identidad validada, más rol por membresía si solicita tenant | JWT no confiere acceso a cualquier EmpresaId |
| Miembro de A | Token y empresa A solicitada | Scope y recurso propios de A según membresía activa | Alterar cabecera, slug o ID a B no concede permiso |
| Miembro multitenant A/B | Sesión con dos membresías distintas | Selección comprobada, nuevo rol y contexto por empresa | Reutilizar respuesta/caché/rol de A después de cambiar a B |
| Administrador empresarial | Capacidad/RBAC en empresa A | Operar recursos de A sólo según función autorizada | No elevar a plataforma ni modificar recurso de B |
| Operador plataforma | Grant explícito y diferenciado | Acción plataforma auditada si posee permiso real | No derivar autoridad del claim `esAdministrador` |
| Worker/servicio autorizado | Contexto confiable, evento y recurso | Acción de ámbito explícito y trazable | No inferir tenant desde un valor arbitrario de evento |
| Revisor técnico | SHA y evidencia A0–A4 de otro TASK_ID | Dictamen A5 independiente con evento A:Z/26 | Rechazar revisor=autor o `A5_PASS` sin prueba |

Las identidades del usuario de negocio, cuenta GitHub y ejecución de Task son principales distintos. El host y la cuenta de automatización no sustituyen la autorización comercial.

## 2. Matriz IO de alcance preliminar (sin inventar endpoints)

| IO | Entrada o punto de contacto | Salida/capa consumidora esperada | Validación y frontera |
| --- | --- | --- | --- |
| IO-01 | `Host`, forwarded host, slug/subdominio | Candidato de empresa pública en API/gateway | Normalización y allowlist del registro de dominios confiable; no dar acceso por host sin membresía |
| IO-02 | JWT con issuer, audience, firma y expiración | Identidad server-side validada | Rechazo de token falso, revocado o caducado |
| IO-03 | `EmpresaId` solicitado, query/header/DTO | Selección que ingresa a servicio de tenant | Nunca se interpreta el ID recibido como grant |
| IO-04 | Usuario autenticado + Empresa solicitada | `UsuarioEmpresa` activa y rol de ese tenant | Usuario, empresa, rol y estado deben comprobarse |
| IO-05 | Sesión con varias membresías | Empresa inequívoca seleccionada y scope nuevo | Sin selección válida, fail-closed; no `First()` arbitrario |
| IO-06 | Rol y capacidades empresariales | Decisión RBAC en API y repositorio | Rol global no derivado de rol de empresa |
| IO-07 | Sucursal y recurso por ID | Verificación propiedad empresa/sucursal | `EmpresaId` nulo o no positivo, o recurso ajeno, se deniegan |
| IO-08 | Consulta EF / bypass de filtros | Lista/documentos de una sola empresa autorizada | `IgnoreQueryFilters` requiere contexto explícito y evidencia negativa A/B |
| IO-09 | Cambio de empresa con petición tardía | Estado frontend y cache separado por tenant | Descartar respuesta stale y purgar datos previos de A |
| IO-10 | Error/autorización fallida, evento auditado | Código HTTP seguro, correlación, logs sin PII/JWT | Denegación sin filtrar existencia y sin secreto en BITACORA |

**Advertencia contractual:** estas IO son superficies a diseñar/verificar; no se crean rutas, DTO, controller, middleware, tablas ni servicio host nuevos en esta unidad. El codebase existente puede implementar únicamente una parte.

## 3. Fuentes de código real leídas: evidencia vs límite

| Componente | Blob Git referencial | Observación acotada |
| --- | --- | --- |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | `ObtenerRolEfectivo` sólo retorna para membresía activa del usuario/empresa |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | `DesdeMembresia` y `ExigirEmpresa` deniegan contexto cruzado |
| `backend/src/Application/Interfaces/IContextoTenantService.cs` | `97d0a05456c66d069a56b9743bbc286af07502b1` | `empresaIdSolicitada` es selección no autoridad y puede resolver `null` |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Camino tenant contra memberships; fallback monoempresa legacy requiere revisión |
| `backend/src/Infrastructure/Services/CurrentUserService.cs` | `b6e3570a9cbd002527a64358bbdd32f4be229feb` | Lectura de claims, que no certifica privilegios sin backend RBAC |
| `backend/src/Infrastructure/Persistence/TenantQueryBypassGuard.cs` | `2ba91bab0420f7e0fc840cf9c96e2b974838694d` | Ruta `AuthorizedReadEmpresaId` es frontera que requiere pruebas reales en consumidores |

**Limitaciones:** blobs por sí solos no demuestran que cada controlador y endpoint invoca estas guardias, ni permiten certificar DNS, host validation, HTTP, MySQL, E2E o aislamiento de todas las rutas. Releerlos en HEAD vigente antes de implementar.

## 4. Casos de aceptación propuestos, NO ejecutados contra runtime

| Caso | Escenario | Resultado requerido |
| --- | --- | --- |
| P-01 (+) | JWT válido de U, membresía activa en A, recurso propio A | Contexto A legítimo, RBAC y trazabilidad |
| P-02 (+) | U miembro de A y B, selecciona B | Nuevo rol de B; ninguna respuesta de A reutilizada |
| P-03 (+) | Host registrado de A y miembro autorizado de A | Identidad/ámbito A coherentes tras corroborar membresía |
| N-01 (-) | Cambiar `Host` o `X-Forwarded-Host` a dominio no confiable | No conceder tenant ni revelar datos |
| N-02 (-) | Cambiar `EmpresaId` A→B en body, header, URL | Denegación, sin lectura o write de B |
| N-03 (-) | Membresía A revocada, usuario bloqueado o empresa inactiva | Denegación aunque el token esté firmado |
| N-04 (-) | JWT caducado, firma falsa o audiencia distinta | 401/403 seguro, cero acceso |
| N-05 (-) | Usuario multitenant sin selección verificable | No escoger primera empresa ni inferir rol global |
| N-06 (-) | Recurso de B y contexto verificado de A | Rechazar sin filtrar existencia |
| N-07 (-) | Sucursal huérfana sin empresa positiva | Sin fallback y sin mutación |
| N-08 (-) | Rol empresarial intenta operación de plataforma | Denegar por permiso real; no confiar en `esAdministrador` |
| N-09 (-) | Query bypass sin marca `AuthorizedReadEmpresaId` | Denegar salida tenant; test A/B de DB exigible posteriormente |
| N-10 (-) | Respuesta HTTP/log con secreto/JWT/PII | Redactar y correlacionar; no persistir secretos |

P-01–03 y N-01–10 son **especificaciones**, no pruebas vivas; futura suite .NET/MySQL/Playwright proporcional debe ejecutarse en GitHub Actions sobre SHA del código implementado.

## 5. Puertas, no-alcance y reversión

**TIPO 1 DOCUMENTAL:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI_REVISION_DOCUMENTAL`, `requiere_scope_lock=SI` en publicación a dev. No hay cambios ejecutables, DB, migraciones, secretos, DNS, Cloudflare, JWT/RBAC real, configuración, proveedores, UI o datos.

- A0: diff/documento/CHANGELOG acotados, blobs fuente y ausencia de secretos.
- A1: matriz de ocho actores, diez IO, tres positivos y diez negativos especificados.
- A2: límites host falso, membresía revocada, A/B, rol plataforma, PII y fail-closed; ninguna afirmación de pruebas runtime.
- A3: DAG VALIDADO, dieciséis prerequisitos LISTO y coherencia PLAN/COLA/BITACORA de 26 columnas.
- A4: commit directo fast-forward dev, SHA exacto, check Scope Lock `completed/success`, diff/rollback y readback.
- A5: distinta Task; al cerrar dev registrar `A5_PENDIENTE` sin autopromoción.

**Rollback:** `git revert` normal del commit documental con HEAD actualizado, sin force-push, reset, PR ni ramas. La nueva documentación no certifica cierre C00/C01, M01.06/M02, QA/main/PROD ni migración de imágenes (inventario cero = NO_APLICA).
