# Fase 8 — Validación completa automatizada

Fecha de cierre automatizado: 2026-07-29  
Rama certificada: `Desarrollo`  
Commit funcional: `688cbd195e720d8f9c1d04d28c287c7c934035f2`

## 1. Alcance certificado

La Fase 8 ejecutó una validación transversal y reproducible sobre infraestructura temporal y descartable. No se utilizó ni modificó Producción.

Se verificaron:

- compilación Release de ASP.NET Core;
- pruebas unitarias del backend;
- compilación productiva de Angular;
- auditoría de dependencias .NET y npm;
- aplicación de migraciones EF Core sobre MySQL 8.4 temporal;
- health y readiness;
- seguridad HTTP y superficie pública;
- autenticación, permisos y aislamiento entre usuarios;
- regresión funcional de módulos;
- facturación, impuestos, descuentos, costos de envío y pagos;
- variantes, SKU e inventario consolidado;
- compras, ventas, anulaciones y sobreventa;
- cargas masivas y reportes de errores;
- PDF, perfiles de impresión y correo SMTP aislado;
- accesibilidad semántica y navegación por teclado;
- responsive en `320 × 568` y `3840 × 2160`;
- consola, errores de página y respuestas fallidas inesperadas;
- presupuestos de rendimiento en ambiente controlado;
- auditoría de logs y exposición de secretos.

## 2. Hallazgos corregidos durante la fase

### 2.1 Nombres accesibles en interruptores de estado

Se detectó que varios `mat-slide-toggle` usaban un atributo que no transfería correctamente el nombre accesible al botón interno de Angular Material.

Se corrigieron los interruptores de estado en Productos, Clientes, Proveedores, Categorías, catálogos de producto, Usuarios, Roles, Descuentos e Impuestos.

### 2.2 Configuración sin etiquetas accesibles completas

Se corrigieron:

- selector de archivo del logo;
- campos de colores hexadecimales;
- nombres accesibles dinámicos para controles visuales.

### 2.3 Desbordamiento móvil en Cargas masivas

La tabla histórica imponía un ancho mínimo al contenedor y provocaba desbordamiento horizontal global en `320 × 568`.

Se mantuvo la información completa y se corrigió mediante:

- `min-width: 0` en contenedores de grid y flex;
- límites de ancho en secciones y tarjetas;
- desplazamiento interno en tablas;
- protección para nombres largos, chips y mensajes de error.

## 3. Evidencia de ejecución

### 3.1 Workflows

| Validación | Run | Resultado |
|---|---:|---|
| Desarrollo — Compilación y pruebas | `30474905738` | `success` |
| Desarrollo — Aceptación funcional integral | `30474905564` | `success` |
| Fase 2 — Auditoría de configuración y dependencias | `30474905571` | `success` |
| Fase 8 — Validación completa automatizada | `30474905679` | `success` |

### 3.2 Pruebas

- Regresión Playwright integral: **81 aprobadas, 0 fallos**.
- Suite especializada de Fase 8: **7 aprobadas, 0 fallos**.
- Validación SMTP aislada y PDF adjunto: **aprobada**.

### 3.3 Artefacto especializado

```text
Nombre: fase8-validacion-completa
ID: 8733300881
SHA-256: b0b5962f4230dc90039c744d767bd6e5ef87f011c3ceb5e54d8e29d537a62aa0
```

El artefacto contiene reporte Playwright, resultados de pruebas, logs auditados, evidencia SMTP y manifiesto de certificación.

## 4. Límites de esta certificación

La automatización no sustituye pruebas que dependen de servicios o dispositivos externos reales. Permanecen pendientes:

- recepción en un buzón de correo real;
- clasificación en bandeja principal o spam;
- envío real desde Render Desarrollo;
- comprobación de Cloudinary Desarrollo;
- comprobación de Aiven Desarrollo;
- comprobación de Vercel y Render Desarrollo;
- WhatsApp desde teléfono físico;
- impresión física A4, Carta, Legal, Oficio, A5, POS 58 mm y POS 80 mm;
- dispositivos Android, iPhone y tablet reales;
- pruebas de red móvil y conectividad intermitente real.

## 5. Conclusión

```text
FASE 8 AUTOMATIZADA: COMPLETADA Y CERTIFICADA
VALIDACIONES EXTERNAS Y FÍSICAS: PENDIENTES
AUTORIZACIÓN PARA PRODUCCIÓN: NO CONCEDIDA
```

Esta certificación no autoriza merge, despliegue, migraciones productivas ni modificación de recursos de Producción.