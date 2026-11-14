# SOLQARYN V5 FINAL — C04-03.M01.02: actores, entradas, salidas y superficies de metering

**Autor:** PRIMARY DEV 03, JAVIER_CHATGPT, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`; **repositorio:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14. **Tipo:** preparación documental, NO implementación. Documento rector [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**Contrato de M01.02:** identificar actores, entradas, salidas y superficies afectadas en la futura medición de usuarios, almacenamiento, operaciones y archivos por tenant, con límites, eventos idempotentes y conciliación. El preflight registró PLAN H651=PENDIENTE/I651 vacío, COLA G/O=SI y DAG F612=VALIDADO/G612=SI. Sus 60 dependencias de preparación C01-01..04, C02-01..04 y C03-01..04 M01.01–05 estaban LISTO. Nada de esto aprueba un worker, API, DB, precios, facturación, nueva autorización o promoción.

## Fuentes reales cotejadas y certeza

| Fuente | Blob Git | Hecho verificable | No inferir |
| --- | --- | --- | --- |
| `backend/src/Domain/Entities/SuscripcionesSaaS.cs` | `d9b5dbe19fb728293b00b748f10ac10336debd2d` | PlanLimite nullable, suscripción ligada a EmpresaId y estados Activa/Suspendida/Cancelada/Expirada | Ledger o evento de metering |
| `backend/src/Application/Services/SuscripcionesSaaSService.cs` | `bb501285f11cd4b32709a58a2c51efa19c3664ea` | Resolución tenant servidor e idempotencia de onboarding | Idempotencia de consumo o cobros |
| `backend/src/Application/DTOs/SuscripcionesSaaSApiContracts.cs` | `6045a167fb90fe3dea2fa87280d0a29cec57c6be` | DTO límites y entitlement | Nuevo DTO/API de metering |
| [M01.01](c04-03-m01-01-alcance-metering-no-regresion.md) | `9f84047a310a558f1266052eeab545fd6a44088f` | Invariantes NR01–NR14, P01–P05 y N01–N12 como criterios | Evidencia runtime o CI de medición |

Los SHA de esta tabla son blobs, no resultados de pruebas ni de bases de datos reales.

## Actores, entrada y autorización (solo modelo futuro)

| ID | Actor | Entrada / autoridad mínima |
| --- | --- | --- |
| ACT01 | Usuario de empresa A | Operación medida únicamente tras comprobar membresía y permiso backend |
| ACT02 | Usuario de empresa B | Aislamiento B y ausencia total de lectura desde A |
| ACT03 | Administrador empresarial | Lectura autorizada de métricas de su empresa, nunca global por inferencia |
| ACT04 | Administrador de plataforma | Permiso institucional explícito y propósito auditable, no bypass |
| ACT05 | Anónimo o rol insuficiente | Rechazo backend sin consumo ni fuga |
| ACT06 | Servicio Application | Emisor futuro con tenant, operación e identidad verificados |
| ACT07 | Adaptador de almacenamiento | Fuente de bytes y objetos autenticada, sin confiar en Angular |
| ACT08 | Almacén eventual de eventos | Garantía de deduplicación transaccional y durabilidad cuando aplique |
| ACT09 | Reconciliador eventual | Diferencias y correcciones compensatorias verificadas, no reescritura |
| ACT10 | Revisor A5 Javier | Task distinta de autor sobre SHA y evidencia, nunca QA automática |

No se crearon cuentas, roles, worker ni API. Los actores de adaptador, almacén y reconciliación describen trabajo FUTURO.

## Mapa de entradas, salidas y superficies (interfaces conceptuales, no endpoints existentes)

| ID | Entrada y emisor → receptor | Salida deseada | Negativo/falla a observar después |
| --- | --- | --- | --- |
| IO01 | Usuario → controlador autenticado | Tenant efectivo servidor | Header/slug forjado crea permiso |
| IO02 | Operación → servicio Application | Intención de consumo validada | Replay duplica efecto |
| IO03 | Suscripcion y PlanLimite → evaluador futuro | Tipo, unidad y regla acotados | Entitlement reemplaza RBAC |
| IO04 | Fuente operación → métrica allowlisted | Cantidad normalizada | Métrica arbitraria |
| IO05 | Almacenamiento → medidor futuro | Bytes medidos por fuente acreditada | Valor solo del browser |
| IO06 | Archivo → agregador eventual | Uso por tenant sin PII innecesaria | Conteo cruzado de archivos |
| IO07 | Usuarios → ventana de medición | Cantidad con criterio/tiempo claro | Revocados inflan activos |
| IO08 | Cabecera correlación → observabilidad | Correlación saneada | Correlación utilizada como grant |
| IO09 | EventID y clave idempotente → store futuro | Un efecto por tenant+operación | Dos efectos concurrentes |
| IO10 | Reloj servidor → periodo | Corte UTC/versionado | Timestamp cliente cambia periodo |
| IO11 | Eventos → proyección | Agregados reproducibles | Suma mezcla tenants |
| IO12 | Proyección → conciliador | Diferencia/compensación | Alteración silenciosa de historia |
| IO13 | Proyección → Angular tipado | Estados y límites legibles | UI decide RBAC/entitlement |
| IO14 | Evento → auditoría/export autorizado | Minimización y permisos | Token, PII o secreto en log |

Futuro evento mínimo conceptual: clave de métrica allowlisted, unidad, cantidad validada, tenant real, fuente, OperationId, clave idempotente y timestamp UTC confiable; prohibidos JWT, contraseñas, recovery tokens, secretos e identificadores privados innecesarios. Un PlanLimite no representa por sí mismo uso medido.

## Oráculos posteriores positivos y negativos — NO EJECUTADOS en esta entrega documental

| ID | Signo | Resultado futuro exigido |
| --- | --- | --- |
| P01 | + | Operación válida en A cuenta una vez y se atribuye solo a A |
| P02 | + | Dos retries del mismo evento no incrementan doble saldo |
| P03 | + | Bytes de fuente fiable se cuentan con unidad y periodo correctos |
| P04 | + | Consulta A muestra solo métricas A con paginación segura |
| P05 | + | Conciliación permite compensación trazable sin borrar historia |
| N01 | - | Tenant A intenta contabilizar/leer B: denegación |
| N02 | - | Correlation ID se usa como permiso: rechazo |
| N03 | - | Retry concurrente produce duplicados: fallo |
| N04 | - | Unidad o cantidad inválida se usa para facturar: rechazo |
| N05 | - | Suscripción suspendida/cancelada se trata como activa sin política: rechazo |
| N06 | - | Usuario sin permiso lee agregado/export: 403/404 |
| N07 | - | UI atribuye tamaño como verdad única: no contar ni facturar |
| N08 | - | A recibe PII o registros de B: falla tenant |
| N09 | - | Persistencia crítica falla y operación se confirma: fallo fail-closed |
| N10 | - | Reprocesamiento de archivo/periodo duplica conteo: rechazo |
| N11 | - | Registro contiene JWT, tokens, secretos o PII excesiva: fallo privacidad |
| N12 | - | M01.02 habilita API, pagos, worker, migración, QA/main/PROD: NO_GO |

Estas pruebas representan criterios documentados de fases posteriores, **no** certificaciones .NET, MySQL/EF, HTTP, Angular ni Playwright.

## A0–A5, impacto y rollback

A0: revisar contrato, fuentes/blob, tabla de actores y de interfaces, no secretos, procedencia CHANGELOG. A1: cotejo estructural de 5 positivos, 12 negativos y 14 interfaces; A2: lectura de límites tenant/JWT/RBAC/privacidad, no ejecución de seguridad runtime; A3: coherencia con M01.01 y PLAN/COLA/DAG sin modificar contratos existentes; A4: publicar únicamente 2 archivos Markdown fast-forward/CAS en dev y comprobar Scope Lock+V5 sobre SHA exacto, BITACORA 26 columnas y PLAN/COLA readback. A5 por otra Task Javier distinta de J20 y no autoriza QA/main/PROD.

Cualquier futura implementación de metering requiere su propia microtarea, pruebas reales .NET/API, EF/MySQL efímero, aislamiento A/B y regresión Angular/E2E cuando aplique. Para **este documento exclusivamente**, `dotnet/Angular/MySQL/Playwright=NO_APLICA`, nunca PASS atribuido. Rollback mediante `git revert <SHA>` normal, evento compensatorio sin borrar historial ni force-push. M01.02 no cierra M01/M02 ni activa una capacidad.
