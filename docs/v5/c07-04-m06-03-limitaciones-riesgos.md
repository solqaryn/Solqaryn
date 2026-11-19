# C07-04.M06.03 — limitaciones, riesgos abiertos y decisiones (dev)

**Microtarea:** C07-04.M06.03 · **Acción de PLAN_MAESTRO!D:** «Documentar limitaciones, riesgos abiertos y decisiones tomadas». **Tipo:** R0 documental; las superficies comerciales que se describen son R2. **Rama y alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Documento de evidencia, no migración, release ni certificado de fase M06.

## A0 — Alcance verificable y fuentes

- **Etapa anterior:** C07-04.M05.06 fue entregada sobre `0f78a8f783c6afcf47e9a92d21fc67d178673683`. La evidencia registrada describe cuatro suites Chromium y **15/15 casos aprobados** en [GitHub-hosted run 44607415955](https://github.com/solqaryn/Solqaryn/actions/runs/44607415955), con lint/build Angular, Scope Lock y V5. Existe A5 independiente de DEV04 en BITACORA. Es evidencia **de pruebas de UI dirigidas**, no del ciclo fiscal/pagos completo.
- **C07-04.M05.04** [aceptación de UI](c07-04-m05-04-aceptacion-pedidos.md): Angular y router reales; respuestas de pedidos, credenciales, empresa y permisos simuladas. Un fallo del mock del permiso de detalle produjo una regresión y exigió prueba negativa adicional.
- **C07-04.M05.03** [MySQL aislado](c07-04-m05-03-mysql-pedidos.md): escenarios de persistencia y aislamiento en MySQL 8.4 efímero, sin HTTP/JWT firmado ni PSP/fiscal.
- **C07-04.M05.05** [denegación tenant](c07-04-m05-05-tenant-negative.md): filtro real in-process, pero `IPermisoService` simulado; no demuestra revocación/membresía real en API/DB.
- **C07-04.M03.05** [revisión acotada](c07-04-m03-05-commercial-isolated-review-batch.md): lote acotado y recibos no privilegiados; no es worker hosted, cola duradera, registro de cobro ni integración de proveedor.
- **PLAN_MAESTRO!E** es la meta acumulativa de fase (pedidos, cotizaciones, facturas, caja, pagos, anulaciones y exportaciones). No se interpreta como obligación cumplida por esta unidad documental. La salida atómica de **M06.03** es este registro de límites, riesgos y decisiones, con readback y A5 por otra Task.

## Matriz de límites y riesgos pendientes

| Frente | Evidencia existente | Lo que no se ha certificado en esta microtarea | Decisión/gate |
| --- | --- | --- | --- |
| UI/teclado/responsive | Suites Chromium de M05, 15/15 en SHA indicado | Dispositivos físicos, métricas de campo LCP/INP/CLS, navegadores distintos, flujos E2E facturados | Mantener resultados como pruebas sintéticas/CI; no extrapolar a experiencia real de clientes |
| Identidad y autorización | Guard/filtro in-process, negativas con mocks y rutas Angular reales | JWT firmado + membresía efectiva/revocación consultada al servidor + tenant A/B sobre recursos comerciales por HTTP | Fallar cerrado si falta claim/rol/empresa; exigir matriz adversarial HTTP+JWT real en unidad técnica correspondiente |
| Pedidos e idempotencia | Índice y transacción en MySQL 8.4 efímero, claves repetidas A/B | Reserva/compromiso de inventario, concurrencia de negocio, reintentos fuera de un solo proceso y consistencia multioperación | No ejecutar efectos comerciales por recibo de preflight; revalidar autorización y clave durable en límite transaccional |
| Facturación/caja/pagos | Contratos/documentos y adaptador de lectura acotado | PSP, webhooks firmados, reversión de cargo, conciliación, evidencia fiscal legal y caja real | R3 NO_GO para operaciones reales; requerir autorización separada y proveedor accesible, nunca fingir su certificación |
| Worker e integraciones | Batch in-process máximo 16 | Cola persistente, dead-letter, retries transaccionales, disponibilidad de proveedor externo | No nombrar «worker operativo» ni otorgar éxito de negocio a una lectura previa |
| Observabilidad/rollback | Documento de trazabilidad, GitHub SHA, registros de CI y BITACORA | Restauración comercial de transacciones reales, auditoría externa o recuperación de datos productivos | Rollback de cambios documentales por revert normal en dev; nunca forzar HEAD ni borrar registros históricos |

**Riesgos abiertos:** P1/P2 si se confundieran mocks de permisos con autorización de servidor, éxito de pruebas de UI con pago realizado, o éxito de preflight con transacción idempotente. La severidad definitiva de cada incidente se determina con evidencia de falla; este cuadro no inventa defectos ni los declara cerrados. La frontera crítica permanece en el backend y en la persistencia real.

## Decisiones y próxima evidencia causal

1. No cambiar API, DTO, UI, EF, MySQL, autenticación, RBAC, estados del negocio, secrets ni proveedores con esta microtarea. Ningún contrato adicional queda autorizado implícitamente.
2. Las pruebas in-process, Chromium con mocks y MySQL efímero se etiquetan con exactitud. Para ampliar a certificación comercial se requieren pruebas end-to-end con JWT firmado, tenant A/B, revocación, idempotencia durable, cancelación y rollback, además de evidencia fiscal y de proveedor **solo donde el alcance de la microtarea futura lo exija**.
3. Mantener `COLA` y `DAG_TECNICO` derivados por fórmulas; no liberar M06.06/QA/PROD por este documento. A5 genuino por Task Javier distinta de DEV05, sin autoaprobación.
4. Revalidar HEAD y hashes antes de cualquier cambio posterior; cambios de código deben tener pruebas contra regresiones negativas y CI exact-SHA. Ante discrepancia CI, conservar el estado real y registrar defecto/limitación, nunca transformar SKIPPED/IN_PROGRESS en PASS.

## A1–A5 de esta unidad documental

- **A1 (consistencia):** comprobar que fuentes enlazadas, ID y separación entre evidencia dirigida y aceptación de fase coinciden con el árbol `dev` y el Sheet vivo. No afirmar pruebas ejecutadas por el mero hecho de escribir este documento.
- **A2/A3:** NO_APLICA a **este diff documental** probar HTTP/JWT/MySQL/PSP/Chromium nuevamente; son límites comerciales pendientes descritos arriba, no aprobaciones técnicas ni sustitutos de pruebas futuras.
- **A4:** exigir `verify-solqaryn-project-scope` y V5 completados en `SUCCESS` para el commit publicado; readback de PLAN/COLA/BITACORA y trazabilidad del SHA. Si la CI sigue ejecutándose, conservar `VALIDANDO`.
- **A5:** revisión independiente real por otro TASK_ID Javier: confirmar que las referencias existen, que no se reduce un P0/P1, que las limitaciones se mantienen explícitas y que la evidencia no certifica fases ajenas.

**Rollback:** Git revert aditivo sobre el commit documental específico en `dev` tras cotejar HEAD y trabajo concurrente; mantener BITACORA histórica y el control de exclusión V17. **Prohibidos:** force-push, QA/main/PROD, pagos reales y secretos.
