# Cierre final — Fases 2C a 2G

Fecha: 2026-08-07

## Alcance

Este documento consolida el cierre técnico de las fases 2C, 2D, 2E, 2F y 2G del ciclo funcional complementario de Solqaryn.

La revisión se realizó exclusivamente sobre la rama `Desarrollo`. `main` permanece congelada y Producción no forma parte de este cierre.

Candidato funcional revisado antes de este commit documental:

```text
82ec565a199be6a46a3af43e5e25854afd4dd0c6
```

`main` permanece en:

```text
85b4e02814823e9671803c23798a6ff0bf05c8f6
```

PR oficial:

```text
#2 Desarrollo -> main
Estado: abierto
Draft: sí
Merge: no realizado
Auto-merge: no autorizado
```

---

## Fase 2C — Escáner, variante técnica y autocomplete remoto

### 2C.1 — Variante técnica y migración

Estado: **COMPLETADA**.

Se mantiene la compatibilidad de productos simples mediante variante técnica y la validación permanente correspondiente en CI.

### 2C.2 — Ciclo de vida de variante técnica

Estado: **COMPLETADA**.

Se verificó la existencia de reglas de creación/reactivación, sincronización de cantidad/costo/precio/umbral/estado y protección frente a coexistencia inválida con variantes comerciales.

### 2C.3 — Backend del escáner

Estado: **COMPLETADA**.

Endpoints operativos:

```text
GET /ventas/productos/por-codigo?codigo={valor}
GET /compras/productos/por-codigo?codigo={valor}
```

El backend resuelve SKU o código de barras exacto, conserva ceros iniciales, diferencia DTO de venta/compra, evita exposición de costo en ventas, admite variantes técnicas/comerciales y devuelve respuestas controladas para entrada inválida, inexistencia, conflicto y estado no operativo.

### 2C.4 — Frontend del escáner

Estado: **COMPLETADA**.

Se encuentra implementado:

- lector USB/Bluetooth por entrada + `Enter`;
- consolidación de escaneos repetidos;
- control de stock en ventas;
- recepción de costo en compras;
- lector por cámara e imagen con `html5-qrcode`;
- carga diferida;
- liberación del stream de cámara;
- política de cámara del frontend;
- pruebas E2E y validación específica del escáner.

### 2C.5 — Autocomplete remoto

Estado: **COMPLETADA**.

Se eliminó la dependencia funcional de cargar el catálogo masivo en los formularios y se utiliza búsqueda remota con debounce/cancelación, límites del servidor y proyección diferenciada para ventas y compras.

### 2C.6 — Certificación de cierre

Estado: **COMPLETADA**.

Documento existente:

```text
docs/FASE2C6_CERTIFICACION_CIERRE.md
```

Dictamen consolidado de 2C:

```text
FASE 2C: CERRADA
BACKEND ESCÁNER: APROBADO
FRONTEND ESCÁNER: APROBADO EN CI
AUTOCOMPLETE REMOTO: APROBADO
VARIANTE TÉCNICA: APROBADA
REGRESIONES BLOQUEANTES AUTOMATIZADAS CONOCIDAS: 0
VALIDACIÓN FÍSICA DE CÁMARA/LECTORES: EXTERNA, NO PRODUCTIVA
```

---

## Fase 2D — Redondeo y distribución monetaria

Estado: **COMPLETADA**.

Documento existente:

```text
docs/FASE2D_CERTIFICACION_REDONDEO_FACTURACION.md
```

Se verificó en la implementación:

- `decimal` para cálculos monetarios;
- redondeo explícito a 2 decimales con `MidpointRounding.AwayFromZero`;
- subtotal por línea antes de sumar el documento;
- distribución determinista de descuentos e impuestos;
- asignación determinista de residuos de centavos;
- persistencia del snapshot monetario por línea de factura;
- costo de envío incorporado exactamente una vez;
- conciliación entre líneas y encabezado.

Dictamen:

```text
FASE 2D: CERRADA
REDONDEO: APROBADO
DISTRIBUCIÓN DE CENTAVOS: APROBADA
CONCILIACIÓN: APROBADA
ENVÍO ÚNICO POR DOCUMENTO: APROBADO
```

---

## Fase 2E — Anulación conservadora de compras

Estado: **COMPLETADA**.

Documento existente:

```text
docs/FASE2E_CERTIFICACION_ANULACION_COMPRAS.md
```

Se verificó:

- anulación dentro de transacción;
- cabecera bloqueada mediante `FOR UPDATE`;
- validación del estado confirmado;
- bloqueo de inventario mediante el coordinador de concurrencia;
- búsqueda de movimientos posteriores por `ProductoId + ProductoVarianteId`;
- bloqueo fail-closed si existen movimientos posteriores;
- snapshots de costo y stock anterior/nuevo;
- rechazo de restauración automática de históricos sin snapshots suficientes.

Dictamen:

```text
FASE 2E: CERRADA
ANULACIÓN TRANSACCIONAL: APROBADA
MOVIMIENTOS POSTERIORES: BLOQUEO APROBADO
SNAPSHOTS DE VALORACIÓN: APROBADOS
HISTÓRICOS INCOMPLETOS: FAIL-CLOSED APROBADO
```

---

## Fase 2F — Seguridad de imágenes en backend

Estado: **COMPLETADA**.

La implementación actual utiliza `ImagenUploadSecurity` antes de transferir imágenes a Cloudinary.

Controles verificados:

- máximo de 10 MB;
- extensiones permitidas JPG/JPEG/PNG/WEBP;
- validación de firma binaria real (magic numbers);
- coherencia entre extensión, MIME declarado y contenido real;
- máximo 4096 x 4096;
- máximo 16 megapíxeles;
- identificación y decodificación completa mediante ImageSharp;
- recodificación de la imagen desde píxeles decodificados;
- eliminación de metadatos EXIF, ICC, IPTC, XMP y CICP;
- nombre de salida generado por el sistema;
- errores técnicos externos no expuestos al usuario;
- integración real en `CloudinaryImageStorageService` y almacenamiento de imagen de perfil.

La versión efectiva de ImageSharp del árbol actual es:

```text
SixLabors.ImageSharp 3.1.12
```

Esta versión efectiva prevalece sobre referencias históricas del plan a versiones anteriores y es la que fue compilada/auditada por los workflows actuales.

Pruebas específicas verificadas:

```text
ImagenUploadSecurityTests
ProductoImagenValidationTests
ProductoImagenesValidatorTests
```

Incluyen rechazo de ejecutable renombrado a PNG, MIME inconsistente, dimensiones excesivas, más de 16 MP y archivo mayor de 10 MB.

Dictamen:

```text
FASE 2F: CERRADA
VALIDACIÓN DE FIRMA: APROBADA
VALIDACIÓN MIME/EXTENSIÓN: APROBADA
LÍMITES DE TAMAÑO/RESOLUCIÓN: APROBADOS
RECODIFICACIÓN Y SANEAMIENTO: APROBADOS
INTEGRACIÓN CLOUDINARY: APROBADA EN DESARROLLO/CI
```

---

## Fase 2G — Logs seguros de búsqueda y escáner

Estado: **COMPLETADA**.

`MedirRendimientoBusquedaFilter` mide exclusivamente las rutas operativas:

```text
/ventas/productos/buscar
/ventas/productos/por-codigo
/compras/productos/buscar
/compras/productos/por-codigo
```

Los logs estructurados registran únicamente:

```text
Ruta
DuracionMs
LongitudTermino
CantidadResultados
EstadoHTTP
CorrelationId
```

No registran el término, SKU ni código de barras recibido.

El filtro está registrado globalmente en MVC y se limita internamente a las rutas objetivo.

`BusquedaRendimientoFilterTests` verifica expresamente que un término sensible no aparezca en el log y que las rutas no operativas no generen este registro de rendimiento.

Dictamen:

```text
FASE 2G: CERRADA
TÉRMINOS DE BÚSQUEDA EN LOGS: NO EXPUESTOS
SKU/CÓDIGO EN LOGS: NO EXPUESTOS
MÉTRICAS ESTRUCTURADAS: APROBADAS
PRUEBAS DE NO EXPOSICIÓN: APROBADAS
```

---

## Evidencia CI del candidato funcional 82ec565a

Sobre el candidato funcional revisado finalizaron correctamente:

```text
Desarrollo - Compilación y pruebas: success
Desarrollo - aceptación funcional integral: success
Fase 2 - Auditoría de configuración y dependencias: success
Fase 8 - Validación completa automatizada: success
Bloque 2C.1 - Variante técnica y migración: success
Solqaryn CI: skipped por condición del workflow
```

Un workflow omitido por condición no se registra como ejecución aprobada ni como fallo.

---

## Dictamen global 2C -> 2G

```text
2C: COMPLETADA Y CERRADA
2D: COMPLETADA Y CERRADA
2E: COMPLETADA Y CERRADA
2F: COMPLETADA Y CERRADA
2G: COMPLETADA Y CERRADA
REGRESIONES BLOQUEANTES EN LOS WORKFLOWS OBLIGATORIOS DEL CANDIDATO: 0
MAIN: NO MODIFICADA
PR #2: ABIERTO Y EN BORRADOR
MERGE: NO REALIZADO
AUTO-MERGE: NO HABILITADO
PRODUCCIÓN: NO MODIFICADA
```

## Pendientes que no invalidan este cierre

Permanecen fuera de la certificación automatizada y no autorizan Producción:

- cámara real en Android;
- cámara real en iPhone/iOS;
- lector USB/Bluetooth físico;
- validaciones externas/productivas definidas en el plan de liberación general.

Este cierre no autoriza merge a `main`, despliegue productivo, cambios de secretos, dominios, bases de datos o servicios productivos.