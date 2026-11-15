# SOLQARYN V16 — C13-03.M02.04: configuración tipada y flags seguros de rendimiento y ligereza

**Microtarea:** C13-03.M02.04, PLAN_MAESTRO fila 1955. **Autor:** PRIMARY DEV 03 JAVIER (\`6adc2e4bd8a48191ac7d8641ed95a1a3\`). **Clasificación:** R0 documental; \`solqaryn/Solqaryn@dev\`, \`PROJECT_SCOPE_LOCK=STRICT\`. Los nombres propuestos NO son opciones \`IOptions\` registradas, claves de \`appsettings\` operativas, flags habilitados, SQL, secretos o endpoints.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\` y PLAN/COLA/DAG institucionales. Alcance: definir configuración tipada, ámbitos y flags seguros para carga, navegación, imágenes, tablas, datos y móvil sin afectar funcionamiento presente.

## 1. Precedentes, límites y decisiones abiertas

- Elegibilidad revalidable por ID: PLAN H1955=PENDIENTE e I1955 vacía antes del claim, COLA G/O=SI y DAG F=VALIDADO/G=SI. \`C13-03.M01.01–06+A5\` son prerequisitos. Las microtareas hermanas M02.01–03 son contratos independientes y no conceden permisos de runtime.
- \`C13-03.M01.06\` fija G01–G16, RB01–RB08 y diez pruebas **FUTUROS_NO_EJECUTADOS**; presupuesto de bundle **650–750 KB frente a 1–2 MiB SIN_RESOLVER**. Ninguna cifra es umbral aprobado; no se afirma PASS de performance.
- M02.01 propone E01–E14, REL01–REL12, INV01–INV18. M02.02 OP01–OP08 y DTO01–DTO08, errores y validaciones; M02.03 familias de persistencia e índices **hipotéticos**. Este documento no registra esas clases, tablas, rutas ni contratos en runtime.
- Frontera: Angular 22.2.1 sólo refleja UX; ASP.NET Core 10 verifica JWT/RBAC, tenant A/B, revocación y objeto. Configuración de caché, seguridad, imágenes o autorizaciones nunca se decide por un flag enviado por el navegador. EF/MySQL 8.4 y proveedores no son modificados.

## 2. Catálogo conceptual de opciones tipadas OPT01–OPT14

| ID | Campo/configuración candidata (no implementada) | Ámbito y valor seguro ante ausencia |
| --- | --- | --- |
| OPT01 | \`PerformanceContractVersion\` | Global/versionado; ausente = NO_EVALUADO, nunca decisión implícita |
| OPT02 | \`ApprovedBundleThreshold\` | Global con aprobador y SHA; **PENDIENTE** por conflicto presupuestario |
| OPT03 | \`MetricDefinitions\` | Global con unidades, percentiles y muestras mínimas; falta = NO_MEDIDO |
| OPT04 | \`DeviceProfiles\` | Sólo perfiles sintéticos allowlist, no identificadores de usuarios |
| OPT05 | \`RouteMeasurementAllowlist\` | Rutas públicas/privadas etiquetadas; privadas requieren verificación servidor |
| OPT06 | \`CriticalResourceHints\` | Clases de recursos y dimensiones, nunca URL privada o token |
| OPT07 | \`ImageFallbackPolicy\` | Fallback público seguro sin alterar contenidos empresariales |
| OPT08 | \`PublicCachePolicy\` | Public-only, TTL acotado, \`Vary\` correcto; privado no-cache por defecto |
| OPT09 | \`MeasurementRateLimit\` | Disabled/0 si no se aprobó instrumento y cuota |
| OPT10 | \`MeasurementRetention\` | Captura deshabilitada sin clasificación de privacidad y autorización |
| OPT11 | \`TablePageLimit\` | Tamaño acotado sujeto a política backend; no omitir filas silenciosamente |
| OPT12 | \`ReducedMotionBehaviour\` | Accesibilidad obligatoria, no opcional para completar operaciones |
| OPT13 | \`RollbackEvidenceContract\` | Exigir SHA, diff, CI y plan de retorno; no automatizar reversiones |
| OPT14 | \`TenantSafeDiagnostics\` | Sólo agregados sin PII/secreto, con supresión A/B y permisos servidor |

**No equivalencia:** OPT01–OPT14 son conceptos para futura implementación; no hay que crear catorce flags individuales. Primero comparar opciones ya existentes, interfaces, seguridad y volumen. Una configuración ausente o incompatible **no** autoriza habilitar captura de métricas, caché privada, cargas privilegiadas ni exposición de tenant.

## 3. Precedencia y separación de autoridad

| Origen conceptual | Potestad permitida en futuro | No permite |
| --- | --- | --- |
| Contrato de producto aprobado | Definir presupuesto y perfil de medición sin contenido sensible | Inventar thresholds o declarar LCP/INP/CLS medidos |
| Backend autorizado | Validar contexto JWT, empresa, rol y objeto al servir datos privados | Confiar en un \`EmpresaId\` del navegador |
| Frontend Angular | Aplicar UX, placeholders, imágenes y movimiento reducido | Otorgar RBAC, evitar validación servidor o variar caché privada |
| Instrumentación dev/CI | Emitir métricas sintéticas de entorno permitido | Enviar JWT, cookies, PII o costos a tercero |
| Config por tenant | Selección autorizada por backend, versionada y auditable | Propagar flags, caché o datos entre empresas A y B |
| Fallback/rollback | Mantener funcionalidad y trazabilidad con defaults seguros | Hacer force-push, borrar auditoría o desactivar checks |

Precedencia propuesta para eventual implementación: seguridad fail-closed de backend **siempre** prima; después contrato aprobado/versionado; después opciones específicas de ámbito validadas; finalmente fallback seguro. Sin aprobación, la captura/telemetría se mantiene deshabilitada; UX existente conserva su comportamiento, sin sacrificar protección.

## 4. Dieciséis validaciones VAL01–VAL16 y ocho fallos FAL01–FAL08

| ID | Invariante de validación propuesto |
| --- | --- |
| VAL01 | Presupuesto sólo válido con unidad, revisión, autor y estado aprobado; conflicto sigue PENDIENTE |
| VAL02 | Percentiles requieren muestra, entorno, dispositivo, red y origen real |
| VAL03 | Flag privado se decide por backend con empresa y rol autorizados |
| VAL04 | Privacidad no se reduce por optimización: JWT, cookie y PII fuera de métricas |
| VAL05 | Config de tenant A no afecta B, aunque compartan host o navegador |
| VAL06 | Cambiar sesión/tenant invalida caché privada y vistas anteriores |
| VAL07 | Cache-Control público nunca cubre respuestas con autorización o datos sensibles |
| VAL08 | Reglas de recursos críticos no generan CLS o bloqueos de teclado |
| VAL09 | Flags de medios sólo permiten rutas/formatos públicos comprobables |
| VAL10 | Retry/backoff acotado evita loops y solicitudes duplicadas |
| VAL11 | Paginación conserva orden, permisos, importes y cardinalidad |
| VAL12 | El móvil 320px, bajo ancho de banda, conserva navegación accesible |
| VAL13 | Estado deshabilitado no borra auditoría ni reescribe resultados históricos |
| VAL14 | Errores de proveedor no sustituyen una métrica nueva por dato viejo como PASS |
| VAL15 | Config y tests requieren SHA exacto, blob y CI causal; SKIPPED no es PASS |
| VAL16 | Cambios futuros de CI, caché, JWT, tenant o MySQL escalan a riesgo R2 |

| Fallo | Condición | Fallback permitido |
| --- | --- | --- |
| FAL01 | Config faltante | Deshabilitar medición, marcar NO_MEDIDO |
| FAL02 | Presupuestos incompatibles | No elegir valor automáticamente; bloquear certificación |
| FAL03 | Tenant inválido o rol revocado | Negar acceso/telemetría privada |
| FAL04 | Muestra o percentil ausente | Resultado NO_MEDIDO, sin cifra ficticia |
| FAL05 | Caché privada expuesta | Rechazar uso, preservar evidencia segura |
| FAL06 | Proveedor de métricas falla | No habilitar servicio alternativo pagado por defecto |
| FAL07 | Config tipada inválida | Fail closed, log censurado y sin secretos |
| FAL08 | CI falla, cancela u omite check obligatorio | Mantener VALIDANDO, reparar; no emitir PASS |

## 5. Diez pares de pruebas T01–T10 (FUTUROS_NO_EJECUTADOS)

| ID | Positivo futuro | Negativo/frontera futuro |
| --- | --- | --- |
| T01 | Threshold aprobado por responsable se acepta con unidad | Conflicto 650–750 KB/1–2 MiB no se resuelve por default |
| T02 | Perfil y SHA generan evidencia correlacionable | Perfil sin N/entorno deja NO_MEDIDO |
| T03 | Contenido público puede usar ETag sin PII | Página privada cacheada públicamente se rechaza |
| T04 | Empresa A obtiene su propia configuración autorizada | Tenant B/cookie previa no hereda A |
| T05 | Cambio de rol revocado invalida permiso y caché | Frontend flag no consigue saltar 403 |
| T06 | Lazy loading de medio no crítico preserva estabilidad | Imagen hero sin dimensiones causa fallo de CLS |
| T07 | Teclado, foco y movimiento reducido mantienen flujo | Animación obligatoria rompe navegación y falla |
| T08 | Paginación reduce carga y conserva filas y totales | Datos truncados/costos inconsistentes no se aprueban |
| T09 | Fallo de proveedor devuelve estado seguro | Retorno a valor viejo o secreto en log se rechaza |
| T10 | Revert normal conserva SHA de comparación y roles | Force-push/CI cancelada presentada como éxito falla |

**Ejecución real actual: 0/10**. Son contratos de prueba futura, no evidencias de rendimiento, JWT/HTTP, MySQL, Angular o Playwright. LCP, INP, CLS continúan \`NO_MEDIDO\`. Cualquier prueba runtime deberá registrar proveedor, job, artefacto, sha, logs, positivos/negativos y denominador.

## 6. Control A0–A5 y reversión

- **A0:** fuente V5, M01.06, M02.01–03, opciones OPT01–OPT14, VAL01–VAL16 y FAL01–FAL08; diff de documento/CHANGELOG únicamente.
- **A1:** revisión de oráculos T01–T10 positivos, negativos, frontera/error **futuros**. No adjudicar PASS de ejecución.
- **A2:** documento exige JWT/RBAC/revocación, tenant A/B, confidencialidad y fail closed sin modificar backend.
- **A3:** .NET, MySQL, HTTP, Playwright, Angular y proveedores \`NO_APLICA\` para el diff R0 Markdown; serán requeridos si se implementa config/seguridad en código.
- **A4:** commit individual fast-forward/CAS en dev, Scope Lock y procedencia CHANGELOG V5 \`completed/success\` exact-SHA, PLAN/COLA/DAG/BITACORA readback; CI en progreso no cuenta como PASS.
- **A5:** revisión independiente de otra Task Javier, puede detectar y rechazar defectos; LISTO de documento no promociona QA/main/PROD.

**Rollback documental:** \`git revert\` normal con HEAD revalidado, sin editar BITACORA histórica. No se ha activado captura de métricas, caché, flags, rutas, SQL, proveedor, secret o servicio real. Presupuesto y métricas continúan pendientes.
