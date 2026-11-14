# SOLQARYN V5 — C11-02.M01.02: actores, entradas/salidas y fronteras de API modular

**Unidad:** C11-02.M01.02, PLAN_MAESTRO fila 1623, preparación documental `M01` del bloque `C11-02 | API modular`. **Autor:** PRIMARY DEV 01 / JAVIER_CHATGPT / TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT, 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

## Propósito y límites de evidencia

Esta unidad identifica **actores, entradas, salidas y fronteras de confianza** para una API modular segura: puertos, casos de uso, DTOs, validación y controladores HTTP pequeños son objetivo futuro del bloque, no implementación en este documento. Se apoya en M01.01 (objetivo y NR01–NR14) sin declararlo autorización de nuevos endpoints. DAG_TECNICO registra F=VALIDADO/G=SI para C11-02.M01.02 y COLA G/O=SI; ese gate habilita únicamente análisis documental, NO IAM/JWT/RBAC ejecutable, EF, workers, migraciones ni QA/main/PROD.

## Actores A01–A12 y responsabilidad esperada

| ID | Actor o componente | Entrada/confianza esperada | Límite de autoridad |
| --- | --- | --- | --- |
| A01 | Visitante anónimo | HTTP público con datos no confiables | Únicamente rutas explícitamente anónimas |
| A02 | Usuario autenticado | JWT validado por middleware y estado vigente | Token no concede pertenencia a cualquier empresa |
| A03 | Usuario de empresa A | Membresía activa comprobada por servidor | Nunca puede leer recursos de B |
| A04 | Usuario de empresa B | Igual control separado para B | IDs coincididos no permiten cruce A↔B |
| A05 | Administrador de empresa | RBAC relacional y scope empresarial | No hereda autoridad de plataforma global |
| A06 | Propietario del SaaS | Rol de plataforma verificado sobre fuente real | No se transforma en tenant comercial por defecto |
| A07 | Soporte de solo lectura | Contexto de soporte y auditoría | Prohibido persistir cambios o elevar privilegios |
| A08 | Angular autenticado | URL/headers/body/cookies controlados por cliente | Guards/UI no sustituyen autorización backend |
| A09 | Storefront público | Slug/host/tenant binding no confiable inicialmente | Aislamiento público/privado y rate limits |
| A10 | API Controller | Model binding, routing y HTTP | Ejecuta validación, no toma claims como permiso autónomo |
| A11 | Application Service | Contexto de usuario/tenant derivado del servidor | Reglas y casos de uso sin depender de infraestructura |
| A12 | Infrastructure/EF/Integración | Repositorios y proveedores reales bajo DI | Persistencia/auditoría dentro de ámbitos transaccionales |

## Matriz de interfaces, entradas y salidas (I/O01–I/O12)

| Flujo | Entrada potencialmente no fiable | Salida esperada y contrato | Control/rechazo obligatorio |
| --- | --- | --- | --- |
| I/O01 login | credenciales y metadatos del cliente | respuesta `ApiResponse<LoginResponseDto>` o 401 | rate limit AuthLogin, no filtrar credenciales |
| I/O02 renovar | JWT previo/cookie según contrato real | sesión actualizada o 401 | revocación y usuario vigente |
| I/O03 empresas seleccionables | JWT/solicitud UI | listado sólo de empresas autorizadas | membresía activa leída en servidor |
| I/O04 contexto tenant | `empresaId` de ruta | contexto aprobado o Forbid | nunca confiar en ID de ruta |
| I/O05 sesión de plataforma | JWT sin empresa obligatoria | autoridad global verificada o Forbid | auditoría y `no-store` |
| I/O06 CRUD privado futuro | body/DTO y entidad por ID | DTO validado o error seguro | RBAC, tenant A/B y propiedad del recurso |
| I/O07 búsqueda/paginación | query, sort, filtros y rango | colección acotada | allowlist de orden/campos, cuotas |
| I/O08 datos económicos | ID documento y payload | resultado transaccional o rechazo | consistencia inventario/contabilidad |
| I/O09 API pública storefront | host, slug, path | datos públicos mínimos | host binding y no exponer DTO privado |
| I/O10 soporte | identidad de operador, recurso | lectura auditada o denegación | soporte NO escribe `SaveChanges` |
| I/O11 errores HTTP | excepciones, validación, cancelación | status y objeto seguro con correlación | sin SQL, stack, token, PII |
| I/O12 futuras integraciones | mensajes/firmas/IDs externos | respuesta tipada e idempotente | **no implementar ahora** workers, webhooks ni dispatcher |

I/O01–I/O05 se anclan en código inspeccionado; I/O06–I/O12 son escenarios de diseño **FUTUROS_NO_IMPLEMENTADOS** y no acreditan superficie existente.

## Fronteras de confianza F01–F10

1. **F01 navegador → API:** toda ruta, query, header, cookie, token y body son entrada controlable por el cliente; autenticar y autorizar del lado servidor.
2. **F02 JWT → contexto vivo:** claims no sustituyen consulta del estado actual ni membresías/roles vigentes.
3. **F03 tenant A → tenant B:** comprobar autorización de objeto y scope en backend; evitar fuga de existencia y filas cruzadas.
4. **F04 plataforma global → empresa:** ámbitos diferenciados; un rol plataforma no es una membresía empresarial tácita.
5. **F05 soporte → mutación:** soporte de lectura jamás puede usar endpoint, EF tracking o SaveChanges para escritura.
6. **F06 controlador → Application:** casos de uso con contratos explícitos, validación, cancellation token y propagación segura de errores.
7. **F07 Application → Infrastructure:** repositorios limitan joins, transacciones e índices a dominio; no mezclar conexiones productivas.
8. **F08 backend → Angular/storefront:** una vista o guard no concede permiso real, y el DTO privado no se serializa públicamente.
9. **F09 API → observabilidad:** ocultar PII, headers de autenticación, contraseñas, tokens, SQL sensible y secretos; registrar IDs de correlación acotados.
10. **F10 API → servicios externos:** no crear ni activar webhooks, workers, colas, pagos, Cloudflare/Hostinger, secretos o migraciones mediante M01 documental.

## Evidencia concreta de código de la rama dev

| Archivo | Blob inspeccionado | Observación verificable |
| --- | --- | --- |
| `backend/src/API/Controllers/AuthController.cs` | `af437c869b4800a499c42aeff00347de3a3cc521` | `AllowAnonymous`/rate limit de login y `Authorize` en renovación; respuesta ApiResponse |
| `backend/src/API/Controllers/TenantContextController.cs` | `2407c2eb6779ed724e841c688ee14dabbf163171` | selección no autoritativa; rechazo si falta membresía activa |
| `backend/src/API/Controllers/PlataformaSesionController.cs` | `965a05321a8b8c3f90011addb84e14bcc5978775` | autoridad global consultada en solicitud, auditoría obligatoria y no-cache |
| `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | composición, validación y configuración; **no es auditoría completa** |
| `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | .NET 10 API y referencias a Application/Infrastructure |
| `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9 |

La Skill canónica y `references/architecture.md` exigen Domain <- Application <- Infrastructure, API como composición, Angular 22.2.1 y controles JWT/RBAC/tenant. Estos blobs prueban existencia de texto en Git, no ejecución ni despliegue. El inventario de cobertura completa de API corresponde a M01.03.

## Escenarios de aceptación futuros, nunca ejecutados en esta microtarea

**Positivos P01–P08:** login válido con rate limit conservado; renovación válida; listado tenant autorizado; consulta tenant A con membresía; sesión plataforma global propia; DTO validado y compatible; flujo soporte sólo lectura; controller→Application con cancelación y auditoría proporcional.

**Negativos N01–N12:** credenciales inválidas; JWT expirado/revocado; EmpresaId de B; rol de B; empresa inactiva; cliente intenta elevar rol desde body; soporte intenta mutar; DTO con entrada inesperada; fuga de PII/SQL/stack en error; ruta pública serializa DTO privado; rate limit eludido; fallo transaccional deja escritura parcial.

**No funcionales futuros:** límites de tamaño/tiempo, logging con correlación segura, compatibilidad de respuesta, latencia observada con carga representativa, cero secretos en repo, rollback de cambios contractuales. Ninguno fue medido aquí.

## Auditorías, A5 y rollback

**A0:** Skill, identidad, Plan, DAG/Cola, HEAD y exclusión de archivos. **A1:** matriz de 12 actores, 12 I/O y 10 fronteras; P/N son oráculos no resultados. **A2:** razonamiento de aislamiento tenant A/B, JWT/RBAC/PII, soporte read-only. **A3:** correspondencia documental con código API, Application/Infrastructure; pruebas HTTP/MySQL/Playwright NO_APLICA a este diff. **A4:** publicación serializada fast-forward con dos Markdown, comprobación de blobs y `verify-solqaryn-project-scope` y check V5 CHANGELOG `completed/success` exact-SHA, readback institucional. Los jobs runtime ajenos no son PASS del documento.

**A5_PENDIENTE:** revisión independiente por otra Task Javier, sin autoaprobación ni permiso de QA/main/PROD. **Rollback:** `git revert` normal del commit documental, preservando trabajo concurrente y BITACORA; sin reset, force-push, SQL, infraestructura ni recreación de Tasks.
