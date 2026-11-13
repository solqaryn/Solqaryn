# SOLQARYN V5 FINAL — C01-03.M01.02: actores, entradas, salidas y superficies de configuración dinámica

**Clasificación:** preparación exclusivamente documental M01 de C01-03. **Resultado exigido futuro C01-03:** separar valores editables de código y protegerlos por tipo y ámbito. Esta unidad sólo identifica actores, entradas, salidas y fronteras para revisar en M02; no crea endpoints, DTO, entidades, flags, jobs o migraciones y no acredita un sistema de configuración universal ya implantado.

**Fuente:** Plan V5 FINAL de Google Drive, `PLAN_MAESTRO` y `COLA` de la misma institución; `DAG_TECNICO` con F=VALIDADO/G=SI para C01-03.M01.02; prerrequisitos C00-01.M02.01–06 y C00-03.M01.01–05 en LISTO. Los adelantos M01.01–05 no liberan M01.06 ni M02. Revalidar con C00-01 final e identidad C01-01 antes de implementar, cambiar permisos o promover a QA/main/PROD.

## 1. Actores y límites de confianza

| Actor | Operación legítima futura | Credencial/autoridad requerida | Denegación imprescindible |
| --- | --- | --- | --- |
| Usuario sin sesión | Acceder sólo a valores explícitamente públicos y no sensibles | Política pública de endpoint por clave/ámbito; no inferir autenticación desde URL | Intentar leer claves privadas o ejecutar cambios devuelve respuesta sin datos |
| Miembro de empresa A | Leer configuración que su pertenencia y rol permitan | JWT válido y membresía/ámbito verificados en servidor | Cliente cambia `EmpresaId`, `tenant_id`, slug o cabecera hacia empresa B: sin fuga cross-tenant |
| Administrador autorizado de A | Modificar únicamente claves allowlisted, con límites, versión y ámbito permitidos | RBAC real en backend; permisos no derivados de UI ni feature flag | Rol faltante, token inválido o petición de secreto: 401/403 sin mutación |
| Cuenta de plataforma | Administrar exclusivamente configuración global autorizada por política explícita | Rol de plataforma diferenciado y auditado, no herencia de tenant | Miembro A no puede escribir configuración global |
| Servicio/trabajador autorizado | Consumir configuración efectiva para caso de uso propio | Identidad de servicio/claims y ámbito resuelto en backend, no confiar en payload externo | No saltar tenancy para procesar colas, jobs o eventos |
| Revisor técnico independiente | A5 de SHA y evidencia técnica, sin cambios de autoría | TASK_ID distinto del autor, conexión real y registro BITACORA | Autoaprobación y promoción implícita a QA/main/PROD prohibidas |

**No se confunden** actor humano, Task física, cuenta Github y cuenta propietaria de los datos. No inventar permisos por existir una pantalla o por configurar un usuario como administrador del equipo ChatGPT.

## 2. Entradas y salidas propuestas (contratos todavía por diseñar)

| ID | Entrada candidata | Salida esperada / contrato por definir en M02 | Validación y riesgo |
| --- | --- | --- | --- |
| IO-01 | Clave `key` del catálogo permitido | Valor tipado, valor por defecto y versión aplicados al scope autorizado | Clave desconocida: denegación; no ejecución de código |
| IO-02 | `value` booleano, número acotado, enum o string limitado | DTO validado y versionado; no CSS/JS/SQL libre | Rechazar `<script>`, `javascript:`, SQL literal y valores fuera de rango |
| IO-03 | Contexto de solicitud y credenciales | Ámbito real inferido de JWT/membresía; identidad del actor auditada | `tenant_id` o URL del cliente no conceden autoridad |
| IO-04 | Versión previa / `If-Match` o equivalente | Aceptación si versión coincide; conflicto seguro si es obsoleta | No sobrescribir un update simultáneo |
| IO-05 | Solicitud de lectura autorizada | Valor efectivo con precedencia/fallback explícita y PII redactada | Nunca mezclar configuración de A con B |
| IO-06 | Solicitud de restaurar default | Cambio autorizado idempotente/auditable o denegación sin parcialidad | Rollback y transacción real antes de activar |
| IO-07 | Error de validación, persistencia o proveedor | Estado seguro, código explícito y correlación sin secretos | No loguear JWT, contraseñas ni datos de otro tenant |
| IO-08 | Cambio de contexto/ruta del navegador | UI descarta respuesta tardía, tema/SEO coherente con ruta vigente | Respuesta A→login/B no reintroduce tema o datos A |

El documento es un mapa de entradas y salidas **propuestas**, no una firma de API aprobada. No exigir que el editor del navegador envíe un `EmpresaId` confiable, ni usar una clave dinámica como predicado de autorización.

## 3. Superficies reales inspeccionadas y consumidores por revisar

| Superficie observada | Archivo y blob Git verificable | Límite de la observación |
| --- | --- | --- |
| Modelo de tema | `frontend/src/app/core/models/tema-visual.model.ts` — `9d99a1da70c9c6da408d9a564e91512150b4aedd` | `TemaVisual` y `CAMPOS_TEMA` son contrato parcial de apariencia, no configuración dinámica universal |
| Cliente HTTP de tema | `frontend/src/app/services/tema-visual.service.ts` — `588aa46fc4c10f8d6be85628e919a7bd89f366ac` | `GET/PUT /tema-visual`, `POST /tema-visual/restaurar`; verificar controller/API real, seguridad y DTO antes de cierre ejecutable |
| Aplicación CSS | `frontend/src/app/services/theme-applier.service.ts` — `cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3` | Regex hex y tokens definidos no sustituyen RBAC backend ni protección de futuras superficies |
| Composición backend | `backend/src/API/Program.cs` — `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Evidencia de stack ASP.NET/JWT/EF, no certificación de endpoint genérico o DB |

Los consumidores futuros implican API, Application, Domain, Infrastructure, MySQL, Angular y workers sólo cuando se implementen. Evitar modificar arquitectura por esta fase. La migración de imágenes históricas es NO_APLICA con inventario cero; nuevos archivos requerirán controles específicos en otro scope.

## 4. Casos documentales de aceptación y rechazo

**POSITIVO-01:** una ficha de entrada contiene clave, valor tipado, actor, scope, versión, salida y error documentados; el servidor es fuente de autoridad, no el navegador.

**POSITIVO-02:** el cliente HTTP existente muestra operaciones GET, PUT y POST restaurar para /tema-visual sin extrapolar roles o contratos que no se han inspeccionado en backend.

**NEGATIVO-01:** usuario de empresa A envía `tenant_id=B` y la solicitud **no** se considera autorizada; validar en futuras pruebas reales de backend con A/B.

**NEGATIVO-02:** una clave `javascript:` o `<script>` no debe entrar a catálogo tipado ni renderizarse como código, HTML o CSS; falta prueba ejecutable cuando se diseñe el editor.

**NEGATIVO-03:** miembro sin rol intenta editar un secreto o configuración global y recibe denegación sin mutación; no se usa un feature flag para elevar permisos.

**NEGATIVO-04:** respuesta de tema de empresa A llega después de navegación a login/B; UI no debe restaurar tema/SEO ajeno. Requerir regresión Angular/E2E dirigida si el cambio modifica ese consumidor.

**NEGATIVO-05:** si existe error de DB, transacción y auditoría deben evitar datos parciales; la ausencia de test MySQL real no constituye PASS de persistencia.

**NEGATIVO-06:** un check GitHub `SKIPPED`, pendiente o de SHA distinto no demuestra test requerido. El cierre de esta documentación se valida con diff, fuentes, Scope Lock exact-SHA y readback institucional, no con build ajeno.

## 5. Riesgos, impacto y continuidad

Impacto de esta unidad: **cero** archivos de runtime, API, DTO, migraciones, autenticación, datos, permisos, secretos, proveedores o UI; sólo nuevo documento V5 y CHANGELOG aditivo. `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI` por límites documentales JWT/RBAC/tenant y `requiere_scope_lock=SI` por commit.

A0 auditoría estática del diff y lectura fuente/blobs, scanner simple de claves/secrets; A1 coherencia de casos positivos/negativos, incluyendo mutaciones que deben invalidar la especificación; A2 límites y denegaciones sin alegar pruebas live; A3 cotejo del V5/DAG y consumidor real; A4 SHA, Scope Lock SUCCESS y readback PLAN/COLA/BITACORA; A5 por una Task distinta, sin autoaprobación.

**Próximas decisiones M02:** catálogo allowlisted, resolución de scopes, precedencia, permisos precisos, versionado/idempotencia, proveedores, validadores y error HTTP coherente. Se revalidará con C01-01 y C00-01 final antes de M01.06/M02; `LISTO_DEV` en M01 no autoriza despliegue ni afirmar que las pruebas de seguridad/persistencia/UI fueron ejecutadas.

**Rollback:** `git revert <SHA_DOCUMENTAL>` tras comprobar HEAD, mediante commit normal; no force-push ni eliminación de eventos BITACORA. A5 independiente y aprobación específica siguen siendo obligatorias para QA/main/PROD.
