# SOLQARYN V17 — C11-01.M01.06: bloqueo, reversibilidad y salida del modelo relacional normalizado

**Microtarea:** `C11-01.M01.06` · PLAN_MAESTRO fila 1591 · **Autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`) · fecha local 2026-11-15 · repositorio `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo R0:** contrato documental; no toca código ejecutable, tablas, migraciones, repositorios de datos, JWT, RBAC, flags, workers ni producción.

**Autoridad única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Skill canónica, normas V15–V17, PLAN_MAESTRO/COLA/DAG_TECNICO y evidencias Git exactas. El objetivo del lote C11-01 es preparar claves primarias y foráneas, índices, propiedad empresarial, compatibilidad con EF Core 10/MySql.EntityFrameworkCore Oracle 10.0.9 y MySQL 8.4, con diccionario de datos y migraciones **futuras** reversibles. La única entrega actual define bloqueo, rollback y criterio de liberación M01. M02 y cualquier implementación posterior deben reclamar su propia ID cuando el DAG lo autorice.

## 1. Prerrequisitos institucionales y fuentes con blobs

Antes del claim: `PLAN H1591=PENDIENTE`, `I1591` vacío; `COLA G1591/O1591=SI`; `DAG A1552=C11-01.M01.06, F1552=VALIDADO, G1552=SI`. Se adquirió `SQV17_C11_01_M01_06` (`namedRangeId=2000001591`) antes de la transición H por CAS con `occurrencesChanged=1`; I se asignó al Task ID autor y se confirmó lectura H/I + COLA + BITACORA. Una sola EN_PROGRESO propia. Esto sólo avala la preparación documental.

| Antecedente LISTO | Archivo leído en dev y blob de contenido | Evidencia real, alcance limitado |
| --- | --- | --- |
| M01.01 | `docs/v5/c11-01-m01-01-objetivo-no-alcance-modelo-relacional.md` · `530958065528c4d2f8b0d9044b2e55776b0697e6` | Objetivo, NR01–NR14; no esquema desplegado |
| M01.02 | `docs/v5/c11-01-m01-02-actores-interfaces-modelo-relacional.md` · `6dc4b1abcf2c1ed557dbbed788ec934982caa11a` | Actores A01–A12, I/O, límites de confianza |
| M01.03 | `docs/v5/c11-01-m01-03-inventario-codigo-evidencia-modelo-relacional.md` · `4f45d732115187fcc9cc6b3a0e85e4c2896c96b8` | Quince fuentes, brechas G01–G12 de evidencia, NO defectos confirmados |
| M01.04 | `docs/v5/c11-01-m01-04-riesgos-supuestos-decisiones-modelo-relacional.md` · `29bcf22d6f20382c863c400e29fac2fe02fc27b3` | R01–R16 hipotéticos, S01–S10 no verificados, D01–D10 abiertas |
| M01.05 | `docs/v5/c11-01-m01-05-criterios-aceptacion-modelo-relacional.md` · `5873d61759e973762a780125408f92f9c179991f` | P01–P16/N01–N16 y RNF01–RNF10, todos FUTUROS_NO_EJECUTADOS |

Los cinco antecedentes tienen A5_PASS independiente de la Task Javier DEV01 respecto de la autoría DEV06 al momento de esta reclamación, según PLAN_MAESTRO. La fórmula DAG incluye además los antecedentes `C01-01.M01.06+A5` y `C07-01.M01.06+A5`, con dependencias preparatorias de bloques C01/C02/C03/C07/C10; jamás se fuerza un SI. Revisiones sobre Markdown no acreditan resultados EF/MySQL/HTTP de datos reales. Arquitectura respetada: Domain ← Application ← Infrastructure, API de composición, EF Core 10.0.12 y provider Oracle 10.0.9 en Infrastructure.

## 2. Dieciséis gates: bloqueo y condición causal de salida

| Gate | Control necesario para la fase futura correspondiente | Si no acredita, estado |
| --- | --- | --- |
| G01 | Gobierno/Skill, autor Javier, dev, Scope Lock STRICT y SHA confirmado | NO_GO a escritor/claim |
| G02 | PLAN H/I, COLA G/O y DAG F/G verificados; predecesoras LISTO y A5 causal si se exige | No liberar sucesoras |
| G03 | NamedRange V17 exclusivo, CAS H exactamente 1, owner I y BITACORA idempotente | Abort por colisión/escritor |
| G04 | Claves PK estables por entidad y FK de alcance tenant/sucursal | No migrar modelo ambiguo |
| G05 | Propiedad EmpresaId en todas las relaciones comerciales y consultas | Denegar mezcla tenant A/B |
| G06 | Filas con EmpresaId NULL de legado reconciliadas con prueba y auditoría, nunca propiedad global | Cuarentena/read-only, fail-closed |
| G07 | FK y unique constraints compuestas que respeten empresa, no sólo Id global | Impedir enlace A→B |
| G08 | Índices que respalden filtros tenant, orden/paginación y consulta acotada | No introducir full-scan sin evidencia |
| G09 | Versionado de migraciones EF/DDL, orden, locks, preflight y backup aislado | No ejecutar DDL real |
| G10 | Migración reversible o compensación auditada, prueba MySQL 8.4 efímera | No certificar rollback |
| G11 | Transacciones de inventario, crédito, facturación y pagos coherentes | NO_GO a efectos parciales |
| G12 | Idempotencia, concurrency tokens y negative replay/doble escritura | No aceptar duplicación o pérdida |
| G13 | JWT/RBAC, membresía/propiedad y privilegios por recurso aplicados en API | UI/slug/header no son autoridad |
| G14 | Compatibilidad de lectores, API/DTO, NULL legacy y consumidores Angular con cambios aditivos | No romper datos históricos |
| G15 | A0–A4 con xUnit e integración real EF/MySQL cuando aplique, exact-SHA CI Scope Lock/V5 completed/success | CANCELLED/SKIPPED/IN_PROGRESS/FAIL nunca PASS |
| G16 | A5 independiente Task Javier distinta, ausencia de P0/P1 y readback PLAN/COLA/DAG | Sin desbloqueo técnico/QA/main/PROD |

Estas puertas son **requisitos para implementación futura**. En esta microtarea NO se ejecuta `dotnet ef database update`, migración, DDL, transacción o tenant SQL; no se afirma que el esquema vivo coincida con el modelo conceptual. Si un paso R3 productivo o irreversible se solicitara en el futuro, requiere una autorización específica y un scope diferente.

## 3. Doce pares de escenarios FUTUROS_NO_EJECUTADOS

| Par | Positivo necesario | Negativo que debe detectar el defecto |
| --- | --- | --- |
| P01/N01 | PK y FK de empresa A permiten registro legítimo A | FK de A a recurso B falla |
| P02/N02 | Consulta A retorna exclusivamente A con índice filtrado | ID conocido B no se lee ni se infiere |
| P03/N03 | Legado NULL reconciliado auditadamente queda asociado | NULL sin evidencia no obtiene propiedad global |
| P04/N04 | Creación de entidad conserva EmpresaId real backend | Cabecera/DTO manipulados no conceden empresa |
| P05/N05 | Índice de paginación A mantiene orden estable | Sort arbitrario o consulta no acotada rechazada |
| P06/N06 | Migración EF MySQL 8.4 efímero aplica y revierte | Falla DDL a mitad deja rollback sin efectos |
| P07/N07 | Transacción comercial A conserva saldo y referencias | Error de guardado no deja pago/inventario parcial |
| P08/N08 | Concurrencia válida aplica una actualización por versión | Doble request/replay no duplica crédito o registro |
| P09/N09 | Soft delete conserva historial referencial y auditoría | Hard delete de venta o relación histórica se impide |
| P10/N10 | Usuario con RBAC/tenant válido lee recurso A | Membresía revocada o lector B recibe 401/403 |
| P11/N11 | Cliente Angular/API mantiene DTO compatible y errores privados | Campo tenant/rol inyectado o SQL exception no filtra |
| P12/N12 | Rollback de feature restaura lectura y traza de datos de prueba | Revert no borra BITACORA ni datos referenciados |

**Ejecutados en M01.06:** `0/12` pares de pruebas runtime. Estos oráculos **NO SON PASS**: integración futura requiere servicio MySQL 8.4 efímero con proveedor Oracle real y migraciones, pruebas positivas/negativas, aislamiento tenant A/B, rollback y seguridad proporcional. No se tocan datos Aiven ni QA/main/PROD.

## 4. Condiciones de liberación del lote y decisiones pendientes

**D01–D10:** conservar preguntas y decisiones abiertas de M01.04: claves canónicas/ownership, relaciones EmpresaId+sucursal, nulabilidad histórica y método de reconciliación, índices únicos y filtros, migración/rollback, transacciones, concurrency/idempotencia, consumidores API/Angular, privacidad/auditoría, entorno y pruebas de proveedor. NO se inventan decisiones ni permisos para reescribir tablas. Hasta documentar la autoridad causal, las migraciones que dependan de estas decisiones siguen bloqueadas.

**M01.06 ahora:** crear únicamente dos archivos Markdown afectados (este documento y sección autocontenida de `CHANGELOG_AI.md`), commit serial fast-forward dev, verificar blobs, A0–A4, checks Scope Lock/V5 y readback de BITACORA/PLAN/COLA/DAG. El A5 queda pendiente de otra Task Javier y nunca equivale a permiso QA/main/PROD.

**M02 futuro:** cada contrato de entidad, ownership y API se reclama con ID única y candado V17 si H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI. `LISTO_DEV` documental no habilita M02 si el DAG exige A5 de M01.06.

**M03–M06:** modificaciones auténticas deben implementar Domain/Application/Infrastructure/API, EF Core 10.0.12 Oracle MySQL 10.0.9/MySQL 8.4 o Angular 22.2.1 según alcance, con xUnit, integración efímera, RBAC/tenant A/B, seguridad, rollback y checks del SHA causal. El cumplimiento documental no sustituye pruebas de migración ni licencia de un proveedor.

## 5. Rollback y recuperación segura (ocho pasos)

1. Identificar ID, Task autora/revisora, lock, source SHA, tested SHA, HEAD, blobs, requisitos, impacto y lector legítimo.
2. Verificar ausencia de escritores concurrentes y leases activos; si hay ambigüedad NO_GO solo ese scope, no robar owner ni borrar pruebas.
3. Separar fallo documental de defecto de DDL, integridad, tenant, CI o relectura de fórmula; conservar P0/P1 real como gate.
4. Para error en Markdown, usar corrección mínima o `git revert` normal fast-forward tras revalidar HEAD; no force-push, reset, borrar historial o fórmulas.
5. Antes de cambiar EF/MySQL posteriormente, realizar backup aislado, snapshot de filas de prueba y validación de integridad/referencias; no tocar Aiven productivo.
6. Ensayar en MySQL 8.4 efímero reversión/compensación con fallo a mitad, concurrency e idempotencia, cotejar conteos y checksums; no inferir que build verde basta.
7. Repetir pruebas negativas tenant A/B, propiedad NULL, FK compuesta, roles revocados, transacción, rollback y contratos afectados; confirmar Scope Lock/V5 exact-SHA SUCCESS.
8. Registrar evento idempotente con owner anterior/nuevo si aplica, conservar BITACORA A:Z append-only, readback PLAN/COLA/DAG y revisor A5 independiente antes de liberar puertas técnicas.

## 6. A0–A5 y alcance limitado

- **A0:** requisitos, cinco antecedentes con blobs, DAG real, diff sólo este Markdown y CHANGELOG sin encabezado vacío ni viñetas intercaladas.
- **A1:** revisar G01–G16, doce pares P/N y rollback de ocho pasos; pruebas runtime NO_APLICA por ser documentación, no confundir presencia de matriz con ejecución.
- **A2:** análisis de aislamiento A/B, EmpresaId NULL, JWT/RBAC, relaciones cruzadas, pagos, errores, datos personales e idempotencia como controles futuros; no prueba live.
- **A3:** no cambia esquema EF/MySQL, API/HTTP, Angular ni transacciones; test de MySQL 8.4, xUnit y Playwright NO_APLICA motivado al diff R0.
- **A4:** GitHub dev HEAD fast-forward, dos Markdown, blobs verificables, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success sobre SHA causal y PLAN/COLA/BITACORA readback.
- **A5:** PENDIENTE exclusivamente revisor Javier Task ID distinto, capaz de A5_FAIL. Un cierre de dev no libera QA/main/PROD.
- **Rollback documental:** revert Git aditivo del SHA (sin borrar registros) y verificar nuevo HEAD si una premisa de seguridad/documentación falla.

**Conclusión técnica:** La salida M01.06 delimita la documentación y sus condiciones de cierre; NO certifica tablas, esquema aplicado, pruebas de migraciones, API, MySQL real, aislamiento de tenants real, secretos ni entornos externos.
