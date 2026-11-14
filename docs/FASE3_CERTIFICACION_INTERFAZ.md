# FASE 3 — Certificación de corrección integral de interfaz

Fecha de cierre técnico: 27 de julio de 2026.

Rama modificada y certificada: `Desarrollo`.

Producción permaneció congelada. No se modificaron `main`, dominios, variables, servicios, bases, credenciales, activos ni despliegues productivos.

## 1. Alcance ejecutado

La Fase 3 corrigió y verificó los problemas de texto cortado, superpuesto, fuera del contenedor o comprimido en las áreas prioritarias:

- formulario administrativo de Usuario;
- Perfil;
- formulario de creación y edición de Producto;
- galería de fotografías del Producto;
- búsqueda, filtros, tabla y tarjetas de Productos;
- cabecera, usuario, rol y menú lateral;
- textos de ayuda, validación y errores;
- acciones de formulario y acciones de las tarjetas móviles.

Perfil fue auditado en escritorio y teléfono. Su estructura existente aprobó la certificación visual y no requirió cambios funcionales adicionales.

## 2. Hallazgos corregidos

### 2.1 Ayudas y errores con altura rígida

Los formularios de Usuario y Producto contenían ayudas extensas dentro de cuadrículas estrechas. Los campos no utilizaban de forma consistente el cálculo dinámico del área auxiliar de Angular Material.

Corrección:

- `subscriptSizing="dynamic"` en campos con ayudas o errores;
- separación vertical suficiente entre filas;
- mensajes con `white-space: normal` y `overflow-wrap: anywhere`;
- contenedores con `min-width: 0` para evitar desbordamientos de Grid/Flexbox;
- mensajes de error y ayuda con altura real, sin invadir el siguiente campo.

### 2.2 Jerarquía insuficiente en edición de Usuario

Se reorganizó la pantalla con:

- título y subtítulo diferenciados;
- identificación del usuario editado;
- aviso de edición protegida por acción;
- explicación clara del permiso para asignar roles;
- requisitos completos de contraseña;
- acciones adaptables sin cortar etiquetas.

### 2.3 Formulario de Producto demasiado compacto

Se separaron explícitamente:

- fotografías;
- identificación y clasificación;
- descripción;
- inventario;
- costo y precio;
- acciones finales.

La galería ahora conserva acciones visibles con teclado y dispositivos táctiles. Los textos auxiliares se expanden sin superposición.

### 2.4 Tabla de Productos comprimida

La tabla forzaba demasiada información dentro del ancho disponible y mezclaba estilos flexibles directamente con celdas de tabla.

Corrección:

- tabla dentro de un contenedor con desplazamiento horizontal propio;
- semántica nativa de celdas preservada;
- estado agrupado dentro de la celda, sin convertir el `td` en flexbox;
- nombres, marcas, modelos, colores y tallas con salto de línea;
- valores monetarios y cantidades estables;
- columna de acciones con ancho reservado;
- encabezado de imagen accesible para lectores de pantalla.

### 2.5 Tarjetas móviles con elipsis y acciones estrechas

Las tarjetas ocultaban parte de nombres y clasificaciones mediante elipsis. Las tres acciones podían comprimir la palabra “Eliminar”.

Corrección:

- eliminación de truncamientos deliberados en información del producto;
- texto multilínea para nombre, marca, modelo, color, talla y categoría;
- acciones en una cuadrícula que reduce columnas cuando el ancho no es suficiente;
- botones con área táctil y etiquetas completas;
- estado, stock y precio organizados en grupos independientes.

### 2.6 Cabecera y rol comprimidos

Se corrigieron:

- nombre de marca con hasta dos líneas;
- descripción superior adaptable;
- nombre de usuario multilínea;
- rol debajo del nombre, con ancho flexible;
- ocultación progresiva de información secundaria en anchos reducidos;
- menú móvil y zonas táctiles preservados.

## 3. Archivos principales modificados

- `frontend/src/app/app.component.scss`.
- `frontend/src/app/features/usuarios/usuario-form.component.html`.
- `frontend/src/app/features/usuarios/usuario-form.component.scss`.
- `frontend/src/app/features/productos/producto-form.component.html`.
- `frontend/src/app/features/productos/producto-form.component.scss`.
- `frontend/src/app/features/productos/productos-list.component.html`.
- `frontend/src/app/features/productos/productos-list.component.scss`.
- `frontend/e2e/fase3-interfaz.spec.ts`.
- `frontend/e2e/productos-filtros.spec.ts`.
- `.github/workflows/catalogos-aceptacion.yml`.

## 4. Certificación automatizada específica

Se añadió `frontend/e2e/fase3-interfaz.spec.ts`.

La prueba genera datos con nombres deliberadamente extensos y certifica las rutas:

```text
/usuarios/{id}/editar
/perfil
/productos/nuevo
/productos
```

Viewports utilizados:

```text
Escritorio: 1440 × 1000
Teléfono:   390 × 844
```

Comprobaciones automáticas:

- ausencia de desbordamiento horizontal del documento;
- textos prioritarios dentro del viewport;
- ausencia de `scrollWidth` interno causado por recorte;
- campos Material sin intersección geométrica;
- acciones móviles visibles y completas;
- navegación real con sesión administrativa;
- datos largos en tabla y tarjetas;
- ocho capturas de pantalla completas como evidencia.

También se estabilizó la prueba de filtros de Productos usando interacción de teclado para el selector de Talla, evitando que la etiqueta flotante de Angular Material intercepte eventos de puntero.

## 5. Evidencia visual

El artefacto `desarrollo-aceptacion-integral` de la ejecución final contiene:

- `desktop-usuario.png`;
- `desktop-perfil.png`;
- `desktop-producto-form.png`;
- `desktop-productos.png`;
- `mobile-usuario.png`;
- `mobile-perfil.png`;
- `mobile-producto-form.png`;
- `mobile-productos.png`.

Las capturas finales fueron inspeccionadas y muestran:

- ayudas y errores separados de los campos siguientes;
- nombres largos completos;
- cabecera sin superposición;
- tabla estable en escritorio;
- tarjetas legibles en teléfono;
- acciones Ver, Editar y Eliminar completas;
- formularios con orden visual y botones accesibles.

## 6. Certificación final

Commit funcional final:

```text
0bbc73f00bb8024e72a5837310456311d23f8740
```

Resultados:

- `Desarrollo - Compilación y pruebas`, run `30268149664`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30268149490`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30268149847`: **success**.

La aceptación integral ejecutó 27 pruebas sin fallos ni errores, incluida la certificación visual de Fase 3.

## 7. Límites y siguiente fase

La Fase 3 certifica la corrección de textos, jerarquía, contenedores y acciones en los viewports prioritarios indicados. No sustituye la matriz completa de dispositivos y resoluciones.

La Fase 4 realizará la certificación responsive exhaustiva en:

- teléfonos pequeños y grandes;
- tablets;
- laptops;
- Full HD;
- 2K;
- 4K.

Completar la Fase 3 no autoriza merge, despliegue o modificación de Producción.
