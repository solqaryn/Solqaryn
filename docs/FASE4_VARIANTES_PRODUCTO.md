# Fase 4 — variantes de producto e inventario por color/SKU

## Estado

Completada y certificada exclusivamente en la rama `Desarrollo`.

## Alcance funcional

- Formulario principal con lista dinámica de colores.
- Botón **Agregar otro color**.
- Color obligatorio y no repetible por producto.
- Cantidad, SKU, código de barras, costo, precio y umbral por variante.
- SKU automático cuando el usuario no proporciona uno.
- Stock consolidado calculado como suma física de variantes no eliminadas.
- Edición y sincronización transaccional de producto y variantes.
- Protección contra eliminación de variantes con existencias.
- Selección obligatoria de variante activa en compras y ventas.
- Incremento, reducción y reversión del stock de la variante exacta.
- Snapshot de color y SKU en compras, ventas, facturas y movimientos.
- Conversión segura de productos legados con color a una variante inicial.
- Interfaz responsive y trazabilidad histórica.

## Corrección posterior de guardado y privacidad visual

- Se retiraron del inicio de sesión los indicadores visibles **Acceso protegido**, **Permisos por rol** y **Operaciones auditadas**, además de la nota descriptiva sobre transmisión de credenciales.
- El entorno `solqaryn-api-desarrollo` aplica sus migraciones pendientes antes de declararse listo mediante `/health/ready`.
- Producción no fue modificada.
- Los errores de persistencia entregan mensajes seguros y una referencia de seguimiento sin mostrar detalles técnicos internos.
- Se agregó una prueba de interfaz que guarda exactamente dos colores con cantidades 2 y 3, SKU vacío, generación automática de SKU y stock consolidado de 5 unidades.

## Migración

`20260728165321_Fase4VariantesInventario`

## Commit funcional certificado

`5f57219b597d9ec47cf34bbb53cbbb77882c056f`

## Evidencia de CI

- Compilación y pruebas: ejecución `30403337110` — success.
- Aceptación funcional integral: ejecución `30403337076` — success.
- Auditoría de configuración: ejecución `30403337213` — success.

## Seguridad

- No se modificó `main`.
- No se crearon ramas.
- No se fusionó el PR #2.
- No se habilitó auto-merge.
- No se desplegó ni se modificó Producción.
- No se aplicaron migraciones contra bases productivas.
