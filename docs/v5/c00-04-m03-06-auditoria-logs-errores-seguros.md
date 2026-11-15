# SOLQARYN V5 — C00-04.M03.06: auditoría y errores seguros de gobierno

**Alcance:** implementación real .NET 10 en Application/Governance con xUnit; autor Task Javier DEV06. Fuente autorizada: Standard V5 y PLAN_MAESTRO/COLA/DAG, únicamente rama dev y Scope Lock STRICT.

## A0 — Componentes, fronteras y riesgos

Se incorpora GobiernoCambiosAuditoriaService, envoltura estrictamente inerte del servicio de autorización C00-04.M03.03. Reutiliza la verificación servidora de tenant y RBAC relacional; no acepta decisiones client-side, no concede permisos ni ejecuta cambios. No se registra en DI ni se expone HTTP/endpoint. Sin SQL/EF, migraciones, worker, Tasks, webhook, eventos, API, Angular, proveedores, secretos o producción. El logging es estructurado en proceso, no demuestra entrega durable, integridad institucional ni auditoría de compliance en un sistema vivo.

Consumidores: exclusivamente llamadas .NET futuras explícitamente autorizadas. No modifica contratos previos ni su comportamiento. Capas: Application usa Domain y Microsoft.Extensions.Logging, sin referencia inversa a Infrastructure. El rollback es revert ordinario del changeset tras releer HEAD, preservando la historia institucional.

## A1–A3 — Contrato y pruebas causales

La envoltura exige CorrelationId válido y logger habilitado antes de consulta. Para una preparación legítima invoca el guard de pertenencia/permiso actual y registra evento 4606 con código de resultado de lista cerrada, sin elevar PENDIENTE_AUTORIZACION a autorización. Para denegaciones por tenant, revocación o entrada malformada, registra evento 4607 con código fijo y propaga la excepción, sin copiar texto de error, body, motivo, JWT, idempotency key, EmpresaId ni PII a los logs.

Nueve pruebas xUnit cubren éxito inerte, tenant A contra B antes del I/O, membresía revocada, cambio sensible NO_GO, entrada forjada, logger deshabilitado fail-closed, excepción del sink que impide devolver éxito, correlación vacía y dependencias nulas. Los tests son aislados con snapshots en memoria: **NO** prueban persistencia de logs, conectores, MySQL, integración HTTP real ni CAS distribuido.

Seguridad A2: denegación por defecto, ausencia de filtración entre empresas, no confundir un log exitoso con aprobación ni evidencia A5. Integración A3: servicios de Application con el guard de identidad existente y ILogger; no crea frontera adicional ni cambia código productivo en DI.

## A4–A5 — Gates y reversión

Requeridos: backend net10.0 build/xUnit, seguridad multiempresa y autorización, Scope Lock y procedencia CHANGELOG en SHA publicado exacto. Un job en curso/cancelado/omitido/fallido no acredita PASS. Tras publicar en dev, mantener VALIDANDO hasta comprobar CI causal y readbacks de PLAN, COLA, DAG y BITACORA; solo entonces LISTO_DEV. A5 independiente por una Task Javier distinta de DEV06. Prohibido QA/main/PROD, datos productivos y migraciones.

Reversión: git revert normal de archivos de servicio, pruebas, documento y CHANGELOG, más evento institucional compensatorio si fuera preciso; no borrar BITACORA ni reescribir historial.
