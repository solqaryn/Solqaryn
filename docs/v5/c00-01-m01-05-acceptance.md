# C00-01.M01.05 - Aceptacion funcional y tecnica observable V5 FINAL

Proyecto solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. Microtarea C00-01.M01.05. Fuente V5 FINAL Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA y seguimiento Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU. Responsable PRIMARY_DEV_03 / JAVIER_CHATGPT / TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3.

## Alcance y resultado
Definir requisitos de aceptacion del baseline C00-01, con señal positiva, negativo, herramienta, evidencia futura y propietario tecnico. Esta matriz no implementa las funciones futuras ni certifica entornos vivos. No cerrar toda la fase M01 por este documento. El catalogo comercial V5 contiene 2.340 IDs; nunca crear un ID 2.341 ni reutilizar V3 historico. V13 aporta solo controles A0-A5 y el protocolo de dev directo.

## Matriz de aceptacion observable

| ID | Requisito y consumidor | Positivo exigido | Negativo obligatorio | Herramienta/superficie | Evidencia futura exigida | Estado documental |
| --- | --- | --- | --- | --- | --- | --- |
| AC01 | Identidad, gobierno y repo solqaryn/Solqaryn | Skill canonica coincide y Scope Lock SUCCESS en SHA exacto de dev | Repositorio ajeno, skill distinta o SHA desactualizado impiden escritura | GitHub, Skill y Scope Lock | Blob, SHA y check exact-HEAD verificables | DEFINIDO_NO_CERTIFICADO |
| AC02 | Plan comercial V5 y selector de tareas | V5 FINAL conserva 2.340 IDs unicos y mismo orden | Una tarea V3 o ID 2.341 no entra en el catalogo | Google Docs y PLAN_MAESTRO | IDs fuente, revision y conteo unico | DEFINIDO_NO_CERTIFICADO |
| AC03 | PLAN_MAESTRO/COLA/BITACORA para estados | PLAN_MAESTRO!H es autoridad; COLA!E/G/O deriva por formula | Doble claim o COLA manual bloquean LISTO | Google Sheets, PLAN_MAESTRO, COLA y BITACORA | Owner, readback, hora y EVENT_ID 21 campos | DEFINIDO_NO_CERTIFICADO |
| AC04 | API de autenticacion y tenancy multiempresa | JWT y RBAC impiden leer registros de empresa B desde empresa A | Token o rol ajeno y tenant de URL se deniegan sin PII | .NET 10 HTTP y MySQL real | Positivo, 403 negativo y consulta aislada | DEFINIDO_NO_CERTIFICADO |
| AC05 | DTO y errores seguros de servicios | Contratos API validos y DTO versionado consumible | Parametros malformados y excepciones no filtran datos internos | .NET 10 y consumidor Angular | Build, codigos HTTP y pruebas contrato | DEFINIDO_NO_CERTIFICADO |
| AC06 | Transacciones y persistencia MySQL | Insert y consulta transaccional usan indices validos | Falla parcial y retry no duplican asiento | EF Core 10 y MySQL real | Prueba transaccional, indice y migracion reversible | DEFINIDO_NO_CERTIFICADO |
| AC07 | UX ligera, teclado y accesibilidad | Pantalla Angular da estados de carga, foco y labels | Modal sin Escape, contraste pobre o doble accion se rechazan | Angular 22 y Playwright real | Resultado de navegador, a11y y presupuesto de carga | DEFINIDO_NO_CERTIFICADO |
| AC08 | Calidad tecnica y publicacion directa dev | A0, A1, A2 y A3 PASS causal antes de FF, A4 post SHA | Test SKIPPED, CI FAIL o HEAD cambiado impiden LISTO | Node, dotnet, Angular y Actions segun impacto | Comandos, versiones, diffs, CI exact-SHA y readback | DEFINIDO_NO_CERTIFICADO |
| AC09 | A5 independiente y promocion controlada | A5_PENDIENTE al cerrar; otro TASK_ID acredita A5_PASS | Autorrevision o A5 ficticio bloquean QA/main/PROD | GitHub, pruebas dirigidas y BITACORA | Reviewer autentico, SHA, positivo, negativo y EVENT_ID | DEFINIDO_NO_CERTIFICADO |
| AC10 | Concurrencia, rollback y trazabilidad | git revert <SHA_PUBLICADO> aditivo preserva historia y trabajo ajeno | force-push, reset o borrar BITACORA estan prohibidos | GitHub expected_sha y Google Sheets | Parent/tree/diff, prueba de reversibilidad y evento correctivo | DEFINIDO_NO_CERTIFICADO |
| AC11 | Proveedores y archivos, fase futura | Orden, backup, restauracion y seguridad antes de mover proveedor | Hostinger, Cloudflare o R2 no se suponen READY; imagen historica NO_APLICA | Vercel, Render, Aiven, Cloudinary y planes futuros | Backup, TLS, MIME, tamano, tenant, rollback independientes | DEFINIDO_NO_CERTIFICADO |
| AC12 | Regresion y salida de M01 | Requisito -> consumidor -> prueba -> evidencia y dependencias explicitas | Mocks no acreditan PASS real ni M02 se libera sin puertas | docs/v5, Node y COLA!P:Q | SHA, evidencias causales, decisiones y prueba de no regresion | DEFINIDO_NO_CERTIFICADO |

## Aceptacion de esta unidad y limites
Las doce filas AC01-AC12 unicas definen criterios, no sustituyen las pruebas futuras. El test Node de este cambio valida estructura, requisitos y negativos fail-closed; no afirmar HTTP, MySQL, Angular browser ni tenancy A/B live. Nunca afirmar A5_PASS propio, autorizacion QA/main/PROD ni migraciones ejecutadas por un documento.

No modificar Angular, API, Domain, Application, Infrastructure, JWT, BCrypt, RBAC, PII, MySQL, DNS, secretos, Cloudinary, Hostinger, Cloudflare, main, qa ni datos reales. Inventario historico de imagenes cero implica NO_APLICA, pero archivos futuros exigen MIME, tamano, sanitizacion, RBAC y aislamiento tenant.

## Flujo V13 y rollback
El ID ya se reclamo EN_PROGRESO con TASK_ID y readback. El candidato completo pasa VALIDANDO antes de A0-A3. A0 revisa diff, sintaxis y secretos; A1 la matriz positiva; A2 negativos; A3 Node sobre documentacion. Luego commit directo fast-forward dev sin PR ni rama; A4 inspecciona exact-SHA, diff, pruebas reproducidas, CI y PLAN_MAESTRO/COLA/BITACORA.

LISTO_DEV solo con A0-A4 PASS real, EVENT_ID de 21 campos y A5_PENDIENTE de otro TASK_ID. A5_PASS independiente y orden separada son imprescindibles para QA/main/PROD. Rollback aditivo git revert <SHA_PUBLICADO>, preservar BITACORA y commits ajenos; no reset ni force.
