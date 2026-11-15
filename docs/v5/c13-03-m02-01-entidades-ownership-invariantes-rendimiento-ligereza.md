# SOLQARYN V16 — C13-03.M02.01: entidades, ownership y reglas invariantes de rendimiento y ligereza

**ID:** C13-03.M02.01; PLAN_MAESTRO fila 1952; autor PRIMARY DEV 03 JAVIER (Task \`6adc2e4bd8a48191ac7d8641ed95a1a3\`). **Riesgo V15:** R0, contrato documental conceptual. **Ámbito:** \`solqaryn/Solqaryn@dev\`; \`PROJECT_SCOPE_LOCK=STRICT\`. No se implementan entidades .NET, tablas, endpoints, UI, caché, métricas instrumentadas ni cambios de entorno.

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`, PLAN_MAESTRO, COLA y DAG_TECNICO institucionales. **Resultado:** identificar conceptos, propiedad, relaciones e invariantes de carga inicial, navegación, medios, tablas, datos y móviles, sin presentar hipótesis como mediciones.

## 1. Predecesores y arquitectura existente

- Preflight individual: \`PLAN_MAESTRO!H1952=PENDIENTE/I1952\` vacío, \`COLA!G1952=SI/O1952=SI\`, \`DAG_TECNICO!F1913=VALIDADO/G1913=SI\`; la Task obtuvo posteriormente un CLAIM exclusivo por estado y readback. Prerrequisitos: \`C13-03.M01.01–06\` LISTO y A5 independiente. La A5 de M01.06 es contrato de preparación y no autoriza por sí sola aceptar calidad de implementación.
- M01.06 declara dieciséis puertas G01–G16, ocho acciones RB01–RB08, diez pares T01–T10 **futuros** y el conflicto de presupuesto **650–750 KB frente a 1–2 MiB SIN RESOLVER**. Ningún valor se elige como aprobado. LCP, INP, CLS, TTFB, resultados en móvil y métricas de accesibilidad siguen \`NO_MEDIDO\`.
- Arquitectura actual: Angular 22.2.1 standalone/Signals/Material con guards para UX, API .NET 10 como autoridad de acceso y MySQL/EF Core 10 con proveedor Oracle para datos. Ningún objetivo de performance puede saltarse permisos JWT/RBAC, revocación, aislamiento tenant A/B ni reglas transaccionales. La aplicación existente puede contener elementos homónimos: las entidades E01–E14 descritas aquí son **conceptos de evidencia**, NO entidades físicas nuevas ni nombres prescritos de DB.

## 2. Modelo conceptual E01–E14

| ID | Concepto / dueño de la decisión | Responsabilidad, frontera y evidencia futura |
| --- | --- | --- |
| E01 | PerformanceBaseline / equipo dev | SHA, versiones, build, umbrales *aprobados* o estado PENDIENTE, y fecha de comparación; no inventar baseline |
| E02 | MeasurementRun / CI autorizada | ID de run, entorno efímero, repeticiones, muestra, éxito/error y artefactos; sin métricas sintéticas presentadas como reales |
| E03 | MeasurementProfile / QA técnico futuro | Dispositivo, viewport, red, CPU, navegador, localidad, zona horaria y accesibilidad |
| E04 | RouteScenario / frontend | Ruta pública o privada, propósito, estado de sesión, cargas dependientes, prioridad y condición negativa |
| E05 | TenantVisibilityBoundary / backend | Autoridad de empresa/rol/objeto para recursos privados; el navegador no define tenant confiable |
| E06 | CriticalResourceManifest / build | Recursos above-the-fold, orden de descarga, tamaño y dimensiones reservadas de medios |
| E07 | MediaDeliveryRule / medios | Variantes responsive, formato, prioridad, fallback, caducidad y prohibición de URL privada pública |
| E08 | DataPresentationScenario / API-UI | Paginación, orden estable, filtros, campos permitidos, tamaño de lote y manejo de errores |
| E09 | CachePolicyContract / backend-seguridad | Cache pública vs privada, ETag/304, Vary, TTL y denegación de contenido sensible |
| E10 | NavigationTransition / frontend | Estado carga, error, vacío, revalidación y foco/teclado; límites de transiciones duplicadas |
| E11 | AccessibilityScenario / frontend | Resoluciones móviles, contraste, semántica, movimiento reducido y navegación sin mouse |
| E12 | MetricDefinition / equipo de calidad | Nombre, unidad, denominador, percentil, umbral aprobado, origen y variación admisible |
| E13 | EvidenceLedger / auditoría | Relación entre fuente/commit/test/log, actor autorizado, checksum y hallazgo, sin PII ni secretos |
| E14 | RollbackPlan / responsable de cambio | SHA base, impacto, secuencia reversible, evidencia antes/después y límites del revert |

**Propiedad:** E01/E02/E12 requieren propietario responsable de medición; E05/E09 son autoridad exclusiva del backend y seguridad; E04/E06/E07/E08/E10/E11 gobiernan experiencia sin conceder permisos; E13 preserva trazabilidad; E14 exige decisión explícita antes de alterar configuración real. No crear un repositorio paralelo de secretos ni duplicar pertenencia empresarial.

## 3. Relaciones propuestas REL01–REL12, sin FK ni DDL

| ID | Relación conceptual | Regla verificable en una futura implementación |
| --- | --- | --- |
| REL01 | Baseline → uno o varios runs | Cada comparación cita exactamente la versión que evaluó |
| REL02 | Run → un perfil explícito | Nunca mezclar móvil lento y escritorio rápido en una única cifra |
| REL03 | Run → rutas evaluadas | La cobertura incluye públicas, privadas y errores, según necesidad |
| REL04 | Ruta privada → frontera tenant | La autoridad se comprueba en servidor por objeto, rol y revocación |
| REL05 | Ruta → manifiesto crítico | Preload/lazy loading se justifican por ubicación real en viewport |
| REL06 | Manifiesto → reglas de medios | Imagen sin dimensiones reserva espacio o el test registra riesgo CLS |
| REL07 | Ruta → escenarios de datos | Tabla no elimina filas ni importes para aparentar velocidad |
| REL08 | Escenario de datos → política caché | Datos privados y decisiones de autorización no ingresan en caché pública |
| REL09 | Transición de navegación → escenario accesible | Un estado de error mantiene foco y recuperación por teclado |
| REL10 | Definición de métrica → run | No reportar percentil sin muestra, entorno, unidades y umbral |
| REL11 | Evidencia → artefacto/commit | Cada PASS exige fuente verificable y SHA/blob; ausencia implica NO_MEDIDO |
| REL12 | Rollback → baseline y evidencia | Reversión preserva trabajo concurrente, aislamiento y límites de permisos |

## 4. Invariantes de aceptación INV01–INV18

| ID | Invariante | Condición que debe fallar cerrado |
| --- | --- | --- |
| INV01 | Toda métrica se atribuye a SHA, build, perfil y run reales | Falta de fuente o denominador → NO_MEDIDO |
| INV02 | El presupuesto de bundle requiere decisión de gobierno documentada | Conflicto 650–750 KB/1–2 MiB → sin PASS presupuestario |
| INV03 | LCP/INP/CLS se miden y comparan sin inventar percentiles | Ausencia de browser/medición → NO_MEDIDO |
| INV04 | Rendimiento no expone identidad, sesión, costos o PII en telemetría | Logs sensibles → rechazo y redacción |
| INV05 | Contenido privado no se cachea públicamente por URL, ETag o CDN | Tenant A/B cruzado → P0 y NO_GO |
| INV06 | Guard Angular no sustituye autorización del backend | JWT inválido, revocado o rol insuficiente → 401/403 seguro |
| INV07 | Medios críticos conservan tamaño reservado y prioridad justificada | CLS o lazy above-the-fold no justificado → falla futura |
| INV08 | Las rutas públicas no incorporan datos tenant ni estado autenticado | HTML/edge cache con datos privados → fallo |
| INV09 | Paginación/filtros conservan integridad del total y permisos por fila | Pérdida o mezcla A/B de filas → fallo |
| INV10 | Errores y retries son explícitos, acotados e idempotentes donde aplica | Doble solicitud causa escritura duplicada → fallo |
| INV11 | Un cambio de tenant invalida vista/datos/caché del tenant anterior | Datos residuales B↔A → fallo |
| INV12 | Formularios y navegación preservan foco, etiqueta y teclado | Ruta inaccesible o foco perdido → fallo |
| INV13 | Movimiento reducido no bloquea la operación | Dependencia forzada de animación → fallo |
| INV14 | Dispositivos y redes representan casos débiles realistas | Solo desktop rápido no acredita móvil |
| INV15 | Optimización no modifica exactitud de stock, dinero o auditoría | Cambio de cifra/transacción por UI/cache → fallo |
| INV16 | Evidencia es causal y un check requerido no se reemplaza por SKIPPED | CANCELLED/FAILED/QUEUED/IN_PROGRESS → sin PASS |
| INV17 | Rollback conserva cambios ajenos mediante fast-forward/revert normal | Force-push o pérdida de artefactos → prohibido |
| INV18 | A5 necesita revisor Javier de otra Task y SHA/artefacto auténticos | Autoevaluación → sin A5_PASS |

## 5. Diez pares de pruebas futuras T01–T10 (0 ejecutados)

| ID | Afirmación positiva FUTURA | Negativo / frontera FUTURA |
| --- | --- | --- |
| T01 | Build del SHA respeta presupuesto *aprobado* | Falta de aprobación o exceso produce NO_PASS |
| T02 | LCP/INP/CLS medidos con 3 perfiles definidos | Resultado sin muestra/entorno/denominador se rechaza |
| T03 | Ruta pública carga recurso crítico con dimensiones | Hero tardío genera salto y falla |
| T04 | Cambio A→B no retiene contenido privado | Cache/sesión anterior muestra A y falla |
| T05 | API limita DTO/campos por rol y tenant | 403/revocación no expone respuesta privada |
| T06 | Tabla paginada conserva totales/costos permitidos | Filtro pierde líneas o redondea importes incorrectamente |
| T07 | Móvil 320px y red limitada conserva navegación | Estado de error inaccesible impide reintentar |
| T08 | Teclado y movimiento reducido permiten completar flujo | Foco inaccesible o animación obligatoria falla |
| T09 | ETag/304 público respeta datos no sensibles | 304 privada comparte representación y falla |
| T10 | Revert normal mantiene seguridad y mediciones comparables | SHA cambió o CI obligatoria cancelada y se intenta aprobar |

Estos son oráculos y casos por implementar, **0/10 ejecutados** en esta microtarea. No se ha iniciado Chromium, Playwright, Lighthouse, MySQL, API HTTP ni benchmark. El equipo que implemente código R1/R2 debe aportar pruebas genuinas y registros por SHA; esta salida R0 no acredita calidad runtime.

## 6. Auditorías A0–A5 y rollback

- **A0:** alcance M02.01, fuente V5, precedentes M01, E01–E14, REL01–REL12 e INV01–INV18, identidad de archivo y diff de Markdown/CHANGELOG; resultado verificable por inspección.
- **A1:** revisar cobertura de T01–T10 positivos y negativos como **contratos futuros**, nunca PASS de pruebas ejecutadas.
- **A2:** invariantes de JWT, RBAC, revocación, tenant A/B, PII, caché, finanzas e inventario documentadas; ninguna política de seguridad real se modifica.
- **A3:** integración a API/DB/browser \`NO_APLICA\` por diff exclusivamente Markdown; las futuras medidas y la integración MySQL 8.4, cuando corresponda, requieren CI real.
- **A4:** commit individual fast-forward/CAS en dev, Scope Lock y procedencia CHANGELOG V5 completed/success del SHA publicado, estados PLAN/COLA/DAG y BITACORA con readback; no contar checks in_progress/cancelled/skipped como PASS.
- **A5:** pendiente de Task Javier distinta de DEV03 con revisión independiente de requisitos, artefactos y pruebas causales; ningún pase a QA/main/PROD.

**Rollback documental:** \`git revert\` normal del commit exacto después de comparar HEAD, preservando BITACORA histórica; no requiere rollback de datos porque no se alteraron. Los 16 bloqueos y la elección pendiente de presupuesto M01.06 siguen vigentes; este documento no implementa optimización ni autoriza aprobar métricas no medidas.
