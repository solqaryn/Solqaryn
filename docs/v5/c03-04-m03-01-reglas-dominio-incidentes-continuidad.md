# SOLQARYN V5 — C03-04.M03.01 reglas de dominio para respuesta y recuperación

**Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Rama:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT.

## Preflight y alcance
El contrato de C03-04.M02.01–06 está LISTO y revisado A5 por Tasks Javier independientes; M02.06 fue publicado por DEV01 en SHA `433480dbbf36474ef680dc92a915798f459578e0`, blob `8f0cd1807b9230a5ead3a1ff3cd5cec3785380fd`, revisado por DEV06. Tras A5, el cálculo reactivo confirmó COLA G/O=SI y DAG F=VALIDADO/G=SI para M03.01. Se reclamó PLAN H554 por compare-and-replace y owner I554 Dev06 con readback.

## Implementación real, sin efectos externos
- `V5ContinuityIncidentDomainRules` es un preflight **.NET 10 Domain puro** para incidentes/contención/resolución. Exige JWT, membresía, permiso, ámbito tenant/sucursal comprobado en backend, propiedad persistida, versionado, evidencia saneada, razón de auditoría, idempotencia e independencia de decisión.
- Bloquea tenant A→B, datos sin propietario, sucursal ajena, revocación, logs inseguros, duplicados/versiones obsoletas y transición inválida de estado. Restauración incluso en sandbox requiere runner distinto y autorización adicional; el Domain siempre devuelve denegación de `RequestSandboxRestore`.
- `READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT` es sólo un requisito previo para un adaptador autenticado: no ejecuta revocaciones, backups, restore, SQL/MySQL, cambios de clave, incidentes ni certificación de continuidad. xUnit define pruebas positivas/negativas en `V5ContinuityIncidentDomainRulesTests`; las pruebas de Application/API y restore real son futuras.

## Validaciones y rollback
- A0: Domain/Tests sin cambios a Infrastructure ni JWT productivo. A1: CI .NET10 de xUnit positivo/negativo, pendiente de ejecución. A2: pruebas causales de aislamiento A/B, revocación, secretos y permisos; no certificación de penetración. A3: build/tests dirigidos del backend; no simular MySQL ni RPO/RTO. A4: fast-forward y CAS HEAD dev, diff Domain+tests+este documento+CHANGELOG, Scope Lock y V5 exact SHA y CI backend/security pertinente, readback PLAN/COLA/DAG/BITACORA. A5: Task Javier distinta de la autora.
- Rollback normal `git revert` con CI de nuevo SHA; BITACORA append-only. No eventos/webhooks, providers externos, datos productivos, QA/main/PROD, migraciones, reactivación de Tasks ni cambios de programación.
