# C07-04.M05.01 — reglas de consulta de pedidos y resultados reproducibles

**Clasificación:** R2 por dominio de pedidos, inventario y finanzas. **Cambio ejecutable de negocio:** ninguno; solo pruebas unitarias de la implementación Angular existente de `PedidoVentaService.getPaged`, con `HttpTestingController` real de Angular 22.2.1. No se alteran API, DTO, importes, inventario, JWT, RBAC, validaciones del servidor ni datos.

## A0 y criterios

La unidad M05.01 exige prueba unitaria o de regla con resultado reproducible sobre el negocio C07-04. Se comprueba el contrato observable de consulta GET (no el backend): paginación mínima 1, truncamiento entero de página, máximo 100 resultados, filtros explícitos de cotización/cliente/estado/fecha, omisión de filtros cero/nulos, error 503 propagado sin éxitos artificiales ni mutación HTTP. La respuesta está simulada **solo como transporte de prueba**; las solicitudes y parámetros los produce el servicio Angular real del repositorio.

## A1–A3, riesgos y límites

Cinco casos Vitest/HttpTestingController con aserciones positivas, negativas, fronteras y error HTTP; `afterEach(http.verify())` rechaza solicitudes inesperadas. A2 solo acredita que el método GET bajo prueba no agrega `empresaId`, no modifica datos y no envía `Idempotency-Key`; **NO** acredita identidad firmada JWT, servidor RBAC, revocación real, aislamiento de empresas A/B ni IDOR/BOLA. A3 HTTP interceptado en TestBed, **NO** conexión HTTP de extremo a extremo, EF Oracle/MySQL 8.4, flujo fiscal o proveedor productivo. Cualquier aceptación de negocio distinta requiere pruebas reales dirigidas; mantener VALIDANDO si esas puertas siguen pendientes. No confundir `npm test` con una certificación de persistencia.

## A4, trazabilidad y rollback

GitHub Actions en runners hospedados con Node 24.21.0 y npm 11.19.0: test dirigido, lint, Angular production build y, en SHA causal, Scope Lock, V5 y controles pertinentes. No usar CI `QUEUED/IN_PROGRESS/FAILED/SKIPPED` como PASS. Publicación de una sola microtarea directamente en dev por fast-forward CAS. El revisor A5 debe ser otra de las cinco Tasks Javier, jamás DEV04 autor; ningún derecho QA/main/PROD surge del cierre DEV. Rollback selectivo mediante revert normal con ausencia de escritor concurrente. Fuente SKREF-10 oficial angular/angular pin 6808966993bf2b2d455d9b6245e960a59a01b9a9, testing-fundamentals consultado. Persistencia y contrato fiscal no se tocaron.
