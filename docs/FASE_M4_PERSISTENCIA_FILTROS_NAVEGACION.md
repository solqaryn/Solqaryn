# FASE M4 — Persistencia de filtros y navegación

Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

Fecha: 2026-08-09  
Rama: `Desarrollo`  
PR oficial: `#2 Desarrollo -> main`  
Producción: **sin cambios**

> Esta fase modifica únicamente comportamiento de aplicación y pruebas sobre `Desarrollo` y entornos descartables de CI. No incluye despliegue ni modificación de Producción.

---

## 1. Objetivo

Conservar el contexto de trabajo del usuario al buscar, filtrar, ordenar, paginar, entrar a detalle/edición y regresar a los listados principales.

Cobertura M4:

- Productos;
- Ventas;
- Compras;
- Clientes;
- Inventario;
- Finanzas.

La solución usa dos capas complementarias:

1. **query params**: estado visible, navegable y recuperable desde la URL;
2. **sessionStorage**: respaldo del contexto durante la sesión para retornos desde detalle/edición o navegación interna que no conserve explícitamente los parámetros.

La URL tiene precedencia sobre `sessionStorage` cuando ambos contienen el mismo campo.

---

## 2. Servicio común de estado de navegación

Se agregó:

`frontend/src/app/core/navigation/list-navigation-state.service.ts`

Responsabilidades:

- restaurar valores por defecto;
- restaurar estado guardado en sesión;
- aplicar query params sobre el estado restaurado;
- normalizar valores `string`, `number` y `boolean`;
- persistir únicamente parámetros diferentes de los defaults;
- usar `replaceUrl` para no llenar el historial del navegador con cada pulsación o cambio de filtro;
- tolerar navegadores donde `sessionStorage` no esté disponible;
- limpiar el estado de una pantalla de forma explícita.

### Aislamiento por usuario

La clave de sesión se compone con:

`solqaryn.navigation.v1.<usuario>.<scope>`

Por tanto, los filtros de un usuario autenticado no se reutilizan como estado de otro usuario.

No se almacenan credenciales, tokens ni secretos dentro de esta persistencia.

---

## 3. Productos

Se conserva y normaliza el estado de:

- búsqueda;
- categoría;
- marca;
- modelo dependiente de marca;
- color;
- talla;
- estado;
- página;
- `pageSize`;
- columna de orden;
- dirección de orden.

Al restaurar Marca también se recargan sus Modelos, evitando una UI incoherente.

`Limpiar filtros` elimina el respaldo de sesión y devuelve todos los parámetros a sus defaults.

---

## 4. Ventas

Persistencia implementada para:

- búsqueda;
- página;
- `pageSize`;
- orden por Fecha, Cliente o Total;
- dirección ascendente/descendente.

El orden utiliza los campos ya soportados por `VentaRepository`; no se introdujo una segunda autoridad de orden en frontend.

Se agregó `Limpiar filtros`.

---

## 5. Compras

Persistencia implementada para:

- búsqueda;
- página;
- `pageSize`;
- orden por Fecha, Proveedor o Total;
- dirección ascendente/descendente.

El orden utiliza los campos ya soportados por `CompraRepository`.

Se agregó `Limpiar filtros`.

---

## 6. Clientes

El listado existente no contaba con un estado equivalente de búsqueda/filtros/paginación.

Se incorporó:

- búsqueda por nombre, teléfono, identidad/RTN, correo y tipo de cliente;
- filtro Activo/Inactivo;
- orden por nombre, cantidad de ventas y total vendido;
- página;
- `pageSize` 10/25/50;
- paginador;
- `Limpiar filtros`;
- persistencia URL + sesión.

La fuente de datos continúa siendo el servicio existente de Clientes. El filtrado/paginado añadido en M4 es de presentación y no altera contratos históricos del backend.

---

## 7. Inventario

Cobertura sobre `/inventario/movimientos`:

- tipo de movimiento;
- búsqueda por producto, color, SKU, referencia y responsable;
- orden por fecha, producto o tipo;
- página;
- `pageSize`;
- `Limpiar filtros`;
- URL + sesión.

El filtro de tipo continúa utilizando el endpoint existente; búsqueda, orden y paginación de la vista se realizan sobre el resultado recibido.

### Carrera detectada y corregida

La primera regresión enfocada (`31337200840`) detectó que Inventario guardaba el estado restaurado solo después de terminar la llamada HTTP. Si el usuario navegaba fuera antes de recibir la respuesta, el estado visible de la URL todavía no estaba respaldado en `sessionStorage`.

Se corrigió persistiendo el estado restaurado **antes de iniciar la red**.

Por la misma razón se endurecieron preventivamente Clientes y Finanzas, eliminando la misma carrera potencial.

---

## 8. Finanzas

Persistencia implementada sobre los movimientos visibles de Finanzas:

- búsqueda;
- filtro por Ingreso/Egreso/Ajuste;
- orden por fecha, concepto, tipo o monto;
- `Limpiar filtros`;
- URL + sesión.

No se agregó paginación artificial al dashboard financiero porque la pantalla actual no usa un contrato paginado. M4 persiste el estado real de la pantalla sin inventar un flujo backend paralelo.

---

## 9. Precedencia y retorno desde detalle/edición

Regla final:

1. defaults de la pantalla;
2. `sessionStorage` del usuario actual;
3. query params presentes en la URL.

Por tanto:

- una URL explícita siempre puede reemplazar el estado guardado;
- al entrar a un detalle/edición y volver a una ruta limpia, el estado de sesión reconstruye el contexto;
- `Limpiar filtros` elimina el contexto persistido y limpia parámetros no-default de la URL.

---

## 10. Prueba enfocada M4

Se agregó permanentemente:

`frontend/e2e/m4-navigation-state.spec.ts`

Valida en navegador real Chromium:

- restauración desde query params;
- persistencia en `sessionStorage`;
- retorno después de salir de la pantalla;
- precedencia de URL sobre sesión;
- Productos;
- Ventas;
- Compras;
- Clientes;
- Inventario;
- Finanzas;
- `Limpiar filtros`.

Ejecución enfocada definitiva:

- workflow: `M4 validacion enfocada de navegacion`;
- run: `31337474683`;
- MySQL: 8.4 descartable;
- backend: compilación Release aprobada;
- frontend: lint aprobado;
- Angular + Chromium + Playwright: **1/1 prueba M4 aprobada**.

La ejecución previa `31337200840` falló de forma útil al descubrir la carrera de Inventario descrita en la sección 7; no se ocultó ni se contabilizó como verde.

---

## 11. Regresión general

El primer commit funcional M4 (`badec9cdb11d7bbe26f3062d3b02a13199bc985b`) ya había superado:

- Fase 2 — `31336701840`;
- Bloque 2C.1 — `31336701830`;
- Desarrollo - Compilación y pruebas — `31336701818`;
- Fase 8 — `31336701814`.

Además, la regresión enfocada posterior encontró y cerró una condición de carrera que esas suites generales no ejercitaban.

El cierre de M4 exige adicionalmente que el HEAD definitivo, sin workflows temporales, vuelva a superar los gates oficiales antes del reporte final.

---

## 12. Dictamen M4

Implementación funcional: **COMPLETA**  
Persistencia URL: **COMPLETA**  
Persistencia sessionStorage: **COMPLETA**  
Aislamiento por usuario: **IMPLEMENTADO**  
Retorno desde navegación: **CERTIFICADO EN E2E**  
Limpiar filtros: **IMPLEMENTADO**  
Producción: **NO TOCADA**

M5 no depende de cambios adicionales de esquema derivados de M4.
