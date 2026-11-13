# SOLQARYN V5 FINAL — C02-01.M01.05: criterios funcionales y técnicos del contexto tenant

**TIPO 1, preparación exclusivamente documental.** Fecha 2026-11-13; solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT; autor PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. Fuente: Standard V5 FINAL Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA y Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU. Este documento no acredita A5, implementación, API live ni migración.

## Alcance y puerta de dependencia

C02-01 busca resolver host, identidad, membresía y ámbito sin confiar en parámetros del cliente. Esta unidad **define criterios de aceptación falsables** para futuros cambios, no los implementa ni certifica. DAG_TECNICO F=VALIDADO/G=SI, PLAN_MAESTRO PENDIENTE sin dueño y COLA G/O=SI permitieron únicamente M01 preparatoria. M01.06/M02, QA/main/PROD y cambios de seguridad no se habilitan; revalidar al terminar C00 y C01-01.

**Clasificación**: requiere_dotnet=NO; requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO; requiere_security=SI (criterios JWT/RBAC/tenancy/host, PII); requiere_scope_lock=SI (integridad de commit documental). La ausencia de una prueba live no se finge como PASS.

## Fuentes reales examinadas en GitHub dev

| Fuente | Blob SHA | Observación acotada |
| --- | --- | --- |
| backend/src/API/Controllers/TenantContextController.cs | 2407c2eb6779ed724e841c688ee14dabbf163171 | Selector autorizado; sin membresía devuelve rechazo, no contexto B. |
| backend/src/API/Filters/TenantPermissionContext.cs | df7d49f3e185c538f3b3dafabb36c3a971a7257d | X-Empresa-Id es selector único positivo, nunca autoridad. |
| backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs | 17c7d191e3e1b34f56c3d27ba4992b512526542b | Exige identidad y membresía activa antes de marcar tenant. |
| backend/src/Application/Interfaces/IUsuarioScopeService.cs | 5345ee2de1b1e942bff8eded658f5dc27c0a6745 | Contexto tenant-aware; defaults fail-closed de interfaz. |
| backend/src/Infrastructure/Services/UsuarioScopeService.cs | 14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f | Membresía actual con EF; ruta legacy se debe revisar. |
| backend/src/Infrastructure/Services/ConfiguredPublicStoreHostTenantResolver.cs | 833940d3ce2f246d5946daeeb2ed7e14ffaffae2 | Host exacto y StoreKey opt-in, no resolver arbitrario. |
| backend/src/Infrastructure/Services/JwtService.cs | 9051fe6b4aac23e0ce3ebc684a2830c2cd2e6c9b | JWT contiene identidad/rol, no propiedad de tenant. |
| backend/tests/Solqaryn.Tests/ConfiguredPublicStoreHostTenantResolverTests.cs | dd387f8b6e39c087121fbec1928fe1c5425aa66d | Tests escritos para host A/B, desconocido y duplicado; no se ejecutaron aquí. |

## Matriz de aceptación de implementación futura

| ID | Clase | Estímulo | Resultado exigible y prueba futura |
| --- | --- | --- | --- |
| AC01 | POS | Usuario U con membresía A consulta empresas seleccionables | Se lista A y sólo otros ámbitos propios, no B ajena; verificar API + base real descartable. |
| AC02 | POS | U autenticado selecciona A de la que es miembro activo | Scope A y rol vigente devueltos; probar servicio/API sin rol global heredado. |
| AC03 | POS | U autorizado en A y B cambia A→B→A | Roles y recursos independientes de cada empresa; HTTP/DB. |
| AC04 | POS | Host exacto aprobado con StoreKey en tienda pública | Resolver sólo la empresa allowlisted; xUnit y E2E si afecta rutas UI. |
| AC05 | POS | Operación A autorizada genera auditoría | Empresa y actor atribuidos server-side sin JWT/PII; test dirigido. |
| AC06 | NEG | U de A fuerza EmpresaId B en query/header/DTO | Denegar sin leer ni modificar registros B; HTTP y DB. |
| AC07 | NEG | Membresía A revocada con JWT todavía vigente | Denegar; verificar invalidación con datos actuales. |
| AC08 | NEG | Header X-Empresa-Id ausente/duplicado/malformado sin contexto único | No inferir tenant arbitrario; pruebas de filtro y contrato de error. |
| AC09 | NEG | Host desconocido, host similar o subdominio malicioso | No resolver tenant; xUnit host y middleware. |
| AC10 | NEG | HostBindings duplicado o wildcard/StoreKey inválidos | Arranque fail-closed, no mapping ambiguo; test configuración. |
| AC11 | NEG | Administrador A consulta recursos de B | RBAC tenant deniega, sin filtración; xUnit/API A/B. |
| AC12 | NEG | JWT alterado, vencido o sin sesión autorizada | Rechazo sin acceso empresarial; middleware JWT. |
| AC13 | NEG | Marca pública /tienda se usa fuera de /tienda | No autoriza API administrativa; test seguridad. |
| AC14 | NEG | Dos membresías activas y ninguna selección explícita | No seleccionar A o B por omisión; test servicio. |
| AC15 | NEG | ID de recurso o adjunto de empresa B desde A | Denegar lectura, exportación y escritura; HTTP/DB proporcional. |
| AC16 | NEG | Error contiene token JWT o PII sensible en logs | Sanitizar y restringir; inspección de logs y test negativo. |
| AC17 | NEG | Rol de plataforma se confunde con administrador tenant | Separar planos, no elevación automática; RBAC independiente. |
| AC18 | NEG | Check SKIPPED o SHA anterior se ofrece como PASS de cambio nuevo | Rechazar evidencia; consultar check-runs del SHA exacto. |

La forma exacta del HTTP error se decide por endpoint real; no declarar todos los rechazos como un status idéntico inventado. Los 18 casos son **criterios futuros**, no 18 tests ejecutados. Un cambio de C# exige compilación y pruebas .NET reales; uno de MySQL exige integración en job pertinente; UI exige lint/E2E cuando corresponda.

## Auditorías y condiciones de salida de este documento

A0: fuente Git/diff, formato, secretos y sin permisos nuevos. A1: cobertura documental AC01–AC18, 5 positivos y 13 negativos, criterios medibles y no-regresión. A2: fronteras JWT, RBAC, membresía A/B, host falso, revocación, PII y fallo cerrado. A3: referencias de blobs, DAG/PLAN/COLA, BITACORA A:Z (26 columnas) sin afirmar pruebas de backend/DB. A4: commit fast-forward a dev con Scope Lock SUCCESS exact-SHA, readback y rollback. A5: otra Task revisa en SHA publicado antes de QA/main/PROD; nunca autofirmar.

Prohibido cambiar JWT, roles, middleware, API, .NET/Angular, MySQL, DNS, Cloudflare, Aiven, imagen histórica inventariada cero, branches, PR, secretos, producción o horarios por este contrato. Revertir documento y entrada CHANGELOG mediante nuevo commit auditado, no force-push, preservando BITACORA y el trabajo de otros autores.
