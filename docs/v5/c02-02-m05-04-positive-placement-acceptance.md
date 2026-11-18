# SOLQARYN V19 — C02-02.M05.04 aceptación positiva de placement

**TASK autora:** PRIMARY DEV 03 Javier `6adc2e4bd8a48191ac7d8641ed95a1a3`. `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Riesgo V15 **R2** por tenant, JWT/RBAC y routing de storage. PLAN fila 353, COLA G/O=SI, DAG F=VALIDADO/G=SI, claim exclusivo V17. Solo una EN_PROGRESO simultánea; la unidad M05.05 propia está VALIDANDO a la espera de CI externa.

## A0 — superficie y dependencias

Pruebas unitarias xUnit sobre `C0202PlacementApplicationGate` y reglas Domain existentes, sin modificar runtime, infraestructura, base de datos, APIs, rutas, DTOs, roles, secrets, configuración ni despliegues. Consumidor: validación Application read-only de placement shared/dedicated. No se concede un DSN ni conexión de tenant desde la UI; el backend deberá aportar evidencia auténtica y enforcement separado antes de cualquier acceso. Rollback: git revert convencional del changeset, no tocar datos.

## A1/A2 — aceptación reproductible de casos positivos y límites

Se verifican cuatro combinaciones de inspección tenant A/B por shared/dedicated y cuatro propuestas verificadas, exigiendo `AUTHENTICATED_STORAGE_ADAPTER_REQUIRED` con `AuthorizesConnection/Migration/Cutover/Production=false`. Se comprueban una sola lectura por caso, tenants sin reutilización cruzada, long.MaxValue de revisión, IDs de tenant/idempotencia de 128 caracteres, petición repetida, rechazo de evidencia A usada para B y revisión obsoleta. Los assertions son ejecutables y deben fallar ante elevación indebida. No se documenta un resultado CI hasta verificación real.

## A3 — integraciones

La suite usa snapshots **sintéticos** de evidencia confiable en memoria; **no** realiza JWT HTTP firmado, RBAC relacional real, EF, MySQL 8.4, Cloudinary, Aiven, migraciones, router ni despliegue. Por tanto esos sistemas son NO_APLICA al **diff de pruebas**, no están certificados y siguen siendo requisitos causales cuando se implemente el adaptador. No confundir un resultado positivo de preflight con permiso R3.

## A4/A5

Exigir GitHub Actions xUnit .NET 10 Release, backend, seguridad, Authorization, Scope Lock y V5/procedencia completed/success sobre SHA exacto o equivalencia causal rigurosa, con PLAN/COLA/DAG/BITACORA readback. Si CI está QUEUED/IN_PROGRESS, conservar VALIDANDO con SHA/RUN y propietario. A5 sólo por otra Task Javier distinta de autora; no QA/main/PROD. Sin certificación de infraestructura o datos productivos.
