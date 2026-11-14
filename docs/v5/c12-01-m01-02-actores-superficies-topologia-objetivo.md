# SOLQARYN V5 — C12-01.M01.02: actores, I/O y fronteras de topología objetivo

**Unidad:** `C12-01.M01.02` (PLAN fila 1731), C12-01 Topología objetivo, M01 preparación. **Autor:** PRIMARY DEV 05 JAVIER; TASK_ID `6adc2e55f47c81919f6374e561687076`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Repositorio:** `solqaryn/Solqaryn@dev`; **PROJECT_SCOPE_LOCK=STRICT**. Fecha 2026-11-14.

## Alcance y límite de decisión

Esta entrega identifica actores, superficies de entrada/salida y fronteras para la **topología objetivo futura**: frontend Angular 22.2.1, backend ASP.NET Core 10, workers potenciales, MySQL 8.4 privado, DNS/TLS, almacenamiento, volúmenes, backup y rollback. Resultado del bloque: diagrama y ADR respaldados por evidencia, puertos privados y reversibilidad. Todo el documento es preparación **NO_IMPLANTADA**, no una declaración de migración, servicios, seguridad ni SLO ya certificados.

Se preservan los hosts actuales documentados Vercel, Render y Aiven y el proveedor Cloudinary. Hostinger y Cloudflare representan objetivo sujeto a autorización futura. No se conocen ni inventan IPs, hostnames, credenciales, cuotas, RPO/RTO, costes, volúmenes o infraestructura aprovisionada. **Esta microtarea solo añade documentación y CHANGELOG.** Sin VPS, dominios, DNS, cambios de TLS, infraestructura, secrets, API/DB/worker, eventos, webhook, dispatcher, cuentas, nuevas Tasks, QA/main/PROD o gastos. `C12-01.M01.01` es antecedente documental de alcance y NR01–NR16, no autorización para redespliegue.

## A01–A14 — actores y límites de responsabilidad

| Actor | Responsabilidad propuesta | Límite de autoridad |
| --- | --- | --- |
| A01 | Operador infraestructura DEV | Administra manifiestos efímeros y registra evidencias, no compras | No recibe permiso automático sobre PROD, VPS ni claves |
| A02 | Arquitecto de plataforma | Define ADR, puertos y rollback como propuesta | No convierte la topología objetivo en estado vivo |
| A03 | Administrador seguridad | Aprueba secret management, red privada y mínimo privilegio | Nunca pega credenciales o tokens en documentación |
| A04 | Desarrollador API .NET | Mantiene API y prueba health/readiness en sandbox | Sin endpoints nuevos ni configuración Render en esta fase |
| A05 | Desarrollador Angular | Conserva rutas, guards, rewrites y UX ligera | El navegador no concede autoridad tenant/JWT |
| A06 | Responsable persistencia | Diseña MySQL privado, backup, restore y permisos | No cambia Aiven, datos reales o migraciones |
| A07 | Operador red/DNS | Define mapa DNS, TLS, proxy y reglas de entrada | Ningún cambio real en Cloudflare, Vercel o dominios |
| A08 | Proveedor objetivo VPS | Capacidad Hostinger potencial sujeta a contratación | No se asume cuenta, IP, tamaño, disponibilidad o gasto |
| A09 | Proveedor archivos | Conserva Cloudinary actual, estudia R2 futuro | No copia archivos ni crea bucket |
| A10 | Responsable resiliencia | Define RPO/RTO, restore y runbook de reversión | Sin certificación sin ensayo medido |
| A11 | Servicio observabilidad | Propone métricas/alertas sin PII y cuotas | No instala exporters, workers ni alertas |
| A12 | Cliente SaaS / tenant A-B | Consume frontend/API seguros en entorno vigente | No ve redes privadas, datos o endpoints de otra empresa |
| A13 | Revisor A5 independiente | Revisa evidencia causal de otra Task Javier | No autorrevisión ni autorización QA/main/PROD |
| A14 | Propietario comercial | Autoriza gastos, DNS, migración y destino explícitamente | Un documento o DAG SI no sustituye esta orden |

## I01/O01–I12/O12 — entradas/salidas de diseño, no conexiones nuevas

| Par | Entrada de referencia | Salida documental futura | Protección de alcance |
| --- | --- | --- | --- |
| I01/O01 | Declaración Render vigente | Inventario de servicios/variables por referencia | Sin ejecutar redeploy ni extraer secretos |
| I02/O02 | Rewrites y routing Vercel | Mapa de rutas frontend/API | Sin DNS, headers o build live alterados |
| I03/O03 | Dockerfile .NET | Propuesta de contenedor API segregado | No construir o enviar imágenes a proveedor externo |
| I04/O04 | Contrato de entorno API | Matriz de claves esperadas por ambiente | No copiar valores ni connection strings |
| I05/O05 | Guard de BD por ambiente | Contrato de endpoint MySQL privado futuro | Aiven/credenciales actuales inalterados |
| I06/O06 | Arquitectura Angular/.NET/MySQL | Diagrama de límites de confianza por capa | La referencia actual prevalece |
| I07/O07 | Plan de redes privadas futuro | Esquema de puertos/ACL propuestos, no números inventados | No exposición 0.0.0.0 de MySQL |
| I08/O08 | Inventario de secretos | Nombres de referencias opacas y responsables | Sin secretos en Git, logs o Sheet |
| I09/O09 | Volúmenes/backups | Política y flujo de restore sintético futuro | No restaurar ni copiar DB real |
| I10/O10 | Cloudinary/R2 futuro | Inventario de archivos con cero permitido | No inferir objetos inexistentes ni activar R2 |
| I11/O11 | Salud/observabilidad | SLO/SLI futuro con fuente, ventana, costo y retención | Nunca afirmar monitoreo productivo certificado |
| I12/O12 | Aprobación formal/cutover | ADR, checklist de pruebas, readback y rollback | Sin QA/main/PROD y sin migración por esta unidad |

## F01–F14 — fronteras de confianza y riesgos

| Frontera | Transición | Riesgo | Control de diseño futuro |
| --- | --- | --- | --- |
| F01 | Browser→Vercel→API | Token/host/tenant falsificado | JWT/RBAC y pertenencia se validan en backend |
| F02 | Frontend→API privado | CORS/proxy abierto o fuga de cabeceras | Allowlist y headers controlados, no publicar secretos |
| F03 | Internet→Proxy/TLS | Host spoofing y terminación TLS insegura | Host binding explícito y prueba de proxy confiable antes del cambio |
| F04 | Proxy→API contenedor | Puerto público accidental o usuario root | Exponer sólo superficie HTTP autorizada y permisos mínimos |
| F05 | API→MySQL | Conexión abierta y lectura tenant cruzada | Red privada, ACL, consultas servidor y test A/B |
| F06 | API→Volúmenes | Datos privados mezclados entre empresas | Separación lógica/criptográfica y backup con tenant |
| F07 | API→Archivos proveedor | URL firmada reutilizada o permisos laxos | TTL/revocación/ownership/allowlist para proveedor futuro |
| F08 | CI→Registro contenedores | Secrets en artefactos/imágenes | Scan y custodia, tags exact-SHA, no pushes productivos |
| F09 | Admin→Proveedor | Credenciales o pagos sin orden | Aprobación humana, RBAC y trazabilidad |
| F10 | Worker→Cola futura | Retries infinitos/eventos duplicados | Idempotencia, cuota y ningún dispatcher/Task automático actual |
| F11 | Métricas→Collector | PII/JWT/SQL sensible exportado | Tags cerradas, filtros, retención y presupuestos |
| F12 | Dev→QA/main/PROD | Inferencia de permiso por LISTO o A5 | Scopes y autorización independientes, fail-closed |
| F13 | Backups→Restore | Pérdida, confusión de entornos o fuga de datos | Restauración efímera, checksum, rollback, aprobación |
| F14 | Dominios→DNS/CDN | Cutover sin TTL ni salida | Plan reversión probado y propietario autorizado |

## Inventario de evidencia Git, con hashes de blobs, sin certificar proveedor

| Fuente vigente en dev | Blob Git verificado | Interpretación |
| --- | --- | --- |
| `render.yaml` | `f64d291425e2bbfdd9b6466d268753f8f8d86b3d` | Declaración, no prueba de despliegue |
| `frontend/vercel.json` | `5f12bcf667a21afcfc4314b66787ff66627ae666` | Routing/rewrites, no prueba DNS |
| `Dockerfile` | `5bcfd9722c5b88fb7b081a7f23c2b70eb7b56327` | Contrato de build, no imagen Hostinger |
| `backend/src/API/Configuration/RenderEnvironmentContractGuard.cs` | `2c38ebb90a8360b61c9e05df96ff3a9abd5b8f65` | Guard de ambiente actual, no secreto exportable |
| `backend/src/API/Configuration/EnvironmentDatabaseGuard.cs` | `896aa003876f2b4299dfe5d4f1d69b83b5d1be22` | Guard de DB, no MySQL privado nuevo |
| `PROJECT_CONTEXT.md` | `48064b93299e973c33145662897d772712d2fa29` | Resumen de stack actual, no plan de cutover |
| `PROJECT_INDEX.md` | `98c2d82de74ff56ba92474572d39d6afb5bcd89c` | Índice de autoridad actual |
| `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Scope Lock; solo dev |
| `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Arquitectura y seguridad de capas |

## Evidencia futura y negativos obligatorios, NO_EJECUTADOS

| Caso | Positivo futuro verificable | Negativo que debe rechazarse |
| --- | --- | --- |
| P01/N01 | Diagrama de frontend, API, workers y MySQL con fronteras por entorno | Un puerto de DB público o trust implícito desde navegador |
| P02/N02 | Matriz de secretos con nombres de referencia y custodio | Token, clave o connection string incluidos en Git, CI o documentación |
| P03/N03 | Red privada en Docker efímero con test de alcance permitido | Tenant B accede a datos de A o contenedor root/acceso irrestricto |
| P04/N04 | Backend health y readiness separan proceso/DB en ambiente efímero | Un health 200 se declara restauración o PROD certificado |
| P05/N05 | Frontend conserva guards, rutas y CSP/CORS tras fixture de migración | DNS/host falsificado suplanta tenant o habilita origen no permitido |
| P06/N06 | Backup/restore sintético devuelve schema/checksums y rollback | Restaurar en PROD o sobre Aiven real sin orden y respaldo |
| P07/N07 | Prueba de costo, recursos y SLO con umbrales aprobados y muestras | SLO inventado por docs/mock o servicios de pago sin autorización |
| P08/N08 | Cutover simulado conserva reversión segura y trazabilidad | Activar VPS/DNS/R2/workers/Tasks por gate dinámico SI |

Todos los casos son criterios **FUTUROS_NO_EJECUTADOS**; no se simulan resultados, PASS de tests o autorizaciones del propietario. No hay evidencia para declarar topología objetivo implementada.

## Auditoría de la unidad y rollback

**A0:** Skill/copia canónica, Scope Lock STRICT, plan V5 y DAG dinámico, `PLAN_MAESTRO!H1731/I1731` PENDIENTE libre reclamado por CAS, `COLA!G/O=SI`, DAG F/G VALIDADO/SI, preflight de dos Markdown y nueve fuentes Git.

**A1:** cobertura coherente de A01–A14, I/O01–12 y F01–F14, con P01–P08 y N01–N08 futuros. **A2:** tenant A/B, JWT/RBAC en backend, secreto/cadena, aislamiento de entornos/red privada, proveedor/gasto y protección de datos; ningún enforcement modificado. **A3:** nueve blobs existentes, sin Docker/VPS/MySQL/cutover ejecutado. Las suites runtime son NO_APLICA a diff Markdown y nunca PASS ficticio. **A4:** fast-forward CAS a dev, comparar diff 2 Markdown y blobs; exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completados con SUCCESS en SHA exacto; readback PLAN/COLA/BITACORA. **A5:** otra Task Javier con TASK_ID distinto, antes de promocionar.

**Rollback:** `git revert` normal del documento y sección CHANGELOG; no force-push ni modificación retroactiva de BITACORA. No se habilita M01.06, C12 M03, migraciones, infraestructura, QA/main/PROD por esta unidad.
