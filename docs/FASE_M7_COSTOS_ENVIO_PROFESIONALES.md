# FASE M7 — Costos de envío profesionales

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

Fecha de cierre técnico: 2026-08-09 (Honduras)
Rama exclusiva: `Desarrollo`
PR oficial: `#2 Desarrollo -> main` (abierto y borrador)
Producción: no intervenida

## 1. Objetivo

Evolucionar el costo de envío existente hacia un motor profesional de tarifas sin crear un segundo sistema paralelo, preservando la garantía MySQL de un solo predeterminado activo ya saneada en M1.

M7 consolida:

- departamento;
- ciudad;
- zona;
- modalidad/tipo de envío;
- monto;
- prioridad;
- vigencia;
- estado activo/inactivo;
- predeterminado único;
- resolución de tarifa;
- historial/auditoría;
- snapshots inmutables en Venta y Factura;
- eliminación/desactivación segura.

## 2. Arquitectura reutilizada

Se conserva `CostoEnvio` como fuente única de verdad. No se creó otra tabla de tarifas ni otra lógica paralela.

La entidad conserva la infraestructura previa:

- `Nombre`;
- `Descripcion`;
- `Monto`;
- `VigenteDesde` / `VigenteHasta`;
- `Prioridad`;
- `EsPredeterminado`;
- `Activo`;
- `Eliminado`;
- auditoría;
- columna generada `PredeterminadoActivoUnico` e índice único MySQL.

M7 agrega las dimensiones opcionales:

- `Departamento`;
- `Ciudad`;
- `Zona`;
- `Modalidad`.

Los campos son opcionales deliberadamente: una tarifa con dimensión nula actúa como regla general para ese nivel y no obliga a registrar valores ficticios.

## 3. Resolución profesional

Se incorporó resolución de tarifas vigentes por cobertura.

Contrato:

`POST /api/costos-envio/resolver`

Criterios disponibles:

- departamento;
- ciudad;
- zona;
- modalidad;
- fecha UTC de evaluación.

Algoritmo:

1. solo considera registros activos, no eliminados y dentro de vigencia;
2. cada dimensión configurada en una regla exige coincidencia exacta normalizada;
3. una dimensión nula funciona como comodín;
4. se prioriza la regla con mayor especificidad geográfica/modal;
5. a igual especificidad gana el menor valor de `Prioridad`;
6. a igualdad completa se usa el menor `Id` como desempate determinista;
7. si ninguna regla específica coincide, se usa el costo predeterminado vigente;
8. si tampoco existe predeterminado aplicable, la operación falla cerrada.

La resolución evita decisiones ambiguas y no modifica los costos históricos ya aplicados.

## 4. Predeterminado e integridad

La restricción MySQL existente de predeterminado único se mantiene intacta.

M7 preserva además el cambio atómico de predeterminado certificado en M1 y endurece los flujos para impedir dejar el sistema sin regla global por una acción administrativa accidental:

- no se puede desmarcar/desactivar directamente el predeterminado activo sin reemplazo válido;
- no se puede eliminar lógicamente el predeterminado activo sin reemplazo;
- crear o promover otro predeterminado mantiene la transición transaccional segura;
- la base continúa siendo la defensa final contra dos predeterminados simultáneos.

## 5. Vigencia y prioridad

Cada tarifa puede definir:

- `VigenteDesde`;
- `VigenteHasta`;
- `Prioridad` no negativa;
- `Activo`.

El DTO expone `EstaVigente`, calculado por backend, para que la UI no invente reglas temporales distintas a las del dominio.

Ventas solo ofrece tarifas activas y vigentes.

## 6. Historial y auditoría

Se añadió consulta de historial por tarifa:

`GET /api/costos-envio/{id}/historial`

Utiliza la infraestructura central de auditoría y filtra por:

- entidad `CostoEnvio`;
- `ReferenciaId`;
- paginación.

Las operaciones de edición, activación/desactivación y eliminación lógica registran valores anteriores y nuevos cuando corresponde, permitiendo reconstruir cambios de monto, cobertura, modalidad, prioridad, vigencia y estado.

No se creó un historial paralelo que compita con la auditoría central.

## 7. Snapshots históricos de Venta y Factura

Antes de M7 Venta/Factura preservaban principalmente identidad, nombre y monto del costo de envío. M7 amplía el snapshot documental con:

- `CostoEnvioDepartamentoSnapshot`;
- `CostoEnvioCiudadSnapshot`;
- `CostoEnvioZonaSnapshot`;
- `CostoEnvioModalidadSnapshot`.

`CalculoService` transporta la cobertura exacta de la tarifa aplicada; `VentaService` la persiste en la venta y la transfiere a la factura. Los DTO de Venta y Factura devuelven el snapshot almacenado.

Consecuencia: renombrar, cambiar cobertura, modificar modalidad, monto o vigencia de una tarifa futura no reescribe el contexto de una Venta/Factura histórica.

## 8. UI administrativa y Ventas

El mantenimiento de Costos de envío ahora permite administrar:

- departamento;
- ciudad;
- zona;
- modalidad;
- monto;
- prioridad;
- vigencia;
- activo;
- predeterminado.

La tabla muestra cobertura/modalidad y distingue:

- vigente;
- fuera de vigencia;
- inactivo.

El selector de Ventas filtra los costos para presentar únicamente opciones activas y vigentes, preservando la selección explícita cuando el negocio requiere una tarifa concreta.

## 9. Base de datos y migraciones

M7 se implementó mediante migraciones EF Core oficiales y snapshot sincronizado.

### `M7CostosEnvioProfesionales`

Archivo principal:

`backend/src/Infrastructure/Persistence/Migrations/20260810013029_M7CostosEnvioProfesionales.cs`

Incorpora cobertura geográfica/modal e índice de resolución sin destruir datos existentes.

### `M7SnapshotsProfesionalesEnvio`

Archivo principal:

`backend/src/Infrastructure/Persistence/Migrations/20260810014301_M7SnapshotsProfesionalesEnvio.cs`

Incorpora exclusivamente los snapshots profesionales de cobertura en Venta/Factura y actualiza el `AppDbContextModelSnapshot` mediante EF Core.

Durante la certificación se detectó que esta segunda migración intentaba repetir el DDL de `Ciudad`, `Departamento`, `Zona`, `Modalidad` e índice ya creado por `M7CostosEnvioProfesionales`. Se corrigió la migración histórica para eliminar ese DDL duplicado, conservando una sola responsabilidad por migración. El historial completo volvió a aplicarse correctamente sobre MySQL 8.4 descartable.

No se ejecutó ninguna migración contra Producción.

## 10. Pruebas y validaciones

Se añadió/reforzó cobertura para:

- costo explícitamente seleccionado;
- predeterminado vigente;
- exoneración de envío;
- preservación de cobertura profesional Departamento/Ciudad/Zona/Modalidad;
- historial completo de migraciones sobre MySQL 8.4 descartable;
- compilación backend Release;
- pruebas unitarias e integración MySQL;
- frontend lint;
- frontend build producción;
- Playwright integral;
- seguridad/runtime/accesibilidad;
- generación oficial de migraciones EF.

Workflow focalizado de implementación M7:

- `31347786727` — **SUCCESS**.

HEAD funcional certificado:

`68849cf15513fbd76a116db568545403d3acfd20`

Gates oficiales sobre ese HEAD:

- `31348315383` — Desarrollo - Compilación y pruebas — **SUCCESS**;
- `31348315380` — Desarrollo - aceptación funcional integral — **SUCCESS**;
- `31348315366` — Fase 2 - Auditoría de configuración y dependencias — **SUCCESS**;
- `31348315388` — Bloque 2C.1 - Variante técnica y migración — **SUCCESS**;
- `31348315404` — Fase 8 - Validación completa automatizada — **SUCCESS**;
- `31348315372` — Solqaryn CI — **SKIPPED** esperado por su configuración actual.

La ejecución `31348315383` validó adicionalmente:

- backend Release y pruebas;
- frontend producción;
- migraciones EF, variantes y cargas masivas en MySQL 8.4;
- integración MySQL;
- Docker/aislamiento de entornos;
- higiene del repositorio.

## 11. Incidencias encontradas y correcciones

Durante M7 se detectaron y corrigieron tres incidencias sin debilitar validaciones:

1. un primer lote no encontraba una firma exacta en `FacturaConfiguration`; falló cerrado antes de publicar cambios parciales;
2. el primer runner documental del Plan Maestro no creó job correctamente y fue sustituido por una versión mínima que actualizó el plan y se autoeliminó;
3. el gate MySQL detectó DDL duplicado entre las dos migraciones M7; se separaron correctamente sus responsabilidades y el historial completo pasó en MySQL 8.4.

Ninguna incidencia afectó Producción ni dejó cambios parciales inseguros.

## 12. Seguridad del proyecto

M7 no autorizó ni realizó:

- modificaciones de `main`;
- creación de ramas;
- merge o auto-merge del PR #2;
- despliegues productivos;
- migraciones sobre Producción;
- modificación de secretos, credenciales, dominios, bases o activos productivos.

Verificación final:

- `main`: `85b4e02814823e9671803c23798a6ff0bf05c8f6`;
- PR #2: abierto, borrador, `Desarrollo -> main`, sin merge;
- Producción: congelada.

## 13. Dictamen técnico

M7 transforma el costo de envío previo en un motor de tarifas profesional, determinista y auditable, preservando historial documental y compatibilidad.

La regla conceptual final es:

`Tarifa aplicable = vigencia + cobertura más específica + prioridad + fallback predeterminado`

Los documentos confirmados conservan el snapshot real aplicado y no dependen de la configuración futura de la tarifa.

**M7 — COMPLETADA / CERTIFICADA.**

## 14. Siguiente fase

Corresponde continuar con:

**M8 — Búsqueda inteligente y rendimiento**.
