# SOLQARYN V17 — C02-01.M03.05: worker acotado de prevalidación de tenant

Autor: PRIMARY DEV 06 JAVIER, Task 6af16bbad26881918c283abe06e1f32a. Rama dev, PROJECT_SCOPE_LOCK=STRICT, riesgo R2.

## Contrato entregado

Se implementa C0201TenantPreferenceBoundedRevalidationWorker en Infrastructure .NET 10. Es un orquestador secuencial de previsualización en memoria, con máximo 12 solicitudes por lote y un actor/revisión/SHA observados. Rechaza antes de consultar el adaptador los identificadores inválidos, reclamos de autoridad del navegador, solicitudes mezcladas y claves duplicadas. Después compone el adaptador aislado M03.04 y el gate Application M03.03, deniega todo el lote si alguna decisión es negativa y nunca devuelve nombres empresariales ni respuestas parciales.

Un resultado admisible indica únicamente que se requiere verificación o reemisión por backend autenticado. No concede acceso, no firma JWT, no cambia tenant de sesión, no escribe preferencias ni genera colas durables. No hay proveedor real de MySQL, HTTP, DNS, login, cookies, control plane, producción o scheduler; esas integraciones requieren implementación y pruebas independientes explícitas. No se proclama soporte de login productivo.

## Impacto y calidad

- A0: un archivo Infrastructure, once métodos de prueba xUnit, esta documentación y CHANGELOG_AI. Domain/Application, interfaces HTTP, DTO existentes y migraciones permanecen intactos.
- A1: casos positivos 1/N tenant; negativos por revocación, tenant ajeno, identidad y revisión distintas, SHA, hints de navegador, lote vacío o mayor de 12, duplicados, selección mezclada, solicitud nula, conmutación sin destino y cancelación.
- A2: tenant A/B, RBAC, preferencia revocada y denegación de sesión fuera de contexto. El origen TrustedSnapshot es sintético y no autentica entidades reales.
- A3: compilación Release y xUnit en GitHub Actions. MySQL/Playwright/HTTP NO_APLICA por ausencia de cambios en DB, UI o endpoint.
- A4: exigir checks Backend Release, seguridad, autorización cuando aplique, Scope Lock y V5 en SHA publicado o equivalencia demostrada, PLAN/COLA/BITACORA readback.
- A5: revisor Javier de otro TASK_ID; nunca autoaprobar. Rollback por git revert normal; no QA/main/PROD.
