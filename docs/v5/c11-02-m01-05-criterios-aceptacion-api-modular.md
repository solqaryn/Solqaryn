# SOLQARYN V5 — C11-02.M01.05: aceptación funcional y técnica observable de API modular

**Unidad:** `C11-02.M01.05`, fila 1626 de PLAN_MAESTRO, preparación documental M01 de C11-02/API modular. **Autor:** PRIMARY DEV 03 — JAVIER_CHATGPT, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Destino:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14.

## Objetivo y frontera de aceptación

Convertir los riesgos y decisiones *pendientes* de `C11-02.M01.04` en oráculos observables, con entrada controlada, salida esperada, prueba futura e indicación de bloqueo. Los criterios aquí descritos son **CONTRATOS FUTUROS_NO_EJECUTADOS**: no afirman que rutas, cobertura o mecanismos faltantes hayan sido implementados, que un defecto exista ni que HTTP, MySQL, Playwright o CI runtime hayan pasado. Este cambio es únicamente Markdown y no modifica API, DTO, JWT, RBAC, datos, esquemas, workers, secretos, cuentas, Tasks o despliegues.

Referencia de arquitectura: `Domain <- Application <- Infrastructure`, con API como borde HTTP; Angular 22.2.1 cliente, .NET 10, EF Core 10.0.12 y proveedor Oracle MySql.EntityFrameworkCore 10.0.9. La autoridad se resuelve en servidor y cada operación sensible debe denegar tenant ajeno incluso con payload/URL manipulados.

## AC01–AC18 — oráculos funcionales positivos futuros

| ID | Preparación / entrada | Salida observable exigida al implementar | Método futuro |
| --- | --- | --- | --- |
| AC01 | Token válido, usuario habilitado, empresa A con membresía activa | 200 y datos únicamente de A; no fuga de B | HTTP + datos A/B aislados |
| AC02 | Renovar sesión de usuario activo con credenciales válidas | Par nuevo autorizado y claim coherente, sin exponer secretos | HTTP/API y expiración controlada |
| AC03 | Usuario con varias empresas, seleccionar A | Contexto de A procede de membresía del servidor | integración API/Application |
| AC04 | Permiso de empresa válido en A y recurso asignado | Acción permitida sólo dentro del alcance de A | xUnit + HTTP RBAC |
| AC05 | Principal de plataforma con permiso global explícito | Operación de plataforma separada, auditada, sin heredar rol de tenant | HTTP + trazas |
| AC06 | Soporte de lectura con scope autorizado A | Lectura sin efectos de escritura ni tracking persistente | xUnit interceptor + MySQL efímero |
| AC07 | Request válido según DTO concreto | Validación backend y respuesta estable `ApiResponse`/contrato documentado | snapshot de DTO + HTTP |
| AC08 | Propiedad existente y autorizada | 200/201/204 según ruta, con forma de respuesta versionada | contrato HTTP y cliente Angular |
| AC09 | Error de dominio esperado por conflicto de concurrencia | 409 sin mutación parcial y con código estable | HTTP + MySQL transaccional |
| AC10 | Entrada inválida con validación declarada | 400 sin stack/PII y campo de error compatible | xUnit filtro + HTTP |
| AC11 | Recurso inexistente dentro de un tenant autorizado | 404 coherente, sin revelar otro tenant | HTTP A/B |
| AC12 | Paginación, filtro y orden autorizados | límites aplicados por servidor, resultado y total coherentes | integración API + datos |
| AC13 | Operación transaccional sobre varios agregados de A | commit íntegro o rollback íntegro ante falla simulada | MySQL efímero EF |
| AC14 | Solicitud idempotente con mismo identificador válido | Efecto único observable y respuesta repetible según contrato | prueba de concurrencia |
| AC15 | Error inesperado con correlación | respuesta genérica y log depurado vinculados por correlation id | middleware + análisis de logs |
| AC16 | Cliente Angular soportado consume DTO versionado | tipado/build y contrato compatible sin privilegio en frontend | Angular build + Playwright |
| AC17 | Ruta de lectura pública permitida con host/tenant resuelto | Exposición de sólo campos públicos, sin DTO internos | HTTP storefront/host A/B |
| AC18 | Intento legítimo posterior a revocación de rol restaurado y autorizado | Revalidación de estado efectivo antes de permitir | xUnit cambios de membresía |

**Estado de AC01–AC18:** FUTUROS_NO_EJECUTADOS. Cada PASS requerirá ejecución real sobre código implementado, evidencia dirigida y SHA correspondiente; este documento no declara ninguno superado.

## N01–N18 — pruebas negativas y condición de rechazo futuras

| ID | Manipulación / condición adversa | Resultado exigido |
| --- | --- | --- |
| N01 | JWT inválido o expirado | 401, sin datos ni efectos |
| N02 | Usuario de A solicita id/URL de recurso B | 403/404 sin existencia de B ni mutación |
| N03 | Membresía de A revocada después de emitir JWT | Denegar en decisión sensible posterior |
| N04 | Rol global plataforma usado como permiso de empresa | Denegar acceso empresarial sin membresía |
| N05 | Soporte read-only intenta POST/PUT/PATCH/DELETE | 403 y cero escrituras |
| N06 | Campo `EmpresaId`, `Rol` o `SucursalId` forzado en body/headers | No cambia identidad/rol/alcance autoritativo |
| N07 | DTO incluye campos no permitidos para mass assignment | Rechazo/ignorados sin elevación |
| N08 | Formato de request roto o oversized | 400/413 sin consumo o filtración descontrolada |
| N09 | Excepción inyectada contiene SQL/token/PII | Ningún secreto en respuesta o log de aplicación |
| N10 | Reintentos masivos de login/token | 429/medida antiabuso verificable sin enumeración |
| N11 | Parámetro de paginación negativo/excesivo o sort arbitrario | Validación fail-closed, límites y allowlist |
| N12 | Consulta EF omite filtro tenant en ruta de empresa | Prueba falla; cambio no certificable |
| N13 | Falla entre dos escrituras relacionadas | Rollback completo sin estado comercial parcial |
| N14 | Dos requests concurrentes con la misma idempotency key | Un solo efecto, sin carreras ni doble cargo |
| N15 | Host público B solicita payload privado de A | Denegar, sin filtrar DTO empresarial |
| N16 | Guard Angular deshabilitado o manipulado por cliente | Backend conserva denegación real |
| N17 | Check obligatorio cancelled/skipped/in_progress/failed | NO_PASS; mantener scope VALIDANDO |
| N18 | Error 404/403 permite inferir existencia de otro tenant | Respuesta indistinguible según política de no filtración |

**Estado de N01–N18:** FUTUROS_NO_EJECUTADOS. Respuestas 401/403/404/409 son contratos por endpoint a documentar, no afirmaciones de los estados actuales.

## RNF01–RNF12 — verificabilidad y compatibilidad antes de cerrar M03/M05

| ID | Condición no funcional y evidencia futura |
| --- | --- |
| RNF01 | Matriz de ruta, permiso, empresa, sucursal y consumidor con responsable y SHA |
| RNF02 | Versionado de DTO sin romper clientes Angular y consumidores públicos |
| RNF03 | Pruebas servidor contra A/B, rol revocado y plataforma vs empresa |
| RNF04 | MySQL 8.4 efímero, EF real y rollback transaccional cuando haya persistencia |
| RNF05 | Política de protección de PII, headers, CORS y logs sin secretos |
| RNF06 | Rate limiting medido con tolerancia definida en escenarios antiabuso |
| RNF07 | Correlation ID en requests y errores sin conceder autoridad |
| RNF08 | Latencia y paginación bajo carga real, sin umbrales inventados en este documento |
| RNF09 | Contratos HTTP 400/401/403/404/409/versioning con snapshots ejecutados |
| RNF10 | Suites GitHub Actions pertinentes `completed/success` en SHA exacto, sin convertir SKIPPED en PASS |
| RNF11 | Plan de rollback normal (git revert y migraciones reversibles si existen) sin force-push |
| RNF12 | A5 por Task Javier distinta y autorización explícita adicional antes de QA/main/PROD |

## Trazabilidad de fuentes y límites de evidencia

- `docs/v5/c11-02-m01-04-riesgos-supuestos-decisiones-api-modular.md` (base de R01–R16, S01–S10, D01–D10 y escenarios futuros). Ningún riesgo potencial se convierte aquí en vulnerabilidad confirmada.
- `AuthController.cs` blob `af437c869b4800a499c42aeff00347de3a3cc521` y `TenantContextController.cs` blob `2407c2eb6779ed724e841c688ee14dabbf163171`: puntos de entrada de autenticación y contexto; no cobertura completa de todos los endpoints.
- `PlataformaSesionController.cs` blob `965a05321a8b8c3f90011addb84e14bcc5978775`: frontera plataforma separada.
- `ApiResponse.cs` blob `aba995eae50ad756712961c9c394f5038a31a142` y `FluentValidationActionFilter.cs` blob `fec754eeb2c7e41599155f25a24e8adbb53298bb`: forma de respuesta y validación actuales sujetas a pruebas futuras.
- `ExceptionHandlingMiddleware.cs` blob `baee40143e4e48b8df200e77892606af96d60b7f` y `SupportReadOnlyCommandInterceptor.cs` blob `3b80b59899522b9b41681ed75b8269f8720fe297`: superficies de manejo de errores y soporte; no prueba de invulnerabilidad.
- `backend/src/API/Solqaryn.API.csproj` `net10.0`; `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` EF 10.0.12/MySql.EntityFrameworkCore 10.0.9. Verificados en `dev`, sin abrir MySQL ni modificar schema.

## A0–A5 y reversión

**A0 (prepublicación):** verificar Scope Lock/Skill, PLAN `H1626=PENDIENTE,I1626` vacío al adquirir; COLA `G/O=SI`, DAG fila 1587 `F=VALIDADO,G=SI`; inventariar diff sólo Markdown, seguridad y dependencias. **A1 (tras VALIDANDO):** comprobar 18 positivos, 18 negativos y 12 RNF, con cobertura de observabilidad y sin presentar pruebas futuras como ejecutadas. **A2:** inspeccionar fail-closed JWT/RBAC, tenant A/B, plataforma, soporte de lectura, secretos y PII sin cambios runtime. **A3:** trazabilidad API→Application→Infrastructure y compatibilidad documental; `dotnet`/HTTP/MySQL/Playwright son NO_APLICA por diff puramente Markdown. **A4:** publicar con fast-forward CAS, verificar blobs exactos y checks Scope Lock + procedencia CHANGELOG V5 `completed/success` en SHA publicado, y readback PLAN/COLA/BITACORA antes de LISTO_DEV. **A5:** revisión técnica independiente posterior por otro TASK_ID Javier; no autoaprobar ni promover QA/main/PROD.

**Rollback:** revert normal del commit documental, preservando commits de terceros e historial BITACORA A:Z. No se habilitan eventos, workers ni webhooks entre Tasks. Toda futura implementación de API requerirá su microtarea propia con CI causal y revisión independiente.
