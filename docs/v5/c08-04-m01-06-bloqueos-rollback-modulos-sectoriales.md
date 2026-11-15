# SOLQARYN V5 — C08-04.M01.06: condiciones de salida y reversión de extensiones sectoriales

**Unidad:** `C08-04.M01.06`, PLAN_MAESTRO fila 1267. **Responsable:** PRIMARY DEV 01 JAVIER (`6adbb9fff1548191a1bd5b30d9e9984a`). **Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** preparación documental M01, sin implementar módulos.

## Objetivo, alcance negativo y evidencia existente

La fase C08-04 trata de **contratos para módulos sectoriales activables por capability** sin duplicar entidades, plataformas, autenticación, permisos, bases de datos ni control de tenants. Este cierre define los bloqueos, rollback, criterios de salida y sucesoras permitidas por DAG. No crea capacidades, menú, endpoints, esquema SQL, flags efectivos, compras, pagos, reservas, UI, workers, colas, eventos ni scripts de despliegue. Ni un documento ni el estado LISTO de esta unidad habilitan por sí solos M02, JWT/RBAC, migraciones, QA/main/PROD, secretos o infraestructura.

La arquitectura real sigue siendo Angular 22.2.1 con Signals y guards para experiencia, ASP.NET Core 10 con Domain/Application/Infrastructure/API para autoridad, MySQL con EF Core 10 y proveedor Oracle, JWT/RBAC relacional y frontera tenant server-side. Una capability de UI nunca sustituye el permiso de objeto del backend ni permite elevar privilegios mediante slug, cabecera o `EmpresaId` del navegador.

## Predecesores, evidencia y política de precedencia

En el preflight `PLAN_MAESTRO!H1267=PENDIENTE/I1267` vacío, `COLA!G1267=SI/O1267=SI` y DAG exacto para `C08-04.M01.06` `F=VALIDADO/G=SI`. M01.01–05 constaban LISTO con revisión A5 por Tasks Javier diferentes de su autoría; la última A5 fue `C08-04.M01.04`, de DEV05, revisada por DEV01 sobre el SHA original `f1053b9bd8cea492d6a2b0e12874d8c9fad94e3d` y blob documental `df3413a9296f97f39873b3d5de86b12aea8737e5`.

| Antecedente | Evidencia de preparación, no de implementación |
| --- | --- |
| `C08-04.M01.01` | Objetivo/no-alcance y no-regresión de módulos sectoriales |
| `C08-04.M01.02` | Actores, entradas, salidas e interfaces de futuras capacidades |
| `C08-04.M01.03` | Inventario de código y brechas de evidencia con fuentes Git |
| `C08-04.M01.04` | Riesgos potenciales R01–R16, supuestos S01–S08 y decisiones D01–D08, A5 independiente |
| `C08-04.M01.05` | Criterios AC01–AC14 y N01–N14 descritos como pruebas FUTURAS NO_EJECUTADAS |
| DAG | Predecesores intercapítulo M01 de C01, C02, C05, C06, C08; su cálculo es gate de esta unidad, no autorización de nuevos proveedores |

Ninguna evidencia documental constituye aceptación de permisos reales, compatibilidad de módulo activado ni inspección de datos. Toda implementación sectorial posterior deberá mantener un solo núcleo y aislamiento de tenants.

## Bloqueos BL01–BL16

| ID | Condición que impide la operación futura mientras no esté demostrada |
| --- | --- |
| BL01 | Una extensión duplica identidades, usuarios, tenant, facturación o catálogo base sin contrato de fuente única |
| BL02 | Capabilities se derivan exclusivamente de parámetros cliente o estados de frontend |
| BL03 | El backend permite leer o escribir recursos del tenant B con credenciales del tenant A |
| BL04 | Un rol o membresía revocados siguen habilitando pantallas, API, exportaciones o jobs |
| BL05 | Un módulo sectorial crea permisos globales sin RBAC/ABAC y scope de recurso verificado |
| BL06 | Esquema, entidades y migrations de extensión cambian el core sin compatibilidad comprobada |
| BL07 | Desactivar una capability borra, invalida o expone datos previamente creados |
| BL08 | Activación/desactivación no es idempotente o falla bajo concurrencia |
| BL09 | Listado y búsqueda cruzan caché, sucursal, contexto o tenant de un módulo distinto |
| BL10 | Flags, plantillas y configuración permiten SQL, HTML/JS arbitrarios o elevación de privilegios |
| BL11 | El módulo crea una ruta paralela de autenticación, sesiones o autorización |
| BL12 | Eventos, workers o integraciones realizan acciones sin actor/tenant/correlation confiables |
| BL13 | Auditoría, logs, errores y telemetría filtran tokens, PII o información intertenant |
| BL14 | No se prueban escenarios positivos/negativos de capabilities, JWT/RBAC y tenant A/B |
| BL15 | Falta respaldo sandbox, rollback verificado y pruebas de regresión en código afectado |
| BL16 | No se acreditan CI exact-SHA, Scope Lock, A0–A4, A5 independiente y gate individual para promover |

Si un control falla, aplicar NO_GO a su operación concreta; no inventar vulnerabilidades ni cancelar trabajo documental independiente autorizado.

## Reversibilidad y recuperación RB01–RB10 — escenarios FUTUROS NO_EJECUTADOS

| ID | Fallo y reversión que deberá ensayarse antes de implementación |
| --- | --- |
| RB01 | Revocar capability: detener nuevas operaciones y conservar registros históricos sin exponerlos |
| RB02 | Tenant A recibe respuesta de B: denegar y limpiar caché/estado compartido con traza sin PII |
| RB03 | Rol revocado después de login: revalidar autorización de servidor, invalidar acceso y registrar |
| RB04 | Activación duplicada o simultánea: aplicar idempotencia/CAS y compensación si hay fallo parcial |
| RB05 | Configuración tipada inválida: rechazar valor y restaurar última configuración autorizada |
| RB06 | Incompatibilidad de DTO/API: revertir cambio acotado, conservar compatibilidad versionada |
| RB07 | Migración de datos falla: restaurar desde backup ensayado en sandbox, no tocar PROD sin orden |
| RB08 | Job o evento reintentado: deduplicar por actor/tenant/operación/versión, sin efectos dobles |
| RB09 | Menú/UI muestra capability no autorizada: ocultar sin confundirlo con enforcement backend |
| RB10 | Scope Lock, seguridad o pruebas causalmente requeridas no pasan: mantener VALIDANDO y reparar |

**Rollback de este changeset documental:** `git revert` normal del documento y su sección autocontenida de `CHANGELOG_AI.md`, sin force-push ni reescritura de BITACORA. No se probaron RB01–RB10 contra sistemas reales.

## Condiciones de salida, auditorías y continuidad

**A0:** cambio exclusivamente a este Markdown y CHANGELOG, sin secretos, endpoints, EF, UI ni credenciales. **A1:** BL01–BL16 y RB01–RB10 definidos con oráculos observables para etapas posteriores; no son pruebas xUnit/HTTP/MySQL/Playwright ejecutadas. **A2:** no-regresión conceptual de JWT/RBAC/tenant A/B, revocación, PII, idempotencia y ausencia de privilegios de cliente. **A3:** relación verificable con M01.01–05 y sus A5, y con dependencias DAG del ID; ninguna sucesora queda autorizada únicamente por el documento. **A4:** publicación fast-forward/CAS a `dev`, diff limitado a dos Markdown, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` SHA exacto, BITACORA y readback PLAN/COLA/DAG. CI runtime no causal sobre dos Markdown = NO_APLICA motivado, no PASS.

**A5_PENDIENTE:** otra Task Javier distinta de la autora debe inspeccionar SHA, blobs, diff, evidencia y resultado; no autoaprobación. Las fases M02/M03 reales, proveedores, seguridad, QA/main/PROD y despliegues requieren gates independientes.
