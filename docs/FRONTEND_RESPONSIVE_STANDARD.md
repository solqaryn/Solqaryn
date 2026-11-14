# Estándar responsive global de Solqaryn

## Propósito

Solqaryn mantiene una sola base visual empresarial. Toda pantalla nueva debe funcionar desde 320 px hasta escritorio amplio sin crear scroll horizontal de la página, perder acciones ni ocultar información crítica. Este estándar se aplica al shell, formularios, tablas, diálogos, tarjetas, gráficos, medios y flujos de scanner.

## Contrato de viewport

- Breakpoints compartidos: `36rem` (sm), `48rem` (md), `64rem` (lg) y `80rem` (xl).
- Márgenes de página: `--page-gutter` y `--page-gutter-compact`.
- `body`, `.layout`, `.main`, `main`, secciones y tarjetas pueden encogerse (`min-width: 0`).
- La página no usa `overflow-x: hidden` para tapar defectos. El desplazamiento horizontal se reserva para el contenido que lo necesita, como tablas extensas.
- Imágenes, SVG, video y canvas respetan `max-width: 100%`.

## Shell y navegación

En móvil el sidebar es un drawer fuera de pantalla. Se abre con `#menu-toggle`, muestra `.overlay`, devuelve el foco al cerrar y se cierra con Escape. En escritorio permanece visible y el botón móvil queda oculto. El topbar puede encogerse y el contenido principal conserva `min-width: 0`.

## Primitivas compartidas

Las features deben componer primero estas clases del design system:

- `.app-container`, `.app-page-content` para el ancho y espaciado de página.
- `.app-grid`, `.app-form-grid`, `.app-filter-grid`, `.app-detail-grid`, `.app-kpi-grid` para layouts fluidos.
- `.app-responsive-stack` para acciones y bloques que pasan a columna.
- `.app-table-shell` para tablas que requieren desplazamiento interno.
- `.app-dialog-layout` y `.app-dialog-layout__body` para diálogos altos o anchos.

Las reglas específicas de dominio deben ser pequeñas y no imponer `min-width` al viewport.

### Ejemplo mínimo de una página nueva

Una pantalla nueva debe montar las primitivas compartidas y dejar que el sistema resuelva
el ancho disponible:

```html
<section class="app-page-content app-container">
  <header class="app-page-header">
    <h1>Clientes</h1>
    <div class="app-responsive-stack">
      <button type="button">Nuevo cliente</button>
    </div>
  </header>

  <form class="app-form-grid">
    <label>Nombre <input name="nombre" /></label>
    <label>Identidad <input name="identidad" /></label>
  </form>

  <div class="app-table-shell">
    <table><!-- datos --></table>
  </div>
</section>
```

No se agregan `min-width` al `body`, a la página ni a una tarjeta para resolver un caso
particular. Si el contenido necesita más espacio, se usa la primitiva correspondiente
(`app-form-grid`, `app-responsive-stack` o `app-table-shell`) y el gate responsive valida
la nueva ruta en todos los viewports definidos.

## Formularios, tablas y diálogos

- Los controles de texto ocupan el ancho disponible y nunca exceden su contenedor.
- Las acciones tienen objetivos táctiles de al menos 44 px en el shell; los iconos compactos conservan al menos 36 px en móvil.
- Una tabla ancha se envuelve en un shell con scroll horizontal y encabezados legibles; no se comprime hasta volver ilegible el dato.
- Los diálogos quedan dentro de `100vw - 24px` y `100dvh - 24px`, con cuerpo desplazable y acciones alcanzables.
- Texto largo usa saltos de palabra; no se corta información crítica mediante elipsis.

## Gráficos, medios y scanner

Los gráficos y canvas deben medir el contenedor y redibujarse al cambiar el tamaño. Las imágenes y videos son fluidos. Los flujos de scanner HID y cámara conservan sus listeners, permisos, controles de pausa y mensajes de error; el responsive no reemplaza el input ni simula una lectura.

## Accesibilidad

Toda acción visible debe tener nombre accesible. No se usa `aria-hidden="true"` en controles interactivos visibles. El foco permanece visible, el orden de tabulación es lógico y los mensajes de validación se asocian con su control.

## Gate automático

El gate único es:

```bash
npm run test:responsive
```

Se ejecuta desde `frontend` y recorre el inventario de rutas fuente en 320, 360, 375, 390, 412, 430, 768, 1024 y 1440 px. Comprueba overflow del body, elementos fuera del viewport, contenido recortado, tablas sin shell, objetivos táctiles, controles ocultos semánticamente y el comportamiento drawer/escritorio del shell. Una ruta nueva debe agregarse al inventario cubierto en `frontend/e2e/responsive-global.spec.ts` en el mismo cambio.

## Evidencia y revisión

Las corridas visuales se guardan en `docs/evidencias/responsive-global/<timestamp>/` con una matriz de rutas y viewports. Las capturas deben provenir de pantallas reales. Antes de publicar: `npm run lint`, `npm run build:prod`, `npm run test:responsive`, `git diff --check` y revisión de que no hubo cambios de backend, secretos, Producción, `main`, WhatsApp ni flujos transaccionales.

