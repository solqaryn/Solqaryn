# C05-01.M03.02 — Design token Application preflight

Riesgo R2: tenant, permisos RBAC, catálogo e idempotencia. Implementación de servicio Application de solo lectura, con dos interfaces para identidad backend y catálogo tenant; compone reglas Domain de M03.01, sin añadir HTTP, DI, EF/MySQL, proveedor, CSS ni escritura.

A0: servicio y tests nuevos, documento de alcance y sección CHANGELOG. Los consumidores futuros deben construir ActorSnapshot desde autenticación backend real y consultar catálogo por tenant autenticado; el claim cliente solo se compara por igualdad.

A1: pruebas xUnit positivas, negativas, fronteras, fallos de lectores, sesiones obsoletas, concurrencia de revisión, CSS inseguro y cancelación; ejecución real solo tras resultados GitHub Actions por SHA.

A2: denegación por defecto ante JWT/RBAC/membresía revocada, tenant A/B, catálogo ajeno, comparador de clave insensible a mayúsculas, intención de mutación institucional y sesión caducada. Ninguna bandera devuelta confiere permisos.

A3: NO_APLICA MySQL/EF, endpoints HTTP, Chromium y proveedores: no se implementan adaptadores, transacciones, rutas ni UI. Mocks in-memory no son evidencia de integración.

A4: exigir Scope Lock, V5 y checks GitHub-hosted Backend Release/seguridad/autorización pertinentes SUCCESS en SHA exacto o equivalencia causal; PLAN, COLA y BITACORA readback. Si CI está in_progress conservar VALIDANDO y owner.

A5: revisión independiente por otra Task Javier de requisitos, código y tests, no autoblessing. Rollback: git revert normal sin force-push ni reescribir BITACORA. Ningún permiso QA/main/PROD.
