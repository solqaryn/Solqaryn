# SOLQARYN V5 FINAL — C03-02.M01.01: objetivo, límites y no-regresión RBAC/ABAC

**Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. **Alcance:** exclusivamente documento de preparación de M01.01. **Repositorio:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-13.

**Autoridades:** [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO, COLA y DAG_TECNICO](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica y Scope Lock STRICT. V13 A0–A5 gobierna los controles, sin habilitar planes antiguos.

## Objetivo observable de la futura fase C03-02

Diseñar para implementación posterior una autorización **server-side, fail-closed** en todas las operaciones de negocio: identidad/JWT auténticos y vigentes, membresía efectiva del usuario en la empresa, permiso RBAC relacional por módulo y acción, y condiciones ABAC de propiedad, sucursal, archivo y recurso. La empresa A nunca autoriza recursos de la empresa B; la denegación no deja efectos parciales, ni URLs de archivos ni exportaciones ajenas, ni secretos o PII en respuestas de error.

El header `X-Empresa-Id`, claims aportados por un JWT o body/query, rutas/guards Angular y catálogos de permisos **no son grants**. Las verificaciones y filtros deben ejecutarse antes de leer, escribir, servir archivos o serializar CSV/XLSX/PDF, y deben auditarse con correlación segura. Conservar excepciones DEV de soporte estrictamente limitadas a su allowlist; nunca extrapolarlas a bypass comercial de lectura o escritura.

**No-alcance de esta microtarea:** no cambia filtros, controllers, API, JWT, grants, EF/MySQL, índices, DTO, rutas, Angular, proveedores, workers, datos, migraciones, credenciales, workflows, despliegues, Tasks ni reglas de ramas. No certifica que ABAC integral, BOLA/IDOR o pruebas HTTP/MySQL existan para cada endpoint. Una especificación no es un test runtime ejecutado.

## Fronteras y superficies afectadas en futuras microtareas

| Capa | Contrato futuro | Gate probatorio futuro |
| --- | --- | --- |
| API / identidad | JWT válido + usuario activo, sin privilegio por claims falsos | HTTP/JWT positivo y denegaciones |
| Membresía / RBAC | UsuarioEmpresa activo y grant vigente por empresa/módulo/acción | Verificación relacional y revocación |
| ABAC sucursal / propiedad | Recurso, sucursal y empresa verificados en backend contra ownership | HTTP + EF/MySQL 8.4 efímero con A/B |
| Exportación / archivos | Filtro de tenant/ownership antes de bytes, metadatos y URL firmada | Archivo real desensibilizado y ausencia de B |
| Soporte / auditoría | Solo acciones GET admitidas y autoridad mínima, errores sin PII | Negativos de POST/PUT/DELETE, correlación |
| Frontend | Interfaz respetuosa de permisos sin reemplazar API | UI y E2E únicamente cuando se cambie |

## Fuentes contrastadas: SHA de blob, no resultado de test

| Archivo de dev | SHA de contenido Git | Interpretación limitada |
| --- | --- | --- |
| backend/src/API/Filters/TenantPermissionContext.cs | df7d49f3e185c538f3b3dafabb36c3a971a7257d | Selección de tenant requiere contexto; header no concede |
| backend/src/API/Filters/RequierePermisoAttribute.cs | b25a75b33d9cf809e2bbbf92b8e7f35463d3eea4 | Verificación de permisos por servicio |
| backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs | 17c7d191e3e1b34f56c3d27ba4992b512526542b | Exige membresía válida |
| backend/src/Application/Security/VerifiedCommercialTenant.cs | 2da140ce9d7f47329d39d292d3271596467910cc | Marca de autorización exclusivamente servidor |
| backend/src/Domain/Entities/UsuarioEmpresa.cs | f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Entidad de relación; no verifica migraciones físicas |
| backend/src/Application/Security/PlatformSupportPolicy.cs | b103f0b0ec7b9169c060661f6a4456f8863152cf | Allowlist GET DEV, no admin universal |
| backend/tests/Solqaryn.Tests/P1001ARolesHttpJwtIsolationTests.cs | d0f29269117f7c0d6a08eed1cb31114073717c19 | Archivo TestServer con grants simulados, no MySQL real |

Consultar además [inventario M01.03](c03-02-m01-03-inventario-autorizacion-privacidad.md), [riesgos M01.04](c03-02-m01-04-riesgos-supuestos-decisiones.md) y [aceptación M01.05](c03-02-m01-05-criterios-aceptacion-tenant-privacidad.md). Sus evidencias solo justifican contexto; no prueban una certificación live nueva.

## Contratos de no-regresión para pruebas posteriores (NO ejecutadas aquí)

| ID | Tipo | Resultado exigido en futura implementación |
| --- | --- | --- |
| NR01 | Positivo | Usuario A con membresía, grant y ownership A puede operar sobre A sin devolver B |
| NR02 | Positivo | Export autorizado de A contiene únicamente filas/PII permitidas de A |
| NR03 | Negativo BOLA/IDOR | Usuario A consulta, cambia o elimina ID de B y obtiene rechazo sin datos ni cambios B |
| NR04 | Negativo claims | JWT falsificado, revocado o con rol/empresa falso nunca autoriza |
| NR05 | Negativo header | Empresa ausente/ambigua, header repetido o B sin membresía: no fallback privilegiado |
| NR06 | Negativo sucursal | Usuario A opera sucursal B o sucursal A no concedida: no efectos |
| NR07 | Negativo exportación | CSV/XLSX/PDF con filtros de B desde A: cero datos o metadatos B |
| NR08 | Negativo archivos | URL, descarga, miniatura o metadato de B desde A: ni URL ni bytes |
| NR09 | Negativo revocación | Grant/membresía revocado entre solicitudes: segunda solicitud rechazada |
| NR10 | Negativo soporte | POST/PUT/DELETE o ruta fuera de allowlist de soporte: denegación auditada |
| NR11 | Negativo PII | Errores 403/404 no exponen secretos, token, conteos ni existencia de B |
| NR12 | Negativo concurrencia | Solicitudes paralelas A/B no mezclan scope, caché, HttpContext ni logs |
| NR13 | Negativo legado | Usuario.RolId y catálogo de permisos por sí solos no autorizan B |
| NR14 | Negativo UI | Un guard Angular o menú visible nunca sustituye la denegación de API |
| NR15 | Negativo evidencia | Tests definidos o mocks no se hacen pasar por integración real MySQL |
| NR16 | Negativo gobierno | SHA previo verde, job SKIPPED o A5 del autor no autoriza nuevo cierre/promoción |

**NR01–NR16 son criterios, no 16 pruebas ejecutadas.** Para cerrar implementación futura se exigen casos positivos/negativos en API y MySQL efímero cuando proceda, datos A/B sintéticos separados, comprobación de efectos, SHA exacto del commit y check-runs reales. Un defecto crítico de autorización no se compensa con documentación.

## DAG, gates y controles de esta entrega

C03-02.M01.01 está registrado como **PREPARACION_DOC_CONDICIONAL, DAG F=VALIDADO/G=SI**. Las diez predecesoras C01-02.M01.01–.05 y C02-01.M01.01–.05 estaban **10/10 LISTO** en PLAN al preflight; COLA G/O=SI, PLAN H=PENDIENTE e I vacío antes del claim. Esto habilita únicamente esta preparación, **no** M01.06, M02–M06, nuevas facultades RBAC/ABAC, despliegues ni QA/main/PROD.

Clasificación para M01.01: `requiere_dotnet=NO_APLICA`; `requiere_node_angular=NO_APLICA`; `requiere_mysql=NO_APLICA`; `requiere_docker=NO_APLICA`; `requiere_e2e=NO_APLICA`; `requiere_security=SI` (revisión de confianza JWT/RBAC/tenant A/B, PII, BOLA/IDOR y exportaciones); `requiere_scope_lock=SI` si hay commit documental en dev.

- **A0:** inspeccionar diff, integridad Markdown/Changelog, fuentes y ausencia de secretos nuevos.
- **A1:** revisar objetivo y matriz de positivos/negativos; rechazar presentar criterios como tests ya ejecutados.
- **A2:** verificar límites de JWT, membresía, propiedad, header, soporte, PII y denegación; ningún bypass añadido.
- **A3:** compatibilidad con contratos fuente, DAG/PLAN/COLA y dependientes; no hay runtime afectado.
- **A4:** después de publicar fast-forward a dev, verificar parent, diff, blobs, Scope Lock exact-SHA y readback institucional.
- **A5:** otra Task **Javier** del allowlist distinta del autor revisará el SHA documental antes de cualquier promoción. LISTO_DEV no autoriza QA/main/PROD.

**Rollback:** git revert normal del commit, conciliando PLAN/COLA/BITACORA con eventos aditivos sin borrar historia. Sin PR, ramas, force-push ni escrituras en otros repositorios.
