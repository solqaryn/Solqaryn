# SOLQARYN V5 — C08-02.M01.06 — Bloqueos, reversión y condición de liberación de mesas y QR

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER (TASK_ID `6adc2e55f47c81919f6374e561687076`). **Fecha:** 2026-11-15. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-02.M01.06. **Riesgo:** R0, contrato documental sin efecto en runtime. **Scope:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`.

## 1. Objetivo y alcance real

Cerrar únicamente el **contrato documental terminal M01** de mesas, QR, menú, pedidos internos y autoservicio: condiciones de bloqueo, salida, dependencias verificables y reversión antes de cualquier diseño M02. El resultado exigido por el plan incluye evidencia futura de **QR falso, mesa ajena y cierre de pedido**. Las condiciones de seguridad expuestas aquí son obligaciones posteriores, **no** funcionalidades implementadas ni pruebas ejecutadas. M01.06 es una puerta de planificación: no otorga permisos de código, migración, cobro, despliegue, nuevos roles, servicios, scheduler, workers, webhook o cuentas.

Los cinco predecesores directos `C08-02.M01.01–05` están institucionalmente `LISTO` y sus **A5 independientes** se verificaron por Tasks Javier distintas de sus autores; para M01.02–05 se registraron A5_PASS independientes por DEV05 sobre los SHA y blobs originales. Las dependencias adicionales de programa/fase/bloque (`C07-02.M01.06+A5`, `C08-01.M01.06+A5` y las M01 de C01/C02/C06) se comprueban en `DAG_TECNICO` calculado, **no** por intuición ni por un conjunto estático. En el claim, `PLAN_MAESTRO!H1195=PENDIENTE`, `I1195` vacío, `COLA!G1195/O1195=SI/SI`, `DAG_TECNICO!F1156/G1156=VALIDADO/SI`, verificados bajo candado exclusivo V17. Estas lecturas son una foto causal de entrada, nunca una licencia permanente para los sucesores.

## 2. Matriz de bloqueo fail-closed — B01–B16

| Gate | Condición indispensable para una siguiente etapa | Bloquear si se presenta |
| --- | --- | --- |
| B01 — Gobierno | Repositorio y rama originales, Skill canónica y Scope Lock STRICT | Repo/branch distinto o conflicto de Skill/Scope Lock |
| B02 — Trazabilidad | PLAN, COLA, DAG y BITACORA coherentes con ID y propietario único | Estado/owner divergen o candado V17 no acreditado |
| B03 — A5 M01 | Predecesores `M01.01–05` LISTO con A5 por Task diferente | A5 faltante, FAIL histórico o aprobación por autora |
| B04 — Fase/bloque | Predecesores de C01/C02/C06/C07/C08 satisfechos según DAG vivo | Fórmula DAG F distinta de VALIDADO o G distinta de SI |
| B05 — Identidad de mesa | Mesa→sucursal→empresa inequívocos, sin confiar en IDs del cliente | MesaId/SucursalId/EmpresaId manipulados o cruce A/B |
| B06 — QR no autoritativo | Referencia opaca, autenticidad, expiración y revocación verificables en backend | QR falso, copiado, vencido, revocado o de otra sede |
| B07 — Menú público | Proyección publicada mínima con precio/visibilidad canónicos | Producto oculto, PII, stock o precio del cliente como autoridad |
| B08 — Roles | JWT/membresía/rol/sede comprobados en servidor para operaciones protegidas | Mesero/cocina/caja revocados, sucursal ajena o guard UI como autoridad |
| B09 — Pedido y estados | Máquina de estados versionada, transiciones válidas y denegación de terminales | Pedido cerrado/anulado reabierto o modificación obsoleta aceptada |
| B10 — Idempotencia | Clave+fingerprint+reintentos seguros con unicidad transaccional | Replay, doble submit o mismo key/payload distinto duplican efectos |
| B11 — Concurrencia | Control de versión y transacción/rollback de pedido, stock y cierre | Dos operadores pierden actualización o duplican factura/pago |
| B12 — ERP | Ventas, facturas, impuestos, stock y caja siguen autoridad existente | Cambio de saldo, impuesto o reserva sin integración probada |
| B13 — Auditoría | Actor/tenant/sede/correlación y resultado saneados; retención aprobada | JWT, PII, QR secreto, PAN o datos de empresa B en logs |
| B14 — UX | Accesibilidad, teclado/foco, móvil y errores con estados del servidor | Offline/401/403/409/timeout muestran pago o cierre ficticio |
| B15 — Evidencia técnica | Checks requeridos y suites reales por tecnología y SHA exacto/equivalente probado | `skipped`, `cancelled`, `failed` o `in_progress` usados como PASS |
| B16 — Entorno | Trabajo solo dev; A0–A4 propios, A5 independiente, autorización separada para QA/PROD | Promoción, secretos, PSP, MySQL productivo o migración irreversible |

**Precondición universal:** cualquier gate no acreditado debe permanecer bloqueado para la capacidad ejecutable correspondiente. Registrar motivo y próxima prueba; no editar `COLA!G/O` ni `DAG!F/G` a mano, ni convertir una revisión de documento en test JWT, HTTP, MySQL o navegador.

## 3. Matriz de evidencia para liberaciones futuras — E01–E12 (NO_EJECUTADAS)

| Evidencia | Éxito positivo futuro y rechazo adversarial obligatorio | Capa / prueba real requerida |
| --- | --- | --- |
| E01 | QR válido A resuelve mesa/menú publicado; QR falso, B o revocado no revela datos | API, firma/TTL/revocación, HTTP A/B |
| E02 | Mesero A abre pedido mesa A; MesaId/SucursalId/EmpresaId B o rol revocado no escribe | JWT/RBAC, HTTP + MySQL efímero |
| E03 | Precio/IVA/stock vienen del servidor; monto o variante manipulados se rechazan | Application/Domain + integración ERP |
| E04 | Retry idéntico devuelve único pedido; replay o payload distinto no duplica | Idempotencia/índice y concurrencia MySQL 8.4 efímero |
| E05 | Cocina A avanza solo pedido propio; cocina B o caja sin permiso no ve ni muta | Domain/Application + roles A/B |
| E06 | Estado válido avanza con versión; stale o terminal no altera registro | xUnit y conflicto de versión en DB si persiste |
| E07 | Cierre autorizado se realiza una vez; replay no duplica factura/stock/cobro | MySQL transaccional y pruebas negativas de cierre |
| E08 | Caída intermedia revierte/compensa; no deja factura/pago parcial | Rollback/compensación en MySQL efímero; sin PSP real |
| E09 | Pedido cancelado queda terminal; segunda cancelación no genera efectos | xUnit y consulta before/after a datos efímeros |
| E10 | Menú público solo incluye catálogo habilitado; SKU oculto/tenant B no filtra | HTTP+persistencia, caché y PII |
| E11 | UI 320px/teclado/lector muestra estado auténtico; offline/401/409 no finge éxito | Angular/Playwright Chromium real |
| E12 | Log autorizado da correlación saneada; token, QR secreto y PII quedan ausentes | Auditoría y comprobación negativa de registros |

E01–E12 describen **criterios de futuro** derivados de NR01–NR14 (M01.01), actores/interfaces/fronteras (M01.02), siete fuentes ERP auditadas y brechas G01–G10 (M01.03), riesgos R01–R14 y decisiones abiertas D01–D08 (M01.04), y P01–P14/N01–N14/NFR01–08 (M01.05). **Cero** escenarios de runtime E01–E12 se ejecutan en esta M01.06 documental. En una implementación R2 futura, el build aislado no sustituirá pruebas de MySQL 8.4, JWT/RBAC/tenant ni Playwright cuando correspondan.

## 4. Reversas y recuperación — RB01–RB08

| Ruta | Disparador y acción futura verificable | Condición para reabrir |
| --- | --- | --- |
| RB01 — Documento | Divergencia entre documento, Scope Lock y plan | Corrección aditiva o `git revert` normal del commit, evidencia en BITACORA |
| RB02 — QR | QR comprometido, falso, vencido o vinculado a otra sede | Revocar/rotar token en servicio futuro, demostrar rechazo previo y posterior |
| RB03 — Mesa | Propiedad tenant/sucursal inconsistente | Denegar operación, auditar y corregir referencia transaccionalmente |
| RB04 — Pedido | Versión vieja o carrera entre operadores | 409/conflicto seguro y refetch; sin duplicar pedido |
| RB05 — Inventario | Reserva/consumo parcial antes de cierre | Rollback o compensación transaccional con idempotencia y evidencia MySQL |
| RB06 — Facturación | Cierre parcial o respuesta tardía de proveedor | Conciliar estado definitivo servidor antes de factura/pago; sin PSP no autorizado |
| RB07 — UI | Offline, revocación o 401/403/409 tras acción | Invalidar estado optimista, anunciar error accesible, no mostrar éxito falso |
| RB08 — Gobernanza | CI fallida, A5_FAIL, owner/lease ambiguo o SHA cambiante | Mantener VALIDANDO/BLOQUEADO; reparación causal, A0–A5 y readback, sin forzar gates |

Las reversas RB02–RB07 son **contratos de seguridad futuros**, no operaciones realizadas. En la publicación documental actual solo aplica RB01: revertir con commit Git **no destructivo**, conservar historia y añadir evento compensatorio, previa exclusividad y lectura del HEAD.

## 5. Condición de salida de M01.06 y apertura segura de M02

1. **A0 de M01.06:** documento y CHANGELOG con sección autocontenida; plan, cinco M01 LISTO+A5, DAG/cola y impacto delimitado a `docs/v5` y CHANGELOG; sin secrets ni archivo de código.
2. **A1 documental:** verificables B01–B16, E01–E12 y RB01–RB08, oráculos positivos/negativos *pendientes de implementación*; no declarar tests de runtime.
3. **A2 de diseño:** requisitos anti-IDOR, QR falso/revocado, roles revocados, PII, tenant A/B, idempotencia y rollback; no afirmar seguridad viva.
4. **A3 documental:** contraste con M01.01–05, arquitectura y contrato comercial; xUnit, MySQL, HTTP y Playwright = **NO_APLICA por alcance R0**, no PASS de suites no ejecutadas.
5. **A4 publicada:** solo dev; commit fast-forward/CAS, GitHub Scope Lock y procedencia V5 `completed/success` sobre SHA causal; PLAN/BITACORA/COLA readback; ningún check obligatorio incompleto se toma por PASS.
6. **A5 independiente:** otra Task Javier distinta de DEV05 revisa SHA, cambios, criterios, fuentes y posibles defectos; `LISTO_DEV` de M01.06 **no** implica A5_PASS.

**M02 no se desbloquea por esta redacción ni por VALIDANDO.** Para cualquier unidad M02, releer `PLAN_MAESTRO` (H=PENDIENTE, I vacío), `COLA` (G/O=SI) y `DAG_TECNICO` (F=VALIDADO/G=SI); cuando el DAG exija A5 de M01.06, exigir A5_PASS independiente verdadero. No convertir decisiones D01–D08 abiertas en autorizaciones. Prohibido instalar QR, endpoint, migración, pago, cola, webhook, nuevas Tasks, secrets, alterar producción/QA/main o datos reales por este contrato.

## 6. Impacto, observabilidad y trazabilidad

**Impacto actual:** solo documento y CHANGELOG en `dev`. Domain, Application, Infrastructure, API, Angular, MySQL, JWT, RBAC, tenant, usuarios, integraciones, impuestos, inventarios y proveedores **sin modificación**. No hay nueva superficie pública ni permisos. La instrumentación futura debe correlacionar actor, empresa, sede, mesa, transacción, decisión y resultado, filtrando PII, JWT y tokens QR.

**Trazabilidad de diseño:** `C08-02.M01.01` alcance y NR01–14; `M01.02` A01–14/I01–14/F01–09; `M01.03` siete blobs ERP/G01–10; `M01.04` R01–14/S01–08/D01–08 pendientes; `M01.05` P01–14/N01–14/NFR01–08 pendientes. El propietario de esta M01.06 es DEV05; no se acredita A5 propio. Las pruebas futuras carecen de ejecución y SHA de runtime por diseño. Mantener A5 independiente, exact-SHA y BITACORA históricos.
