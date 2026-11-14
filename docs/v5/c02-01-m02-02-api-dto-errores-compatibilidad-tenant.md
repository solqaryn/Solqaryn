# SOLQARYN V5 FINAL — C02-01.M02.02: API, DTO, errores, validaciones y compatibilidad tenant

**Entrega:** contrato documental M02.02; NO implementación. **Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076`; `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Superficie actual inspeccionada: no ampliar permisos implícitamente

Existe `backend/src/API/Controllers/TenantContextController.cs` (blob observado `2407c2eb6779ed724e841c688ee14dabbf163171`): `[Authorize]`, `GET /tenant-context/empresas` que invoca `IUsuarioScopeService.ListarEmpresasSeleccionablesAsync`, y `GET /tenant-context/{empresaId:int}` que invoca `ObtenerActualAsync` y responde `Forbid()` si no existe contexto autorizado. Esto **no demuestra** seguridad de todas las rutas del producto ni define host-routing universal. El cliente solicita, pero el servidor decide.

El dominio actual `UsuarioEmpresa` (blob `f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c`) vincula usuario, empresa y rol con `Activa`, y `VerifiedCommercialTenant` (blob `2da140ce9d7f47329d39d292d3271596467910cc`) extrae sólo la marca server-side autorizada. Contrato previo `C02-01.M02.01` distingue ownership real y diseño futuro. La implementación API, middleware, proveedor de host y persistencia requerirá otro scope/CI.

## 2. Contratos de operación y DTO propuestos, no endpoints agregados

| Operación futura o actual | Entrada de cliente NO autoritativa | Salida autorizada | Política |
| --- | --- | --- | --- |
| GET existente `/tenant-context/empresas` | JWT de usuario, sin `EmpresaId` privilegiado | Lista mínima de empresas seleccionables devuelta por servicio | Membresía activa, usuario/empresa vigentes; nunca catálogo de todas las empresas |
| GET existente `/tenant-context/{empresaId:int}` | `empresaId` positivo en ruta | `UsuarioTenantScopeActual` únicamente si el backend autoriza | Sin membresía -> `Forbid`, no emitir contexto de B |
| Resolución futura de host/slug (NO existe aquí) | host normalizado, path/slug y metadatos de proxy con origen confiable | Resultado interno `ResolvedTenantContext` con empresa verificada, fuente y expiración de request | Mapping host único, sin fallback a tenant default, proxy confiable y autorización empresarial cuando aplique |
| Cambio futuro de contexto (NO existe aquí) | petición `SelectTenantRequest { EmpresaId }` con intención explícita | `AuthorizedTenantView { EmpresaId, RoleScope, PermissionsVersion }` filtrado | Revalidar JWT, membresía actual y RBAC; ningún rol o claim aceptado del DTO |
| Diagnóstico/auditoría futura (NO existe aquí) | correlación opaca del request, sin tokens | Resultado seguro no enumerativo y evento mínimo | Sin datos/PII de B, sin secretos, sin grants implícitos |

Los nombres de tipos `ResolvedTenantContext`, `SelectTenantRequest` y `AuthorizedTenantView` son **propuestas**; no existen como código creado por esta tarea. No se agregan endpoints, respuestas públicas, tablas, middleware, headers confiables, configuración ni migraciones.

## 3. Validación, compatibilidad y errores seguros

- **V01** autenticar firma, audiencia y expiración JWT en el middleware vigente; ningún claim aislado concede acceso a empresa.
- **V02** comprobar `EmpresaId > 0`, formato de host y longitud, sin promover su valor a autoridad.
- **V03** consultar membresía `UsuarioEmpresa` activa, existencia/vigencia de usuario y empresa, rol del tenant y acceso al recurso; nunca reutilizar `Usuario.RolId` legacy como grant global.
- **V04** cruzar `EmpresaId` efectivo con propietarios de recurso/sucursal en el servidor y denegar A→B.
- **V05** no aceptar `X-Empresa-Id`, `X-Tenant`, slug, host o cookie como grant; cabeceras forwarded sólo desde proxy formalmente configurado.
- **V06** cambio o revocación de membresía invalida elegibilidad; no heredar permisos de sesión/caché de otro tenant.
- **V07** privacidad: mensajes de rechazo sin enumerar empresas externas ni exponer PII, secretos o detalles del proveedor.
- **V08** compatibilidad: conservar rutas `/tenant-context/*` y envoltura de `ApiResponse` existentes; si un cambio futuro altera DTO, versionar primero y probar clientes Angular.
- **V09** idempotencia: lecturas no cambian estado; una selección futura no persiste autoridad sólo por enviar mismo request.
- **V10** normalización de errores: 400 entrada inválida, 401 no autenticado, 403 cuando no se autoriza; 404/403 seguros para recursos ajenos sin enumeración; 409 para conflicto real propio; 5xx de dependencias fail-closed sin stack ni datos externos. No afirmar que el controlador actual produzca todos estos códigos.
- **V11** auditoría y observabilidad: correlación opaca y resultado, nunca JWT, cookie, secretos o payload cross-tenant.
- **V12** toda nueva ruta requiere pruebas positivas/negativas HTTP reales, control de JWT/RBAC, A/B y MySQL efímero cuando incorpore persistencia.

No usar respuesta `Ok` de un selector como prueba de autorización de posteriores consultas a recursos. La UI y el dominio público de tienda no pueden emitir una marca empresarial privada.

## 4. Matriz de aceptación propuesta (oráculos, no tests ejecutados)

- **P01:** JWT vigente + membresía A activa -> listado incluye sólo empresas seleccionables.
- **P02:** GET de A por su miembro vigente devuelve contexto A sin atributos de B.
- **P03:** miembro A/B con roles distintos puede seleccionar B tras nueva validación; permisos A no se heredan.
- **P04:** compatibilidad de `GET /tenant-context/empresas` y `GET /tenant-context/{empresaId:int}` preservada en futuras implementaciones.
- **N01:** GET de B por usuario A sin membresía -> rechazo seguro, sin existencia de B en respuesta.
- **N02:** host/slug/forwarded header manipulados -> ningún contexto empresarial concedido.
- **N03:** JWT sin validez, expirado o sin usuario vigente -> no autorizar.
- **N04:** membresía revocada o empresa suspendida con JWT previo -> rechazo sin caché de A.
- **N05:** `EmpresaId=0`, negativo o no entero -> error validado, sin consulta privilegiada.
- **N06:** rol legacy/global no concede permiso de tenant B ni sucursal de B.
- **N07:** recurso A sin propiedad positiva o de B -> bloqueo sin lectura/escritura.
- **N08:** dependencia de autorización/host indisponible o dos empresas para un host -> fail-closed.
- **N09:** respuesta 403/404, logs y auditoría no permiten enumerar empresas ajenas ni revelar tokens/PII.
- **N10:** nombre de DTO propuesto no se transforma en API activa, migración o grant implícito.
- **N11:** check de CI `queued`, `skipped`, `cancelled`, `failed` o de SHA anterior no se marca PASS requerido.
- **N12:** A5 de autora, dueño compartido, COLA sin DAG o cambios QA/main/PROD no pasan gates.

Estos escenarios se formalizan como **contratos documentales** que deberán materializarse con pruebas reales en las microtareas de código pertinentes. Ningún resultado runtime se atribuye a M02.02.

## 5. Dependencias, auditorías, rollback y frontera de entrega

Antes de esta M02.02: `C02-01.M01.01–06` LISTO con A5 Javier independiente, PLAN H297=PENDIENTE/I297 sin responsable, COLA G297/O297=SI y DAG_TECNICO F=VALIDADO/G=SI para el ID exacto, seguido de claim CAS y BITACORA A:Z. La pertenencia y los permisos reales siguen sujetos a sus validaciones de runtime futuras. No se libera M02.06/M03 ni IAM, JWT, DB, despliegue o QA/main/PROD por esta documentación.

**A0**: lectura de código/canon, revisión de diff y CHANGELOG autocontenido sin secretos ni encabezados vacíos. **A1**: comprobar documentación de P01–P04/N01–N12 y validaciones V01–V12; pruebas HTTP no aplican al diff Markdown. **A2**: amenazas A/B, spoofing, revocación, rol, PII, errores y fallas cerradas revisadas documentalmente. **A3**: matriz de predecesores, DAG, código y compatibilidad; no acreditar integración HTTP/MySQL. **A4**: commit fast-forward CAS en dev, dos blobs verificables, Scope Lock y check V5 de CHANGELOG exact SHA `completed/success`, readbacks de PLAN/COLA/BITACORA. **A5**: otra Task Javier distinta del autor, previo a promoción de entornos.

Para este changeset sin código ejecutable: .NET build/tests, Angular/Node runtime, MySQL/EF/Docker y Playwright = **NO_APLICA**, no PASS. Controles de Scope Lock y CHANGELOG sí son obligatorios. Reversión del cambio documental: `git revert` normal tras verificar HEAD; nunca alterar BITACORA histórica ni hacer force-push.
