# FASE M5 — Clientes y segmentación

Fecha: 2026-08-09
Rama: `Desarrollo`
Estado funcional enfocado: **APROBADO**

## 1. Objetivo

Completar la segmentación de Clientes sobre el catálogo normalizado `TipoCliente`, sin crear estructuras paralelas ni etiquetas subjetivas hardcodeadas, y conservar las reglas de identidad corregidas en M1.

## 2. Preflight y decisión arquitectónica

La revisión confirmó que `TipoCliente` ya constituye la fuente de verdad adecuada y no requiere una nueva tabla ni una migración adicional para M5.

La base existente ya proporciona:

- entidad y tabla normalizadas para `TipoCliente`;
- código técnico, nombre, descripción, color, orden y estado;
- CRUD administrable;
- permisos propios del módulo `TiposClientes`;
- auditoría y soft delete;
- tipo predeterminado único protegido transaccionalmente y por la integridad versionada;
- relación de Clientes con `TipoClienteId`;
- protección del tipo de sistema `SIN_CLASIFICAR`;
- conteo de clientes asignados;
- compatibilidad con las reglas de identidad de Clientes endurecidas en M1.

Por tanto, M5 reutiliza esta arquitectura y evita duplicar catálogos o introducir una migración sin necesidad funcional.

## 3. Implementación realizada

### 3.1 Segmentación operativa en Clientes

La pantalla de Clientes incorpora un filtro dinámico por `TipoClienteId`. Las opciones se obtienen del catálogo administrable; no existen nombres comerciales de segmentos codificados en el frontend.

El segmento forma parte del estado navegable de la lista y se conserva mediante el mecanismo certificado en M4:

- query params;
- `sessionStorage` aislado por usuario;
- restauración al regresar a Clientes;
- limpieza mediante `Limpiar filtros`.

### 3.2 Métricas por clasificación

Se agregó un panel de Segmentación que calcula, para cada clasificación disponible:

- cantidad de clientes;
- clientes activos;
- número acumulado de ventas;
- monto total vendido.

Las tarjetas son interactivas y permiten aplicar o retirar el segmento directamente. Las métricas respetan búsqueda y estado, pero mantienen visibles los demás segmentos para permitir comparación.

### 3.3 Compatibilidad de permisos

Si el usuario puede consultar Clientes pero no posee permiso para abrir el mantenimiento de `TiposClientes`, la lista no queda inutilizable. Las clasificaciones presentes en los DTO de Clientes pueden reconstruirse como opciones de lectura para segmentación, sin conceder permisos administrativos ni modificar el catálogo.

El enlace `Administrar clasificaciones` solo aparece cuando el usuario posee permiso `TiposClientes.Ver`.

### 3.4 Reporte / exportación

Clientes incorpora exportación CSV del **conjunto completo resultante de los filtros**, no únicamente de la página visible.

El archivo incluye:

- nombre;
- teléfono;
- identidad/RTN;
- correo;
- dirección;
- clasificación;
- estado;
- cantidad de ventas;
- total vendido.

La exportación conserva UTF-8 con BOM, escapa correctamente valores CSV y utiliza un nombre fechado.

### 3.5 `SIN_CLASIFICAR`

Se conservó la protección backend existente del registro de sistema `SIN_CLASIFICAR`:

- permanece activo;
- no puede desactivarse;
- no puede eliminarse;
- conserva el comportamiento de clasificación segura cuando no se especifica otro tipo aplicable.

M5 no sustituye esta protección por controles exclusivamente visuales.

### 3.6 Identidad del Cliente

M5 no altera la identidad comercial ni documental saneada en M1:

- `Nombre` continúa siendo dato de búsqueda, no identidad universal;
- la identidad/RTN conserva la regla de unicidad cuando existe;
- dos clientes distintos pueden compartir nombre si su identidad documental no colisiona.

## 4. Archivos principales

- `frontend/src/app/features/clientes/clientes-list.component.ts`
- `frontend/src/app/features/clientes/clientes-list.component.html`
- `frontend/src/app/features/clientes/clientes-list.component.scss`
- `frontend/e2e/m5-clientes-segmentacion.spec.ts`

M5 reutiliza, entre otros, los componentes y servicios existentes de:

- `TipoClienteService`;
- `TipoClienteService` backend;
- `TipoClientesController`;
- `TipoClienteRepository`;
- mantenimiento frontend de `tipo-clientes`.

## 5. Certificación enfocada

Ejecución GitHub Actions: `31339633125` — **SUCCESS**.

Entorno de prueba:

- MySQL 8.4 descartable;
- API ASP.NET Core en Development aislado;
- Angular local de CI;
- Chromium / Playwright.

Resultados:

- build backend: 0 errores, 0 warnings;
- pruebas backend enfocadas de Cliente/TipoCliente: **17 aprobadas, 0 fallos, 0 omitidas**;
- lint frontend: aprobado;
- Playwright M5: **3 aprobadas, 0 fallos**.

Los escenarios E2E comprueban:

1. creación de clasificaciones dinámicas y protección real de `SIN_CLASIFICAR`;
2. filtro por segmento, métricas y restauración de navegación;
3. descarga y contenido del CSV filtrado.

La ejecución preliminar `31339453821` detectó únicamente una ambigüedad del selector E2E causada porque el mismo cliente existe simultáneamente en la tabla desktop y en las tarjetas responsive del DOM. El selector se hizo explícito para la tabla desktop; no se modificó lógica de negocio para ocultar el problema y la reejecución completa quedó verde.

## 6. Base de datos y Producción

M5 no introduce migraciones porque el modelo relacional requerido ya existía y fue suficiente para el alcance aprobado.

No se ejecutaron cambios sobre Producción, no se modificaron credenciales productivas, no se desplegó y no se tocó `main`.

## 7. Criterio de cierre

M5 puede declararse completada cuando, además de la certificación enfocada anterior, el HEAD definitivo de `Desarrollo` pase los gates oficiales del repositorio incluyendo la regresión permanente `m5-clientes-segmentacion.spec.ts`.
