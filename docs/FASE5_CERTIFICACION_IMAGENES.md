# FASE 5 — Certificación del tratamiento integral de imágenes

Fecha de cierre técnico: 27 de julio de 2026.

Rama modificada y certificada: `Desarrollo`.

Producción permaneció congelada. No se modificaron `main`, variables, dominios, servicios, bases, credenciales, activos, migraciones ni despliegues productivos.

## 1. Objetivo y alcance

La Fase 5 estandarizó la representación de imágenes de Producto y extendió la imagen principal a los módulos operativos donde facilita la identificación del artículo:

- listado y tarjetas de Productos;
- creación y edición de Producto;
- detalle, galería y ampliación de Producto;
- listados, formularios y detalles de Compras;
- listados, formularios y detalles de Ventas;
- historial de Movimientos de inventario.

No se modificó el esquema de base de datos. Las relaciones y registros de imágenes existentes se conservaron.

## 2. Componente reutilizable

Se añadió:

```text
frontend/src/app/shared/producto-imagen/producto-imagen.component.ts
```

El componente `app-producto-imagen` centraliza:

- imagen válida;
- estado sin imagen;
- recuperación ante URL o recurso roto;
- texto alternativo descriptivo;
- etiqueta accesible del fallback;
- dimensiones intrínsecas para reducir desplazamientos de layout;
- `decoding="async"`;
- carga diferida para listas, miniaturas e historial;
- carga prioritaria para la imagen principal del detalle;
- relación de aspecto y `object-fit` por contexto;
- variantes reutilizables para opción, miniatura, línea, tarjeta, hero, galería y lightbox;
- respeto por `prefers-reduced-motion`.

## 3. Backend y contratos

Se incorporó `ProductoImagenPrincipalUrl` a los contratos de detalles de Compra, detalles de Venta y Movimientos de inventario.

Los repositorios y servicios proyectan la imagen marcada como principal y, cuando no existe, el primer recurso ordenado disponible. Los endpoints históricos continúan siendo compatibles: el campo puede ser nulo sin romper registros anteriores.

Se añadieron o ampliaron pruebas backend para comprobar la propagación de la imagen principal en Compras, Ventas y Movimientos.

## 4. Productos

### Listado

- La tabla de escritorio muestra la imagen principal o un fallback accesible.
- Las tarjetas móviles utilizan la misma fuente y fallback.
- El contador de imágenes adicionales se conserva.
- Las imágenes no prioritarias usan `loading="lazy"`.
- El texto alternativo identifica el Producto correspondiente.

### Formulario

- La galería de edición utiliza el componente resiliente.
- Una URL rota ya no deja un espacio vacío ni un icono del navegador.
- Principal y adicionales tienen textos alternativos diferenciados.
- Las acciones para marcar principal o retirar continúan operativas.

### Detalle

- La imagen principal usa variante `hero`, `loading="eager"` y `fetchpriority="high"`.
- El contenido se muestra completo mediante `object-fit: contain`.
- La galería usa miniaturas operables con teclado.
- El lightbox se abre con teclado, conserva fallback si la imagen falla y se cierra con Escape.
- La descarga individual y múltiple permanece condicionada por permisos.

## 5. Compras y Ventas

Cada línea de los formularios muestra la imagen del Producto seleccionado. Los listados muestran la imagen principal del primer Producto de la operación. Las tablas de detalle incluyen una columna de imagen accesible sin alterar cantidades, importes, impuestos ni reglas de negocio.

## 6. Movimientos de inventario

El historial muestra la imagen principal junto al nombre del Producto. La tabla mantiene desplazamiento horizontal propio, semántica nativa y acceso mediante teclado.

## 7. Resiliencia y accesibilidad

Se certificaron imagen válida, Producto sin imagen y URL rota o inaccesible. En todos los casos existe una representación visible y una descripción accesible.

## 8. Certificación automatizada

Se añadió `frontend/e2e/fase5-imagenes.spec.ts` y se integró en `.github/workflows/catalogos-aceptacion.yml`.

Comprueba carga diferida, fallback accesible, error de red, prioridad del hero, galería por teclado, lightbox, imágenes en Compras y Ventas, miniatura en Movimientos y ausencia de desbordamiento horizontal.

## 9. Evidencia visual

Artefacto:

```text
desarrollo-aceptacion-integral
artifact id: 8662414676
```

Incluye diez capturas:

```text
productos-listado-fallback.png
producto-detalle-galeria.png
producto-lightbox-fallback.png
compras-listado.png
compra-detalle.png
compra-formulario.png
ventas-listado.png
venta-detalle.png
venta-formulario.png
movimientos-inventario.png
```

## 10. Resultado final

Commit funcional certificado:

```text
90eb4ff4c9b7b4a8ed66561fa092f7521ebe7630
```

Commit documental de cierre:

```text
4ec034ba0fc6c7b6486b9c5fe2a8a70818e4841e
```

Ejecuciones sobre el commit funcional:

- `Desarrollo - Compilación y pruebas`, run `30289511599`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30289510773`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30289511930`: **success**.

Resultado Playwright:

```text
47 pruebas totales
47 aprobadas
0 inesperadas
0 inestables
0 omitidas
```

La prueba específica de Fase 5 ejecutó cuatro casos y los cuatro terminaron correctamente.

## 11. Riesgos y validaciones externas pendientes

La certificación aislada no utiliza credenciales reales de Cloudinary y no modifica activos externos. Continúan pendientes para la Fase 8 o una validación manual autorizada en `SOLQARYN_desarrollo`:

- cargar una imagen real desde cámara y galería;
- reemplazar y eliminar un activo real con prefijo `SOLQARYN_desarrollo/`;
- confirmar que Desarrollo no puede eliminar un activo productivo;
- revisar consumo, huérfanos y duplicados;
- probar conexión móvil lenta y archivos cercanos al límite.

## 12. Criterio de cierre

La Fase 5 queda completa y certificada en código, contratos, compilación, pruebas y evidencia visual. La siguiente etapa es la Fase 6 — Facturación e impresión.

Completar esta fase no autoriza merge, despliegue ni modificación de Producción.
