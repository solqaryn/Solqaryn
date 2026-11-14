# SOLQARYN V5 — C12-04.M01.02: actores, interfaces y fronteras de MySQL privado/cutover

**Fecha:** 2026-11-14. **ID:** C12-04.M01.02. **PLAN_MAESTRO:** fila 1839. **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio/rama:** `solqaryn/Solqaryn@dev`. **Estado del artefacto:** DISEÑO_DOCUMENTAL_M01; no es exportación, ensayo de restore, migración ni autorización de infraestructura.

## Autoridad, identidad y exclusiones

Fuente ejecutable: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y `references/architecture.md`. La unidad C12-04 exige **identificar entradas, salidas, actores y superficies afectadas** de una migración futura con exportación, restore de ensayo, comparación, ventana de cambio y reversión. Se realizó preflight individual PLAN H=PENDIENTE/I vacía, COLA G=SI/O=SI, DAG F=VALIDADO/G=SI y claim exclusivo, no inferido de un estado histórico.

Contexto versionado verificado: `backend/src/API/Solqaryn.API.csproj` declara `net10.0`; `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` declara EF Core 10.0.12 y `MySql.EntityFrameworkCore` 10.0.9; `backend/src/Infrastructure/Persistence/AppDbContext.cs` contiene límites `VerifiedCommercialTenant`, `VerifiedPublicStoreTenant`, `VerifiedPublicInvoiceTenant` y `VerifiedPlatformSupportTenant`, dependientes de contexto HTTP verificado. La arquitectura documenta MySQL actual y Vercel/Render/Aiven/Cloudinary, mientras Hostinger/Cloudflare son **topología objetivo no activada**. Los caminos de código no prueban conectividad ni contenidos de bases vivas.

**Alcance presente:** solamente este documento y sección autocontenida en CHANGELOG. Sin accesos Aiven, exports, dumps, restores, DDL, migraciones EF, cambio de DNS/TLS, backups de datos reales, credenciales, pagos, nuevos workers, webhooks, QA/main/PROD ni contratación de Hostinger.

## A01–A14 — actores y límites de autoridad

| ID | Actor o componente | Facultades y control estricto |
| --- | --- | --- |
| A01 | Propietario del proyecto | Debe emitir autorización explícita posterior para migración, destinos, ventana y reversión |
| A02 | Responsable de datos/DBA autorizado | Define inventario, consistencia de snapshot, cadena de custodia y prueba de restauración; M01 no le concede acceso |
| A03 | Tenant empresa A | Conserva solamente datos de A; jamás usa cabeceras/EmpresaId arbitrarios como prueba de pertenencia |
| A04 | Tenant empresa B | No lee, compara ni infiere tablas, métricas o archivos de empresa A |
| A05 | Administrador comercial tenant | Permisos RBAC por ámbito; no puede hacer dump global ni reconfigurar proveedor |
| A06 | Administrador de plataforma auditado | Acciones privilegiadas sujetas a autorización especial, mínimo privilegio y trazabilidad |
| A07 | Angular 22.2.1 | Superficie de usuario, sin autoridad para cambiar conexiones, schema ni tenant efectivo |
| A08 | API ASP.NET Core 10 | Valida JWT, RBAC, tenencia y contratos; se mantiene fuera de escritura de DDL no aprobada |
| A09 | Application/Domain | Mantiene reglas transaccionales comerciales, identidades y dependencias, sin acoplarse al hosting |
| A10 | Infrastructure / AppDbContext | EF/MySQL y límites Verified*; ningún proceso sin HttpContext obtiene un tenant global implícito |
| A11 | Migrador único futuro | Aplica schema/DDL solo con permiso, lock y versión de migración controlados; no existe nueva ejecución aquí |
| A12 | Operador de export/restore futuro | Usa herramientas aprobadas, hashes, cifrado y destino de ensayo aislado, sin reusar credenciales de producción |
| A13 | Plataforma/hosting futuro | Hostinger privado, Cloudflare/DNS y redes son objetivos condicionados, no servicios verificados/provisionados |
| A14 | Auditor/revisor Javier independiente | Compara manifiestos y resultados saneados, sin copiar PII, JWT, hashes de contraseñas o conexiones |

La firma de la persona ejecutora no sustituye la autorización de migración ni el A5 independiente. No se presupone que un usuario comercial pueda acceder a toda la base.

## I01/O01–I12/O12 — entradas y salidas **futuras, no ejecutadas**

| Par | Entrada prevista | Salida exigible y gate |
| --- | --- | --- |
| I01/O01 | Orden aprobada con objetivo, source, target y ventana | Acta autorizada con responsables/ambientes; ausencia => NO_GO |
| I02/O02 | Inventario por tabla, FK, secuencias, tenant y clasificación | Manifiesto trazable de alcance; no se infiere población a partir del repo |
| I03/O03 | Identidad y privilegios de export mínimos, externos al código | Autorización y auditoría del acceso sin exponer secretos |
| I04/O04 | Snapshot consistente y versión MySQL/EF verificadas | Artefacto cifrado con checksum y momento UTC, sin falso PASS por existencia de nombre |
| I05/O05 | Dataset sintético autorizado para ensayo | Restore efímero, no productivo, con integridad referencial comprobable |
| I06/O06 | Conteos y hashes por tabla/tenant antes y después | Informe de diferencias y tolerancias explícitas, no igualdad presunta |
| I07/O07 | Historial de migraciones EF y dueño del DDL | Versiones verificadas, lock único y plan de esquema reversible |
| I08/O08 | Identidad/JWT/RBAC y pertenencia A/B de prueba | Matriz de acceso permitido/denegado sin contaminación de tenants |
| I09/O09 | Workload de escritura con transacciones y outbox | Prueba de no duplicar facturas, pagos o inventario durante el cutover |
| I10/O10 | Readiness, health, latencia y fallos simulados | Evidencia del fail-closed y decisión de abort/rollback |
| I11/O11 | Ventana, freeze, tráfico, DNS/TLS y plan de reversión | Gate de cutover explícito, registro del cambio y comprobación del destino |
| I12/O12 | Procedimiento de retorno + delta de escrituras | Restore del camino original con reconciliación auditada; sin pérdida silenciosa |

Las entradas I01–I12 **no son artefactos hoy obtenidos**, y los outputs O01–O12 **no son resultados existentes**. Métricas reales, RPO/RTO y umbrales quedan **PENDIENTES_DE_MEDICIÓN_Y_APROBACIÓN**.

## F01–F10 — fronteras de confianza y superficies

| ID | Cruce de frontera | Validación indispensable |
| --- | --- | --- |
| F01 | Usuario/Angular → API | JWT/RBAC verificados por servidor; EmpresaId, host, cookies y parámetros no confieren autoridad |
| F02 | API → Application → DbContext | Tenant efectivo resuelto de Verified* y alcance de recurso; soporte temporal estrictamente lectura |
| F03 | Application → transacción MySQL | Integridad FK, serialización de escrituras críticas, trazabilidad por tenant |
| F04 | MySQL actual → export futuro | Acceso mínimo, snapshot atómico, cifrado, manifest, checksum, custodia |
| F05 | Export → transporte/almacenamiento de ensayo | Canal autenticado, TTL/retención, sin secretos/PII en logs ni repos |
| F06 | Backup → MySQL efímero futuro | Restore aislado, esquema y collation controlados, validación de hashes y conteos |
| F07 | Migrador futuro → DB de destino | Exclusividad del DDL, compatibilidad con versión app, rollback probado |
| F08 | API/worker → DB destino | Binding tenant fuera de HTTP explícito, autorizaciones por operación; no global implícito |
| F09 | Control de tráfico → hostname/DNS futuro | Aprobación diferenciada, TLS y confianza de proxy demostrados; sin confiar en x-forwarded-host sin proxy confiable |
| F10 | Observabilidad → operador/auditor | Correlation IDs y decisiones saneadas, sin connection strings, datos sensibles ni tokens |

Los componentes futuros no están implementados ni verificados. Un error de checksum, diferencia no autorizada, fallo de FK, JWT inválido, cambio de RBAC o tenant A/B expuesto impide cutover. Una ruta de rollback debe conciliar escrituras posteriores o definir un punto de detención explícitamente aprobado; “volver a apuntar DNS” no basta.

## P01/N01–P10/N10 — aceptación futura **FUTUROS_NO_EJECUTADOS**

| Par | Positivo requerido en entorno autorizado | Negativo que debe fallar cerrado |
| --- | --- | --- |
| P01/N01 | Orden, source/target y ventana firmados | Sin autorización el proceso no exporta ni conmuta |
| P02/N02 | Snapshot sintético consistente produce hash verificable | Snapshot corrupto o incompleto se rechaza |
| P03/N03 | Restore de ensayo conserva FK, conteos y claves | FK rota o divergencia sin aprobación bloquea |
| P04/N04 | Tenant A accede solo a A tras restore | Empresa B, header falso o URL de A se deniega |
| P05/N05 | JWT/RBAC vigente conserva privilegio exacto | Expirado, revocado o rol insuficiente se rechaza |
| P06/N06 | Un migrador autorizado aplica DDL con lock | Segundo migrador concurrente no ejecuta esquema |
| P07/N07 | Ventana congelada preserva documentos y asientos | Reintento duplica pago/factura y se marca FAIL |
| P08/N08 | Health/readiness refleja DB destino operativa | DB caída jamás retorna healthy para readiness |
| P09/N09 | Rollback ensayado con delta reconciliado | Delta inconsistente no se pierde silenciosamente |
| P10/N10 | Logs evidencian checks sin secretos ni PII | JWT/contraseña/connection string en logs causa FAIL |

**No se ejecutaron pruebas positivas, negativas, MySQL, CI runtime ni rollback real en esta M01.02**. Las futuras etapas M02–M05 deberán implementar y probar los controles que apliquen con CI exact-SHA y servicio MySQL efímero, preservando versión del provider.

## Impacto, A0–A5, dependencias y reversión

- **A0 documental:** revisar Skill/Scope Lock/HEAD, fuentes, PLAN H/I, COLA G/O, DAG F/G y propiedad única; distinguir arquitectura actual de objetivo.
- **A1 documental:** verificar completitud de A01–A14, I/O01–12 y P/N01–10; sin afirmar PASS funcional de export/restore.
- **A2 documental:** inspeccionar límites JWT/RBAC, tenant A/B, Verified*, soporte solo lectura, secretos, PII y confianza de proxies; seguridad runtime NO_APLICA.
- **A3 documental:** contrastar C12-04.M01.01, C12-03.M01.02, C10-04.M01.02 y proyectos .NET/EF; integración DB/runtime NO_APLICA a Markdown.
- **A4:** publicar exactamente dos archivos Markdown (este documento + sección autocontenida CHANGELOG) en `dev` por fast-forward CAS; exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` con `completed/success` en SHA publicado; registrar estados y BITACORA con readback.
- **A5:** una de las otras cinco Tasks Javier deberá revisar independientemente; el autor conserva A5_PENDIENTE sin autoaprobar.
- **Rollback:** revertir normalmente el changeset documental revisando HEAD concurrente; no borrar BITACORA ni reescribir rama.

No se libera M01.06, M03 runtime, migración real, Hostinger, Cloudflare, DNS, QA/main/PROD, secretos ni compras. La entrega es un contrato preparatorio verificable, no certificación de cambio de proveedor.
