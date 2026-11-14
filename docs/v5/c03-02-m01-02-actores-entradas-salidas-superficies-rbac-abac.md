# SOLQARYN V5 FINAL — C03-02.M01.02: actores, entradas, salidas y superficies RBAC/ABAC

**Tipo:** preparación documental M01.02, sin modificación ejecutable ni certificado de aislamiento universal. **Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`; GitHub `jmejia31`. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-13.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), [objetivo y no-regresión M01.01](c03-02-m01-01-objetivo-no-regresion-rbac-abac.md), y Skill/Scope Lock canónicos. En C03-02 el resultado futuro es RBAC/ABAC servidor por tenant, sucursal, propiedad, archivos y operaciones con negativas BOLA/IDOR, claims falsos, cruce de sucursales y exportaciones ajenas. M01.02 **solo identifica superficies y contratos**, no los implementa.

## Actores identificados y límites de autoridad

| Actor | Identidad y contexto necesario | Operación y frontera obligatoria |
| --- | --- | --- |
| A01 Operador empresa A | JWT legítimo y usuario activo, membresía A y grants vigentes | CRUD de recurso A en sucursal permitida; un JWT válido por sí solo no autoriza |
| A02 Administrador empresa A | Membresía/rol/permiso efectivo de A | Administración de A sin privilegio sobre B ni acceso plataforma universal |
| A03 Operador empresa B | JWT, membresía, rol, sucursal y grants propios de B | Nunca reutilizar marcas HttpContext, caché, archivos ni auditoría de A |
| A04 Personal de sucursal | Usuario empresarial con scope sucursal preciso | Denegación si cambia el ID de sucursal o pertenece a B |
| A05 Dueño/gestor de archivo | Propiedad y permisos de recurso antes de URL o bytes | Acceso solo al archivo/miniatura/metadato expresamente autorizado |
| A06 Solicitante de exportación | Permiso explícito de export más filtros tenant/branch | CSV/XLSX/PDF sin filas de otra empresa ni PII no autorizada |
| A07 Soporte restringido | Autoridad especial validada server-side, allowlist real | Solo GET permitido bajo reglas vigentes, no bypass de empresa ni POST |
| A08 Solicitante sin permisos o malicioso | JWT falsificado/revocado o sin membresía, selector falso | 401/403/404 seguros según contrato; no se ejecuta transacción |
| A09 Auditor autorizado | Autoridad para ver eventos de su ámbito | Logs sin tokens, payload ajeno ni acceso a secretos; correlación segura |

Los roles listados son **perfiles conceptuales**, no cuentas reales creadas. La pertenencia a empresa, sucursal y recurso debe validarse en servidor por solicitud. No inferir permisos desde navegación, claims aportados por cliente, cabecera o DTO.

## Matriz de entradas, salidas y puntos de confianza

| ID | Entrada/estímulo | Consumidor / comprobación futura | Salida positiva y negativa exigible |
| --- | --- | --- | --- |
| IO01 | Token JWT / identidad | Middleware y filtro de API | Identidad válida; token alterado/revocado rechazado sin negocio |
| IO02 | Selector `X-Empresa-Id` | TenantPermissionContext / membership | Empresa única y autorizada; ausente o duplicada falla cerrada |
| IO03 | UsuarioEmpresa activo / rol | Servicio de scope y permiso | Membresía persistida; acceso revocado se rechaza |
| IO04 | Módulo y acción solicitada | RequierePermisoAttribute / RolPermiso | Grant válido autoriza acción concreta; catálogo sin grant no |
| IO05 | ID de entidad y ruta API | Controller/servicio/repo ownership | Recurso propio A permitido; ID B denegado sin efectos B |
| IO06 | ID sucursal / ubicación | Servicio de aplicación y consulta DB | Sucursal permitida; sucursal B o no autorizada rechazada |
| IO07 | DTO/body/query empresarial | Validador y servicio backend | Parámetros válidos no sobreescriben empresa verificada |
| IO08 | Identificador de archivo / URL | Servicio de archivo/almacenamiento | Bytes/URL A solo tras check; ningún metadato de B |
| IO09 | Filtros y formato export | Servicio/exportador/serializador | Artefacto solo A y PII permitida; filtros B denegados |
| IO10 | Grant de soporte + verbo HTTP | PlatformSupportPolicy y auditoría | GET allowlisted; POST/PUT/DELETE y ruta extra rechazados |
| IO11 | Contexto concurrente A/B | Scope por request, caché y correlación | Sin mezcla de identidad, tenant, operaciones ni trazas |
| IO12 | Excepción/denegación | Manejador de errores + auditoría | Código neutral sin token, datos B o detalle de esquema |
| IO13 | Revocación de membresía/rol | Lectura posterior de autoridad efectiva | Segunda petición no utiliza grant anterior |
| IO14 | Evento o respuesta de exportación | Auditoría e integración externas | No emitir side effect para autorización fallida |

Los resultados son **criterios contractuales futuros**, no evidencia de llamadas HTTP/DB ejecutadas aquí. Para futuras implementaciones, validar el código de estado y ausencia de cambios/filas/artefactos B con base MySQL 8.4 efímera y casos negativos reales.

## Superficies y dependencias identificadas en código dev

| Referencia | SHA de blob Git inspeccionado | Frontera y evidencia real |
| --- | --- | --- |
| backend/src/API/Filters/TenantPermissionContext.cs | df7d49f3e185c538f3b3dafabb36c3a971a7257d | Cabecera selecciona tenant; no es grant |
| backend/src/API/Filters/RequierePermisoAttribute.cs | b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4 | Permiso módulo/acción comprobado por backend |
| backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs | 17c7d191e3e1b34f56c3d27ba4992b512526542b | Membresía vigente |
| backend/src/Application/Security/VerifiedCommercialTenant.cs | 2da140ce9d7f47329d39d292d3271596467910cc | Marca de empresa autorizada solo servidor |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Relación usuario-empresa; migraciones no probadas aquí |
| backend/src/Application/Security/PlatformSupportPolicy.cs | b103f0b0ec7b9169c060661f6a4456f8863152cf | Allowlist soporte GET, no privilegio general |
| backend/tests/Solqaryn.Tests/P1001ARolesHttpJwtIsolationTests.cs | d0f29269117f7c0d6a08eed1cb31114073717c19 | Archivo de pruebas TestServer con grants simulados, no MySQL físico |

**Consumidores afectados en futuras etapas**: middleware de autenticación, filtros de permisos/membresía, controllers, capa Application, repositorios EF, servicio exportador, archivos/URL y logging; frontend únicamente como consumidor, nunca autoridad RBAC. **En esta microtarea no se modifica ninguno**.

## Matriz de aceptación documental positiva y negativa

| Caso | Tipo | Evaluación del contrato |
| --- | --- | --- |
| P01 | Positivo documental | A01, IO01–IO06 y salida permitida para recurso propio A incluyen JWT+membresía+grant+propiedad |
| P02 | Positivo documental | A06, IO09 incluye autorización de export antes de serialización y ausencia de filas B |
| N01 | Negativo documental BOLA/IDOR | A01 intenta IO05 con ID B: rechazo y sin revelar existencia B |
| N02 | Negativo documental claim/header | A08 falsifica IO01/IO02/IO07: ningún grant por cabecera o body |
| N03 | Negativo documental sucursal | A04 con IO06 de sucursal B: denegación sin escritura |
| N04 | Negativo documental archivo | A05 usa IO08 de recurso B: sin URL ni metadatos |
| N05 | Negativo documental export | A06 filtra B mediante IO09: sin filas ajenas ni PII |
| N06 | Negativo documental soporte | A07 solicita POST o ruta ajena: denegación |
| N07 | Negativo documental revocación | A02 pierde grant en IO13: segunda petición denegada |
| N08 | Negativo documental concurrencia | A01 y A03 invocan IO11: ningún contexto reutilizado |
| N09 | Negativo documental error | A08 invoca IO12: sin secretos ni conteos B |
| N10 | Negativo documental evidencia | Un archivo de test o mock leído no es PASS HTTP/MySQL |
| N11 | Negativo documental UI | Un guard de frontend no concede acción a API |
| N12 | Negativo documental gobierno | LISTO_DEV no significa A5_PASS ni QA/main/PROD, y revisor debe ser otra Javier Task |

Esta tabla son **escenarios diseñados** (2 positivos/12 negativos), no catorce tests ejecutados; sus pruebas funcionales pertenecen a implementación M02–M06.

## Puertas, clasificación y rollback

DAG_TECNICO C03-02.M01.02 = PREPARACION_DOC_CONDICIONAL, F=VALIDADO/G=SI. Los diez prerequisitos C01-02.M01.01–.05 + C02-01.M01.01–.05 estaban PLAN H=LISTO al claim; COLA G/O=SI y PLAN H=PENDIENTE I vacío, luego owner J00 exclusivo. La disponibilidad de otro scope no elimina estas puertas. **No se libera M01.06, M02, roles/grants, JWT, migraciones, despliegues ni QA/main/PROD.**

Clasificación: requiere_dotnet=NO_APLICA, requiere_node_angular=NO_APLICA, requiere_mysql=NO_APLICA, requiere_docker=NO_APLICA, requiere_e2e=NO_APLICA; requiere_security=SI (lectura JWT/RBAC/tenant/archivos/PII BOLA); requiere_scope_lock=SI para publicación documental.

- A0: Markdown, diff, source blobs y ausencia de secretos.
- A1: verificaciones documentales P01/P02 y N01–N12 de mapeo, no resultados runtime.
- A2: revisar contrato DENY por tenant, claim falso, header, RBAC/ABAC, archivos, PII y excepciones.
- A3: coherencia con objetivo M01.01, inventario M01.03, riesgos M01.04, aceptación M01.05, DAG/PLAN/COLA.
- A4: commit fast-forward exact-SHA, parent, diff, check Scope Lock, readback PLAN/COLA/BITACORA.
- A5: PENDIENTE otro TASK_ID distinto, entre las cinco Tasks Javier restantes.

**Rollback**: git revert normal del commit y conciliación institucional con eventos idempotentes, sin borrar BITACORA ni force-push. Sin nuevas Tasks/PR/branches/secretos/datos/PROD.
