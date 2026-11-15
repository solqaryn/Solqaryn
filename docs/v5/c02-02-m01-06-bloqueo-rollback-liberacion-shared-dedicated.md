# SOLQARYN V5 — C02-02.M01.06: bloqueo, rollback y liberación documental shared/dedicated

**Alcance:** cierre documental de M01 de C02-02 (almacenamiento compartido/dedicado y migración futura). **Autor:** PRIMARY DEV 03 / JAVIER_CHATGPT / TASK_ID \`6adc2e4bd8a48191ac7d8641ed95a1a3\`. **Repositorio y rama:** \`solqaryn/Solqaryn@dev\`. **PROJECT_SCOPE_LOCK=STRICT.** **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive ID \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`, PLAN_MAESTRO fila 331. **Fecha:** 2026-11-15.

## Resultado real y no-alcance

El resultado de M01.06 es una **matriz de gates, restauración y condiciones de relevo a contratos M02**. No configura almacenamiento dedicado, DSN, credenciales, mapping tenant→base, router runtime, dual-write, replicación, colas, migraciones EF, backups reales, tablas, servicios HTTP ni despliegues. No cambia datos Aiven, Render, Vercel, Hostinger o Cloudflare. No prueba latencia, integridad de copias ni aislamiento de dos MySQL en vivo. Un DAG con G=SI permite **preparar** una microtarea autorizada, no activar conectividad nueva.

**Línea base verificable:** la preparación M01.01 documenta persistencia shared actual bajo \`AppDbContext\`, \`StorageTenantContext\`, membresía \`UsuarioEmpresa\`, guards de autorización y una conexión configurada en servidor. No acredita selección dedicada por empresa. M01.05 contiene AC01–AC20: seis escenarios positivos y catorce negativos para implementación **FUTURA NO_EJECUTADA**. No confundir criterio escrito con suite xUnit, HTTP, Docker/MySQL o restore realmente aprobado.

## Predecesores y trazabilidad antes de cualquier transición

| Microtarea | Evidencia de preparación | Límite que permanece |
| --- | --- | --- |
| \`C02-02.M01.01\` | Alcance shared/dedicated, NR01–NR13 y fuente Git; LISTO y A5 histórico independiente del DEV02, evento BITACORA fila 2611 sobre SHA \`2047ca1c7ec8bb312827865aa0528f4b76202dab\` | Es definición, no router |
| \`C02-02.M01.02\` | Actores, contexto servidor, entradas/salidas y fronteras, LISTO+A5 en PLAN | No otorga derechos de almacenamiento |
| \`C02-02.M01.03\` | Inventario de código actual y brechas, LISTO+A5 en PLAN | No acredita MySQL dedicado |
| \`C02-02.M01.04\` | Riesgos, supuestos, decisiones pendientes, LISTO+A5 en PLAN | No aprueba secretos ni despliegues |
| \`C02-02.M01.05\` | AC01–AC20 futuros (6 P / 14 N) y condiciones de prueba, LISTO+A5 en PLAN | No ejecuta restore, migración ni E2E |
| \`C01-01.M01.06\` | Precedente documental de plataforma con LISTO y A5 según DAG vigente | No equivale a permitir QA/PROD |
| Gobierno y fuente V5 | AGENTS, Skill canónica, Scope Lock, PLAN H/I/K, COLA G/O, DAG F/G y BITACORA A:Z | Ningún desbloqueo manual de fórmulas |

La revisión A5 de M01.01 ya existía: DEV02 era revisor distinto del DEV04 autor. Su columna K contenía un resumen no canónico; se normalizó **la representación del A5 histórico**, con readback y evento de rectificación, sin realizar ni contar una segunda A5. No se alteró BITACORA histórica. Este hecho no habilita otros requisitos no satisfechos.

## Bloqueos G01–G16 (aplicar fail-closed)

| Gate | Condición de NO_GO | Evidencia mínima de salida, solo para ese gate |
| --- | --- | --- |
| G01 | Algún antecedente exacto no está LISTO+A5 independiente | PLAN H/K, BITACORA A:Z con autor/revisor diferentes, lectura DAG recalculada |
| G02 | Scope en uso o owner de otra Task vigente | Exclusión demostrable, CAS condicional y lectura posterior; nunca takeover por cronómetro |
| G03 | COLA G/O no son SI o DAG F/G no son VALIDADO/SI | Diagnosticar fórmula/registro causal, no forzar puertas |
| G04 | Scope Lock o procedencia CHANGELOG falla/está pendiente en SHA requerido | Check runs \`completed/success\` por \`head_sha\` exacto |
| G05 | Una suite runtime pertinente queda SKIPPED/CANCELLED/FAIL/IN_PROGRESS | Prueba real de SHA aplicable; no usar otras suites como PASS |
| G06 | Un cliente aporta DSN, cadena de conexión, host, ruta a MySQL o \`placement\` arbitrario | Mapping de recursos y secretos exclusivamente confiable en servidor |
| G07 | Usuario o empresa carece de membresía verificada o rol autorizado | JWT/RBAC, propiedad, permisos y prueba adversa tenant A/B |
| G08 | Falta plan de datos históricos nulos, ownership o FKs validadas | Inventario y migración efímera con tratamiento explícito de huérfanos |
| G09 | Backups no verificados mediante restauración | Snapshot aislado, checksums y comparación por empresa A/B en sistema descartable |
| G10 | Switch de ubicación sin versión/fencing o con transacciones activas | CAS de versión de placement, ensayo de carrera y transacción consistente |
| G11 | Migración reintentable duplica filas, jobs o eventos | Token/checkpoint idempotente; pruebas negativas de reintento |
| G12 | Persistencia o respuesta externa incluye secretos/PII/DSN | Redacción, control de acceso, auditoría sin metadatos privados |
| G13 | Destino no disponible induce fallback hacia base ajena | Error fail-closed, sin cruce A/B, prueba de interrupción |
| G14 | Copia difiere en conteos, constraints o hash entre origen/destino | Reconciliación completa y abortar cambio de routing ante divergencia |
| G15 | No existe rollback probado, ventana de reversión o compensación segura | Plan de vuelta, restauración ensayada, objetivos de pérdida/recuperación documentados |
| G16 | Se solicita main/QA/PROD, proveedor/secretos/cobros sin autorización diferenciada | Orden explícita, gates técnicos propios, pruebas de seguridad y rollback; no basta LISTO_DEV |

Todos los gates técnicos son **condiciones para fases futuras**, no declaraciones de que se ejecutaron aquí. \`C02-02.M01.06\` solo acredita que se redactó y revisó este contrato.

## Rollback y recuperación R01–R08

| ID | Incidente futuro o documental | Respuesta verificable |
| --- | --- | --- |
| R01 | Error en el documento/CHANGELOG de esta microtarea | \`git revert\` normal en dev, conservar historial, evento y readback; nunca force-push |
| R02 | Mapping ausente, corrupto o tenant revocado | Denegar conexión, no seleccionar default ajeno, auditar error sin secretos |
| R03 | Inicio de migración con snapshot no restaurable | Cancelar antes de copia, generar snapshot válido, probar restore descartable |
| R04 | Copia interrumpida, checkpoint parcial | Mantener origen autoritativo, reintentar idempotente solo después de conciliar |
| R05 | Divergencia de filas/FKs/checksums A/B | Bloquear cutover y reconciliar en aislamiento, no publicar destino |
| R06 | Cutover con requests activos o edición simultánea | Fencing/versión y pausa controlada, reversión según ventana validada |
| R07 | Fallo del destino después de cutover | Ruta de retorno **aprobada y probada**, no un fallback a DB de otro tenant |
| R08 | Filtración de DSN, token o datos cross-tenant | Fail-closed, rotación/contención por autoridad, pruebas de denegación y auditoría |

**R01 es el único mecanismo de reversión aplicable a esta publicación documental.** R02–R08 son diseños por certificar, sin suponer backups, credenciales, dual-write, RTO/RPO o infra actuales. Un \`git revert\` de Markdown no restaura MySQL ni revierte una operación de despliegue.

## Matriz de aceptación de esta documentación

**P-DOC01:** traza M01.01–05, el A5 histórico con autor/revisor distintos y la dependencia C01-01.M01.06 sin alterar su autoridad. **P-DOC02:** lista los dieciséis gates con NO_GO específico y salida verificable, sin autoliberar el DAG. **P-DOC03:** contiene ocho escenarios de recuperación y separa claramente R01 documental de R02–R08 futuros. **P-DOC04:** distingue los 20 oráculos AC01–AC20 NO_EJECUTADOS de los checks GitHub de integridad.

**N-DOC01:** un \`EmpresaId\`, DSN, host, JWT o header del cliente no decide el datastore. **N-DOC02:** A5 de autor idéntico, owner concurrente o CI requerida cancelada no permite LISTO. **N-DOC03:** ningún avance documental implica que existan router, dedicated, migración, backup/restauración o pruebas A/B reales. **N-DOC04:** no promover QA/main/PROD ni cambiar secretos, proveedores o datos reales por DAG G=SI.

## Protocolo A0–A5 de este artefacto

**A0:** diff exclusivo de este Markdown y la sección contigua del CHANGELOG; fuentes de PLAN, repositorio y Skill; ningún secreto nuevo. **A1:** P-DOC01–04 y N-DOC01–04 verificables estructuralmente. **A2:** autorización JWT/RBAC, contexto tenant A/B, DSN server-only, rollback y fail-closed como condiciones futuras; no se reclama pentesting ni prueba runtime. **A3:** coherencia causal M01.01–05, \`C01-01.M01.06\`, contratos de storage existentes, AC01–AC20 y DAG. **A4:** commit fast-forward/CAS serializado contra HEAD fresco de dev y readback de GitHub SHA, Scope Lock/V5 \`completed/success\`, PLAN/COLA/BITACORA. Compilaciones .NET, Angular o MySQL/Docker son **NO_APLICA** para el diff estrictamente documental, no PASS. **A5:** revisión por un TASK_ID Javier distinto del autor, con evidencia y readback posterior.

## Condición de salida hacia M02

Solo si M01.06 alcanza LISTO_DEV con A0–A4 auténticas se registra A5_PENDIENTE. Una Task Javier diferente debe completar A5 independiente. **Después**, reconsultar PLAN/COLA/DAG para M02 por ID exacto; si \`H=PENDIENTE\`, owner vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y preflight seguro, reclamar exclusivamente una tarea M02 documental. Esto nunca otorga migración real, activación de MySQL dedicated ni liberación de QA/PROD. El flujo permanece recuperable mediante evidencias y sin modificar historias existentes.
