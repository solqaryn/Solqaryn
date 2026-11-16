# SOLQARYN V18 — C11-04.M05.03: integración real ILoggerProvider en proceso

**ID:** C11-04.M05.03. **Autor:** PRIMARY DEV 03 JAVIER, Task ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Rama:** dev. **Lock:** SQV17_C11_04_M05_03 (2000001720). **Riesgo:** R2 (tenancy, auditoría, privacidad y proveedor de diagnóstico).

## A0 — alcance y dependencias
Se comprueba la frontera real entre el sink in-process existente `C1104ObservabilityStructuredAuditSink` y el **pipeline de Microsoft.Extensions.Logging** mediante `LoggerFactory` y un `ILoggerProvider` de prueba que recibe y examina eventos estructurados. Se reutilizan los contratos y controles de M03.05/M03.06, sin añadir runtime, registros de DI de producción, exportadores, agentes externos ni acceso a telemetría privada. El proveedor de prueba está activo dentro del proceso de xUnit en GitHub-hosted, no es Cloudinary, Aiven, SMTP ni un collector externo.

**Persistencia EF/MySQL y proveedor externo: NO_APLICA** porque este changeset no modifica DbContext, migraciones, tablas, conexión, adaptador de almacenamiento ni redes. No se atribuye integración MySQL 8.4, HTTP/JWT real, SLO medido, logging durable, exportación ni dashboards. Los valores de tenants A/B y autenticación son fixtures sintéticos, no pruebas de usuarios reales.

## A1–A3 — oráculos adversariales
Los tests xUnit ejecutan realmente el pipeline LoggerFactory -> Logger -> ILoggerProvider capturador:
- Positivo: EventId 110406, nivel Information, campo AuditCode allowlisted, InspectedCount dentro de 1..16, plantilla constante; cero labels de tenant/actor/SHA/JWT y no grants de acceso.
- Negativo: rechazo de tenant B frente A, membresía revocada, permiso RBAC revocado, barrera de auditoría revocada y SHA observado discrepante, sin emisión de log.
- Frontera: conteos -1/0/17, código falsificado, denegación con conteo positivo, sin llamar el puerto de autoridad.
- Error: LoggerFactory con filtro Warning deshabilita el Info esperado y debe devolver rechazo opaco; fallo del puerto devuelve error fijo sin excepción privada en evento; cancelación anterior al I/O no emite nada.
- Denegación segura de código conocido produce Warning con el mismo EventId y campos tipados.

La integración es **real en proceso** con Microsoft.Extensions.Logging; un fake de autoridad solamente suministra la evidencia de seguridad para evitar permisos y datos productivos. Requiere ejecución dirigida en GitHub-hosted de este archivo y build Release, además de seguridad/autorización, Scope Lock y procedencia V5 en SHA exacto. Un build verde sin ejecutar la suite no valida A1–A3.

## A4 / A5 y recuperación
Comprobar checks completos `completed/success` por SHA, alcance y readback PLAN/COLA/DAG/BITACORA. Si CI sigue QUEUED/IN_PROGRESS dejar VALIDANDO con RUN/SHA y priorizar REVIEW_FIRST en próxima corrida, sin inventar PASS. Una Task Javier distinta revisa código, tests y requisitos para A5, nunca la autora. No se desbloquean sucesoras técnicas por VALIDANDO. Rollback: `git revert` normal del changeset preservando evidencia; sin QA, main, PROD, secretos ni recursos de pago.
