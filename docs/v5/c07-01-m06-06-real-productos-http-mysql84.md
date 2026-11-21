# C07-01.M06.06 — integración incremental R2 de /productos (DEV)
PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev

## A0. Objetivo y cambio

Una suite de Integration ejecuta el **ProductosController real** y el **GET /productos** bajo ASP.NET Core TestServer, validación de JWT firmado, CurrentUserService y servicios verdaderos de membresía y autorización IPermisoService sobre Oracle EF y MySQL 8.4 efímero. ProductoService y ProductoRepository son reales, ejecutan lectura relacional. Los doubles de IUnitOfWork, IAuditoriaService, IImageStorageService y IProductoVarianteService satisfacen dependencias constructoras de otras acciones de escritura nunca invocadas; no acreditan CRUD/archivos/auditoría. No se inserta catálogo comercial: el resultado positivo solo demuestra acceso y consulta vacía por la API real.

A1/A2 dirigidas: token anónimo o manipulado => 401; JWT verdadero de A al seleccionar B y de B al seleccionar A => 403; A y B con membresía propia y grants vigentes => 200; membresía A revocada con JWT no expirado => 403; al reactivar membresía => 200; grant Productos.Ver revocado en base => 403, sin revocar el grant de B. Una cabecera X-Empresa-Id actúa exclusivamente como selector y no como autoridad.

A3 dirigida: esquema único aleatorio en mysql:8.4 loopback, base bootstrap exacta, versión SQL comprobada, inicialización Oracle EF real, limpieza EnsuredDeleted únicamente del esquema aislado. Pipeline GitHub-hosted exige TRX de la suite con al menos un test realmente ejecutado, 0 fail, 0 skipped, y validación exact-SHA Scope Lock/skill.

## Limitación explícita, sin certificación de fase

Este incremento todavía **NO** certifica escrituras reales de Producto y Variante, aislamiento de filas de producto materializadas, conflictos de versión, rollback transaccional de catálogo, navegador Angular conectado a API firmada, auditoría persistente, API productiva hospedada ni migraciones globales Oracle EF. Por ello mantener C07-01.M06.06 EN_PROGRESO y el namedRange V17 legítimo de DEV01. El objetivo E1045 de fase acumulada sigue abierto; agregar pruebas A0-A4 para estos faltantes y obtener SUCCESS exact-SHA antes de VALIDANDO/LISTO. A5 únicamente por una Task Javier distinta. No tocar QA/main/PROD/Aiven ni secretos.

## Impacto y rollback

Solo test de backend, workflow de CI dirigido, este documento y CHANGELOG_AI íntegro. No API/runtime/config productiva, BD, contrato DTO, feature flag o migración. Ante falla de CI, examinar TRX/log y corregir el defecto específico en un siguiente commit FF/CAS; nunca reclasificar FAIL/SKIPPED como PASS. Deshacer un incremento incorrecto mediante git revert seguro con HEAD actualizado y readback; sin force-push o limpieza histórica.
