# SOLQARYN V5 FINAL — C03-02.M01.05: aceptación funcional y técnica observable

**Autor:** PRIMARY DEV 06 — JAVIER (6af16bbad26881918c283abe06e1f32a). **Alcance:** diseño documental de criterios verificables en C03-02.M01, sin implementación, migraciones ni certificación de autorización. **Fecha:** 2026-11-13. **Repositorio:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

Autoridad: [STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [Sheet V5 y DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El objetivo de fase es RBAC/ABAC server-side por empresa, sucursal, propiedad, archivo y operación; evidencia futura incluye BOLA/IDOR, claims falsos, sucursal cruzada y exportación ajena. Esta microtarea **define aceptación**, no declara que esos escenarios ya pasaron.

## Evidencia de referencia y límites de confianza

- C03-02.M01.03: [inventario causal de fuentes y brechas](c03-02-m01-03-inventario-autorizacion-privacidad.md), blob 51b5a01a4770977aadcc780145a17aaed8a3fb41, identifica nueve contratos y cinco clases de pruebas existentes. La lectura de esos tests NO es su ejecución.
- backend/src/API/Filters/TenantPermissionContext.cs, blob df7d49f3e185c538f3b3dafabb36c3a971a7257d: X-Empresa-Id es selector, no autorización.
- backend/src/API/Filters/RequierePermisoAttribute.cs, blob b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4: autorización por empresa/módulo/acción contra IPermisoService.
- backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs, blob 17c7d191e3e1b34f56c3d27ba4992b512526542b: requiere autenticación y membresía vigente.
- backend/src/Application/Interfaces/IUsuarioScopeService.cs, blob 5345ee2de1b1e942bff8eded658f5dc27c0a6745: scope tenant separado de legacy.
- backend/src/Application/Security/VerifiedCommercialTenant.cs, blob 2da140ce9d7f47329d39d292d3271596467910cc: marca de autorización colocada por backend.
- backend/src/Application/Common/CatalogoPermisosBase.cs, blob 6060b4c54c0428902603f2157984b693a324e8f3: catálogo no constituye concesión efectiva.

## Matriz de criterios de aceptación de una implementación futura

Los casos describen resultados obligatorios a medir sobre API y/o MySQL efímeros **cuando el código de C03-02 se implemente**. No confundir su especificación con un PASS runtime.

| AC | Tipo | Estímulo controlado | Resultado observable exigido | Gate futuro |
| --- | --- | --- | --- | --- |
| AC01 | Positivo | Actor A autenticado, membresía A activa, rol y grant de lectura válidos, recurso propio A | Devuelve solo recurso A y auditoría correlacionada; ninguna fila B | HTTP/TestServer + DB A/B |
| AC02 | Positivo | Operación de escritura A autorizada sobre recurso A y sucursal permitida | Persistencia cambia solo A; una transacción/auditoría coherente, sin cascada B | API/EF/MySQL |
| AC03 | Positivo | Exportar catálogo A con autorización explícita de exportación | Artefacto contiene exclusivamente datos A y omite PII no autorizada | API + inspección CSV/XLSX/PDF |
| AC04 | Negativo BOLA | JWT de actor A intenta leer recurso ID propiedad de B | Respuesta 403/404 consistente y sin filtrar contenido ni existencia sensible; 0 filas B leídas en payload | HTTP+DB A/B |
| AC05 | Negativo IDOR | Actor A modifica ID B vía endpoint PATCH/PUT/DELETE | Denegación, 0 modificaciones en B, 0 eventos externos y auditoría de rechazo | HTTP+DB A/B |
| AC06 | Negativo sucursal | Membresía A válida intenta editar sucursal B o sucursal de A sin grant | Denegación y filtros server-side aplicados, sin transacción autorizada | API/EF con ramas A/B |
| AC07 | Negativo claims | JWT válido pero claim empresa/rol falso o desactualizado y header X-Empresa-Id=B | Backend no confía en claim/header como grant; rechaza sin membresía efectiva B | Firma JWT/HTTP + membresía |
| AC08 | Negativo selector | X-Empresa-Id ausente con varias membresías, duplicado, 0, negativo o malformado | Falla cerrada; sin fallback ambiguo ni operación ejecutada | HTTP unitario + integración |
| AC09 | Negativo revocación | Revocar membresía/rol/grant entre dos peticiones con JWT aún válido | Segunda petición rechazada y no reutiliza autorización previa | HTTP+DB concurrencia |
| AC10 | Negativo exportación ajena | Actor A exporta ID o filtros que apuntan a dataset B | 403/404 o export sin filas ajenas según contrato explícito; jamás leak de B ni archivos temporales | API + resultado archivo + DB |
| AC11 | Negativo archivos | Actor A solicita URL, miniatura, descarga, metadato o archivo de B | Sin URL firmada, bytes, metadatos o enumeración de B | HTTP + almacenamiento simulado y verificado |
| AC12 | Negativo soporte | Token de soporte insuficiente o lectura fuera de allowlist, cualquier POST/PUT/DELETE | Rechazo; la excepción temporal no salta rol, tenant ni límites; auditoría | API soporte + control de ruta |
| AC13 | Negativo contexto | Request no autenticada, empresa inexistente o usuario inactivo | No ejecuta acción ni consulta datos de negocio; respuesta de rechazo sin PII | HTTP + DB |
| AC14 | Negativo cruces | Dos requests concurrentes A y B, con filtros/paginación/export bajo carga | No mezcla datos ni cachés, identidades, logs, claves de correlación o marcas HttpContext | Integración MySQL + HTTP concurrente |

## Evidencia que debe acompañar cada criterio en M03/M05 (no producida aquí)

Para cada AC registrar: SHA exacto, workflow/job GitHub Actions, configuración de runner, datos anónimos y aislados A/B, solicitud mínima, respuesta y código HTTP, filas antes/después, auditoría, permisos y rol efectivo. En archivos/export incluir hashes y contenido desensibilizado; en errores comprobar ausencia de secretos/PII. En MySQL/EF incluir migrations/schema aplicados a base efímera y cleanup. Todo caso negativo debe mostrar denegación **y ausencia de efectos**. Un mock aislado nunca sustituye prueba DB/HTTP cuando exista riesgo real de leakage.

**Criterios transversales de no-regresión:** (NR01) usuario sin permiso nunca obtiene una ruta por UI solamente; (NR02) Usuario.RolId legacy no concede autoridad tenant por sí solo; (NR03) cabecera/DTO no marca HttpContext.Items autorizado; (NR04) superadmin o soporte no recibe bypass comercial genérico; (NR05) errores y exportaciones no exponen recursos de terceros; (NR06) evidencias de A0-A4 de SHA anterior no validan cambio nuevo. Control de grants por módulo y acción + membresía + ownership + contexto de sucursal deben combinarse en backend, con defecto DENY.

## Gates y clasificación proporcional de esta microtarea

DAG_TECNICO valida PREPARACION_DOC_CONDICIONAL de C03-02.M01.05, con diez predecesoras C01-02.M01.01-.05 y C02-01.M01.01-.05 en LISTO al claim. PLAN_MAESTRO H/I y COLA G/O se leen para exclusión de owner. **No liberar C03-02.M01.06 ni M02–M06 mediante esta preparación.**

- requiere_dotnet=NO_APLICA ahora (sin C# nuevo); SI al implementar endpoints y suites con dotnet test.
- requiere_node_angular=NO_APLICA ahora; usar npm/build/tests si una tarea posterior cambia frontend.
- requiere_mysql=NO_APLICA ahora; SI en futuras pruebas de ownership/IDOR/exfiltración con servicio MySQL 8.4 efímero.
- requiere_docker=NO_APLICA ahora salvo que un cambio posterior afecte imágenes o jobs.
- requiere_e2e=NO_APLICA ahora; Playwright cuando cambie una superficie de navegación o archivo servido.
- requiere_security=SI en documento: contrastar JWT/RBAC/tenancy, denegación, PII y abuso de export/archivos.
- requiere_scope_lock=SI para documento publicado en dev: check verify-solqaryn-project-scope exact-SHA.

**A0–A3 aplicables a M01.05:** inspección estática Markdown/fuentes/secretos, comprobación documental de AC01-AC14 y NR01-NR06, lectura de contratos de seguridad y enlace a DAG/PLAN. No inferir PASS de API, Playwright, SQL ni A5 por ese control documental. **A4:** confirmar SHA, diff, Source Lock y readback PLAN/COLA/BITACORA. **A5:** otra Task Javier diferente del autor; QA/main/PROD siguen sin autorización.

**Rollback:** git revert normal del commit documental de M01.05. Sin force-push, PR, ramas, migraciones, cambios a JWT/roles, credenciales, datos ni tareas físicas.
