# C11-04.M06.03 — registro de limitaciones, riesgos abiertos y decisiones

**Contexto y gobierno:** SOLQARYN `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`; propietaria de entrega documental DEV04 `6aeacb7820ac8191b7d768a6f33dfa09`; PLAN_MAESTRO fila 1726. Referencia de lectura: `91c219eaba862ff29d01baefdd56c54738bec212`. **Riesgo de este cambio: R0**, solo documentación, ninguna ejecución productiva ni cambio en API/servidor/Angular/EF/MySQL/JWT.

## Decisión basada en evidencia

El conjunto C11-04.M05.01–06 cubre tests de contratos, interfaz y logging **en proceso**, pero NO demuestra observabilidad productiva extremo a extremo. Se toma la decisión de conservar el estado de cada tarea según evidencia A0–A5, presentar el alcance real y las limitaciones, y no usar estas pruebas para habilitar por sí solas QA/main/PROD, exportadores externos o nuevas garantías comerciales.

Fuente del cotejo: `docs/v5/c11-04-m06-01-auditoria-alcance-pruebas-vs-mocks.md` (commit `c30283c16fca5639ddb52864aa455bc2a868a5c0`) y `docs/v5/c11-04-m06-02-registro-versiones-diffs-artefactos-reproducibles.md` (commit `91c219eaba862ff29d01baefdd56c54738bec212`), además de GitHub Actions y PLAN/BITACORA institucional. Ambos documentos distinguen pruebas ejecutadas frente a fixtures de autorización y HTTP.

## Registro causal de riesgo y controles pendientes

| Código | Limitación abierta / evidencia faltante | Impacto si se confunde con PASS | Decisión y remedio comprobable | Estado |
| --- | --- | --- | --- | --- |
| OBS-01 | Tokens y sesiones de Playwright en M05.04–06 son sintéticos; 200/403/429/503 interceptados con `page.route` | Suplantar pruebas JWT/RBAC multiempresa reales | Exigir prueba HTTP autenticada con backend y tenant A/B, rechazo IDOR, revocación y respuestas opacas **antes de certificar ese alcance**, sin recrear ni duplicar tareas | NO_CERTIFICADO |
| OBS-02 | M05.03/06 prueban `LoggerFactory` / `ILoggerProvider` reales en proceso, no exporter externo ni un colector remoto | Atribuir entrega durable o alarmas reales a mocks | Test de integración del proveedor de observabilidad solo si existe conector/entorno permitido; capturar errores, aislamiento y fail-closed, y si no hay acceso mantener NO_APLICA justificado | NO_CERTIFICADO |
| OBS-03 | No hay ensayo de persistencia MySQL real o EF de este módulo como parte de M05.01–06 | Confundir tests in-process con transacción durable o protección de DB | No acreditar MySQL ni migraciones: cualquier modificación futura de EF/DB exige CI con MySQL 8.4 efímero real y tests `Category=Integration` efectivamente ejecutados | NO_APLICA A M05; NO_CERTIFICADO PARA DB |
| OBS-04 | Core Web Vitals, percentiles latencia, uptime SLO, volumen de trazas y coste no tienen muestras comparables | Prometer rendimiento, SLA o ahorro falsos | Mantener `NO_MEDIDO`; para medir exigir fuente real, período, unidades, numerador/denominador y método reproducible, sin usar recuentos DOM como latencia | NO_MEDIDO |
| OBS-05 | Existen checks auxiliares `SKIPPED` y CI global que puede permanecer `QUEUED/IN_PROGRESS` sobre otros SHAs | Falsa certificación por build verde aislado | Contar PASS solo requerido `completed/success` exact-SHA; si usar descendiente demostrar blobs idénticos y dependencias/configuración de tests compatibles | GATE A4 VIGENTE |
| OBS-06 | Documentación observabilidad no constituye aprobación del propietario, permiso de QA o despliegue | Cambiar de entorno o conceder roles sin autoridad | Mantener dev exclusivamente y A5 por una Task Javier diferente del autor de cada alcance; ningún paso humano artificial | GATE A5 VIGENTE |
| OBS-07 | Errores de acceso a la consola pueden generar mensajes de proveedor privados o reintentos duplicados | Exposición de diagnósticos y tormenta de solicitudes | Conservar negativos Chromium 403/429/503, revocación de vistas, scrub de secretos y single-flight; para nuevos endpoints crear pruebas backend negativas reales | CUBIERTO SOLO EN UI SIMULADA |

## Riesgos P0/P1, métricas y cobertura

**No se declara ausencia global de vulnerabilidades.** En el diff exclusivamente documental de esta microtarea no se encontró alteración funcional que produzca un P0/P1 nuevo. La severidad para una futura integración se determina por su impacto real; riesgo incierto => R2, R3 irreversible/producción => NO_GO sin permiso particular.

**Métricas NO_MEDIDO:** SLO disponibilidad, tasa de errores por empresa, p50/p95/p99, CWV (LCP/INP/CLS), RTO/RPO, almacenamiento de telemetría y coste por evento. No existen numeradores/denominadores ni series válidas en este audit; no inventar porcentaje de cobertura global, disponibilidad ni gasto.

El informe sí tiene un denominador verificable: **6 microtareas M05 examinadas de 6 del bloque C11-04.M05**. Sus seis fuentes tienen evidencia de prueba dirigida; ello **no** significa 6/6 certificadas para JWT/MySQL/exportador real. Las pruebas negativas son pertinentes a superficies que fueron probadas, no a un backend que no se ejercitó.

## Decisiones técnicas y reversión

1. Mantener las fuentes y controles actuales; no alterar JWT/RBAC, usuarios, tenants, datos, secretos ni configuración de CI para aumentar volumen.
2. Reparar o reabrir defectos solo ante reproducción causal, manteniendo histórico `QA_DEFECT_FOUND`, `A5_FAIL`, `REOPENED_FOR_REPAIR` cuando corresponda.
3. Seguir exclusivamente el DAG institucional y el candado V17 en cada cambio de PLAN o A5. Las dependencias no se fuerzan a SI.
4. Ante reversión de esta unidad documental, usar `git revert` sin force-push; conservar BITACORA y estados hasta transición institucional con readback, no eliminar evidencias posteriores.

## Auditorías A0–A5 de M06.03

**A0:** diff limitado a nuevo registro y encabezado autocontenido en CHANGELOG; objetivo, tablas y rollback expresos. **A1:** revisión documental negativa: cada riesgo registra qué NO demuestra la prueba, y cinco fronteras de causalidad se mantienen; no inventar xUnit/HTTP adicional. **A2/A3:** NO_APLICA a la edición documental sin runtime y sin datos. **A4:** validación SHA causal de Scope Lock y procedencia V5 en GitHub y readback de PLAN/COLA/BITACORA; checks QUEUED/IN_PROGRESS no son PASS. **A5:** Task Javier revisora distinta de DEV04, nunca autorrevisión. Sin QA/main/PROD.

El SOURCE_SHA final de M06.03 y checks se escriben en el evento institucional de la transición, no se anticipan en el texto.
