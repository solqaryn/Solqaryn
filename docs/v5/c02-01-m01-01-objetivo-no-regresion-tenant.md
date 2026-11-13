# SOLQARYN V5 FINAL — C02-01.M01.01: objetivo y no-regresión del contexto tenant server-side

**Entrega:** preparación exclusivamente documental M01.01 del control plane multitenant. La salida futura de C02-01 debe resolver host, identidad, membresía y ámbito empresarial **en servidor**, sin convertir parámetros aportados por navegador, cliente o cabeceras en credenciales de autorización. Este documento no implementa, habilita, certifica ni despliega esa solución.

**Autoridad:** Standard de Implementación de Automatizaciones V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), `PLAN_MAESTRO`, `COLA`, `DAG_TECNICO`, `AGENTS.md`, Skill canónica y Scope Lock STRICT de `solqaryn/Solqaryn@dev`. El DAG VALIDADO sólo permite M01.01–05 como estudio provisional y exige 16 prerequisitos terminales: C00-01.M02.01–06, C00-03.M01.01–05 y C01-01.M01.01–05. Antes de M01.06, M02, código, migración o QA hay que revalidar C00 final y contratos de identidad C01.

## 1. Objetivo observable y fronteras de confianza

1. El JWT identifica sesión/usuario cuando es auténtico y válido; no prueba por sí solo que una empresa, sucursal o recurso pertenezcan a ese usuario.
2. Resolver `EmpresaId` efectiva a partir de membresía `UsuarioEmpresa` **activa**, usuario/empresa vigentes y rol empresarial verificado por backend. La selección del tenant recibida del cliente es una petición, nunca un grant.
3. Host, subdominio, slug, query, path, `X-Tenant`, `EmpresaId`, cookie y campos DTO constituyen entradas no confiables. Un host vinculado a una empresa requerirá resolución contra registro de dominios autorizado; la existencia de esa resolución universal NO se certifica aquí.
4. El contexto verificado debe acompañar las consultas y escrituras para denegar recursos de empresa B cuando la sesión pertenece sólo a A. Los controles de UI no sustituyen filtros/guardias de API y repositorio.
5. Sucursal y recurso requieren propietario empresarial positivo y verificable; los registros legacy nulos no habilitan un fallback silencioso.
6. El rol de plataforma no se deriva de un rol empresarial ni del claim `esAdministrador`; los permisos efectivos son independientes por ámbito y se auditan.
7. Ante ambigüedad (múltiples membresías, host falsificado, membresía revocada, token inválido o bypass de filtros sin marca autorizada), **fallar cerrado**, sin exponer PII, IDs internos o existencia de datos de otro tenant.

## 2. Anclajes reales de código y limitaciones

| Archivo de referencia observado en `dev` | Blob SHA de referencia | Qué acredita / limitación |
| --- | --- | --- |
| `backend/src/Domain/Entities/UsuarioEmpresa.cs` | `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c` | `ObtenerRolEfectivo` exige membresía activa y par usuario/empresa coincidente; no acredita que cada endpoint use esta fábrica |
| `backend/src/Domain/Security/ContextoTenantActual.cs` | `7377d2d752985cc9baf31d08280f38ad94a6a114` | `DesdeMembresia` y `ExigirEmpresa` rechazan ámbito cruzado; no acredita cobertura global de API |
| `backend/src/Application/Interfaces/IContextoTenantService.cs` | `97d0a05456c66d069a56b9743bbc286af07502b1` | Selección `empresaIdSolicitada` sin autoridad propia y resolución `null` por identidad/membresía inválidas; implementación/consumidores deben auditarse aparte |
| `backend/src/Infrastructure/Services/UsuarioScopeService.cs` | `14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f` | Camino tenant con miembros y rol real; ruta legacy conservada, debe verificarse su confinamiento monoempresa |

Estos blobs son evidencia de contratos puntuales **leídos**, no prueba de una implementación de host matching, aislamiento extremo a extremo, migración o ejecución HTTP/MySQL. Cualquier modificación posterior exige lectura de blobs actuales.

## 3. Matriz de no-regresión y aceptación FUTURA

| ID | Caso observable | Resultado exigible en ejecución futura |
| --- | --- | --- |
| NR-01 (+) | JWT válido, membresía activa de usuario U en empresa A, recurso de A | Resolver tenant A, permiso efectivo y trazabilidad; sólo recursos autorizados |
| NR-02 (+) | U tiene membresías verificadas A y B, selecciona B | Recalcular rol y contexto de B; descartar respuestas tardías de A |
| NR-03 (+) | Host previamente registrado y verificado para A coincide con membresía A | Resolver empresa A desde fuente fiable y corroborar su autorización; sin inferir que hoy existe el mecanismo |
| NR-04 (-) | Usuario A modifica header `X-Tenant`, `EmpresaId`, slug o path a B | Negar; cero información y cero mutación de B |
| NR-05 (-) | Host falso, spoofed `Host`/forwarded host o dominio sin vinculación confiable | No emitir contexto tenant ni revelar clientes; denegación segura |
| NR-06 (-) | Membresía desactivada/revocada o empresa inactiva | Denegar aunque el JWT permanezca criptográficamente válido |
| NR-07 (-) | Usuario tiene dos empresas y no hay selección inequívoca | Denegar o exigir selección verificada; nunca escoger primera empresa |
| NR-08 (-) | Sucursal sin `EmpresaId` positivo, recurso de B o reasignación cruzada | Rechazar operación y dejar DB intacta |
| NR-09 (-) | Rol empresarial intenta operación global de plataforma o claim `esAdministrador=true` falsificado | Denegar por RBAC server-side y auditar de modo seguro |
| NR-10 (-) | JWT caducado/revocado, firma o audiencia inválidas | 401/403 según contrato, sin acceso ni datos personales |
| NR-11 (-) | `IgnoreQueryFilters` sin scope empresarial autorizado | Bloquear salida cruzada; exigir prueba con dos empresas y queries reales |
| NR-12 (-) | Error, reintento o log contiene JWT, password o PII | Redacción y correlación seguras; nunca persistir secreto ni rastros tenant ajenos |

**Estado de estas pruebas:** casos especificados/documentales; **NO ejecutados** contra HTTP, MySQL, Docker, navegador o proveedores durante M01.01. Para autorizar una fase ejecutable posterior, exigir tests positivos/negativos y de seguridad reales en GitHub Actions sobre SHA exacto y MySQL si toca persistencia.

## 4. Riesgos, dependencias, decisión y rollback

- Riesgo CRÍTICO: confiar en host/empresa cliente, que sólo selecciona contexto y nunca concede autoridad; falta evidencia de host registry y de todos los endpoints consumidores.
- Riesgo CRÍTICO: acceso cruzado A→B mediante filtros omitidos, claims o cuentas con doble membresía. Auditar API, consultas, JWT/RBAC, estado y pruebas negativas antes de despliegue.
- Riesgo ALTO: compatibilidad legacy sin empresa explícita; ninguna ruta nueva debe adoptar fallback implícito.
- Riesgo ALTO: pérdida de contexto y respuestas stale al cambiar de empresa, con fuga de UI/PII.
- Decisión M01.01: aprobar únicamente definición preliminar y matriz de aceptación; NO implementar configuración, tenant routing, DNS, JWT, API, UI, EF, migraciones, workers ni permisos de cuentas.
- Consumidores e impacto potencial FUTURO: API, middleware/auth, `UsuarioScopeService`, DTOs, repositorios EF/MySQL, cache, frontend Angular, logs y store público. Sin nuevos cambios a esos consumidores en este changeset.
- Rollback documental: `git revert` normal del commit acotado, con HEAD revalidado y sin reset/force-push; nunca borrar evidencias institucionales.

## 5. Clasificación de pruebas de esta entrega

**TIPO 1 documental.** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` porque no se modifica código ni datos; `requiere_security=SI_DOCUMENTAL` para fronteras JWT/RBAC/tenant; `requiere_scope_lock=SI` para commit documental en `dev`.

- **A0:** diff exclusivamente documental, integridad de los blobs citados, comprobación de no incluir secretos y CHANGELOG aditivo.
- **A1:** revisión de objetivo, seis fronteras de confianza y casos NR-01 a NR-12 con positivos y negativos; no fingir tests de API.
- **A2:** revisión de denegaciones A/B, host falso, rol/claims, PII y estado de membresía sin alterar código de seguridad.
- **A3:** comparación de referencias con código, plan, 16 prerequisitos LISTO, DAG VALIDADO y BITACORA de 26 columnas.
- **A4:** commit directo fast-forward, lectura de SHA/documento/tree, check `verify-solqaryn-project-scope` `completed/success/head_sha` exacto, readback PLAN/COLA/BITACORA y rollback.
- **A5:** revisión independiente de otra Task; al cerrar LISTO_DEV registrar `A5_PENDIENTE`, no autopromover QA/main/PROD.

Un check `SKIPPED` o `CANCELLED` no cuenta PASS. Un job técnico irrelevante y no requerido no debe bloquear el alcance **documental**; Scope Lock y cualquier gate nativo efectivamente obligatorio no se omiten.
