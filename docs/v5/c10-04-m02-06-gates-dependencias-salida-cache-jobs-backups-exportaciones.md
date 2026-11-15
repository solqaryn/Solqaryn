# SOLQARYN V5 FINAL — C10-04.M02.06: dependencias técnicas y contrato de salida M02 de caché, jobs, backups y exportaciones

**Tipo de entrega:** contrato de salida exclusivamente DOCUMENTAL de M02.06. No implementa código, bases de datos, jobs, cache privada, exportaciones, restauración, infraestructura ni eventos reales. **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Alcance:** solo `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`, 2026-11-15.

**Autoridades:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA; Skill canónica, AGENTS.md, Scope Lock y AUTOMATION_STATUS del HEAD de `dev`. El gate de entrada exige PLAN PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI del ID exacto, con claim exclusivo y readback. El gate documental no equivale a autorización de implementación M03, A5, QA/main/PROD ni compra o despliegue.

## 1. Alcance, procedencia y límites del baseline

| Contrato previo del lote | Evidencia documental | Alcance realmente acreditado |
| --- | --- | --- |
| C10-04.M01.06 | `docs/v5/c10-04-m01-06-gates-rollback-liberacion-cache-jobs.md`; blob `bbf0bfaa75d46362c2e5504d6f0a1840d27bda18` | Gates B01-B14 de M01, sin pruebas operativas de restore/backup |
| C10-04.M02.01 | `docs/v5/c10-04-m02-01-entidades-ownership-cache-jobs-backups-exportaciones.md`; blob `041ff979d7a3fb94003e2072be922d727eb3c2c1` | Doce objetos lógicos, 18 invariantes y futuros P/N |
| C10-04.M02.02 | `docs/v5/c10-04-m02-02-api-dto-errores-compatibilidad-cache-jobs-backups-exportaciones.md`; blob `33133b4aeacf5b0bba3af72ec6f8cec268f93885` | Ocho rutas conceptuales API/DTO, rechazo seguro y pruebas no ejecutadas |
| C10-04.M02.03 | `docs/v5/c10-04-m02-03-persistencia-indices-migraciones-fixtures-cache-jobs.md`; blob `0ae2af7671b81f3c94edc60c2a0f8e254bae32fc` | Nueve proyecciones, doce invariantes, migraciones y fixtures futuros |
| C10-04.M02.04 | `docs/v5/c10-04-m02-04-opciones-flags-cache-jobs-backups-exportaciones.md`; blob `1fc69ea59f4ae1dde7fe66a2f5c05f2e3e9c427a` | Catorce opciones propuestas, V01–V14 y P/N futuros; flags NO instalados |
| C10-04.M02.05 | `docs/v5/c10-04-m02-05-eventos-idempotencia-correlacion-limites-cache-jobs.md`; blob `a5d66072f20efe9abb10610fd53763e3f850a802` | Diez señales hipotéticas, 16 invariantes y P/N futuros; NINGÚN evento emitido |

Las cinco unidades M02.01–05 constan LISTO con A5 técnica independiente por otras Tasks Javier en el Sheet antes del claim de este ID. Sus productos son diseños complementarios, no evidencia de SQL, endpoint, procesamiento de jobs, backup cifrado ni rollback operativo. Si cualquier blob cambia en `dev` o aparece un A5_FAIL causal, invalidar esta referencia y revisar el gate antes de M03. El documento no introduce predecesores nuevos fuera del DAG calculado.

**Fuentes técnicas existentes, NO certificadas como funcionamiento de este lote:** `backend/src/Application/Services/OutboxRetryProcessor.cs` (blob `542c0a655a3ee0698e3d80f1854d43903b6470fa`), `backend/src/Application/Interfaces/IPublicStoreCache.cs` (`15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8`), `backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs` (`1a102138b88bd54989eeade5acf7a5ee7bf63867`), `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs` (`454d588851ecc7db810259e63705852360172928`). Su existencia no demuestra scheduler universal, cache distribuida, exportaciones privadas, restore efectivo ni proveedores externos aprobados.

## 2. Dieciséis puertas de salida, todas exigibles antes de implementar M03

| Gate | Condición futura verificable / bloqueo |
| --- | --- |
| G01 | A0–A4 de los cinco contratos M02 auditados, A5 independiente por Task distinta y readback; ninguna declaración textual sustituye ese registro |
| G02 | Scope Lock SUCCESS en HEAD/SHA pertinente, Skill y arquitectura originales, JWT/RBAC y tenant A/B sin bypass |
| G03 | M03 se define por ID exacto en PLAN y DAG, COLA G/O realmente SI y propietario CAS exclusivo, no por estar terminado este documento |
| G04 | Domain no importa Infrastructure; contratos y ownership empresariales se modelan sin crear capacidad trans-tenant implícita |
| G05 | Application aplica autenticación y permiso al estado actual; servicio/worker no acepta EmpresaId ni actor del cuerpo cliente como autoridad |
| G06 | Infrastructure y EF Core/MySQL se prueban en servicio efímero: aislamiento A/B, FK, índices reales y migración reversible antes de cualquier DDL |
| G07 | Cache pública vigente se preserva; cache privada nueva permanece apagada hasta autorización y prueba de clave tenant+permisos+versión |
| G08 | Revocación de sesión/rol y cambio de empresa invalidan resultados obsoletos antes de lectura sensible, incluyendo multiinstancia cuando aplique |
| G09 | Job/idempotencia: claim CAS único, lease con owner/version, retry finito, backoff/deadline, conflicto de payload y caída recuperable |
| G10 | Cualquier exportación requiere permiso, ownership, cuota, redacción, saneamiento CSV, hash, artefacto privado y descarga reautorizada |
| G11 | Backup/restauración se habilitan solo tras integridad, cifrado, retención, permisos segregados y restore probado en MySQL efímero: no PROD |
| G12 | No activar workers, cron, dispatcher, webhook, R2, Cloudflare, Hostinger ni otro proveedor por existir una lista de eventos conceptual |
| G13 | No registrar JWT, OTP, cookies, DSN, URLs firmadas, payloads PII ni secretos en logs, cache, export, BITACORA o mensajes |
| G14 | Error de API/worker/provider aplica rechazo genérico y evidencia segura, sin retries infinitos, cross-tenant fallback ni privilegio implícito |
| G15 | Tests causales de .NET 10/xUnit, integración MySQL/EF 10 efímera, seguridad multiempresa y frontend/Playwright cuando correspondan, SUCCESS exact SHA |
| G16 | Evidencia A5 de Task Javier ajena a la autora, más autorización separada de despliegue/QA; salida M02 NO es PASS de M03 ni autorización PROD |

El orden de implementación se deriva del DAG y de permisos reales, no del número de gates. Ningún conjunto de documentos habilita operaciones sobre datos vivos o infraestructura pagada.

## 3. Handoff por componente y exclusiones

| Componente / consumidor | Contrato que M03 debe producir cuando esté habilitada | Exclusiones actuales |
| --- | --- | --- |
| Domain | Invariantes de recurso, pertenencia, versiones, estados y transiciones puras | No entidades/tablas nuevas en este commit |
| Application | Use cases/DTO y validaciones RBAC, ownership, idempotencia y cuota | No endpoints ni servicio nuevo |
| Infrastructure | Adaptadores EF/MySQL de acceso tenant-aware y almacenamiento protegido | No DDL, backfill, R2 ni restore real |
| API | Authz JWT real y errores seguros; no confiar en parámetros, headers o Host como tenant | No rutas live ni cambios CORS |
| Caché pública/privada | Preservar IMemoryCache público, diseñar nuevas particiones privadas y revocación verificada | No activar cache privada ni afirmar coherencia distribuida |
| Jobs/outbox | Reutilizar contratos existentes, introducir worker SOLO mediante scope y test propio | No programador, Task nueva, webhook o bus |
| Exportaciones y backups | Consentimiento/RBAC, cifrado, retención y artefactos con acceso por recurso | No generar enlaces, snapshots ni archivos reales |
| Observabilidad | CorrelationId solo diagnóstico, eventos auditables redactados, presupuestos de cuota | No telemetría en vivo ni secretos |

**Seguridad transversal:** tenant A y B aislados aun con identificador local igual; usuario A1 y A2 dentro de A no intercambian datos; un token auténtico sin permiso vigente no puede exportar, reintentar ni restaurar. Un A5 documental nunca sustituye pentest, tests HTTP/EF reales ni aprobación de privacidad. La UI puede mejorar el rechazo pero nunca decidir grants por sí sola.

## 4. Ocho estrategias de reversión propuestas — NO EJECUTADAS

| Caso | Reversión y barrera futura |
| --- | --- |
| RB01 | Cambios puramente documentales: `git revert` ordinario, sin reescribir historial ni BITACORA |
| RB02 | Nueva caché privada: desactivar flag de servidor y purgar generaciones con aislamiento por tenant verificado |
| RB03 | Procesador de jobs: detener nueva admisión, drenar/reconciliar leases e idempotencia sin duplicar efectos |
| RB04 | Cambio de contrato API: restaurar compatibilidad sin exponer datos ajenos ni convertir 403 en 200 |
| RB05 | Migración MySQL expand/contract: rollback compatible con datos existentes y backup verificado, solo en entorno autorizado |
| RB06 | Exportación fallida: invalidar artefacto parcial/URL, registrar estado y no enviar notificación de éxito |
| RB07 | Backup corrupto: aislar manifiesto, no permitir restore live ni afirmar RPO/RTO medidos |
| RB08 | Incidente tenant/permiso: fail-closed, revocar sesiones/caches/leases afectados y conservar auditoría redactada |

RB02–RB08 son planes de contingencia que necesitan implementación y ensayo separados; no han sido activados en este documento.

## 5. Doce parejas de aceptación futuras, no tests ejecutados

| Caso | Positivo futuro | Negativo de rechazo futuro |
| --- | --- | --- |
| P01/N01 | Cliente A lee recurso propio con permiso | Cliente de B recibe dato o clave de cache de A |
| P02/N02 | Dos usuarios autorizados de A reciben solo recursos propios | A1 recibe la exportación de A2 por compartir EmpresaId |
| P03/N03 | Revocación incrementa versión de autorización y cache | Sesión revocada lee valor anterior cacheado |
| P04/N04 | Un CAS reclama un job y conserva único efecto | Dos ejecutores completan el mismo lease/versión |
| P05/N05 | Reintentos respetan deadline y máximo | Cliente eleva límite o activa bucle ilimitado |
| P06/N06 | Clave idempotente repetida con mismo payload retorna mismo resultado | Misma clave con otro payload ejecuta segundo efecto |
| P07/N07 | Export autorizado respeta cuota y saneamiento CSV | Fórmula CSV maliciosa, PII no autorizada o cuota excedida |
| P08/N08 | Artefacto privado exige permiso y checksum vigente | URL firmada caducada sustituye verificación de ownership |
| P09/N09 | Restauración efímera verifica conteos/FK/tenants A/B | Restauración toca PROD o mezcla tenants |
| P10/N10 | Config global apagada prevalece sobre tenant | Host/query/header habilita flags o provider |
| P11/N11 | Fallo deja estado auditable y reanudable | Error parcial se presenta como SUCCESS y pierde historial |
| P12/N12 | CI exact SHA PASS y revisión A5 independiente autorizan evaluar próximo gate | CANCELLED/SKIPPED/IN_PROGRESS se interpretan PASS o autor se autoaprueba |

Todas las parejas son **FUTURAS_NO_EJECUTADAS**. Su definición no prueba tasas de error, RPO/RTO, disponibilidad ni aislamiento runtime. Para ejecutar pruebas habrá que reclamar microtarea real y usar GitHub Actions con herramientas instaladas en el runner (no inventar falta de .NET/Angular local como imposibilidad de CI).

## 6. A0–A5, riesgos, publicación y readback

- **A0 (estática):** comparar fuente, Skill/HEAD, alcance, blobs anteriores, precedentes LISTO+A5 y cambio exclusivo Markdown; verificar ausencia de secretos y encabezados vacíos en CHANGELOG.
- **A1 (funcional DOCUMENTAL):** inspeccionar correspondencia M02.01–05, G01–G16, RB01–RB08 y P01/N01–P12/N12. No afirmar ejecuciones de pruebas descritas.
- **A2 (seguridad DOCUMENTAL):** revisar no autoridad del navegador, tenants A/B y actores A1/A2, JWT/RBAC de servidor, idempotencia, PII y barreras fail-closed.
- **A3 (integración DOCUMENTAL):** contrastar interfaces cache/Outbox/storage existentes y DAG; compilación .NET, UI, SQL, worker y Playwright **NO_APLICA al diff de dos Markdown**, no PASS inventado.
- **A4 (publicación):** diff `docs/v5` y sección autocontenida de `CHANGELOG_AI.md`; fast-forward con expected HEAD/CAS; Scope Lock y procedencia V5 `completed/success` en SHA exacto, eventos BITACORA A:Z y PLAN/COLA/DAG readback.
- **A5:** revisión autónoma por otra de las cinco Tasks Javier distinta de la autora, con blob, diff, SHA y pruebas pertinentes, antes de desbloqueos que la exijan. Nunca autoaprobar.

**Riesgos pendientes sin declarar defecto demostrado:** caché privada multiinstancia, permisos caducados, lease duplicado, export parcial, restauración no probada, provider externo no autorizado y alteración de PII. Su cierre requiere evidencia ejecutable en M03/M05/M06, no frases de este contrato.

**Rollback actual:** revertir el commit documental mediante `git revert` normal si aparece un error; BITACORA conserva evidencia histórica y PLAN/COLA/DAG se reconcilian por mecanismo institucional. No cambiar QA, main, PROD, Aiven real, DNS, R2, Hostinger, Cloudinary, Secrets, cuentas, schedules ni otras Tasks.
