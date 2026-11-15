# SOLQARYN V5 — C08-04.M02.04: configuración tipada, ámbitos y flags seguros para módulos sectoriales

**2026-11-15. Autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`). **Autoridad:** STANDARD V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN/COLA/DAG institucional Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`, Skill canónica y PROJECT_SCOPE_LOCK STRICT en `solqaryn/Solqaryn@dev`.

**Frontera:** contrato de M02 para extensiones por rubro; no implementa schemas MySQL, migraciones, API, Angular, flags vivos, JWT/RBAC, eventos, workers, colas, webhooks ni despliegues. Criterios son `FUTUROS_NO_EJECUTADOS`, no resultados de runtime. A5 sólo otra Task Javier.

**Antecedentes reales:** `docs/v5/c08-04-m01-01-alcance-extensiones-por-rubro.md` blob `41dd2a10d667124c021791278d43ceab3dc02c35` NR01–NR12; `...m01-03-inventario-codigo-contratos-extensiones.md` blob `f34c6b59328d9d5a9e106e55eedeb250d50fb900` (11 fuentes y 10 brechas); `...m01-05-criterios-aceptacion-extensiones-sectoriales.md` blob `9ad425e66e7839d38a2416cf97615b20e0e129a2` (AC01–AC14, N01–N14 futuros); `...m01-06-bloqueos-rollback-modulos-sectoriales.md` blob `009a4190314f89ec1516d0a8c4ddc7fe08cb4a96` (BL01–BL16). La existencia de fuentes y blobs no acredita una capability operacional.

## 1. Catálogo de opciones propuestas, sin activación real

| Propiedad futura | Tipo / dominio esperado | Ámbito y denegación |
| --- | --- | --- |
| `capabilityKey` | enum allowlisted reserva, membresía, catálogo, sitio | Global versionada, no slug arbitrario |
| `capabilityVersion` | número de schema aprobado | Empresa; versión desconocida deniega |
| `enabled` | boolean | Empresa validada y sujeta a RBAC; default false |
| `rolloutState` | enum draft, enabled, suspended, retired | Cambio sólo por autoridad backend |
| `branchScope` | conjunto de sucursales verificadas | Fuera de Empresa o rol deniega |
| `audience` | enum public, authenticated, admin | Ninguna audiencia concede permisos |
| `reservationMode` | enum off, manual, permitted | No instala agenda ni pagos |
| `membershipMode` | enum off, read_only, permitted | No confundir UsuarioEmpresa y miembro comercial |
| `catalogMode` | enum off, readonly, enabled | Sin ownership NULL global |
| `siteVisibility` | enum private, public_allowlisted | Host binding validado en backend |
| `maxOperationsPerWindow` | entero positivo con límite futuro medido | No inventar cuotas reales |
| `auditPolicy` | enum strict | Denegar falta de actor, SHA o redacción |

Esta tabla no agrega configuración ni flags ejecutables. Un futuro cliente Angular 22.2.1 recibiría sólo una **proyección segura**; el API .NET 10 decidiría permisos y pertenencia tenant. El guard frontend no habilita acciones por sí mismo. Configuración faltante o inválida = NO_GO; no usar fallback permisivo ni estado cacheado desactualizado.

## 2. Doce controles de aceptación futura

| ID | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| V01 | Clave registrada y versionada | Clave arbitraria deniega |
| V02 | Empresa real activa | Empresa inactiva deniega |
| V03 | Usuario miembro y rol autorizado | Rol revocado deniega |
| V04 | Grant compatible con RBAC | Flag habilitado sin permiso deniega |
| V05 | Sucursal dentro del scope | Sucursal ajena deniega |
| V06 | Cambio A a B invalida caché | Proyección de A no aparece en B |
| V07 | Apagado retiene historial | Apagado no elimina registros |
| V08 | Compatibilidad schema validada | Schema desconocido deniega |
| V09 | Host normalizado permitido | Host/slug arbitrarios no autentican |
| V10 | Auditoría saneada identifica actor | Token y PII no aparecen en logs |
| V11 | Reintento controlado y CAS | Replay con versión vieja no altera estado |
| V12 | Falla de caché rechaza acción | Timeout no habilita capacidad |

## 3. Precedencia y pruebas no realizadas

Precedencia: autoridad backend (identidad y tenant) > permiso recurso/acción RBAC > grant sectorial validado > estado de feature flag seguro > presentación UI. Un flag nunca modifica claims JWT ni configura un servicio paralelo; no se infiere autorización del header `X-Empresa-Id`. El contrato preserva operatividad existente mientras no se active ninguna capability nueva.

A0: revisar diff y M01.01/03/05/06. A1: tipos y V01–V12 definidos, no ejecutados. A2: revisión conceptual de aislamiento y fallos negativos, no prueba real. A3: no API/DI/DB/Angular en diff (NO_APLICA). A4: HEAD exacto y checks obligatorios Scope Lock/V5 `completed/success`; si falla uno, conservar VALIDANDO. A5 ajena obligatoria. Rollback documental por revert y sin migraciones, cambios a secretos, QA/main/PROD.
