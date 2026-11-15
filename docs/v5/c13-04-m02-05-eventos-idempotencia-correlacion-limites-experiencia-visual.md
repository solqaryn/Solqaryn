# SOLQARYN V5 — C13-04.M02.05: eventos conceptuales, idempotencia, correlación y límites de auditoría visual

**Fuente ejecutable:** STANDARD V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO institucional (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`), Skill canónica y `docs/PROJECT_SCOPE_LOCK.md`. **Autor:** PRIMARY DEV 02, Task `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Rama:** `solqaryn/Solqaryn@dev` exclusivamente.

**Frontera:** contrato M02 de diseño de auditoría visual. No modifica Angular, SCSS, API, MySQL, tablas, migraciones, permisos JWT/RBAC, tenant, proveedores, webhooks ni entornos QA/main/PROD. Los casos descritos son `FUTUROS_NO_EJECUTADOS`; no acreditan WCAG, integración, aislamiento A/B ni rendimiento medido. Requiere A5 independiente de una Task Javier distinta de la autora.

## 1. Decisión de diseño: ningún evento activo

El alcance presente es **exclusivamente un contrato futuro** para observaciones de regresión visual en CI o un reporte autorizado. No existe autorización para desplegar webhook, dispatcher, cola persistente, scheduler, telemetría externa, consumidor HTTP o reactivación de otras Tasks. No se modifica configuración de proveedores ni se introduce flujo continuo entre automatizaciones. La matriz de M01.01/M01.05 es la única referencia de aceptación preparada, con pruebas de navegador futuras no ejecutadas.

| Mensaje conceptual | Productor futuro | Consumidor posible | Efecto expresamente no autorizado aquí |
| --- | --- | --- | --- |
| `ui.audit.case.started.v1` | runner CI autorizado | agregado interno de pruebas | No desbloquear DAG ni iniciar Tasks |
| `ui.audit.observation.v1` | prueba Playwright sintética | artefacto de resultados efímero | No almacenar PII/JWT ni copiar a tenant ajeno |
| `ui.audit.case.completed.v1` | runner tras oráculo efectivo | resumen reproducible por SHA | No marcar PASS si test fue cancelado |
| `ui.audit.regression.flagged.v1` | comparador SHA-baseline | revisión humana autorizada | No abrir cambios fuera de dev ni ejecutar rollback solo |

Todos los tipos anteriores son **nombres de propuesta**, no tipos TypeScript/C# existentes ni mensajes emitidos. Un cambio futuro requiere alcance separado, permiso, límites del proveedor, observabilidad y pruebas de idempotencia.

## 2. Envelope mínimo propuesto y ownership

| Campo | Forma conceptual | Regla de seguridad |
| --- | --- | --- |
| `schemaVersion` | `1` | Rechazar versión incompatible; migración explícita futura |
| `eventId` | UUID no reutilizable | No suplantar RUN_ID nativo de una Task |
| `idempotencyKey` | hash determinista de `sourceSha+caseKey+attempt` | Dedupe mismo intento y evitar colisiones entre casos |
| `correlationId` | UUID por escenario sintético | Propagar sólo dentro de frontera de CI permitida |
| `sourceSha` | 40 hex del commit inspeccionado | No aceptar captura de otro SHA como PASS |
| `caseKey` | ID de caso del catálogo auditable | No URL con query sensible ni dato personal |
| `tenantAlias` | A/B ficticio en fixtures | Separación A/B; no ID real de cliente en logs públicos |
| `viewportKey` | perfil UI de fixture | No reemplaza identidad ni permiso |
| `outcome` | `pass,fail,not_run,cancelled` | Sólo pass acreditado permite reporte positivo |
| `evidenceRef` | identificador de artefacto depurado | Rechazar secretos, PII y rutas externas no autorizadas |

## 3. Idempotencia, errores y límites no negociables

- D01: dos entregas con mismo `idempotencyKey` se procesarían una vez; conflicto de payload con misma clave se rechaza y registra.
- D02: resultados de SHA diferentes nunca se mezclan salvo comparación explícita verificada de equivalencia de artefactos.
- D03: `cancelled`, `skipped`, `in_progress`, `not_run` y fallo de timeout nunca equivalen a PASS.
- D04: clave de correlación no es credencial ni permiso; backend autoriza cualquier consulta futura con tenant validado y RBAC.
- D05: reintentos futuros requerirían presupuesto limitado, backoff y operación idempotente; no bucle ilimitado.
- D06: cuotas, tamaño máximo, retención y periodo de purga requieren medición y aprobación previa; no inventar umbrales productivos.
- D07: error de almacenamiento resulta en `not_run` o evidencia incompleta, nunca éxito silencioso.
- D08: métricas y capturas sólo contienen datos sintéticos redactados; nunca JWT, tokens o identificadores privados.
- D09: soporte no puede emitir aprobaciones A5 ni transiciones PLAN por recepción de un mensaje de evento.
- D10: aislamiento incluye claves y archivos por run/case/tenant sintético; invalidar al cambiar contexto A/B.
- D11: consumo futuro debe ser explícito y auditado; no instalar broker, webhook ni worker en esta microtarea.
- D12: duplicados y reordenamiento deben tener pruebas negativas con eventos fuera de secuencia y misma clave.

## 4. Plan de pruebas futuras (no ejecutado)

| Positivo futuro | Negativo obligatorio futuro |
| --- | --- |
| Doble envío de mismo caso produce un resultado idempotente | Duplicado altera contador o certificación |
| Caso aislado A y B no comparten correlación | Salida B contiene dato o tema A |
| CI completed/success con oráculo real tiene evidencia SHA | CI cancelled/skipped se computa PASS |
| Timeout devuelve estado pendiente y próxima acción clara | Falla de red desencadena reintentos infinitos |
| Mensaje sin PII llega depurado | Captura incluye token, cookies o correo real |
| Cambio de versión aplica validación de schema | Versión desconocida se deserializa sin rechazo |
| Evidencia anulada conserva historia append-only | Se sobrescribe evidencia previa o BITACORA histórica |
| Ausencia de consentimiento/permiso deniega lectura | Alias o correlación se usa para eludir RBAC |

A0: contrato delimitado al documento. A1: pares positivos/negativos futuros definidos, no ejecutados. A2: amenazas de correlación, datos sensibles y tenant identificadas, no certificadas runtime. A3: ninguna integración instalada (NO_APLICA justificado). A4: comprobar archivo, CHANGELOG, Scope Lock y V5 en SHA exacto; si CI requerida no está completada en success, conservar VALIDANDO. Rollback: revert Git de este Markdown; ningún mensaje fue emitido o consumido. **A5 únicamente por revisor Javier independiente.**
