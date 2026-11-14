# FASE 4 — Certificación responsive exhaustiva

Fecha de cierre técnico: 27 de julio de 2026.

Rama modificada y certificada: `Desarrollo`.

Producción permaneció congelada. No se modificaron `main`, variables, credenciales, dominios, servicios, bases de datos, activos, migraciones ni despliegues productivos.

## 1. Objetivo

Certificar que Solqaryn conserve navegación, legibilidad, jerarquía, acciones y comportamiento estable desde un teléfono de 320 px hasta una pantalla 4K.

La fase amplía la certificación visual prioritaria de la Fase 3 a una matriz sistemática de resoluciones y módulos.

## 2. Matriz certificada

| Perfil | Resolución | Navegación esperada |
|---|---:|---|
| Teléfono pequeño | 320 × 568 | Menú lateral superpuesto |
| Teléfono grande | 430 × 932 | Menú lateral superpuesto |
| Tablet vertical | 768 × 1024 | Menú lateral superpuesto |
| Tablet horizontal | 1024 × 768 | Menú lateral permanente |
| Laptop | 1366 × 768 | Menú lateral permanente |
| Full HD | 1920 × 1080 | Menú lateral permanente |
| 2K | 2560 × 1440 | Menú lateral permanente |
| 4K | 3840 × 2160 | Menú lateral permanente |

## 3. Rutas certificadas

Cada resolución navegó por las siguientes 30 rutas:

```text
/dashboard
/productos
/productos/nuevo
/categorias
/categorias/nueva
/colores
/tallas
/marcas
/modelos
/compras
/compras/nueva
/proveedores
/proveedores/nuevo
/ventas
/ventas/nueva
/clientes
/clientes/nuevo
/finanzas
/inventario/movimientos
/usuarios
/roles
/roles/nuevo
/permisos
/descuentos
/descuentos/nuevo
/impuestos
/impuestos/nuevo
/auditoria
/configuracion
/perfil
```

Total de navegaciones responsive específicas:

```text
8 resoluciones × 30 rutas = 240 navegaciones
```

## 4. Comprobaciones automáticas

La prueba `frontend/e2e/fase4-responsive.spec.ts` valida en cada resolución:

- ausencia de desbordamiento horizontal del documento;
- títulos, subtítulos, cabeceras, acciones, paginadores y pie dentro del viewport;
- ausencia de textos críticos recortados por `overflow`, `nowrap` o elipsis;
- áreas interactivas de la aplicación con dimensiones utilizables;
- apertura y cierre real del menú lateral en teléfono y tablet vertical;
- menú lateral permanente en tablet horizontal, laptop y monitores;
- ausencia de errores JavaScript de página;
- ausencia de errores en consola durante la navegación;
- carga correcta de cada módulo y formulario principal.

Los elementos internos de implementación de Angular Material no se contabilizan como controles independientes cuando su interacción pertenece al componente público contenedor.

## 5. Evidencia visual

Se generaron 32 capturas representativas:

```text
8 resoluciones × 4 pantallas = 32 capturas
```

Pantallas capturadas en cada resolución:

- Dashboard;
- Productos;
- formulario de Producto;
- formulario de Venta.

Para teléfonos y tablets se capturó la página completa. En 2K y 4K se capturó el viewport para mantener evidencia manejable sin perder la composición visible.

El artefacto final de GitHub Actions es:

```text
desarrollo-aceptacion-integral
artifact id: 8657147614
```

También contiene `test-results/fase4/matriz.json`, que registra las resoluciones, rutas, 240 navegaciones y 32 capturas.

## 6. Defecto real encontrado y corregido

### Auditoría en tablet horizontal

Resolución afectada:

```text
1024 × 768
```

Problema:

- la tabla de Auditoría extendía el documento 63 px horizontalmente;
- el contenido quedaba parcialmente fuera del viewport;
- la tabla no disponía de un contenedor de desplazamiento propio.

Corrección:

- se añadió un contenedor `.table-scroll` alrededor de la tabla;
- el desplazamiento horizontal queda limitado al área de resultados;
- se preservó la semántica nativa de la tabla;
- el contenedor es alcanzable por teclado y tiene descripción accesible;
- las tarjetas móviles continúan utilizándose por debajo del breakpoint existente.

Archivo corregido:

```text
frontend/src/app/features/auditoria/auditoria-list.component.html
```

## 7. Ajustes de la infraestructura de pruebas

Durante la construcción de la matriz se corrigieron tres aspectos del propio arnés de pruebas:

- sintaxis del argumento de `evaluateAll`;
- espera explícita al final de la transición del menú móvil;
- exclusión de la implementación interna `.mdc-switch` de Angular Material al medir controles públicos.

Estos ajustes eliminaron falsos positivos y permitieron que los defectos reportados correspondieran a la interfaz real.

## 8. Certificación final

Commit funcional certificado:

```text
898ada1d9e4c5c22353ba0fbed5589c52a0366de
```

Commit documental de cierre:

```text
4425c0fdc21a798079d728acfc0022d13f8a716f
```

Resultados de GitHub Actions sobre el commit funcional:

- `Desarrollo - Compilación y pruebas`, run `30276203737`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30276203714`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30276203753`: **success**.

Resultado Playwright integral:

```text
43 pruebas
0 fallos
0 errores
0 omitidas
```

Resultado de Fase 4:

```text
240 navegaciones responsive certificadas
32 capturas de evidencia
8 resoluciones aprobadas
```

## 9. Alcance y límites

La certificación utiliza Chromium y dimensiones de viewport controladas. Verifica comportamiento responsive, no equivalencia de renderizado pixel a pixel entre todos los motores de navegador ni características físicas específicas de cada dispositivo.

La validación en equipos físicos y navegadores adicionales puede incorporarse en la Fase 8 como validación final, pero no existe un defecto crítico o alto conocido dentro de la matriz certificada.

## 10. Siguiente fase

La Fase 5 revisará el tratamiento integral de imágenes, especialmente:

- imagen principal de Producto;
- fallbacks accesibles;
- carga y dimensiones;
- Compras;
- Ventas;
- detalles e historial.

Completar la Fase 4 no autoriza merge, despliegue ni modificación de Producción.
