# FASE M8 — Búsqueda inteligente y rendimiento

Fecha de cierre: 2026-08-10  
Rama: `Desarrollo`  
HEAD funcional certificado antes del cierre documental: `377dfbff26f4bceb900733d6b72e8d0f3bc58692`  
Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

## 1. Objetivo

Cerrar la evolución de búsqueda y rendimiento de Solqaryn sin crear un motor paralelo, reutilizando la arquitectura existente de productos, variantes, clientes, proveedores, ventas y compras. La fase cubre búsqueda multidimensional, autocompletado, paginación, cancelación de consultas obsoletas, observabilidad segura de latencia y decisiones de índices basadas en evidencia.

## 2. Cobertura funcional cerrada

### 2.1 Productos y variantes

La búsqueda de productos/variantes reconoce, según el contexto operativo:

- nombre del producto;
- descripción;
- categoría;
- SKU;
- código de barras;
- Marca;
- Modelo;
- Color;
- Talla;
- compatibilidad con campos legacy que todavía forman parte del contrato de lectura.

El escáner mantiene resolución exacta por SKU/código de barras y detección de ambigüedad. El autocompletado de variantes sigue limitado, ordenado y sin cargar inventario completo en memoria.

### 2.2 Clientes

El autocompletado de clientes permite buscar por:

- nombre;
- identidad/RTN;
- correo;
- teléfono.

La consulta es `AsNoTracking`, limita el resultado a un máximo técnico de 30 elementos y conserva la regla M1 de que el nombre no constituye identidad global.

### 2.3 Proveedores

El autocompletado de proveedores permite buscar por:

- nombre;
- documento;
- correo;
- teléfono.

La consulta es `AsNoTracking` y limita el resultado a un máximo técnico de 30 elementos.

### 2.4 Ventas

La lista paginada de ventas amplía la búsqueda a:

- número de venta;
- nombre del cliente;
- identidad/RTN del cliente;
- teléfono del cliente;
- correo del cliente;
- notas/observaciones pertinentes.

La lectura paginada utiliza `AsNoTracking` y conserva ordenamiento, paginación y alcance por usuario/administrador.

### 2.5 Compras

La lista paginada de compras amplía la búsqueda a:

- número de compra;
- nombre del proveedor;
- documento del proveedor;
- teléfono del proveedor;
- documento de referencia;
- notas/observaciones pertinentes.

El correo del proveedor sí forma parte de la búsqueda del mantenimiento/autocompletado de proveedores. No se inventó una búsqueda por correo dentro del snapshot de `Compra`, porque la entidad histórica actual no persiste `ProveedorCorreo`.

La lectura paginada utiliza `AsNoTracking` y conserva ordenamiento, paginación y alcance por usuario/administrador.

## 3. Rendimiento y comportamiento del frontend

Se reforzó el patrón de búsqueda reactiva sin aumentar timeouts ni relajar validaciones:

- `debounce` en búsquedas interactivas;
- cancelación explícita de solicitudes HTTP anteriores en listas de Productos, Ventas y Compras;
- protección por secuencia para impedir que una respuesta tardía reemplace resultados más recientes;
- liberación de suscripciones en `ngOnDestroy`;
- `switchMap` en autocompletados de venta donde ya corresponde;
- paginación backend en listados de volumen;
- límites técnicos en autocompletados;
- preservación del estado de navegación implementado en M4.

No se introdujeron cargas completas de grandes catálogos para resolver búsquedas operativas.

## 4. Observabilidad p50/p95

Se consolidó telemetría de rendimiento para las rutas de búsqueda relevantes:

- `/productos`;
- `/clientes/buscar`;
- `/proveedores/buscar`;
- `/ventas/productos/buscar`;
- `/ventas/productos/por-codigo`;
- `/compras/productos/buscar`;
- `/compras/productos/por-codigo`.

La ventana de medición se mantiene acotada a 200 muestras por ruta y calcula p50/p95. La telemetría registra únicamente metadatos operativos seguros como ruta, duración, percentiles, cantidad de muestras/resultados, longitud del término, estado HTTP y correlation ID.

**No se registran términos de búsqueda, SKU, códigos de barras, teléfonos, correos ni otros identificadores de negocio en la telemetría de rendimiento.**

## 5. Índices y estrategia de base de datos

Se auditó el modelo existente antes de agregar índices nuevos.

Índices/restricciones relevantes ya disponibles:

- SKU de variante: único;
- código de barras de variante: único cuando existe;
- identidad/RTN de cliente: único cuando existe;
- documento de proveedor: único cuando existe;
- FKs e índices de Producto/Marca/Modelo/Color/Talla en la arquitectura de variantes.

No se agregaron índices especulativos para consultas con `%Contains%`, porque un índice B-tree convencional no garantiza resolver ese patrón de forma eficiente. Cualquier índice textual adicional deberá justificarse con medición real de base de datos y plan de ejecución, evitando sobreindexación y degradación de escrituras.

M8 no requiere una migración de esquema nueva.

## 6. Archivos principales involucrados

Backend:

- `backend/src/API/Filters/BusquedaRendimientoMetricas.cs`
- `backend/src/API/Filters/MedirRendimientoBusquedaFilter.cs`
- `backend/src/Infrastructure/Repositories/ProductoRepository.cs`
- `backend/src/Infrastructure/Repositories/ProductoVarianteRepository.cs`
- `backend/src/Infrastructure/Repositories/ClienteRepository.cs`
- `backend/src/Infrastructure/Repositories/ProveedorRepository.cs`
- `backend/src/Infrastructure/Repositories/VentaRepository.cs`
- `backend/src/Infrastructure/Repositories/CompraRepository.cs`
- `backend/tests/Solqaryn.Tests/M8BusquedaRepositoriosTests.cs`

Frontend:

- `frontend/src/app/features/productos/productos-list.component.ts`
- `frontend/src/app/features/ventas/ventas-list.component.ts`
- `frontend/src/app/features/compras/compras-list.component.ts`
- `frontend/src/app/features/ventas/venta-form.component.ts`

## 7. Regresión permanente M8

Se incorporaron pruebas permanentes que verifican:

1. búsqueda paginada de ventas por teléfono y notas, sin tracking residual;
2. búsqueda paginada de compras por referencia/documento de proveedor, sin tracking residual;
3. límite máximo de 30 resultados en autocompletado de clientes, sin tracking residual;
4. límite máximo de 30 resultados en autocompletado de proveedores, sin tracking residual.

Durante la implementación el CI detectó dos defectos de compilación en las pruebas/cambio iniciales —un snapshot de correo inexistente en `Compra` y el nombre real de `AppDbContext.TipoClientes`—. Ambos se corrigieron en la rama antes de certificar; no se ocultaron ni se relajaron gates.

## 8. Evidencia automática de certificación

### 8.1 Desarrollo — Compilación y pruebas

Run `31362727082` — **SUCCESS**.

Evidencia relevante:

- backend Release: **0 errores / 0 warnings**;
- pruebas backend no integración: **236 passed / 0 failed / 0 skipped**;
- frontend lint: **SUCCESS**;
- frontend build producción: **SUCCESS**;
- Docker/aislamiento: **SUCCESS**;
- migraciones/variantes sobre MySQL descartable: **SUCCESS**.

### 8.2 Fase 8 — Validación completa automatizada

Run `31362727118` — **SUCCESS**.

Entorno y resultados:

- MySQL 8.4 descartable;
- backend Release: **0 errores / 0 warnings**;
- suite backend completa: **254 passed / 0 failed / 0 skipped**;
- auditoría de paquetes .NET: sin paquetes vulnerables reportados por el gate;
- auditoría de dependencias de producción npm: **0 vulnerabilidades** en el gate de producción;
- lint y build de producción Angular: **SUCCESS**;
- pruebas Playwright especializadas: **7 passed / 0 failed**;
- validación responsive: 320x568 y 3840x2160;
- validación de seguridad HTTP y superficie pública: **SUCCESS**;
- presupuesto controlado de rendimiento de API/navegación: **SUCCESS**;
- auditoría de runtime: **APROBADA**;
- evidencia publicada como artifact `fase8-validacion-completa`.

### 8.3 Desarrollo — aceptación funcional integral

Run `31362727124` — **SUCCESS**.

El gate integral terminó correctamente incluyendo backend, MySQL descartable, frontend, Angular, aceptación Playwright, validación SMTP/PDF y publicación de evidencia.

### 8.4 Gates complementarios

- `Bloque 2C.1 - Variante técnica y migración` run `31362727109` — **SUCCESS**.
- `Fase 2 - Auditoría de configuración y dependencias` run `31362727170` — **SUCCESS**.
- `Solqaryn CI` run `31362727113` — **SKIPPED** por su condición de workflow; no se contabiliza como gate verde.

## 9. Seguridad y compatibilidad

La fase no modifica Producción, no crea ramas, no toca `main`, no fusiona PR #2 y no habilita auto-merge.

Se preservan:

- separación Producto/ProductoVariante;
- stock exacto por variante;
- snapshots históricos;
- permisos y alcance por usuario;
- reglas de identidad de Cliente/Proveedor;
- M4 de persistencia de filtros/navegación;
- escáner exacto de SKU/código de barras;
- política de no registrar datos sensibles en telemetría.

## 10. Cierre

**FASE M8 — Búsqueda inteligente y rendimiento: ✅ COMPLETADA Y CERTIFICADA AUTOMÁTICAMENTE.**

El siguiente bloque del Plan Maestro es **M9 — Cargas masivas profesionales**. M9 no forma parte de este cierre y no se inicia sin autorización expresa.
