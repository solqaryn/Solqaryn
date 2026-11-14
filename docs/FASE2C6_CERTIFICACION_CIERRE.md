# Fase 2C.6 — Certificación y cierre integral del Bloque 2C

Fecha de certificación técnica: 2026-08-07

## 1. Alcance

Esta fase cierra documentalmente el Bloque 2C sin añadir reglas de negocio nuevas. Consolida la evidencia técnica real de las fases 2C.1 a 2C.5 sobre la rama `Desarrollo` y mantiene expresamente separadas las validaciones automatizadas de las validaciones físicas o externas.

El candidato funcional certificado por esta revisión corresponde al commit:

```text
c5942990a36287ccb476c66f6f73c7d361d9eca3
```

La rama `main` no forma parte del alcance de ejecución y permanece congelada en:

```text
85b4e02814823e9671803c23798a6ff0bf05c8f6
```

Producción no fue utilizada ni modificada.

---

## 2. Componentes certificados

### 2C.1 — Variante técnica y migración

- Compatibilidad de productos simples mediante variante técnica.
- Migraciones EF verificadas en MySQL descartable de CI.
- Verificación de snapshot EF sin cambios pendientes en el flujo correspondiente.
- Workflow permanente `Bloque 2C.1 - Variante técnica y migración` aprobado sobre el candidato funcional vigente.

### 2C.2 — Ciclo de vida de variante técnica

- Creación o reactivación automática de una única variante técnica para productos simples.
- Sincronización de cantidad, costo, precio, umbral y estado.
- Protección frente a edición/eliminación manual incompatible con el carácter técnico.
- Reglas de conversión entre producto simple y variantes comerciales protegidas por existencias.

### 2C.3 — Backend del escáner

Endpoints exactos:

```text
GET /ventas/productos/por-codigo?codigo={valor}
GET /compras/productos/por-codigo?codigo={valor}
```

Cobertura funcional:

- coincidencia exacta por SKU o código de barras;
- SKU normalizado sin destruir códigos de barras con ceros iniciales;
- variante técnica y variante comercial;
- códigos inexistentes, ambiguos y no operativos;
- ventas sin exposición de costo;
- compras con costo;
- ventas con validación de stock operativo;
- cancelación mediante `CancellationToken`.

### 2C.4 — Frontend del escáner

- lector físico USB/Bluetooth por captura de código y `Enter`;
- consolidación de lecturas repetidas;
- incremento controlado de cantidad;
- validación de stock en venta;
- incorporación de costo retornado por backend en compra;
- lector por cámara/imagen mediante `html5-qrcode`;
- carga diferida del escáner;
- liberación de recursos del stream;
- política de cámara configurada para el frontend;
- validación estática específica del escáner integrada al `lint`;
- workflow dedicado `Fase 2C.4 - Frontend del escáner` aprobado sobre el candidato funcional vigente.

### 2C.5 — Autocomplete remoto

- eliminada la carga inicial masiva de productos desde los formularios de venta y compra;
- búsqueda remota con `debounceTime(300)`, cancelación de solicitudes anteriores y límite de servidor;
- mínimo de 2 caracteres, máximo 100 y hasta 30 resultados;
- selección exacta de variante;
- venta sin exposición de costo y sin variantes agotadas;
- compra con costo y admisión de stock cero;
- hidratación puntual de productos referenciados al editar borradores;
- preservación de imágenes en listas, detalles y formularios;
- convergencia de autocomplete y escáner sobre la misma lógica de incorporación y consolidación de líneas.

---

## 3. Evidencia automatizada vigente

### 3.1 Backend Release

Ejecución verificada en `Desarrollo - Compilación y pruebas`:

```text
Run: 31215765026
Build Release: aprobado
Warnings: 0
Errors: 0
Pruebas unitarias/no integración: 201/201 aprobadas
Fallidas: 0
Omitidas: 0
```

El artefacto de pruebas generado por el workflow fue:

```text
Nombre: desarrollo-backend-tests.zip
Artifact ID: 9008432398
```

### 3.2 MySQL 8.4

- servicio real de CI: MySQL 8.4.11;
- migraciones actuales: aprobadas;
- pruebas de integración categorizadas: aprobadas;
- validación de variante legado, cargas y snapshot: aprobada;
- base de datos: efímera/descartable de CI;
- Producción: no utilizada.

### 3.3 Frontend

- `npm ci`: ejecutado;
- TypeScript/lint: aprobado;
- validación estática específica 2C.4: aprobada;
- build Angular de producción: aprobado.

El `lint` confirmó expresamente:

```text
Fase 2C.4: validación estática del frontend del escáner aprobada.
```

### 3.4 Playwright integral

Ejecución verificada en `Desarrollo - aceptación funcional integral`:

```text
Run: 31215765514
Playwright: 87/87 aprobadas
Fallos: 0
```

La suite incluyó expresamente los escenarios de 2C.4:

1. Venta consolida lecturas repetidas, conserva ceros iniciales y bloquea superar stock.
2. Compra consolida lecturas y conserva el costo retornado por backend.
3. Cámara e imagen quedan cableadas al formulario sin activar hardware real en CI.

También incluyó los escenarios de 2C.5:

1. Venta consulta bajo demanda, no carga catálogo masivo y no expone costo.
2. Venta excluye variantes agotadas del autocomplete remoto.
3. Compra admite stock cero, recibe costo y consolida selecciones repetidas.
4. Regresión: compra y venta seleccionan la variante exacta mediante autocomplete remoto.
5. Regresión: listas, detalles y formularios conservan imagen de producto con búsqueda remota.

La aceptación integral volvió a cubrir además variantes, facturación, cargas masivas, responsive, accesibilidad, permisos, aislamiento por usuario, sesión, seguridad básica y rendimiento controlado.

### 3.5 Correo/PDF aislado

- SMTP efímero: aprobado;
- reintento transitorio: aprobado;
- un único mensaje persistido: aprobado;
- PDF adjunto generado y validado: aprobado.

Artefacto integral:

```text
Nombre: desarrollo-aceptacion-integral.zip
Artifact ID: 9008664999
```

### 3.6 Workflows obligatorios sobre `c5942990...`

```text
Desarrollo - Compilación y pruebas
Run 31215765026 — success

Desarrollo - aceptación funcional integral
Run 31215765514 — success

Fase 2 - Auditoría de configuración y dependencias
Run 31215766762 — success

Fase 8 - Validación completa automatizada
Run 31215765159 — success

Bloque 2C.1 - Variante técnica y migración
Run 31215767604 — success

Fase 2C.4 - Frontend del escáner
Run 31215765486 — success

Solqaryn CI
Run 31215765320 — skipped por condición del workflow
```

Un workflow `skipped` por condición no se contabiliza como validación ejecutada ni como fallo.

---

## 4. Observaciones de dependencias y runners

Los workflows obligatorios concluyen correctamente conforme a las políticas actuales del repositorio. Sin embargo, los runners muestran avisos de deprecación asociados a Node.js 20 en algunas Actions y `npm ci` informa vulnerabilidades dentro del árbol completo de dependencias, principalmente asociado al entorno de desarrollo/herramientas.

Estos avisos no se silencian, no se presentan como inexistentes y deberán continuar bajo auditoría en los bloques posteriores. No constituyen una regresión funcional bloqueante de 2C según los controles vigentes que finalizaron en `success`.

---

## 5. Límites y pendientes que NO se certifican como completados

1. **Dispositivos físicos:** CI valida la integración sin activar hardware real. Permanecen como validaciones manuales posteriores:
   - cámara real Android;
   - cámara real iPhone/iOS;
   - lector USB físico;
   - lector Bluetooth físico.
2. **Permisos del navegador:** el comportamiento final ante permisos de cámara depende del navegador/dispositivo real y debe probarse externamente.
3. **`TipoInventario`:** la diferenciación `MercaderiaVenta` / `InsumoAdministrativo` pertenece al bloque funcional de insumos administrativos y no se declara implementada por 2C.
4. **Dependencias/deprecaciones:** los avisos de runners y dependencias se mantienen registrados para seguimiento; no se declaran corregidos por esta certificación.
5. **Producción:** no existe certificación productiva, despliegue productivo ni autorización de merge derivada de este documento.

---

## 6. Gobernanza

Estado verificado al cierre funcional:

```text
Rama de trabajo: Desarrollo
Candidato funcional: c5942990a36287ccb476c66f6f73c7d361d9eca3
PR oficial: #2 Desarrollo -> main
PR #2 abierto: sí
PR #2 borrador: sí
Merge: no autorizado
Auto-merge: no autorizado
main: 85b4e02814823e9671803c23798a6ff0bf05c8f6
Producción: congelada y no modificada
```

Este documento no autoriza despliegues, migraciones, modificaciones de secretos ni cambios de infraestructura productiva.

---

## 7. Dictamen de Fase 2C.6

Con la evidencia automatizada vigente:

```text
BLOQUE 2C — DESARROLLO: APROBADO
2C.1 VARIANTE TÉCNICA: APROBADA
2C.2 CICLO DE VIDA TÉCNICO: APROBADO
2C.3 BACKEND DEL ESCÁNER: APROBADO
2C.4 FRONTEND DEL ESCÁNER: APROBADO EN CI
2C.5 AUTOCOMPLETE REMOTO: APROBADO
BACKEND RELEASE: 201/201
PLAYWRIGHT INTEGRAL: 87/87
MYSQL 8.4.11: APROBADO
REGRESIONES BLOQUEANTES AUTOMATIZADAS: 0
VALIDACIONES FÍSICAS: PENDIENTES Y EXTERNAS
PRODUCCIÓN: NO AUTORIZADA
MERGE A MAIN: NO AUTORIZADO
```

El Bloque 2C queda funcionalmente cerrado en `Desarrollo`. Las validaciones físicas permanecen explícitamente fuera de esta certificación automatizada.