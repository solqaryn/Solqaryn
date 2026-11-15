# SOLQARYN V5 — C01-04.M03.01: implementación de reglas Domain para expediente universal

**Task autor:** PRIMARY DEV 06 JAVIER (`6af16bbad26881918c283abe06e1f32a`). **Repositorio/rama:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT.

## Alcance y predecesoras
El expediente universal conecta cronología, observaciones, decisiones, adjuntos y versionado. M02.01–06 están LISTO y A5 independientes; M02.06 (autor DEV01) revisado por esta Task sobre SHA `3c414455d6440d0eb6d6ff0f52c2424bbf1f6d2f`, blob `aeaa8a78b7942033959f823aeb82d5e8a1041528`. La fórmula reactivó COLA G/O=SI y DAG F=VALIDADO/G=SI para M03.01. Claim por CAS de PLAN H266 e I266 Dev06; readback realizado.

## Código ejecutable en Domain
- `V5UniversalCaseFileDomainRules` aporta decisión pura **fail-closed** basada en una autoridad *server-verified*, snapshot de propiedad persistida, sucursal, versión y acción solicitada.
- Casos: `ReadTimeline`, `AddObservation`, `RecordDecision`, `LinkAttachment`. Deniega membre­sía revocada, permiso obsoleto, tenant A consultando B por ID, legado con owner NULL, selección de tenant desde cliente, sucursal ajena, expediente archivado en escritura, versión obsoleta, clave idempotente ausente, razón de auditoría inexistente, decisión sin revisión y adjunto no verificado o enlace expirado.
- Respuesta `READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT` es **requisito previo**, no permiso. No crea tabla ni línea de tiempo persistida, contrato API, migración MySQL, archivo, emisión de eventos o endpoint. Concurrencia CAS, dedupe y redacción de PII necesitarán transacciones y pruebas de integración en M03 posteriores.
- xUnit `V5UniversalCaseFileDomainRulesTests` define positivos y negativos del contrato, sin fingir pruebas HTTP/EF/MySQL.

## A0–A5
A0: nuevo Domain puro y xUnit, sin depender de Infrastructure. A1: aserciones xUnit de estados y versiones, sujetas al backend CI. A2: negativos tenant A/B, JWT/rol, datos ajenos, sucursal y adjuntos, sin pentest real. A3: integración con backend net10.0 validada por GitHub Actions exact-SHA, sin conectar API/MySQL. A4: commit fast-forward CAS HEAD con Domain/test/doc/CHANGELOG, Scope Lock y control V5 exact-SHA, CI backend/security causal, readback PLAN/COLA/DAG/BITACORA. A5: otra Task Javier, no la autora.

Rollback mediante `git revert` ordinario con nuevo CI, manteniendo BITACORA append-only. No QA/main/PROD, secretos, proveedores de pago, datos de clientes, workers, dispatcher, migraciones ni cambios IAM runtime.
