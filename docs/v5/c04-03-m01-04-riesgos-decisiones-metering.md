# SOLQARYN V5 FINAL — C04-03.M01.04: riesgos, supuestos y decisiones pendientes del metering

**Tipo:** preparación documental M01.04, NO implementación ni autorización comercial. **Autor:** PRIMARY DEV 03 / JAVIER_CHATGPT / `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repositorio/ámbito:** `solqaryn/Solqaryn@dev` Scope Lock STRICT; 2026-11-14.

**Fuentes únicas:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) y [M01.03 inventario real](c04-03-m01-03-inventario-evidencia-metering.md). M01.04 registra incertidumbres sobre futura medición de usuarios, operaciones, almacenamiento, archivos y consumo por tenant, con idempotencia, cuotas y conciliación. Preflight `PLAN H653=PENDIENTE/I653=vacío`, `COLA G/O653=SI/SI`, `DAG F614/G614=VALIDADO/SI`; dependencia preparatoria C01/C02/C03 M01.01–05 LISTO. Ninguna puerta técnica de runtime queda abierta por este documento.

## 1. Hallazgos de fuentes existentes y nivel de certeza

El inventario previo acredita once blobs reales, de los cuales `SuscripcionesSaaS.cs` contiene `PlanLimite` y suscripción por EmpresaId (`d9b5dbe19fb728293b00b748f10ac10336debd2d`); `SuscripcionesSaaSService.cs` documenta tenant verificado e idempotencia solo del onboarding (`bb501285f11cd4b32709a58a2c51efa19c3664ea`); `SuscripcionesSaaSRepository.cs` conserva ledger de idempotencia de la **suscripción**, no un medidor (`f3340ca687867d6dc2d99dfff5452bbe5c9bbb79`); `SuscripcionesSaaSServiceTests.cs` contiene xUnit/Moq de onboarding (`090bf81e7a8f08e86dcdc7ddf5b0c20625ae2839`). La interfaz Angular de límites muestra `valorMaximo ?? 'Sin límite'` (`7909433c414089b89b87ebff20017c2704a7f085`) y no demuestra uso ni tarifa. Estas observaciones de lectura no certifican aislamiento HTTP+MySQL, metering live, cobro ni consentimiento/regulación.

## 2. Registro de riesgos futuros (12 riesgos, NO vulnerabilidades confirmadas)

| ID | Superficie/riesgo causal | Impacto potencial | Acción de verificación / tratamiento |
| --- | --- | --- | --- |
| R01 | Tenant derivado de cabecera/cuerpo, no membresía backend | A contabiliza cuota de B | Tests HTTP/DB A/B y deny por identidad |
| R02 | Reintentos concurrentes cuentan dos veces | Overcount y cobro indebido | Idempotency-key por fuente/tenant/operación y DB efímero |
| R03 | Unidades sin contrato (bytes/archivos/usuarios/operaciones) | Totales inconsistentes | Catálogo tipado, dimensionalidad y pruebas de conversiones |
| R04 | Periodos/fechas elegidos por cliente | Evasión de límites o periodo incorrecto | Reloj servidor y periodización UTC/versionada |
| R05 | `null` se equipara incorrectamente a consumo ilimitado | Bypass de cuotas | Semántica explícita de plan y prueba negativa |
| R06 | Estado Suspendida/Cancelada/Expirada ignorado | Uso indebido de recursos | Política por estado aprobada, RBAC y tests |
| R07 | Falla DB en evento crítico se oculta | Consumo real sin evidencia persistida | Transacción estricta, rollback y prueba de error inyectado |
| R08 | Retención/exportración sin mínimo privilegio | Exposición de PII y patrones de cliente | Política, paginación y minimización aprobadas |
| R09 | Reconciliador borra o reescribe eventos históricos | Pérdida de trazabilidad | Compensación append-only y prueba de reproducibilidad |
| R10 | Uso de Angular como fuente de verdad | Manipulación de consumo/saldo | Medición únicamente de fuente autorizada en backend |
| R11 | Cambio de plan altera periodo/costo previo | Repricing retroactivo o historiales corruptos | Versionar política, periodos y reglas antes de ejecutar |
| R12 | Preparación documental se interpreta como gate runtime | Migración/cobro/worker sin CI real | A0–A5 independientes, DAG y Scope Lock exact-SHA |

**Estado de todos los riesgos:** `POR_VERIFICAR`. Un riesgo listado no es una exposición confirmada; cualquier certeza de incidencia o cierre requiere test causal y resultado exact-SHA.

## 3. Supuestos no certificados (8)

| ID | Fundamento observado | Qué lo invalidaría |
| --- | --- | --- |
| S01 | PlanLimite valida valores positivos o null | Métrica nueva sin unidad o no ligada a regla aprobada |
| S02 | Suscripcion tiene EmpresaId y estado | Cálculo de uso sin scope server-side |
| S03 | Onboarding usa resolver tenant | Consumer futuro confía en dato del navegador |
| S04 | Repository registra idempotencia onboarding | Falso traslado de garantía a consumo |
| S05 | API SaaS tiene Authorize | Nueva ruta sin RBAC o con bypass |
| S06 | EF mapea plan y suscripción | No existe tabla/evento metering ni migración aplicada |
| S07 | Angular muestra límites | UI pretende decidir cobro o grant |
| S08 | M01.01–03 son entregas documentales | Se presentan como pruebas runtime o liberación de M02 |

Estos supuestos están **PENDIENTES de verificación según el alcance implementable**, no son certificaciones de cumplimiento.

## 4. Decisiones pendientes y autoridad aprobadora FUTURA (8)

| ID | Decisión todavía NO aprobada | Rol aprobador requerido antes de implementar | Criterio de dictamen y estado |
| --- | --- | --- | --- |
| D01 | Métricas allowlisted y unidades por rubro | Propietario producto/comercial + arquitecto backend | Especificación tipada, casos de redondeo, PENDIENTE |
| D02 | Semántica de usuarios activos y periodo | Propietario producto + responsable seguridad/privacidad | Identidad/membresía, corte temporal y GDPR/local aplicable, PENDIENTE |
| D03 | Medición almacenamiento/archivos: fuente y reversiones | Arquitecto infraestructura + dueño de datos | Bytes/objetos, duplicados y coste sin inferir proveedor, PENDIENTE |
| D04 | Límites, 0, null, desbordes y grace periods | Propietario comercial + arquitectura/seguridad | Plan/versionado, RBAC y test fail-closed, PENDIENTE |
| D05 | Clave idempotente y transacción de evento | Responsable backend + responsable seguridad | Unicidad por tenant/operación y MySQL/EF tests, PENDIENTE |
| D06 | Retención, export, minimización y acceso a lecturas | Responsable legal/privacidad + seguridad | Política escrita y controles A/B, PENDIENTE |
| D07 | Ajustes de conciliación y compensación | Arquitectura backend + responsable de auditoría | Sin editar eventos pasados; doble revisión para ajustes sensibles, PENDIENTE |
| D08 | Uso comercial: precio, moneda, facturación y cambio de plan | Propietario comercial autorizado + responsable financiero | Aprobación explícita separada antes de cobro, PENDIENTE |

**La tabla asigna roles candidatos a futuros aprobadores, NO afirma su identidad personal ni otorga aprobación actual.** Ninguna D01–D08 se ejecuta o cambia precios, estados, JWT/RBAC/tenant o entorno. Se prohíbe utilizar un PASS documental como autorización comercial.

## 5. Oráculos de revisión causal (solo documento)

| ID | Signo | Verificación requerida |
| --- | --- | --- |
| P01 | + | Los 12 riesgos R01–R12 tienen superficie, impacto y tratamiento diferenciados |
| P02 | + | Los 8 supuestos S01–S08 tienen condición de falsación y no son PASS |
| P03 | + | D01–D08 tienen rol aprobador futuro y todas PENDIENTE |
| P04 | + | Inventario M01.03 conserva blobs reales sin extrapolar ledger a consumo |
| N01 | - | Se afirma que frontend autoriza tenant o cobra: rechazo |
| N02 | - | Se presenta idempotencia de onboarding como metering completo: rechazo |
| N03 | - | Se declara una fuga A/B sin test causal: corregir como riesgo |
| N04 | - | Se inventan tarifas, impuestos, currency o proveedor: no aprobar |
| N05 | - | Se activa evento, worker, migración o API desde M01: NO_GO |
| N06 | - | Se denomina PASS a job queued/skipped/failed: NO_GO |
| N07 | - | Se elimina A5_FAIL histórico o una decisión queda sin rol futuro: NO_GO |
| N08 | - | Autor se revisa A5 a sí mismo o toca QA/main/PROD: NO_GO |

P01–P04 y N01–N08 son criterios estructurales, no pruebas runtime de MySQL/HTTP o conducta de producción.

## 6. A0–A5, dependencias y rollback

A0 compara código/blobs/inventario y ausencia de secretos; A1 constata 12 riesgos, 8 supuestos, 8 decisiones y 4P/8N; A2 mantiene tenant A/B, JWT/RBAC, datos privados y fail-closed en su frontera actual; A3 comprueba PLAN/COLA/DAG y M01.01–03 sin liberar M01.05/06; A4 publica dos archivos Markdown fast-forward por CAS y comprueba Scope Lock+provenance V5 completed/success SHA exacto, diff y BITACORA/PLAN/COLA readback. A5 debe ser de otra Task Javier distinta del autor.

Para **esta microtarea solo documental**, `dotnet/Angular/MySQL/Playwright=NO_APLICA`, nunca PASS presunto. El rollback es `git revert <SHA>` ordinario y transición compensatoria append-only sin borrar BITACORA; cero PR, branches adicionales, secretos, cobros, despliegues, QA/main/PROD.
