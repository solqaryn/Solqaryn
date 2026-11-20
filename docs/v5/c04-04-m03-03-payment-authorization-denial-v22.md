# C04-04.M03.03 - Aislamiento y denegacion de autorizacion de pagos V22

Repo solqaryn/Solqaryn rama dev, PROJECT_SCOPE_LOCK=STRICT. Owner DEV04. Riesgo R2: finanzas, JWT, tenant, RBAC. PLAN D700 exige implementar autorizacion y denegacion por defecto. PLAN E700 es objetivo acumulativo de fase M03, no validacion de PSP.

## Codigo real por capas, sin efecto financiero
- C0404PaymentAuthorizationGate es una politica Application deny-by-default con reader de autoridad servidor IC0404PaymentAuthorizationEvidenceReader. Ningun adaptador DI/API/PSP/EF nuevo, sin pagos o datos productivos.
- El cliente solo expresa tenant, factura y operacion CreateLink/Refund: nunca puede aportar rol, token, membresia, HMAC, cobro inmediato ni bypass. El snapshot servidor exige JWT validado criptograficamente por API, session actual, rol/grant vigente de persistencia, lectura tenant-fenced, propiedad real de factura, no revocacion, feature habilitada; refund exige permiso distinto y recibo/version ledger autenticados.
- La decision positiva habilita SOLO inspeccion de plan Application puro. Autoriza cero cobros, reembolsos, llamadas PSP, escrituras ledger o cambio de tenant. Falta de backend, excepciones, firma invalida, tenant B, factura ajena o sesion vencida deniega igual sin filtrar secretos.

## Gates honestos y transferencia
A0: clase, tests y workflow reales, sin conectar API ni infraestructura. A1: pruebas xUnit positivos/negativos/fronteras y excepciones falsables sobre el gate. A2: snapshots simulados, NO JWT firmado criptograficamente ni membresia real MySQL; no declarar seguridad completa. A3: adapter confiable de lectura JWT/RBAC/tenancy con API HTTP y MySQL 8.4 efimero, pruebas A/B y revocacion reales PENDIENTE. Este componente queda VALIDANDO, no LISTO ni A5, hasta probar A0-A4 completos y CI real en SHA causal. A4: GitHub-hosted .NET 10 Release, xUnit TRX, verify Scope Lock, V5, backend y seguridad; SKIPPED/QUEUED/FAIL nunca PASS. A5: Task Javier distinta de autor y reparador.
Rollback por git revert ordinario en dev; sin force-push, QA/main/PROD, secrets, PSP, cargo real ni certificacion de fase.

## V22 — reparacion adversarial de cancelacion en vuelo (19-nov-2026)
La politica Application relee CancellationToken despues de await ReadCurrentAsync: si un reader lento ignora la cancelacion y devuelve un snapshot previamente valido, ya no se entrega READONLY_PAYMENT_PLAN_PREFLIGHT_ONLY. El xUnit nuevo cancela al terminar la lectura de un snapshot positivo y exige OperationCanceledException.
A0/A1: diff minimo y asercion adversarial. A2: limite de cancelacion; NO certifica JWT/RBAC real. A3: adapter de membresia y rol MySQL 8.4 efimero, JWT firmado y HTTP real siguen PENDIENTES; mantener H700 VALIDANDO y A5 independiente PENDIENTE. A4: CI exact SHA GHA .NET10, Scope Lock, V5 y seguridad pertinente deben aprobar antes de acreditarse. Sin PSP/cobros, QA/main/PROD. Rollback: git revert ordinario.
