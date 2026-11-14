# SOLQARYN V5 FINAL — C02-01.M01.03: inventario de contratos, código y evidencia del contexto empresarial

**Tipo:** 1, estudio documental provisional, sin cambios ejecutables. **Fecha:** 2026-11-13. **Autor:** PRIMARY DEV 01 — JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a; repo exclusivo solqaryn/Solqaryn@dev y PROJECT_SCOPE_LOCK=STRICT.

**Fuente:** Standard V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA) y Sheet (1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU), con PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA de 26 columnas. El estado de elegibilidad antes del claim fue PENDIENTE, sin responsable, COLA G/O=SI y DAG F=VALIDADO/G=SI. Esta preparación no permite liberar C02-01.M01.06 ni implementar M02 por excepción.

## 1. Resultado de esta unidad y frontera

La fase C02-01 busca resolver contexto de empresa, identidad y membresía sin confiar en señales del cliente. **M01.03** inventaría contratos y archivos realmente existentes y separa evidencia directa de supuestos. No acredita integración completa, casos multiempresa live, despliegues, migraciones ni un esquema definitivo de identidad universal.

**No modificar:** C#, JS/Angular, configuración de JWT, flags, permisos, endpoints, BD, MySQL, migraciones, proveedores, credenciales, DNS/host, CI, QA/main/PROD, Tasks, reglas de ramas ni archivos físicos. No invocar plan histórico V13 como backlog. Inventario de imágenes históricas cero: migración NO_APLICA.

**Pruebas aplicables a esta redacción:** requiere_dotnet=NO; requiere_node_angular=NO; requiere_mysql=NO; requiere_docker=NO; requiere_e2e=NO; requiere_security=SI (revisión de límites JWT/RBAC/tenancy/secretos); requiere_scope_lock=SI (integridad documental del SHA). No utilizar un build genérico como evidencia funcional de una solución que este documento no implementó.

## 2. Inventario de fuentes verificadas en el repositorio

| ID | Fuente y Git blob SHA | Contrato u observación verificable | Límite de inferencia |
| --- | --- | --- | --- |
| F01 | backend/src/API/Controllers/TenantContextController.cs — 2407c2eb6779ed724e841c688ee14dabbf163171 | Endpoint autenticado tenant-context; selector delega verificación a IUsuarioScopeService y deniega sin membresía | No certifica todas las rutas API ni acceso live a BD. |
| F02 | backend/src/API/Filters/TenantPermissionContext.cs — df7d49f3e185c538f3b3dafabb36c3a971a7257d | Selector X-Empresa-Id sólo acepta valor único positivo; marca autorizada exige gate posterior | Selector no es permiso; no representa host ni JWT suficiente. |
| F03 | backend/src/API/Filters/RequiereMembresiaEmpresaAttribute.cs — 17c7d191e3e1b34f56c3d27ba4992b512526542b | Requiere identidad y UsuarioEmpresa activa, asigna marca del servidor antes de la acción | No presume que todos los controllers apliquen este filtro. |
| F04 | backend/src/Application/Interfaces/IUsuarioScopeService.cs — 5345ee2de1b1e942bff8eded658f5dc27c0a6745 | Contrato de scope tenant-aware y fallback seguro cuando hay cero o más de una membresía | Las implementaciones legacy deben revisarse individualmente. |
| F05 | backend/src/Infrastructure/Services/UsuarioScopeService.cs — 14f4acbc5e1dc6a3ed1cf3485d54c7f1121b0e6f | Revisión de membresía activa desde EF/MySQL y diferencia de uso legacy/tenant | Leer transacciones, índices, filtros y migraciones antes de implementar. |
| F06 | backend/src/Infrastructure/Services/JwtService.cs — 9051fe6b4aac23e0ce3ebc684a2830c2cd2e6c9b | Emite JWT de identidad con claims de usuario/rol; requiere secret de configuración | Un claim de rol no da permiso automático sobre empresa o recurso. |
| F07 | backend/src/Infrastructure/Services/ConfiguredPublicStoreHostTenantResolver.cs — 833940d3ce2f246d5946daeeb2ed7e14ffaffae2 | Mapeo host exacto + StoreKey habilitado opt-in y con rechazo de entradas inválidas/duplicadas | No implica activación en producción ni cobertura de todos los hosts. |
| F08 | backend/src/Application/Security/VerifiedCommercialTenant.cs — 2da140ce9d7f47329d39d292d3271596467910cc | Marca autorizada de empresa por servidor; no se lee directamente de header/query/DTO | No convierte registros legacy sin ámbito en recursos de empresa. |
| F09 | backend/src/Application/Security/VerifiedPublicStoreTenant.cs — 1bd272b848a2293184aae0e10f84f99f2b0b5b3d | Marca limitada a /tienda; no atribuye acceso administrativo | Host público no sustituye autenticación/membresía comercial. |
| F10 | backend/src/Domain/Entities/UsuarioEmpresa.cs — f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c | Membresía usuario/empresa/rol activa y exclusión del rol legacy como prueba tenant | Entidad sola no prueba EF/DB en todos los entornos. |
| F11 | backend/tests/Solqaryn.Tests/ConfiguredPublicStoreHostTenantResolverTests.cs — dd387f8b6e39c087121fbec1928fe1c5425aa66d | Casos de prueba declarados para hosts exactos A/B, desconocido, duplicado e inválido | Código de test, no ejecución en esta tarea. |
| F12 | backend/tests/Solqaryn.Tests/Application/Security/AuditoriaTenantOwnershipTests.cs — 1a7b21a67cc22f7f334fbac8a77c98682ebdd73d | Contrato de pruebas de atribución auditada por marca del servidor y rechazo de cabecera | Mocks no son MySQL/proveedor live. |

Los 12 blobs documentan **qué código existe** en el HEAD de lectura; los SHA deben releerse si otro commit modifica los consumidores. Estos contratos se contrastarán de nuevo al cierre de fases C00-01/C01-01 y antes de C02-01.M01.06/M02.

## 3. Consumidores, límites de confianza y cobertura pendiente

| Superficie | Dependencia visible | Prueba futura obligatoria cuando se modifique runtime |
| --- | --- | --- |
| UI / tenant selector | TenantContextController, IUsuarioScopeService | Empresas visibles sólo para usuario autorizado; sin divulgación de B. |
| Controllers con permiso | RequiereMembresiaEmpresaAttribute y TenantPermissionContext | A autorizado; B con header A→B denegado incluso si JWT autenticado. |
| Persistencia | UsuarioScopeService, UsuarioEmpresa y EF | Membresía inactiva/revocada, empresa inexistente e intento cross-tenant no producen resultado. |
| Tienda pública | ConfiguredPublicStoreHostTenantResolver, VerifiedPublicStoreTenant | Host exacto permitido y host similar/malicioso denegado, sin privilegio admin. |
| JWT/roles | JwtService y consumidor de autorizaciones | Token válido sin membresía no accede a recursos; rol legacy no eleva permisos. |
| Auditoría | Marcas verificadas y prueba de auditoría tenant | Datos y errores de A no aparecen para B; evitar PII/JWT en logs. |

**Evidencia todavía faltante:** ejecución xUnit exact-SHA para cambios futuros, MySQL 8.4 cuando afecte persistencia, E2E cuando cambie flujo Angular, inventario exhaustivo de rutas/DTO y cabeceras/proxy, métricas de rechazo y rollback de cambios de BD. Esas pruebas **NO_APLICA** a esta entrega sólo documental, pero no están certificadas ni sustituidas.

## 4. Casos positivos y negativos de auditoría documental

- **P01 positivo:** cada F01–F12 indica ruta Git exacta, SHA y alcance de su afirmación; los 12 archivos son fuentes actuales leídas, no rutas propuestas.
- **P02 positivo:** se identifica qué controles corresponden a host, JWT, selección de empresa, autorización RBAC y persistencia; queda claro su consumidor.
- **N01 negativo:** X-Empresa-Id, request Host, slug, query, un claim JWT o rol legacy jamás cuentan como autorización suficiente.
- **N02 negativo:** clase o test presente no equivale a PASS live ni a certificación MySQL/HTTP, frontend o infraestructura.
- **N03 negativo:** el inventario no crea endpoints, entidades, contraseñas, usuarios, migraciones ni un ejecutor adicional.
- **N04 negativo:** el documento no habilita M01.06/M02, QA/main/PROD ni borra FAIL de revisiones anteriores.

**A3 documental:** contrastar Standard V5, PLAN H/I, COLA E/G/O, DAG F/G VALIDADO, blobs F01–F12 y BITACORA A:Z (26 campos). **A4:** publicación fast-forward dev, diff, SHA exacto, Scope Lock obligatorio, readback institucional y reversión aditiva. **A5:** revisión independiente por TASK_ID distinto del autor, antes de promociones. No registrar A5_PASS propio.
