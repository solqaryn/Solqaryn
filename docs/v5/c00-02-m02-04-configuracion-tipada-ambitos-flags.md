# SOLQARYN V5 FINAL — C00-02.M02.04: configuración tipada, ámbitos y feature flags seguros

**Entrega:** definición **documental**, sin modificaciones del runtime, de la configuración del modelo de trabajo V5. **Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`; rama exclusiva `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA.

**Resultado exigido de M02:** definir cola, dependencias, autoría, estados y cierre sin duplicados. Esta M02.04 fija **qué configuración es legítima**, sus tipos y ámbitos, y qué feature flags deberán gobernar cambios futuros, sin activarlos. No crea endpoints, tablas, variables de entorno, proveedores o nuevas Tasks. El bloque previo `C00-02.M01.06` está LISTO; `COLA!G=SI/O=SI`; no existe entrada particular para este ID en `DAG_TECNICO`, por lo que no se autoriza adelantar otros gates.

## 1. Evidencia actual verificable (solo código, no pruebas de runtime)

| Fuente de dev | Blob SHA comprobado | Observación acotada |
| --- | --- | --- |
| `backend/src/API/Configuration/EnvironmentDatabaseGuard.cs` | `896aa003876f2b4299dfe5d4f1d69b83b5d1be22` | Verifica entorno Development/Staging/Production, binding DB/usuario/servidor/puerto y TLS requerido; no inferir permisos productivos |
| `backend/src/API/Configuration/RenderEnvironmentContractGuard.cs` | `2c38ebb90a8360b61c9e05df96ff3a9abd5b8f65` | Contrato declarativo de 28 claves gestionadas, conjuntos esperados y huellas sin exponer secretos; no copiar valores privados |
| `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Configura JWT secret/issuer/audience, CORS, rate limiter y validación env guard para Render; valores vivos dependen de entorno |
| `frontend/src/environments/environment.ts` | `fcc3144df39328605e7d7b561d4024b930fdfd59` | `production: false`, API local; archivo no es autoridad para permisos |
| `frontend/src/environments/environment.prod.ts` | `8512c57918c3f9bc061e83ea89f7549106c45fe7` | `production: true`, API proxy same-origin; no prueba de configuración real de despliegue |

Los SHA de blobs son evidencia de lectura de Git, **no de pruebas .NET/Angular ni de un proveedor vivo**. La configuración efectiva requiere readback autorizado específico cuando se implemente una modificación; no abrir secrets/variables privadas por esta entrega.

## 2. Contrato tipado propuesto y fronteras de confianza

| Clave lógica / clase | Tipo y validación propuesta | Ámbito y autoridad |
| --- | --- | --- |
| `EnvironmentName` | enum `Development/Staging/Production`, no aceptar arbitrarios | Entorno servidor autorizado, no desde cliente |
| `ConnectionStrings.DefaultConnection` | secret/string no vacío con servidor, database, user y TLS validados | Proveedor backend, nunca transferir a UI, log o BITACORA |
| `Jwt.Secret` | secreto no vacío y longitud segura; issuer/audience obligatorios | Solo backend secret store, rotación fuera de este scope |
| `Security.LoginRateLimitPerMinute` | entero positivo en cotas verificables, rechazo fail-closed | Backend, sin override por usuario, tenant o header |
| `Cors.AllowedOrigins` | array no vacío de orígenes HTTPS autorizados por ambiente | Backend y deployment aprobado; sin wildcard inferido |
| `AppSettings.BackendPublicUrl` | URL absoluta según contrato de entorno | Backend, no atributo de grant |
| `Database.ApplyMigrationsOnStartup` | booleano controlado por ambiente y revisión de migración | Backend; nunca habilitar producción por flag de chat |
| `Cloudinary.*`, `Smtp.*` | opciones tipadas; secret keys separadas de nombres no secretos | Proveedores autorizados; ninguna migración automática |
| `Frontend.production`, `Frontend.apiUrl` | booleano + URL o ruta proxy coherente | Build frontend, solo UX/transporte; no autorización |
| `FeatureFlag.*` **futuro** | identificador allowlisted + bool/versión + expiración opcional | Config controlada por operador autenticado, sin alterar fuente PLAN/COLA |

La matriz anterior **define un contrato futuro**, no asegura que hoy existan clases C# de options, validadores, flags, pruebas E2E o valores secretos configurados para cada clave. Las reglas críticas server-side siguen siendo los únicos mecanismos de autorización JWT/RBAC/empresa/sucursal; flags del frontend jamás pueden conferir permisos.

## 3. Scopes y cambios autorizables

- **Plataforma:** solo configuración transversal verificada con autoridad administrativa diferenciada y auditoría; nunca inferir superusuario desde rol de empresa.
- **Entorno:** DEV, QA y PROD tienen límites distintos. Esta ejecución permite trabajar en rama `dev` exclusivamente; no escribir a ambientes ni secret stores de QA/PROD.
- **Empresa / tenant:** opciones no sensibles de UX podrían particularizarse tras contrato futuro; todo cambio de seguridad, acceso o recursos exige membresía y validación backend para empresa A y B. Config de A no alcanza B.
- **Usuario/sesión:** preferencia visual no equivale a permiso; tokens, JWT, credenciales y encabezados enviados desde navegador no gobiernan flags de seguridad.
- **Task/automatización:** únicamente las seis Javier habilitadas pueden operar V5. El TASK_ID real vincula eventos; no crear ejecutores, claims simultáneos ni A5 por autor.
- **Fuente de trabajo:** PLAN_MAESTRO!H/I es estado/owner; COLA!E/G/O deriva por fórmula, DAG_TECNICO agrega restricciones verificadas. Ningún feature flag del sistema puede saltarse estas autoridades.

## 4. Ciclo de cambio y eventos de idempotencia

1. **Preflight:** establecer ámbito, tipo, valor permitido, propietario, consumidor, secretos involucrados y riesgo transversal; registrar hash de definición no secreta; revalidar Scope Lock y PLAN/COLA/DAG vivos.
2. **Claim:** solo ID `PENDIENTE`, owner vacío, G/O=SI, dependencia real y TASK_ID entre seis Javier. Transición `EN_PROGRESO` única y evento A:Z de 26 campos; readback de COLA.
3. **VALIDANDO:** documento/código candidato completo y huella, **antes** de A0–A3. Un flag de funcionalidad no sustituye prueba de RBAC/tenant.
4. **A0–A3:** inspección estática, caso positivo/negativo, seguridad, consumidores y pruebas de integración **según los archivos modificados**; sin compilar runtime por mera definición documental.
5. **Publicación/A4:** commit fast-forward `dev` y check `verify-solqaryn-project-scope` SUCCESS en SHA exacto; un job SKIPPED/CANCELLED que fuera obligatorio no es PASS. Readback de diff, owner/estado y evento.
6. **Cierre:** registrar `A5_PENDIENTE` a otra Task Javier y `LISTO_DEV` solo con A0–A4 aplicables PASS. No QA/main/PROD sin autorización distinta y A5_PASS independiente.

**Idempotencia:** asignar `EVENT_ID` estable e `IDEMPOTENCY_KEY` causal por ID/operación/fingerprint/TASK_ID; las repeticiones deben leer antes de append. `RUN_ID` queda vacío si no existe ejecución nativa verificable. BITACORA A:Z append-only; no insertar filas vacías ni reescribir historia.

## 5. Validación documental: positivos y negativos

| Caso | Signo | Oráculo contractual (NO ejecución real de sistema) |
| --- | --- | --- |
| P01 | + | Config no secreta de DEV con tipo válido, scope exacto y owner autorizado → contrato documentado, sin modificar valores de QA/PROD |
| P02 | + | Flag futuro de UX explícitamente permitido, con control backend independiente → solo interfaz cambia tras autorización específica |
| P03 | + | Claim V5 y cambio documental con G/O=SI y Scope Lock exact-SHA → ciclo A0–A4 y A5_PENDIENTE rastreables |
| N01 | - | Flag frontend pretende conferir permiso plataforma o acceso A→B → rechazar sin membresía backend |
| N02 | - | Se propone imprimir `Jwt.Secret`, contraseña DB, SMTP o API key en log o BITACORA → rechazar |
| N03 | - | Flag o actor externo intenta liberar trabajo sin DAG, o M01.06/M06.06 por VALIDANDO provisional → denegar |
| N04 | - | Entorno desconocido, URL sin política o config no tipada → fail-closed; exigir definición aprobada |
| N05 | - | Se modifica `Database.ApplyMigrationsOnStartup` o secreto en producción desde dev → NO_GO sin autorización |
| N06 | - | Dos Tasks reclaman el mismo ID o revisora=autora A5 → rechazar segundo claim/revisión |
| N07 | - | Evento A:Z truncado a 21 campos, clave duplicada o RUN_ID inventado → no append ni cierre |
| N08 | - | Job obligatorio pendiente/fallido/cancelled/skipped o SHA incorrecto → conservar VALIDANDO |
| N09 | - | Un readback de configuración Vercel/Render no existe pero se afirma despliegue o feature flag activo → rechazar certificado |

## 6. Impacto, auditorías y rollback

**Impacto previsto:** dos archivos Markdown solamente (este contrato más sección aditiva `CHANGELOG_AI.md`). Sin cambios en C#, TS, JSON env, migrations, esquemas MySQL, dependencias, Secrets, API/DTO, UI, workflows, proveedores o tenant data. Riesgos tratados: secretos, alcance de configuración, aislamiento A/B y trazabilidad de cambios.

**Auditoría documental:** A0 estructura, diff, blobs y ausencia de secretos; A1 P01–P03/N01–N09 como requisitos textuales con oráculos falsables; A2 secreto/JWT/RBAC/tenancy, roles y gates de ambiente; A3 correspondencia con fuentes reales del código y Sheets 26 columnas, gate del lote M01.06; A4 SHA publicado/check Scope Lock y readback. Clasificación: `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`. Estas comprobaciones documentales **no certifican** comportamiento real ni despliegue.

**Rollback:** `git revert` normal, aditivo, tras revalidar HEAD, conflictos y SHA; preservar autoría, BITACORA original y nuevas evidencias; no force-push/reset ni despliegue. A5 corresponde a otro TASK_ID Javier; no autoaprobar QA/main/PROD.
