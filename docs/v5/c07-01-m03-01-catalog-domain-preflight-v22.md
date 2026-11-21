# C07-01.M03.01 — Reglas puras para catálogo universal (V22)

**Alcance atómico PLAN D1022:** implementar reglas de dominio y validaciones server side. Riesgo R2 por ownership/tenancy, pero esta entrega es exclusivamente una nueva regla pura de dominio y pruebas xUnit; no cambia la autorización de API, no agrega endpoints, EF, migraciones, pricing vigente ni acciones sobre producción.

## A0. Componentes y riesgo
- Nueva clase `backend/src/Domain/AutomationV5/C0701CatalogDomainRules.cs`: valida contexto verificado en servidor, producto, versión, sucursal, categoría y variante del mismo tenant, tipo producto/servicio, código SKU canónico, nombre seguro, dinero decimal de dos posiciones, tasa expresada explícitamente en basis points (0..10000), relaciones activas y denegación de productos antiguos sin EmpresaId.
- Nueva suite `backend/tests/Solqaryn.Tests/Domain/C0701M0301CatalogDomainRulesTests.cs` con 10 pruebas y negativos de cliente/tenant A/B, sucursal, categorización, variante, permisos revocados, estado, concurrencia CAS, datos adversos, redondeo y tipos.
- No cambios en `Producto`, `ProductoVariante`, servicios existentes, tablas, contratos JSON ni reglas activas. No afirmar integración con el runtime: las pruebas corren clase de dominio real, no HTTP/JWT/MySQL.

## A1. Casos y política fail-closed
El retorno positivo solo significa `DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED`, sin capacidad para conceder lectura/escritura. Las decisiones denegadas tienen códigos diferenciados; ningún resultado habilita API/DB. Producto legacy sin atribución se rechaza, no se asigna tenant por inferencia. Para cualquier futura operación real, Application/API deben revalidar JWT firmado, membresía/rol relacional, ownership de recurso y versión dentro de transacción MySQL.

## A2–A3. Frontera auténtica
La lógica no verifica un JWT ni ejecuta MySQL: A2 de las reglas puras se comprueba mediante aserciones negativas tenant/RBAC/versión; integración de JWT/HTTP/Oracle EF/MySQL no aplica **a este diff sin consumidor runtime**, no se declara PASS ni se simula. Los hitos M03.02–M03.06 deberán conectar DTO, API, EF y gates de pruebas con datos efímeros reales. Antes de toda activación, probar resistencia a stale membership y carreras de inventario/precios. La matriz futura Mxx sigue pendiente.

## A4 / trazabilidad
Publicar commit fast-forward CAS sobre `dev` y full `CHANGELOG_AI.md` preservado, y validar `dotnet test` de la suite en GitHub-hosted, `verify-solqaryn-project-scope`, procedencia V5, backend Release y seguridad pertinente en SHA exacto. Ningún required QUEUED/IN_PROGRESS/SKIPPED/FAIL implica PASS. Con CI pendiente conservar H=VALIDANDO, autor DEV03, SHA/run y handoff sin cerrar LISTO. A5 debe efectuarla otra Task Javier independiente, jamás autor. Rollback por `git revert` normal con HEAD revalidado; no forzar ramas, no cambiar QA/main/PROD, pagos ni secretos.
