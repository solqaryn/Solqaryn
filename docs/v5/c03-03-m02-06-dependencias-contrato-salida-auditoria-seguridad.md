# SOLQARYN V5 FINAL — C03-03.M02.06: dependencias y contrato de salida de auditoría de seguridad

**Naturaleza:** contrato técnico DOCUMENTAL, no implementación ni certificación runtime. **Autor:** PRIMARY DEV 06 — JAVIER_CHATGPT; TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repositorio y rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuentes ejecutables:** Standard V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, Skill canónica y Scope Lock del HEAD.

**Resultado exigido:** fijar dependencias técnicas y un contrato de salida verificable del lote M02 (contratos y modelo). La meta futura del bloque C03-03 es auditar autorizaciones, denegaciones, cambios críticos y accesos excepcionales **sin secretos**, con correlación, retención autorizada y aislamiento A/B. Esas capacidades NO se vuelven reales por completar este documento.

## 1. Precondiciones y prueba de independencia

El preflight del ID exacto exige `PLAN_MAESTRO!A517=C03-03.M02.06`, `H517=PENDIENTE`, `I517` vacío, `COLA!G517/O517=SI/SI` y `DAG_TECNICO!A483=C03-03.M02.06`, `F483=VALIDADO`, `G483=SI`. La regla dinámica de DAG cita `C03-03.M01.01–06+A5`, `C03-03.M02.01–05+A5` y `C02-04.M01.06+A5`; deben mantenerse LISTO y con revisión independiente de una Task Javier distinta de cada autora. El gate reactivo no es una aprobación de seguridad runtime ni autoriza migraciones, exports o cambios de entorno.

Las cinco M02 anteriores fijan contratos que este documento integra sin duplicar su autoridad:

| Entrada de C03-03 | Contrato verificable y su frontera |
| --- | --- |
| M02.01 — entidades, ownership | Identidad del evento, actor minimizado, recurso y empresa con separación explícita plataforma/tenant; NO tabla EF certificada |
| M02.02 — API/DTO/errores | Ocho operaciones lógicas y 14 errores seguros; rutas futuras no son endpoints instalados ni permisos concedidos |
| M02.03 — persistencia/índices | Doce agregados conceptuales, ocho índices candidatos y plan de reversión futuro; NO DDL, EXPLAIN ni MySQL ejecutado |
| M02.04 — configuración/flags | Defaults de autorización, redacción, retención y límites de disponibilidad fail-closed; NO flags operativos activados |
| M02.05 — eventos/idempotencia | Doce eventos propuestos, sobre mínimo, reglas de reintento y correlación; NO instalar dispatcher, webhook, worker, outbox ni cola |

**Dependencias transversales:** `AuditoriaController`, `AuditoriaService`, `RegistroAuditoria`, `AuditoriaRepository`, `CorrelationIdMiddleware` y `AppDbContext` se consideran código existente a revisar antes de cualquier M03. `RegistrarAsync` es best-effort y `RegistrarEstrictoAsync` propaga fallos: ninguna garantiza por mera declaración que un cambio de negocio y su registro sean una única transacción. `X-Correlation-ID` saneado es diagnóstico, no credencial, permiso o clave de deduplicación. `EmpresaId=null` requiere política de ámbito plataforma explícita, nunca acceso global por defecto.

## 2. Contrato de salida M02 y gates de implementación posterior

| Gate | Evidencia exigida para M02 documental | NO_GO que conserva M03 o promociones |
| --- | --- | --- |
| S01 — catálogo/identidad | V5 2340 IDs, PLAN/COLA/DAG, propietario único y CAS/readback por ID | ID duplicado, owner activo, dependencia no LISTO+A5 o fórmula engañosa |
| S02 — control JWT/RBAC | Actores, acciones, recurso, membership y `Auditoria.Ver` resueltos por backend | Header, rol legacy, flag o scope del cliente amplía permisos |
| S03 — tenancy BOLA/IDOR | Contrato de consultas A/B, cursor ligado a tenant, `EmpresaId` validado | Actor A puede enumerar evento, recuento, grant o recurso B |
| S04 — auditoría estricta | Semántica futura de persistencia/rollback, conflicto y ACK después del COMMIT | Operación crítica aplicada cuando la escritura estricta falló |
| S05 — información sensible | Lista de exclusiones JWT, contraseñas, cookies, hashes, secretos, PII y logs de error | Payload arbitrario o old/new privados en log, export o excepción |
| S06 — excepciones | Reautenticación/MFA, aprobación distinta, ámbito, TTL y revocación como gates futuros | Autoaprobación, expiración ignorada o privilegio de plataforma heredado |
| S07 — retención/export | Política y autorización verificables antes de cualquier purga/exportación | DELETE, retención, export masivo o restauración sin política, respaldo y rollback |
| S08 — código M03 | Pruebas .NET 10 + integración real HTTP/EF/MySQL efímero 8.4, casos A/B, fallos transaccionales, Scope Lock SHA exacto | Sustituir pruebas por documentos/mocks, usar check queued/cancelled/skipped/failed como PASS |
| S09 — idempotencia futura | Distinguir correlationId, operationRef e identidad de evento; efecto único por operación crítica | Replay que crea cobro, grant, revocación o auditoría duplicados |
| S10 — entornos | A0–A4 causales en dev y A5 independiente por Task del allowlist Javier | LISTO_DEV/A5 convertido en permiso implícito para QA/main/PROD, IAM o proveedores |

M02 finaliza **diseño y condiciones de prueba**, no certificación de esas capacidades. Los requisitos de runtime S02–S09 deberán acreditarse por sus propias microtareas, con evidencia de código, CI y rollback; esta salida no libera un gate sensible sin verificación individual.

## 3. Casos de aceptación documental y negativos no ejecutados

| Caso | Positivo a cotejar contra los contratos | Negativo de rechazo obligatorio |
| --- | --- | --- |
| P/N01 | Cinco entradas M02, M01 y C02-04.M01.06 LISTO+A5, DAG VALIDADO/SI y CAS único | Un precedente VALIDANDO, A5 faltante o doble owner no habilita M02.06 |
| P/N02 | Documento distingue consulta empresarial de autoridad plataforma | `EmpresaId=null` o identidad B no revela registros A |
| P/N03 | JWT vigente, membresía y RBAC autorizarán una lectura futura | Cookies, headers, filtros o IDs cliente no conceden permisos |
| P/N04 | Write estricto futuro requiere transacción y confirmación de auditoría | Caída MySQL produce efecto comercial sin registro obligatorio |
| P/N05 | Correlación saneada enlaza diagnósticos, con límite ASCII 64 | CRLF, token o 65 caracteres no contaminan logs ni autentican |
| P/N06 | Export futuro minimizado con cursor tenant-bound y grant explícito | Cursor B, consulta masiva o retención no aprobada no exportan ni eliminan |
| P/N07 | Grant excepcional requiere aprobador distinto, MFA y TTL vigente | Autoaprobación, grant revocado o evento retrasado no reautoriza |
| P/N08 | Reintento de efecto crítico futuro se deduplica con clave server-side | Reutilizar solo correlationId o eventId no concede exactamente-once |
| P/N09 | Un cambio de código futuro ejecuta tests HTTP/EF/MySQL y A/B negativos en CI | Mock o plan Markdown no es PASS de integración real |
| P/N10 | Un commit documental exacto pasa Scope Lock/V5 y conserva trazabilidad | Scope Lock FAIL/queued, SHA ajeno o changelog intercalado impiden cierre |
| P/N11 | A5 documental por Task Javier distinta evalúa el diff y pruebas aplicables | Autorrevisión u otra cuenta no desbloquea sucesoras |
| P/N12 | Reversión documental es git revert y evento compensatorio append-only | Reescribir historia Git/BITACORA o inventar rollback DB está prohibido |

**Los doce pares anteriores son oráculos de diseño comprobables por inspección, FUTUROS_NO_EJECUTADOS** para HTTP, JWT real, A/B persistido, MySQL, colas, efectos críticos, retención y export. No se reportan como pruebas funcionales ejecutadas.

## 4. A0–A5, publicación, reversión y límites

**A0:** corroborar Skill/AGENTS/Scope Lock, independencia, DAG y cinco fuentes M02; revisar Markdown, SHA de archivos, secrets, encabezados/viñetas de CHANGELOG y límite sin API/SQL/worker.

**A1 documental:** correspondencia de S01–S10 y P/N01–12 con la salida de M02; validar condiciones positivas y negativas por lectura del artefacto, sin calificar escenarios futuros como tests.

**A2 seguridad documental:** JWT/RBAC/tenant A/B, BOLA/IDOR, PII, denegación, MFA y política de retención figuran como **condiciones futuras**, sin ampliar permisos ni invocar credenciales.

**A3 integración documental:** conservar alineación Domain ← Application ← Infrastructure, consumidores HTTP/EF y esquema conceptual; `dotnet`, Angular, MySQL, Docker y Playwright = **NO_APLICA al diff exclusivamente Markdown**, no PASS y nunca prueba de transacción.

**A4 publicación:** documento + sección autocontenida de CHANGELOG mediante commit fast-forward y CAS contra HEAD vivo de dev; verificar dos blobs, diff limitado, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA publicado, y readback de PLAN H/I/K, DAG, COLA y BITACORA A:Z con eventos idempotentes. Si el check sigue en progreso, dejar este ID en VALIDANDO.

**A5:** revisión independiente por una de las otras cinco Tasks Javier, distinta de DEV06, antes de cualquier gate que la exija. Tampoco un A5_PASS autoriza por sí solo QA/main/PROD o servicios productivos.

**Reversión:** `git revert <SHA>` ordinario, conservando commits ajenos y registrando la corrección institucional con nuevo EVENT_ID; nunca force-push, reset destructivo, edición de BITACORA histórica ni cambios MySQL/infraestructura de producción. **Prohibido implementar eventos en esta entrega**: no dispatcher, webhook, worker, outbox, cola, automatización adicional, API o migración.
