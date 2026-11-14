# C00-04.M03.03 - Autorizacion y aislamiento de consulta de gobierno

## Contrato real
\`GobiernoCambiosAuthorizationService\` exige un \`ContextoTenantActual\` obtenido por autoridad servidora, empresa coincidente antes del primer I/O y snapshot fresco a traves de \`IdentidadUniversalAuthorizationGuard\`. La lectura de preparacion requiere asignacion relacional activa \`Configuracion/Administrar\` de ambito empresa, sin bypass por rol legacy ni por \`EsAdministrador\`. Solicitud y SHA se validan antes del I/O. Ningun ID de cliente, cookie, header, slug o rol global prueba autoridad.

## Limites estrictos
Solo entrega \`ResultadoPreparacionCambio\` inerte. Nunca ejecuta, persiste, concede permisos ni aprueba ninguna propuesta. Cambios de seguridad, persistencia/migracion o infraestructura/produccion conservan \`NO_GO\` incluso con permiso de consulta y rollback declarado. No se registra DI, API, endpoint, proveedor MySQL, JWT, worker, webhook ni nueva Task. No valida permisos comerciales del propietario ni actua sobre Google Sheets.

## Evidencia y rollback
Pruebas xUnit: positivo tenant A y sucursal A; negativos tenant B antes de I/O, membresia revocada, rol cambiado, falta de permiso, rol legacy, permiso de accion distinta, sucursal B, propuesta sensible y solicitud forjada. CI dirigida backend .NET 10, seguridad tenant A/B y autorizacion/secretos, Scope Lock y procedencia CHANGELOG en SHA exacto; si cualquier check requerido queda pendiente/falla, conservar VALIDANDO. A5 requiere otra Task Javier. Rollback solo git revert normal sobre dev tras comprobar consumidores; sin QA/main/PROD.
