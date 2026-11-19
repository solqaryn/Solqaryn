# C14-04.M06.03 — Riesgos abiertos, limitaciones y decisiones verificables

**Corte:** 2026-11-18. **Proyecto:** SOLQARYN dev. **PROJECT_SCOPE_LOCK:** STRICT. **Task autora:** Javier DEV05. **Cambio:** R0 documental; los riesgos subyacentes de producción, identidad, tenant, restauración y continuidad son R2/R3.

## A0 — Referencias y separación de evidencias

La microtarea documenta limitaciones y decisiones del bloque C14-04, sin convertir resultados unitarios o navegador local en autorización productiva. Fuente de la matriz M05.01–06: documento C14-04.M06.01 (commit 8f28c76289c61c26154b2a7354db348c8cee0207), seis entregas M05 LISTO y A5 independientes en PLAN_MAESTRO K. El origen de esta unidad es el conjunto actual de evidencias y dependencias, no una aprobación humana ni una simulación de releases. El estado de C14-04.M06.02, M06.04 y M06.05 se verifica separadamente: no se presupone A5 ni cierre por M06.03.

## A1 — Limitaciones falsables

| Tema | Evidencia real existente | Limitación o bloqueo | Decisión |
| --- | --- | --- | --- |
| Frontend/UX | Pruebas Chromium y build/lint Angular en runners GitHub para M05.04/M05.06 | Sesión interceptada y respuestas sintéticas; no prueban JWT backend, aislamiento A/B ni rutas productivas | Mantener SIN_VERIFICAR autorización HTTP real |
| Dominio/Application | xUnit .NET 10 completado en M05.01, M05.02 y M05.05 | Pruebas in-process no demuestran transacción EF/MySQL 8.4 ni protección IDOR/BOLA | Requerir suite de integración persistente y negativa A/B |
| Automatizaciones/CI | Cliente GET read-only y checks SHA observables en M05.03 | Un build verde genérico, SKIPPED o QUEUED no certifica otros workflows requeridos | Sólo SUCCESS causal del SHA y blobs compatibles |
| Rendimiento y accesibilidad | Smoke DOMContentLoaded local, teclado y movimiento reducido | No equivale a WCAG integral, p95/p99, SLO o carga real | NO_MEDIDO: numerador/denominador no disponibles |
| Respaldo/recuperación | Documentos de mitigación y límites M05 | No hay demostración de restauración del entorno destino, RPO/RTO ni failover real con permisos de producción | NO_MEDIDO/NO_GO sin pruebas y autorización |
| Go-live y soporte | Plan de soporte, alertas e incidentes en la fase | No demuestra guardias, observabilidad efectiva, canales de escalamiento ni aceptación de incidente de extremo a extremo | R3 NO_GO para QA/main/PROD o cambios irreversibles |

## A2–A3 — Reglas de rechazo y controles pendientes

Un resultado positivo sintético nunca concede permisos. El caso negativo de sesión/tenant B debe probarse contra una API con JWT firmado y un MySQL aislado con dos empresas antes de declararse verdadero. No reutilizar secretos ni datos productivos. No activar DNS, Render/Vercel productivo, Cloudflare/R2 real, migraciones irreversibles, restauración real o cambios de cuentas. La documentación no toca código de runtime, Domain, EF, SQL, Angular, CI ni credenciales, por lo que compilaciones, JWT HTTP y MySQL son NO_APLICA para **este diff R0**. Eso no convierte los gates de aceptación global en PASS.

## A4–A5 — Decisiones, dependencia y reversión

La decisión institucional es preservar las lagunas como SIN_VERIFICAR o NO_MEDIDO y exigir mediciones con fuente, numerador y denominador cuando correspondan; una declaración de calidad futura debe relacionar el SHA causal, los jobs y el alcance probado. No modificar fórmulas de DAG/COLA ni escribir SI a mano. Para cerrar sólo la entrega documental DEV se requieren diff revisado, commit fast-forward a dev, controles Scope Lock y procedencia V5 exact-SHA SUCCESS, CAS V17 y readback de PLAN/BITACORA. A5 corresponde a otra Task Javier independiente, nunca la autora. La aprobación de M06.03 no certifica liberación M06.06 ni autoriza QA/main/PROD.

Rollback: revert normal del commit documental tras revalidar HEAD y escritores; conservar historia BITACORA, FAIL previos, propietarios históricos y todos los gates de seguridad.
