# FASE M9 — Cargas masivas profesionales

Fecha de cierre: 2026-08-10  
Rama exclusiva: `Desarrollo`  
HEAD funcional certificado: `2aad9ce8f66310b9448fbb339d52ffb4f024f38f`  
PR oficial: `#2 Desarrollo -> main`  
Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

## 1. Objetivo

Profesionalizar la infraestructura de cargas masivas existente en Solqaryn sin crear un segundo motor paralelo y sin debilitar las garantías ya certificadas de seguridad, idempotencia, concurrencia y atomicidad.

M9 conserva como fuente única de verdad el subsistema existente `CargasMasivas` y agrega contrato versionado, seguimiento por etapas, métricas operativas, vista previa controlada, regresión permanente y un gate CI específico.

## 2. Preflight real

Antes de modificar código se comprobó que Solqaryn ya disponía de:

- `CargasMasivasController`;
- `ICargaMasivaService` / `CargaMasivaService`;
- entidades `CargaMasiva` y `CargaMasivaError`;
- soporte CSV y XLSX sin macros;
- preview previo a persistencia;
- validación estructural y de negocio;
- códigos de error consistentes;
- descarga de errores en CSV/XLSX;
- hash SHA-256 e idempotencia por archivo/tipo;
- bloqueo de confirmación concurrente;
- confirmación dentro de una transacción completa;
- rollback fail-closed;
- protección contra stock obsoleto después de validar;
- historial y auditoría;
- carga `VariantesInventario` sobre `ProductoVariante` con Producto, Marca, Modelo, Color y Talla normalizados.

Por tanto, M9 amplió esta arquitectura en vez de reemplazarla.

## 3. Contrato profesional de plantilla

Se incorporó un contrato explícito y versionado:

- versión vigente: `M9.1`;
- formatos admitidos: CSV/XLSX según configuración existente;
- máximo de filas conservado desde el motor seguro existente;
- tamaño de lote operativo publicado: `250` filas;
- máximo de filas mostrado simultáneamente en vista previa: `200`;
- etapas estándar: `Carga`, `Lectura`, `Validacion`, `VistaPrevia`, `Confirmacion`.

Todas las plantillas descargadas incorporan la versión en el nombre del archivo.

Si un consumidor solicita explícitamente una versión distinta de `M9.1`, el backend falla cerrado y obliga a descargar la plantilla vigente. No se interpreta silenciosamente una versión antigua como si fuera actual.

### Decisión de atomicidad

El tamaño de lote operativo de 250 filas sirve como contrato de capacidad/UI y como base para evolución de procesamiento incremental. **No se introdujeron commits parciales por lote**: la confirmación completa continúa dentro de la misma transacción de base de datos. Si cualquier fila falla durante confirmación, todo el conjunto se revierte.

Esta decisión prioriza integridad de inventario y documentos sobre aparentar progreso mediante escrituras parciales irreversibles.

## 4. Progreso por etapas y métricas

Se agregó:

`GET /cargas-masivas/{id}/progreso`

El endpoint conserva las mismas reglas de autenticación, permiso y aislamiento por propietario/administrador que el detalle de la carga.

Expone:

- estado global;
- etapa actual;
- porcentaje;
- total de filas;
- filas correctas;
- filas con error;
- filas omitidas cuando existan advertencias con semántica `OMIT*`;
- filas procesadas;
- registros creados;
- registros actualizados;
- versión de plantilla;
- estado individual de cada etapa.

No se inventa un número de omitidos: la métrica se deriva de códigos explícitos de advertencia y permanece en cero cuando no existen filas realmente omitidas.

## 5. UI/UX de cargas masivas

La pantalla fue evolucionada a **Cargas masivas profesionales** e incorpora:

- versión vigente visible;
- tamaño de lote operativo visible;
- descarga de plantilla Excel/CSV versionada;
- selección segura de archivo;
- validación antes de confirmar;
- progreso visual por etapas;
- barra de porcentaje;
- métricas Correctas / Errores / Omitidas / Procesadas;
- códigos técnicos de validación visibles en observaciones;
- vista previa limitada a un máximo configurado para evitar renderizar miles de filas;
- descarga completa de errores en Excel/CSV;
- mensaje explícito de atomicidad;
- historial de cargas y resultados.

## 6. Variantes multidimensionales

`VariantesInventario` continúa siendo el flujo profesional para cantidades, costo, precio, SKU, código de barras y dimensiones exactas de inventario:

`Producto + Marca + Modelo + Color + Talla`

Se preservan:

- resolución contra tablas normalizadas `Marcas`, `Modelos`, `Colores`, `Tallas`;
- relación Modelo -> Marca;
- SKU/código de barras;
- snapshots de cantidad al validar;
- detección de cambios concurrentes antes de confirmar;
- movimiento de inventario por diferencia de stock;
- recálculo del resumen agregado del Producto.

El importador histórico `Productos` conserva temporalmente su compatibilidad legacy existente. M9 **no lo presenta como fuente de verdad de stock multidimensional**; para inventario exacto el contrato oficial es `VariantesInventario`.

## 7. Seguridad e integridad preservadas

M9 no relajó ninguna de las garantías existentes:

- archivos con extensiones no permitidas: rechazados;
- XLSX potencialmente peligroso/macros: rechazado por la capa de seguridad existente;
- archivo confirmado previamente: no se reimporta;
- confirmaciones simultáneas de la misma carga: bloqueadas;
- carga ajena: no accesible salvo administrador autorizado;
- fila/archivo inválido: no confirmable;
- stock cambiado después del preview: exige revalidar;
- error en confirmación: rollback completo;
- auditoría de validación y confirmación: conservada;
- Producción: no intervenida.

M9 no requirió una nueva migración de esquema.

## 8. Regresión permanente M9

### Backend

Archivo:

`backend/tests/Solqaryn.Tests/M9CargaMasivaProfesionalTests.cs`

Cubre:

1. versión M9.1;
2. tamaño de lote operativo;
3. límite de vista previa;
4. etapas estándar;
5. versión por tipo de plantilla;
6. DTO de progreso y métricas;
7. endpoint `/{id:int}/progreso`;
8. permanencia de `VariantesInventario` como tipo oficial.

El gate M9 ejecuta además la regresión de concurrencia preexistente de cargas.

Resultado certificado: **7 pruebas backend / 7 aprobadas / 0 fallos / 0 omitidas**.

### Playwright

Archivo nuevo:

`frontend/e2e/m9-cargas-masivas-profesionales.spec.ts`

Se ejecuta junto con:

`frontend/e2e/fase5-cargas-masivas.spec.ts`

Comprueba:

- contrato M9.1;
- plantilla vigente;
- rechazo de versión obsoleta;
- progreso por etapas;
- métricas correctas/error/omitidas;
- UI profesional;
- regresión histórica de cargas masivas.

Resultado certificado: **7 pruebas Playwright / 7 aprobadas / 0 fallos / 0 omitidas**.

## 9. Defectos detectados durante implementación

Se encontraron dos fallos reales antes del cierre y ambos se corrigieron sin reducir cobertura ni aumentar timeouts:

1. el nuevo archivo de pruebas backend omitía `using Xunit;`, provocando fallo de compilación del test;
2. una expectativa Playwright comparaba texto JSON crudo y fallaba porque el serializer representó `á` como Unicode escapado; se corrigió para validar el JSON parseado y el mensaje real.

Los dos fallos quedaron visibles en CI y se corrigieron antes de certificar.

## 10. Evidencia automática

HEAD funcional certificado:

`2aad9ce8f66310b9448fbb339d52ffb4f024f38f`

### Gate específico M9

Workflow: `M9 - Cargas masivas profesionales`  
Run: `31388228721` — **SUCCESS**

Incluyó:

- MySQL 8.4 descartable;
- backend Release;
- regresión backend M9 + concurrencia;
- API real;
- Angular lint;
- Angular build de producción;
- Angular servido;
- Playwright M9 + regresión histórica;
- publicación de evidencia.

Artifact:

- nombre: `m9-cargas-masivas-profesionales`;
- ID: `9062652130`;
- SHA-256: `4a1a59babbfb3fb47d416e1ea00388e2f5a05dc535aa9370795ba6921e1509cf`.

### Gates transversales sobre el mismo HEAD

- `Desarrollo - Compilación y pruebas` run `31388228716` — **SUCCESS**;
- `Desarrollo - aceptación funcional integral` run `31388228725` — **SUCCESS**;
- `Fase 8 - Validación completa automatizada` run `31388228755` — **SUCCESS**;
- `Bloque 2C.1 - Variante técnica y migración` run `31388228723` — **SUCCESS**;
- `Fase 2 - Auditoría de configuración y dependencias` run `31388228793` — **SUCCESS**;
- `Solqaryn CI` run `31388228732` — **SKIPPED** por condición propia del workflow; no se contabiliza como fallo.

## 11. Archivos principales de M9

Backend:

- `backend/src/API/Controllers/CargasMasivasController.cs`;
- `backend/src/Application/DTOs/CargaMasivaDto.cs`;
- `backend/tests/Solqaryn.Tests/M9CargaMasivaProfesionalTests.cs`.

Frontend:

- `frontend/src/app/core/models/carga-masiva.model.ts`;
- `frontend/src/app/services/carga-masiva.service.ts`;
- `frontend/src/app/features/cargas-masivas/cargas-masivas.component.ts`;
- `frontend/src/app/features/cargas-masivas/cargas-masivas.component.html`;
- `frontend/e2e/m9-cargas-masivas-profesionales.spec.ts`.

CI:

- `.github/workflows/m9-cargas-masivas.yml`.

## 12. Seguridad del repositorio

Durante M9:

- solo se trabajó sobre `Desarrollo`;
- `main` no fue modificada;
- no se creó ninguna rama nueva;
- PR #2 no fue fusionado;
- no se habilitó auto-merge;
- no se ejecutaron migraciones contra Producción;
- no se modificaron credenciales, dominios, bases, servicios ni activos de Producción.

## 13. Cierre

**FASE M9 — Cargas masivas profesionales: ✅ COMPLETADA Y CERTIFICADA AUTOMÁTICAMENTE.**

Siguiente fase del Plan Maestro:

**M10 — UI/UX empresarial y accesibilidad.**

M10 no forma parte de este cierre.
