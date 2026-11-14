# Certificación current-standard — N8.11 Seguridad

Fecha de revalidación: 2026-09-17
Rama autorizada: `Desarrollo`
Autoridad operativa: `docs/VAEP_AUTHORITY.md`

## Resultado material

N8.11 fue revalidado contra el estado actual de Desarrollo sin confiar automáticamente en cierres históricos. Las etapas A–G requeridas antes de esta certificación fueron drenadas secuencialmente; para SEC_AUDIT y TEST_CI se ejecutó REVIEW_FIRST fresco con P0=0 y P1=0.

La superficie de seguridad vigente conserva aislamiento tenant y autorización fail-closed, protección del último administrador activo, resolución de IP de cliente con frontera explícita de proxy confiable para rate limiting, endurecimiento de secretos/configuración y observabilidad/auditoría con redacción de datos sensibles.

## Evidencia causal vigente

- Functional head probado: `6fd3e28cbf28164d110d6b83756b9094cec654a6`.
- Priority 3 — ERP / security / observability: run `35258962286`, SUCCESS.
- Priority 4 — Scale / multi-branch / operations: run `35258969104`, SUCCESS.
- Auditoría de configuración y dependencias: run `35233179389`, SUCCESS; jobs .NET `105242293551`, npm `105242293981`, configuración `105242294463`.
- La comparación desde el head auditado `abd61462f2063587433e2e17d4609296aa018a51` hasta el functional head no contiene cambios a `package-lock.json` ni a archivos `.csproj`, por lo que la auditoría de dependencias es equivalente para los blobs de dependencia vigentes.
- Los commits posteriores usados para REVIEW_FIRST/receipts de N8.11.F/G son exclusivamente evidencia documental; no alteran la implementación probada.

## Cobertura

Las pruebas aplicables incluyen autorización/tenant/auditabilidad dirigidas, suite backend, integración MySQL, build frontend de producción, E2E en Chromium/Firefox/WebKit, accesibilidad y regresión. No existe delta de esquema/migración ni delta de comportamiento de performance dentro del scope acotado de N8.11; la frontera de rate limiting queda cubierta por contratos de seguridad.

## Riesgo residual

P2 no bloqueante: el bearer token del frontend continúa almacenado en `localStorage`; migrarlo a cookies HttpOnly exige un rediseño transversal de contrato y queda fuera del cierre acotado de N8.11. No hay P0 ni P1 abiertos.

## Seguridad de ejecución

No se tocó `main`, Producción, PR #2, secretos, DNS ni certificados. No se realizaron writes productivos.
