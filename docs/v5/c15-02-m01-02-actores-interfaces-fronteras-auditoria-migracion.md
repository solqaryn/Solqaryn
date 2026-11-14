# SOLQARYN V5 FINAL — C15-02.M01.02: actores, contratos y fronteras de arquitectura/migración

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 06 JAVIER (`6af16bbad26881918c283abe06e1f32a`). **Repositorio/rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** definición de contratos documentales, no prueba de integración ni auditoría de infraestructura ejecutada.

**Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, Skill canónica, `references/architecture.md` y [M01.01](./c15-02-m01-01-alcance-no-regresion-auditoria-arquitectura-migracion.md). PLAN fila 2199 H=PENDIENTE/I vacío antes del CAS, COLA G/O=SI y DAG fila 2160 F=VALIDADO/G=SI habilitan sólo preparación. M01.01 propia conserva comprobaciones pendientes; este documento no declara liberación terminal ni entrega runtime.

## 1. Delimitación y reglas para leer las interfaces

La futura auditoría C15-02 deberá contrastar arquitectura vigente declarada (Angular 22.2.1/ASP.NET Core 10/EF Core 10.0.12/MySQL 8.4, Vercel/Render/Aiven/Cloudinary) con objetivo V5 (separación en Hostinger y Cloudflare DNS/TLS/R2). Las entradas/salidas que se enumeran son **contratos a verificar**, no un inventario de rutas activas o sistemas desplegados; su estado es `NO_ACREDITADO` hasta obtener código, config, logs redactados, pruebas efímeras y readbacks exact-SHA.

La frontera principal es `Browser → Angular → HTTP → API → Application → Infrastructure/EF → MySQL`. En cada salto identificar identidad, datos clasificados, autoridad, límites, reintentos, observabilidad y reversión. El usuario no selecciona libremente `EmpresaId`, tenant, sucursal, archivo ni proveedor; únicamente el backend puede autorizar acciones contra recursos de pertenencia verificada.

## 2. Doce actores o participantes de análisis (A01–A12)

| ID | Participante | Acción o responsabilidad de auditoría | Restricción de confianza |
| --- | --- | --- | --- |
| A01 | Propietario/administrador autorizado | Aprobar exclusivamente alcance y cutover futuro | DEV/A5 no otorga permisos QA/main/PROD |
| A02 | Cliente/usuario de tenant A | Navegar, autenticar y operar recursos propios | No ve ni modifica tenant B |
| A03 | Cliente/usuario de tenant B | Control negativo simétrico para A | No hereda rol de A |
| A04 | Operador de sucursal | Operar recursos permitidos de la sucursal | No puede elegir arbitrariamente otra propiedad |
| A05 | Plataforma/superadmin autorizado | Control plane con identidad y auditoría reforzada | La UI no crea privilegios |
| A06 | Angular router/guard | UX y solicitudes HTTP | No sustituye validación backend |
| A07 | API y middleware JWT/RBAC | Validar identidad, permiso, origen y tasa | Host/body/header no autentica tenant |
| A08 | Application/servicios de dominio | Coordinar casos y contratos | No depende de proveedor de infraestructura |
| A09 | Infrastructure/EF/MySQL | Persistencia, transacciones y pertenencia | Ningún cross-tenant silencioso |
| A10 | Integraciones actuales (Cloudinary/SMTP/QuestPDF) | Entregas y documentos del sistema vigente | Datos y credenciales limitados |
| A11 | Operador de servicios futuros Hostinger/Cloudflare/R2 | Contrato objetivo aún sin activación | No se asume cuenta, dominio ni objeto existente |
| A12 | Auditor independiente / CI GitHub | A0–A5, pruebas y trazabilidad SHA | No autopermiso, no PASS de skipped/queued |

## 3. Doce entradas y salidas que la auditoría deberá verificar

| Contrato | Entrada a comprobar | Salida verificable futura | Criterio negativo requerido |
| --- | --- | --- | --- |
| IO01 Identidad | JWT/sesión legítima | Principal, roles y tenant confiables en API | Token alterado/revocado falla |
| IO02 Recurso | Request de actor autorizado y recurso propio | Respuesta de sólo su tenant/sucursal | ID de empresa B se niega |
| IO03 Frontend | Build Angular y ruta autorizada | Vista consistente y bundle medido | Guard UI no autoriza endpoint |
| IO04 API | Rutas, DTO y contrato HTTP | Respuestas y errores tipados sin datos sensibles | Host falso no induce tenant cruzado |
| IO05 BD | Esquema EF, migración versionada y fixture | Transacción consistente y conteos verificados | DDL destructivo sin backup falla |
| IO06 Backup | Artefacto cifrado y metadatos comprobables | Restore descartable, conteos/checksum e informe | Backup sin restore no se certifica |
| IO07 Archivo | Objeto realmente inventariado y owner | Entrega propia controlada | Objeto de B o TTL vencido falla |
| IO08 Worker | Job con tenant, correlación y clave idempotente | Efecto único/auditable tras reintento | Replay duplica o cambia tenant |
| IO09 Observabilidad | Correlation ID y eventos redactados | Traza sin token ni PII indebida | Fuga de secretos en logs bloquea |
| IO10 DNS/TLS | Host binding, certificado, TTL y plan | Prueba de resolución y retorno reversibles | Certificado no válido o host ajeno |
| IO11 CI | Commit/árbol, diff y suite causal | `completed/success` en SHA exacto | cancelled/skipped/queued no PASS |
| IO12 Control operativo | Permiso de entorno y review A5 separado | Acta verificable de autorización/rollback | A5 documental dispara PROD automáticamente |

**Todas las salidas anteriores son objetivos FUTUROS_NO_EJECUTADOS**, salvo la mera lectura estática de contratos de este documento. No se han realizado HTTP, SQL, Playwright, Docker, restore, DNS, Cloudflare/R2 ni pruebas de integraciones operativas.

## 4. Diez fronteras de confianza (F01–F10)

| Frontera | Riesgo a impedir | Evidencia pendiente |
| --- | --- | --- |
| F01 Browser → Angular | Sesión/estado cliente manipulado | Negativos de guard y refresh, sin privilegio servidor |
| F02 Angular → API | Headers, host o DTO controlados por cliente | Pruebas HTTP 401/403 y CORS |
| F03 JWT → roles/tenancy | BOLA/IDOR o claim falsificado | Matriz usuario/rol/tenant A/B |
| F04 API → Application | Caso de uso sin comprobación de propietario | Reglas y pruebas positivas/negativas |
| F05 Application → Infrastructure/EF | Query sin filtro tenant o transacción parcial | Integración MySQL descartable |
| F06 API/worker → proveedor | Secretos, reintentos y efectos duplicados | Credencial segregada y test de fallo |
| F07 Storage → descarga/URL firmada | Fuga entre propietarios o link vencido | Ownership/TTL con objetos sintéticos |
| F08 Infraestructura → DNS/TLS | Host spoofing, certificado o cutover irreversible | Matriz host/tenant y simulacro |
| F09 Backup → restore | Datos cruzados, corrupción y falta de reversión | Restore efímero con checksum/conteos |
| F10 GitHub/Sheet → publicación | SHA/owner/gates concurrentes o A5 propia | CAS/readback, CI exact-SHA y auditor distinto |

## 5. Ocho pares de aceptación propuestos — NO_EJECUTADOS

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | Actor A opera sólo recurso A | Actor A ve ID B por host, query o header |
| P02/N02 | API verifica RBAC/identidad y devuelve estado esperado | Guard UI o rol retirado permite HTTP |
| P03/N03 | EF conserva pertenencia y transacciones en fixture | Migración mezcla tenant o pierde filas |
| P04/N04 | Restore efímero recupera checksum y conteos | Backup no restaurable pasa por certificado |
| P05/N05 | Objeto inventariado propio y link válido descarga | Objeto de B, inexistente o expirado descarga |
| P06/N06 | Worker procesa idempotente y audita tenant | Retry duplica o usa identidad de otra empresa |
| P07/N07 | Host/TLS autorizado y reversible se comprueba | DNS real cambiado sin permiso o rollback |
| P08/N08 | Scope Lock/CI exact-SHA y A5 Task distinta pasan | SHA distinto, skipped, cancelled o autor propio se aprueba |

Ningún resultado P01–P08/N01–N08 se declara PASS. El contrato de pruebas deberá registrar actor, origen, tenant, entorno, SHA, esperado, observado, job, timestamp y reversión sin exponer secretos. Cualquier P0/P1 futuro necesita evidencia causal; la mera ausencia de una prueba no es un fallo verificado.

## 6. Impacto A0–A5, exclusiones y rollback

**Cambio:** este Markdown y nueva sección autocontenida en `CHANGELOG_AI.md`. No afecta al frontend, API/DTO, JWT/RBAC, Domain/Application/Infrastructure, MySQL, migraciones, datos, Cloudinary, R2, DNS, TLS, proveedores, webhooks, CI, QA/main/PROD ni seis Tasks. **Consumidor:** M01.03 inventario de código y evidencia y posteriores M01.04–06 conforme a DAG, no release.

**A0:** 12 participantes, 12 contratos y 10 fronteras, fuentes y no-alcance. **A1:** consistencia documental más P/N FUTUROS_NO_EJECUTADOS; pruebas funcionales `NO_APLICA` al Markdown. **A2:** seguridad JWT/RBAC/tenant A/B protegida conceptualmente, sin manipularla ni acreditar tests HTTP reales. **A3:** contratos cotejables con Stack/Skill/plan; .NET/Angular/MySQL/Playwright `NO_APLICA` al diff. **A4:** commit directo dev, fast-forward/CAS HEAD, diff limitado, Scope Lock y V5 provenance exact-SHA `completed/success` y readback PLAN/COLA/BITACORA antes de LISTO_DEV. **A5:** pendiente de otro TASK_ID Javier; no autoaprobación.

**Rollback:** `git revert` normal del commit tras lectura de HEAD; sin fuerza ni modificación histórica de BITACORA. No crear eventos, PR, ramas, ejecutores, compras, migraciones, nuevos servicios ni despliegues.
