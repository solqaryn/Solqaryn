# SOLQARYN V22 — C04-02.M03.01: reglas de dominio contractual, sin efectos económicos

**Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R2 por contratos, tenancy y potencial exposición económica. **Task autora:** JAVIER DEV01, `6adbb9fff1548191a1bd5b30d9e9984a`. **PLAN:** fila 626, acción D «Implementar reglas de dominio y validaciones server side». **Salida de esta microtarea:** una máquina de estados contractual y validaciones concretas en Domain, con regresiones xUnit negativas; la meta acumulativa de M03 en PLAN E no representa que toda M03 ya esté implementada.

## Alcance y frontera real

Se incorpora `backend/src/Domain/AutomationV5/C0402ContractDomainRules.cs` (sin EF, API, migración ni integración de proveedor) y la suite `backend/tests/Solqaryn.Tests/Domain/C0402M0301ContractDomainRulesTests.cs`. Las reglas no reutilizan ni alteran `Suscripcion` técnica, `Plan`, JWT, roles, UI o servicio existente. No hay firma, pago, renovación automática, suspensión real, refund, webhook, outbox, worker ni llamada a proveedor. El método `Evaluate` emite exclusivamente un código de precondiciones; **todas** las decisiones mantienen `AuthorizesMutation=false`, `AuthorizesPaymentOrRefund=false` y `CertifiesJwtHttpOrMysql=false`, incluido el camino positivo de una regla de estado. Esto evita que una prueba unitaria simule efectos o autorice por sí misma un contrato.

La arquitectura de Domain queda independiente de Infrastructure/API. M03.02 deberá introducir un servicio con fuente autenticada verdadera, membresía/RBAC consultada desde servidor, ownership contractual persistente y política comercial verificable. Las referencias de `VerifiedActor` y `CommercialEvidence` son snapshots solo para evaluación pura, **no** autoridades construibles desde body, ruta, cookie, flags ni Angular; ningún controlador actual los consume. `D01–D08` permanecen NO APROBADAS según M02.06. Este commit no las aprueba por representar `true` en fixtures de xUnit; en runtime actual no hay fuente aprobada que permita invocar cambios.

## Máquina de estados y oráculos

- Borrador → PendienteAceptacion solo con evidencia aprobada D01+D02, sin firma efectiva.
- PendienteAceptacion → Activa solo con D01+D02, sin activar suscripción técnica ni cargo.
- Activa → Suspendida y Suspendida → Activa solo con D05 y conciliación verificada; timeout o callback no prueban mora.
- Activa/Suspendida/PendienteAceptacion → Cancelada solo con D07+D08; no implica devolución ni eliminación de historial.
- Activa → Expirada únicamente al fin UTC documentado; Cancelada/Expirada siguen terminales.
- Activa → propuesta de renovación solo con D04 y período futuro comprobable; `RENEWAL_PROPOSAL_ONLY_NO_EFFECT` nunca altera vigencia ni factura.

Las validaciones fallan cerrado por contexto nulo, actor sin JWT verificado, miembro/rol inactivo, permiso revocado, EmpresaId/ContractId cruzado A/B, estado u operación desconocidos, versión esperada obsoleta, instante no UTC, intervalo invertido, retroactividad y causa vacía, desbordada o con caracteres de control. Se exige baseVersion exacta; el control CAS transaccional real se implementará en M03.04 con MySQL efímero y no está acreditado por este diff.

## A0–A5 y calidad por causa

**A0:** requerimiento y diff concretos M03.01; Domain solo define reglas independientes, tests xUnit y esta evidencia. Fuente institucional Doc V5, `PLAN_MAESTRO!A626:M626`, `COLA!G626/O626`, `DAG_TECNICO!A587/G587`; se obtuvo V17 `SQV17_C04_02_M03_01` ID `2000000626` y CAS de `PENDIENTE→EN_PROGRESO` con `occurrencesChanged=1`, readback del autor DEV01. No se reclama ningún otro scope mientras persista ese WIP.

**A1:** xUnit con positivo de precondiciones sin efectos, múltiples negativos de versión, estados inválidos, UTC, retroactividad, actor, petición y límites. Una suite exitosa evidencia únicamente esta unidad de reglas, no firma ni cumplimiento de toda la fase.

**A2:** rechazo explícito A/B, principal revocado, JWT/RBAC/membresía ausentes y ausencia de autorización de escritura. **Limitación:** los booleanos de fixtures no prueban JWT real, política comercial aprobada ni lectura segura de otro tenant; M03.03 debe integrar API autenticada y pruebas negativas verdaderas.

**A3:** `NO_APLICA` a MySQL 8.4, HTTP/servidor, Angular/Chromium, SMTP y proveedores, porque este diff modifica solamente código Domain inerte y su suite unitaria, sin ruta, ORM ni frontend. **No se certifica integración** y no se usan mocks para afirmarla. Cuando M03.04 toque EF y DB, exigir Category=Integration real en MySQL 8.4 efímero; cuando M03.03 toque auth, exigir JWT/RBAC tenant A/B autenticados.

**A4:** publicar commit fast-forward por CAS sobre HEAD dev; verificar checks `verify-solqaryn-project-scope`, procedencia V5 y build/tests backend pertinentes con `status=completed/conclusion=success` del SHA causal antes de pasar a LISTO. `QUEUED`, `IN_PROGRESS`, `SKIPPED`, `CANCELLED` o `FAIL` de checks requeridos nunca son PASS. Registrar `BITACORA` idempotente y readback PLAN/COLA/DAG sin alterar sus fórmulas.

**A5:** PENDIENTE tras A0–A4, por otra Task JAVIER distinta de la autora y de cualquier reparadora, con revisión real; nunca autoaprobación. No autoriza QA, main, PROD o decisiones D01–D08.

**Rollback:** `git revert` ordinario solo de este changeset tras releer HEAD y writers; no force-push, no revertir cambios ajenos y no borrar BITACORA ni tocar datos/contratos efectivos.

## Continuidad

Si CI del SHA publicado sigue externa en curso, conservar `VALIDANDO` con OWNER histórico DEV01, SHA, job exacto, fallos si existiesen y siguiente acción de A4 en PLAN/BITACORA. Sólo dar `LISTO` mediante lock propio, CAS y readback tras A0–A4 pertinentes; después A5 independiente. Las siguientes M03.02–.06 no se liberan por `VALIDANDO`, ni por una certificación de dominio aislada.
