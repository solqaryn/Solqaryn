# SOLQARYN V5 — C12-04.M01.06: bloqueos, rollback y liberación documental de MySQL privado/cutover

**Autor:** PRIMARY DEV 05, `TASK_ID=6adc2e55f47c81919f6374e561687076`; **fecha:** 2026-11-15; **repositorio:** `solqaryn/Solqaryn@dev`; **PROJECT_SCOPE_LOCK:** STRICT. Este es el contrato terminal **exclusivamente M01 preparatorio**, no migración, ensayo restore, confirmación de cutover ni autorización para infraestructura.

## 1. Autoridad y objetivo

El alcance se extrae del STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y del plan institucional (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`). Se deben conservar Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, autoridad DEV y dependencias calculadas PLAN/COLA/DAG. El lote C12-04 trata **una posible exportación, restore de ensayo, comparación, ventana de cambio y vuelta atrás**, sin ejecutar ninguna de esas acciones en M01.

Preflight de esta unidad en PLAN fila 1843: H=PENDIENTE, I vacío; COLA G/O=SI y DAG_TECNICO fila 1804 F=VALIDADO/G=SI. Se efectuó transición condicional a EN_PROGRESO, owner único y readback con evento BITACORA. Las fórmulas permanecen intactas. Revalidar SHA actual, Scope Lock y ausencia de conflictos en cada publicación; estos hechos iniciales no conceden autorización para transferir datos.

## 2. Predecesores originales y A5

| Microtarea | Contrato publicado, blob Git | Evidencia delimitada |
| --- | --- | --- |
| M01.01 | [Alcance/no-regresión](./c12-04-m01-01-alcance-migracion-restore-reversion.md), `31772bba4315c7669d1b73c8aaac480fe6a1d383` | NR01–NR16 y diez pares P/N futuros, A5 independiente |
| M01.02 | [Actores/fronteras](./c12-04-m01-02-actores-interfaces-mysql-privado-cutover.md), `4117d3060be6861e0ee3be85c54a340fe5719aa7` | 14 actores, 12 I/O, 10 límites, A5 independiente |
| M01.03 | [Inventario Git](./c12-04-m01-03-inventario-codigo-evidencia-mysql-cutover.md), `4da1375d688b2b96ce09f375baf3993866f18451` | 16 blobs de código y 14 brechas, A5 independiente |
| M01.04 | [Riesgos/decisiones](./c12-04-m01-04-riesgos-supuestos-decisiones-mysql-cutover.md), `9e16aa24a01ea10772171bc9afcaae130a7040dd` | R01–R14, S01–S10, D01–D10 abiertas, A5 independiente |
| M01.05 | [Criterios futuros](./c12-04-m01-05-criterios-aceptacion-mysql-cutover.md), `289c0acd94a3e39f898faa8ced800f3f8643cae3` | 16 positivos, 16 negativos y diez NF NO_EJECUTADOS, A5 independiente |

PLAN H/K y DAG registraron las cinco unidades en LISTO con A5 de Task Javier distinta del autor al comenzar este trabajo. Cada bloque contiene contratos y referencias, no evidencia de export, restore, ensayos SQL ni permisos externos. El inventario y los blob SHA se utilizan únicamente para trazabilidad del material original.

## 3. Gate de salida B01–B16

| Gate | Prueba o decisión necesaria para ejecutar fases futuras | Si falta |
| --- | --- | --- |
| B01 | Repo dev, Task de allowlist, Skill, Scope Lock STRICT y SHA exacto | No editar ni certificar |
| B02 | PLAN H/I/K, COLA G/O, DAG F/G correctos, owner único y BITACORA | No forzar fórmulas o reclamar |
| B03 | M01.01–05 LISTO+A5 independientes con blobs sin cambios | No cerrar M01.06 |
| B04 | Registro aprobado del origen Aiven/MySQL y destino, versiones reales y compatibilidad | No conectar proveedores ni presumir destino |
| B05 | Autoridad humana/técnica expresa para trasladar datos y ventana de cambio | M01 no la sustituye |
| B06 | Inventario de tablas, FKs, índices, triggers, rutinas y migraciones existentes | No construir DDL a ciegas |
| B07 | Estrategia de export por snapshot consistente y artefactos cifrados con acceso mínimo | No exportar PII ni secretos |
| B08 | Restore completo en MySQL **efímero autorizado**, con logs y rollback verificables | No declarar restauración probada |
| B09 | Conteos, hashes/canonicalizaciones y referencialidad verificados para tenant A/B | No declarar equivalencia ni integridad |
| B10 | Autorización JWT/RBAC, ownership y sucursal probadas con actores A/B y revocación | No conceder acceso cruzado |
| B11 | Datos personales y campos sensibles depurados en evidencia, retención autorizada | No copiar dumps/logs públicos |
| B12 | RTO/RPO estimados y **medidos** en ensayo real; no inventar umbrales | No habilitar cutover |
| B13 | Ventana de observación, mantenimiento, freeze de escritura y comunicación aprobados | No tocar DNS/routers/API live |
| B14 | Plan de fallos y recuperación con control de dual-write/colas, sin duplicados ni pérdida | No cambiar endpoints productivos |
| B15 | Comparación base vs destino, auditoría de errores, smoke y aprobación humana final | No promover QA/main/PROD |
| B16 | A0–A4 documental, CI Scope Lock/V5 exact SHA y A5 independiente de otra Task | No liberar sucesores sin DAG |

## 4. Positivos y negativos de aceptación (FUTUROS_NO_EJECUTADOS)

| Caso | Positivo futuro requerido | Negativo que debe bloquear el avance |
| --- | --- | --- |
| P01/N01 | Snapshot consistente reproduce conteos por tabla y tenant | Conteos ambiguos o export incompleta |
| P02/N02 | Restore efímero recrea FKs, índices, constraints y tipos | Pérdida de integridad, FK rota o tipo truncado |
| P03/N03 | Comparación semántica y checksum reproducibles por SHA y lote | Diferencia oculta por orden/locale/NULL |
| P04/N04 | JWT y membresía A leen sólo filas A | Tenant A accede a datos B por ID |
| P05/N05 | Operación sin grant produce 401/403 y cero cambios | Rol visual o query string concede acceso |
| P06/N06 | Migración aditiva en CI efímera preserva reversión | DDL irreversible ejecutado en DB real |
| P07/N07 | Falla inyectada revierte estado de prueba y conserva auditoría | Restore parcial o progreso marcado LISTO |
| P08/N08 | Ventana autorizada y stop-on-failure verificables con tiempos reales | RTO/RPO supuestos tomados como medición |
| P09/N09 | Logs de ensayo con PII redactada, trazabilidad y permisos mínimos | Token/dump/backup expuesto |
| P10/N10 | Cutover futuro aprobado por humano tras smoke en destino autorizado | Cambio DNS/credencial provocado por documento |
| P11/N11 | A5 auténtico de revisor distinto y SHA causal | Autor se aprueba o CI cancelada cuenta PASS |
| P12/N12 | Comparación de rutas y apps no altera tráfico existente | 503, rollback omitido o doble escritura |

No se han ejecutado tests HTTP, xUnit de integración específicos, EF/MySQL, restauraciones, backups, migraciones, medidas de disponibilidad, DNS, cambios de proveedor, R2, VPS ni despliegues. La matriz es planificación de pruebas futuras, no certificación de datos existentes.

## 5. Reversión y handoff RB01–RB10

| Caso | Acción mínima segura |
| --- | --- |
| RB01 Scope Lock FAIL | Inspeccionar log causal y corregir sin apagar verificadores |
| RB02 HEAD distinto o CAS rechazado | Releer árbol actual, regenerar commit fast-forward, sin force-push |
| RB03 Owner de otra Task activo | No intervenir ni transferir propiedad sin CAS y ausencia demostrada de escritor |
| RB04 CI queued/in_progress | VALIDANDO con owner; buscar otra tarea independiente |
| RB05 CI cancelled/skipped/failed | Nunca PASS de prueba requerida; nuevo SHA o reparación dirigida |
| RB06 A5 propio o inexistente | Exigir otro TASK_ID Javier y readback, no liberar DAG |
| RB07 Backup/restore hipotético sin ensayo | No trasladar datos ni simular resultado en documentos |
| RB08 Aislamiento tenant o PII no comprobados | Mantener bloqueo, probar caso negativo en entorno efímero |
| RB09 Error de BITACORA/duplicado | Reconcilia con eventos compensatorios aditivos, sin reescritura |
| RB10 Documento M01 equivocado | `git revert` normal del changeset tras analizar HEAD concurrente; cero rollback real de datos |

## 6. A0–A5 y criterio terminal

**A0:** verificar alcance M01 exclusivamente Markdown+CHANGELOG y procedencia Git de cinco documentos. **A1:** consistencia de gates B01–B16, escenarios P01–P12 y negativos N01–N12, todos FUTUROS_NO_EJECUTADOS. **A2:** revisar estáticamente mínimo privilegio, tenant A/B, JWT/RBAC, revocación, PII y plan de rollback, sin validar infraestructura. **A3:** contratos M01 relacionados y no aplicabilidad razonada de integración MySQL, frontend o HTTP para este diff sin runtime. **A4:** commit fast-forward/CAS dev de sólo dos Markdown, CHANGELOG con encabezado y viñetas contiguas, checks Scope Lock/V5 `completed/success` en SHA exacto, eventos y readback PLAN/COLA/DAG/BITACORA. **A5:** pendiente de otra Task Javier independiente después de LISTO_DEV, previa a cualquier sucesor condicionado.

**Decisión:** cerrar M01.06 significa únicamente preparar M02 para que **sus propios** PLAN/COLA/DAG y permisos sean comprobados; no habilita una migración, cambio de proveedor, QA/main/PROD ni despliegue. El rollback de este documento es un revert Git, nunca un rollback de información comercial.
