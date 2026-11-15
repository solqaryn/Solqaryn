# SOLQARYN V5 — C07-04.M03.03 autorización fail-closed de documentos comerciales

**Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076`. Rama exclusiva `solqaryn/Solqaryn@dev`, Scope Lock STRICT. Referentes: STANDARD V5 FINAL, PLAN/COLA/DAG/BITACORA institucional, Skill canónica, Domain `V5CommercialDocumentDomainRules` M03.01, Application `V5CommercialDocumentApplicationService` M03.02. No se usan cuentas ni Tasks fuera del allowlist Javier6.

## A0: cambio real y análisis de impacto

`V5CommercialDocumentAuthorizationGate` es un nuevo componente de Application .NET 10 que exige `ServerAccess` de un adaptador backend futuro, con identidad autenticada, membresía/rol vigente, propiedad comprobada en persistencia y tenant exacto. A diferencia de M03.02, el `ClientCommand` **no contiene** `TrustedContext` ni `PersistedSnapshot`: se inyectan únicamente desde `ServerAccess` recibido por frontera backend confiable. Comprueba actor y tenant coincidentes y deniega propietarios nulos de datos heredados o documentos pertenecientes a B cuando el actor es A. Luego delega la evaluación de estado, operación, importe, moneda, idempotencia y control de proveedores/fiscalidad en M03.02/Domain. NO crea su adaptador real, ruta HTTP, DI, query MySQL, firma JWT, rol de servidor, grants, autorización de pago, facturación ni permisos de ambiente.

## A1: pruebas xUnit reales

Nueve tests [Fact] prueban positivo estructural sin habilitar efectos; denegación por identidad nula/revocada/rol insuficiente; tenant B solicitado por cliente; actor o tenant distintos entre autoridades backend; propiedad histórica nula o de tenant extranjero; operación sin grant y versión obsoleta; SHA y evidencia insegura; moneda/importes y huella de idempotencia inválidos; y flags PSP/fiscal no válidos. Ninguna aserción infiere pagos, transacciones o autorizaciones reales. El resultado positivo `DURABLE_COMMERCIAL_ADAPTER_AND_A5_REQUIRED` exige otra implementación y revisión.

## A2: límites de seguridad

`ServerAccess` **debe** resolverse en código backend autenticado con lectura actual de membership, ownership y grants; su booleana no garantiza por sí sola JWT, RBAC o controles de BD. La gate es de solo lectura y siempre reporta `AuthorizesMutation`, `AuthorizesCrossTenantRead`, `AuthorizesPayment`, `AuthorizesFiscalIssuance` y `AuthorizesRelease` en `false`. El cliente nunca configura la identidad ni el contexto de permisos, y operación A no se transforma en efecto en tenant B. Todo error o recurso desconocido produce DENY sin log de secretos.

## A3: integración causal

Se integra sólo en C# Application -> M03.02 -> Domain; ejecuta xUnit en GitHub Actions hosted .NET 10. Son obligatorias compilación/tests backend, seguridad multiempresa y controles Scope Lock/procedencia V5 sobre SHA exacto. Autenticación de endpoint real, MySQL/EF, PSP, comprobantes fiscales, JWT y proveedores live son NO_APLICA a esta entrega y NO quedan certificados; serán tareas futuras autorizadas.

## A4/A5: publicación y reversión

Commit único fast-forward/CAS directo a dev con servicio, xUnit, documento y una sección autocontenida CHANGELOG. Entrar VALIDANDO antes de A1–A3 y conservar owner durante cualquier CI in_progress o cancelada. LISTO_DEV requiere Scope Lock, procedencia V5, build/pruebas backend, seguridad y readback PLAN/COLA/BITACORA, no checks descartados. A5 requiere distinta Task Javier; no QA/main/PROD, endpoints, migraciones, PSP, workers, webhooks ni secretos. Rollback `git revert` normal, auditoría histórica inmutable.
