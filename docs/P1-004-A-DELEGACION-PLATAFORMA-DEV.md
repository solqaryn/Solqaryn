# SOLQARYN — P1-004.A: delegación administrativa delimitada (DEV)

## Autoridad y estado

Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, P1-004.A (fila 32). Dependencia P0-012.B LISTO. Esta propuesta sustituye el PR histórico #3807 cerrado sin merge; no se reutiliza su rama ni su evidencia EF InMemory como certificación. P1-004.B se certificará después del merge de A con pruebas HTTP/JWT reales, MySQL DEV y auditoría.

## Capacidad y separación

- PlataformaScopeService.ObtenerActualAsync exige rol global **de sistema** EsSistema=true además de identidad y asignación activa revalidada MySQL. Las rutas anteriores de gestión/soporte/empresas/tiendas conservan su frontera superadmin.
- PlataformaDelegacionService.ObtenerDelegadoActualAsync exige usuario JWT vivo (ID/nombre), una sola asignación global activa, rol de plataforma personalizado no sistema activo y no eliminado, EsAdministrador=true, EmpresaId=null, y grant relacional activo con código PLATAFORMA.DELEGACION.EMPRESAS_VER (Configuracion.ConsultarHistorial, Ambito=Plataforma). No se confía en claims ni X-Empresa-Id.
- GET /plataforma/delegaciones/empresas permite exclusivamente al delegado ese grant consultar los primeros 50 metadatos (empresaId, nombre) de empresas activas, sin tablas comerciales. Revalida después de la consulta, audita con EmpresaId=null, CorrelationId y Resultado=Exito y solo responde si la auditoría se persiste.
- POST /plataforma/delegaciones requiere superadmin vivo y acepta exclusivamente UsuarioId y Motivo (10–300). Nunca el propio solicitante ni usuario bloqueado/inactivo/ya global. En transacción serializable crea/reutiliza un rol delegado no sistema, el permiso fijo y RolPermiso, activa UsuarioRolPlataforma y registra auditoría en la misma transacción.
- DELETE /plataforma/delegaciones/{usuarioId} requiere superadmin y motivo; desactiva exclusivamente la asignación delegada de ese usuario y registra auditoría atómica. La siguiente consulta relee MySQL y rechaza la autorización.
- Se reserva la pareja ModuloSistema.Configuracion + AccionPermiso.ConsultarHistorial con Ambito Plataforma, no contenida en CatalogoPermisosBase.Configuracion: evita colisión con el índice único actual sin migraciones. SeedPermisoService limita los grants automáticos empresariales a permisos Ambito=Empresa.

## Amenazas y casos negativos

- Sin JWT, identidad vigente, nombre correcto, asignación única, rol/permiso/grant válido, o con usuario bloqueado, inactivo o eliminado: denegar. Nunca autorizar por EsAdministrador JWT, Usuario.RolId, cabecera X-Empresa-Id o membresía empresarial A/B.
- Delegados no pueden gestionar ni autoconceder privilegios globales; no existe un DTO capaz de elegir otra acción/módulo. El superadmin tampoco puede autodelegarse y esa denegación se audita.
- No se ignoran filtros de datos comerciales ni se agregan permisos a roles empresariales. Un intento de insertar un permission code diferente o role inválido no se repara elevándolo: fail closed.
- Concesión/revocación/auditoría fallida no produce éxito; la transacción MySQL preserva atomía. La GET audita antes de devolver datos.

## Pruebas, impacto y rollback

- Capas: Application interfaz, Infrastructure autorización/semilla, API endpoint y DI, roles/identity RBAC existentes; sin migraciones ni cambios Angular. Consumidores SaaS y ERP conservan separación de ámbito.
- A: tests xUnit P1004ADelegacionPlataformaTests con fixtures efímeros EF InMemory positivos/negativos (no equivalen a HTTP/JWT/MySQL real); Core 3/3, regresión de seguridad y revisión independiente de SHA final y merge protegido.
- B: prueba HTTP/JWT y MySQL real en DEV con identidad delegada autorizada, permisos por tenant/módulo, revocación en vivo, auditoría persistente y rechazo de autoelevación, con readback separado. La certificación de A jamás sustituye B.
- Sin DML directo Aiven durante este PR, sin modificación de usuarios o membresías DEV existentes, secretos ni proveedores; las acciones futuras ocurren solo por API explícita autorizada. Reversión por PR protegido y desactivación auditada de delegaciones que existan en DEV, sin tocar QA/main/PROD.

## Orden de cierre

1. P1-004.A: revisión independiente real, CI exact HEAD, merge protegido a DEV, readback de código y Sheets.
2. P1-004.B: tras A LISTO, prueba LIVE independiente con evidencias propias y negativa de autoelevación; cerrar solo tras PASS, merge si aplica y conciliación.
