# DEV — Punto 8: medición y adelgazamiento Angular

Fecha de ejecución: 2026-09-29 / 2026-09-30 UTC  
Scope: `solqaryn/Solqaryn` / rama de trabajo `p8-angular-bundle-20260929` -> `dev`  
Servicios pagos: **0**

## Baseline causal antes de optimizar

Workflow: `DEV - Punto 8 Angular bundle`.

Angular CLI:

- Initial total raw: **730.17 kB**.
- Estimated transfer: **171.55 kB**.
- `main`: 126.32 kB.
- `styles`: 123.42 kB.
- `polyfills`: 34.59 kB.

`stats.json` mostró en el grafo inicial:

- `@angular/material`: ~161.8 kB.
- `@angular/cdk`: ~37.8 kB.
- `@angular/animations`: ~62.7 kB.
- `zone.js`: ~34.6 kB.
- El tema prebuilt de Material aporta ~103.7 kB raw de CSS.
- `html5-qrcode` ya estaba correctamente lazy (~374.2 kB) y no fue movido al arranque.

## Cambios

1. El shell raíz y la navegación dejaron de importar `MatButtonModule` y `MatIconModule`; conservan los mismos iconos mediante la fuente Material Icons ya cargada y botones nativos accesibles.
2. `provideAnimations()` se sustituyó por `provideAnimationsAsync()`, por lo que el renderer legacy de animaciones deja de formar parte del grafo inicial y queda en chunk lazy.
3. La portada usa `@defer (on idle)` para contenido por debajo del fold; hero y LCP permanecen inmediatos.
4. Se añadió `AfterRenderSelectivePreloadingStrategy`: sólo precarga rutas marcadas, después de que la aplicación esté estable y únicamente dentro del storefront SOLQARYN.
5. Rutas seleccionadas: catálogo, detalle de producto y categorías. No se usa `PreloadAllModules`.
6. Se añadió medición reproducible con `ng build --configuration production --stats-json`, baseline raw/gzip/Brotli y análisis de contributors por paquete/módulo.
7. Se añadió `validate-angular-bundle-policy.mjs` al lint canónico.
8. Budget `initial` reducido de **1 MiB warning / 2 MiB error** a **650 kB warning / 750 kB error**.

## Resultado post-optimización

Workflow causal post-cambio: `36650833149` — **SUCCESS**.

Angular CLI:

- Initial total raw: **580.96 kB**.
- Estimated transfer: **137.46 kB**.
- `main`: **66.94 kB**.
- `styles`: **123.44 kB**.
- `polyfills`: **34.59 kB**.

Comparación:

- Raw inicial: 730.17 -> 580.96 kB = **-149.21 kB / -20.4%**.
- Transfer estimado: 171.55 -> 137.46 kB = **-34.09 kB / -19.9%**.
- Main: 126.32 -> 66.94 kB = **-59.38 kB / -47.0%**.
- Material/CDK JS dejó de figurar en el grafo inicial; permanece el CSS global del tema.
- `@angular/animations` pasó a chunk lazy de ~67.8 kB.
- `html5-qrcode` continúa lazy, sin regresión.

## Decisiones explícitas

- No se retiró el tema global Material en este punto: aunque pesa ~103.7 kB raw, su CSS global comprimido es pequeño frente al riesgo transversal de retheming de toda la administración.
- No se cambió Zone.js ni se migró la app a zoneless: sería una intervención arquitectónica independiente.
- No se compró Vercel Pro, observabilidad, CDN, APM ni ningún otro servicio.
- Sin migraciones, datos, secretos, `main` ni PROD.

## QA

El pipeline de Punto 8 ejecuta:

- `npm ci`;
- lint/TypeScript y guardas del storefront;
- política de bundle;
- build production con `stats.json`;
- baseline raw/gzip/Brotli;
- artifact con evidencia.

Permanece una advertencia preexistente e independiente: `SOLQARYN-producto.component.scss` mide 17.59 kB frente al warning de estilo individual de 16 kB. No se elevó ese límite para ocultarla.
