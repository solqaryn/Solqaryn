# SOLQARYN V5 FINAL — C07-04.M03.01: reglas de dominio comerciales y validación server-side

**Fecha:** 2026-11-14 · **Autor:** PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` · **Repositorio:** `solqaryn/Solqaryn@dev` · **Scope Lock:** STRICT.

## Entrega real y dependencia

Implementación real y **puramente de dominio .NET 10** en `backend/src/Domain/AutomationV5/V5CommercialDocumentDomainRules.cs`, con pruebas xUnit en `backend/tests/Solqaryn.Tests/Domain/V5CommercialDocumentDomainRulesTests.cs`. No basta documentar M03: se añaden clases compilables, veredictos tipados y positivos/negativos automatizados.

Antes de reclamar: PLAN_MAESTRO fila 1130 PENDIENTE/I vacío, COLA G/O=SI, DAG_TECNICO fila 1091 F=VALIDADO/G=SI; el precedente `C07-04.M02.01–06` figura LISTO y con A5 verificadas. La reclamación de Sheets fue lectura + actualización + readback, **no CAS distribuido atómico**; un solo owner fue observado. La clase corresponde sólo al subalcance M03.01, no a API/pagos/PSP/migración en vivo.

## Reglas implementadas y límites

El contexto `TrustedContext` **debe construirse en backend autenticado**; jamás desde tenant, EmpresaId, rol o flag aportado por el navegador. `PersistedSnapshot` sólo proviene de lectura de persistencia autorizada, no de DTO cliente. Un candidato valida:

- Usuario y tenant autenticados, grant server-side específico para la operación.
- Empresa de documento, cliente y padre persistidos, todos positivos e idénticos al tenant confiable; NULL legado y recursos de tenant B denegados.
- `expectedVersion` igual a versión persistida, estados permitidos por operación y conflicto tipado para versión obsoleta.
- Importe decimal positivo, con escala monetaria máxima de dos decimales y coincidencia con el snapshot; moneda ISO alfabética mayúscula de tres letras e idéntica en comando.
- Clave de idempotencia imprimible, acotada a 128 caracteres y fingerprint hexadecimal SHA-256 de 64 posiciones; el almacenamiento durable del fingerprint y la resolución de carreras **siguen pendientes de Application/Infrastructure**.
- Emisor fiscal verificado para emisión y autorización previa de PSP para aplicar pago; no hay llamadas al PSP ni cobros reales.

Veredictos de rechazo: `TRUSTED_CONTEXT_REQUIRED`, `OPERATION_PERMISSION_REQUIRED`, `TENANT_OWNERSHIP_MISMATCH`, `STALE_DOCUMENT_VERSION`, `INVALID_DOCUMENT_AMOUNT`, `CURRENCY_MISMATCH`, `INVALID_IDEMPOTENCY_CONTRACT`, `DOCUMENT_STATE_CONFLICT`, `FISCAL_AUTHORITY_NOT_VERIFIED`, `PAYMENT_PROVIDER_NOT_AUTHORIZED`. El único veredicto positivo `VALID_FOR_BACKEND_TRANSACTION_REVIEW` **NO autoriza pago, emisión fiscal, operación HTTP, LISTO ni despliegue**.

## Tests positivos/negativos y alcance sin atribuciones ficticias

Pruebas xUnit para: confirmación válida, permiso denegado, contexto ausente, tenant A/B y NULL legado, conflicto de versión, valor monetario divergente/fraccionario, moneda, fingerprint/key inválidos, estado incoherente, emisor fiscal no comprobado, PSP no autorizado y positivos de operaciones sujetas a revisión posterior. Los tests corren en GitHub Actions contra .NET 10 en el SHA publicado; no atribuir PASS hasta `completed/success` de jobs backend/seguridad causales. No se ejecuta MySQL real, API HTTP, migración, transacción, compensación fiscal, inventario, exportación ni browser; esto no autoriza completar M03.02–06 sin gates.

## A0–A5 y rollback

**A0** análisis de Domain, no dependencias Infrastructure/API, sin secretos/DDL; **A1** xUnit positiva/negativa de veredictos; **A2** negación A/B, NULL, grants, PSP y fiscal; **A3** compilación .NET y suite de backend en CI, no equivale a HTTP/MySQL real; **A4** commit fast-forward/expected HEAD, verificación Scope Lock/V5 y pruebas pertinentes `completed/success` en SHA exacto, PLAN/COLA/BITACORA readback; **A5** revisión independiente obligatoria por otra Task Javier. Si CI requerida queda en cola, preservar VALIDANDO con owner y evidencia.

Rollback: revert Git normal, sin reescribir historia ni modificar BITACORA histórica. Ninguna operación sobre producción, QA/main, base viva, proveedores, DNS, webhooks o dispatchers.
