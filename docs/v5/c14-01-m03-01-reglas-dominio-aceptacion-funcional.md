# SOLQARYN V5 - C14-01.M03.01: reglas de dominio de aceptacion funcional

Codigo real Domain .NET 10 + xUnit en rama dev, autor DEV06. Fuente Standard V5 y PLAN/COLA/DAG con Scope Lock STRICT; A5 independiente posterior.

## A0, impacto y no alcance

Se introduce V5FunctionalAcceptanceDomainRules como validador puro de evidencia de aceptacion funcional, no motor de negocio. Requiere contexto servidor verificado (actor, tenant, membresia, RBAC y propiedad persistida, SHA exacto), catalogos aprobados por backend, caso/accion, referencia saneada, evidencia positiva/negativa, rollback e idempotencia. Solo devuelve READY_FOR_FUNCTIONAL_REVIEW; no autoriza lectura tenant, cobros, mutaciones, QA/main/PROD o certificacion de release.

No se conecta JWT real, Application, Infrastructure, EF/MySQL ni endpoint. No activa proveedor de pago, worker, webhooks, eventos, flags, Angular, Playwright, migraciones ni datos reales. Domain permanece independiente de infraestructura y no se altera modelo actual.

## A1-A3, comportamiento y riesgos

xUnit 11 escenarios reales de logica pura: positivo sin permiso nuevo; negativo cruce tenant A/B; revocacion/rol/ownership ausente; commit obsoleto; caso o accion no allowlist; evidencia con newline; PII sin censura; peticion de efecto productivo; positivo/negativo incompleto; rollback/idempotencia faltantes; autoridad o catalogo nulos. No atribuir estas pruebas a integracion HTTP/MySQL, comportamiento real del PSP ni browser. Ninguna propiedad booleana suplanta comprobaciones del backend, que requieren un adaptador confiable independiente. A2 tenant A/B fail closed y proteccion de PII; A3 Domain puro sin side-effects.

## A4-A5 y rollback

Git commit fast-forward CAS exact HEAD, diff solo Domain+xUnit+doc+CHANGELOG. Requeridos CI backend NET10 xUnit, seguridad multiempresa, Scope Lock y check V5 completados en success del SHA aplicable; cancelled/skipped/in progress/failure no PASS. Con checks pendientes conservar PLAN VALIDANDO, owner DEV06, BITACORA y ultimo SHA. A5 por Task Javier distinta; QA/main/PROD requieren permisos adicionales. Reversion con git revert normal y evento aditivo si se precisa, sin modificar BITACORA historica.