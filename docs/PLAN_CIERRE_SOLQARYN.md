# Plan obligatorio de trabajo por fases — Solqaryn / SOLQARYN

Rama exclusiva de trabajo: `Desarrollo`.

Pull Request: `Desarrollo -> main`, en borrador hasta autorización expresa de Javier Mejía.

## Entornos oficiales

Solo existen dos entornos lógicos autorizados:

```text
SOLQARYN_producción (Producción)
SOLQARYN_desarrollo
```

Los nombres técnicos actuales de proyectos, servicios, dominios, usuarios y claves se conservan cuando renombrarlos o recrearlos pueda afectar funcionamiento. Cada recurso debe estar asignado documentalmente a uno de los dos entornos; un nombre técnico diferente no constituye un tercer entorno por sí solo.

## Reglas generales

Antes de iniciar cada fase se debe:

1. analizar el alcance completo;
2. identificar riesgos;
3. verificar dependencias;
4. confirmar que ningún cambio afecta Producción;
5. definir pruebas y evidencia de cierre.

No se avanza a la fase siguiente mientras la fase actual conserve un requisito pendiente.

Producción queda congelada durante todo el plan. No se modifican `main`, variables, credenciales, dominios, servicios, despliegues, bases, activos, claves, usuarios administrativos ni migraciones productivas.

## FASE 1 — Entornos y recursos — COMPLETA

Se estandarizaron los entornos `SOLQARYN_producción` y `SOLQARYN_desarrollo`. `main` permanece de solo lectura, `Desarrollo` es la única rama de cambios, `avnadmin` se conserva, Cloudinary usa prefijo de Desarrollo y las migraciones productivas permanecen deshabilitadas.

## FASE 2 — Auditoría general — COMPLETA Y CERTIFICADA

Se cerraron rate limiting, validación JWT, proxy, HSTS, health/readiness, ejecución Docker no privilegiada, AllowedHosts, aislamiento del logo, vulnerabilidades .NET y auditoría npm.

Commit: `20e5bbc917c02946433948355c5c20697b0fe259`.

- Compilación `30263028300`: **success**.
- Aceptación `30263028360`: **success**.
- Auditoría `30263028335`: **success**.

Detalle: `docs/FASE2_AUDITORIA_GENERAL.md`.

## FASE 3 — Corrección de interfaz — COMPLETA Y CERTIFICADA

Se corrigieron ayudas, errores, textos extensos, jerarquía visual, formularios, galería, tabla de Productos, tarjetas móviles, cabecera y rol.

Commit: `0bbc73f00bb8024e72a5837310456311d23f8740`.

- Compilación `30270049875`: **success**.
- Aceptación `30270049661`: **success**.
- Auditoría `30270049562`: **success**.

27 pruebas aprobadas. Detalle: `docs/FASE3_CERTIFICACION_INTERFAZ.md`.

## FASE 4 — Responsive — COMPLETA Y CERTIFICADA

Se certificaron ocho resoluciones entre 320 × 568 y 3840 × 2160, con 240 navegaciones específicas. Se corrigió el desbordamiento de Auditoría.

Commit: `898ada1d9e4c5c22353ba0fbed5589c52a0366de`.

- Compilación `30276203737`: **success**.
- Aceptación `30276203714`: **success**.
- Auditoría `30276203753`: **success**.

43 pruebas aprobadas. Detalle: `docs/FASE4_CERTIFICACION_RESPONSIVE.md`.

## FASE 5 — Imágenes — COMPLETA Y CERTIFICADA

Se implementó el componente reutilizable de imágenes, fallback accesible, texto alternativo, carga diferida, hero prioritario, galería y lightbox por teclado, e imagen principal en Productos, Compras, Ventas y Movimientos.

Commit: `90eb4ff4c9b7b4a8ed66561fa092f7521ebe7630`.

- Compilación `30289511599`: **success**.
- Aceptación `30289510773`: **success**.
- Auditoría `30289511930`: **success**.

47 pruebas aprobadas. Artefacto `8662414676`. Detalle: `docs/FASE5_CERTIFICACION_IMAGENES.md`.

## FASE 6 — Facturación e impresión — COMPLETA Y CERTIFICADA

### Perfiles implementados

- A4 — 210 × 297 mm.
- Carta — 215.9 × 279.4 mm.
- Legal — 215.9 × 355.6 mm.
- Oficio — 215.9 × 330.2 mm.
- A5 — 148 × 210 mm.
- POS 58 mm — rollo continuo.
- POS 80 mm — rollo continuo.

### Correcciones cerradas

- Catálogo backend de perfiles y alias controlados.
- Generador QuestPDF para página fija y rollo térmico.
- A4 preservado para correo, WhatsApp y enlaces públicos.
- Endpoint de catálogo y descarga seleccionable.
- Encabezado, nombre de archivo y auditoría por formato.
- Selector de papel y preferencia local.
- Vista previa proporcional para A5 y POS.
- Descarga e impresión del perfil elegido.
- Desplazamiento interno para papeles grandes en teléfono.
- Snapshot fiscal preservado sin recálculo.
- Sin migración de base de datos.

### Certificación

Commit funcional: `14bf32069f9d87f731e59f230b9e9f5f16ade14e`.

- `Desarrollo - Compilación y pruebas`, run `30295557180`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30295557155`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30295557157`: **success**.

51 pruebas aprobadas. Artefacto `8664760725`. Detalle: `docs/FASE6_CERTIFICACION_FACTURACION_IMPRESION.md`.

### Validaciones físicas pendientes

Solo en Desarrollo: impresoras reales de oficina y térmicas, drivers, USB/red/Bluetooth, márgenes, densidad, corte, avance y diálogos de impresión por dispositivo.

La Fase 6 no modificó Producción y no autoriza merge ni despliegue.

## FASE 7 — Envío de correo — IMPLEMENTACIÓN COMPLETA; ACEPTACIÓN EXTERNA PENDIENTE

### Implementación cerrada

- Validación completa de configuración SMTP.
- Autenticación configurable y TLS sin bypass de certificados.
- Timeout y máximo de intentos configurables.
- Reintentos acotados ante errores transitorios.
- Códigos de error seguros y respuestas HTTP diferenciadas.
- Logs con host, remitente y destinatario enmascarados.
- Plantilla HTML responsive y alternativa de texto plano.
- PDF oficial A4 adjunto.
- `X-Solqaryn-Message-Id` para trazabilidad.
- Idempotencia ante doble clic y repetición HTTP.
- Historial con resultado y cantidad de intentos.
- Endpoint `GET /facturas/correo/estado`.
- Variables declarativas únicamente para Render Desarrollo.
- Pruebas unitarias con servidor SMTP real en proceso.
- Prueba E2E con SMTP efímero, fallo temporal intencional, reintento y captura `.eml`.

### Certificación aislada

Commit funcional: `53db49dff838779a707c360bc3b0294939407387`.

- `Desarrollo - Compilación y pruebas`, run `30302605498`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30302605307`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30302605671`: **success**.

```text
52 pruebas totales
52 aprobadas
0 fallos
0 errores
0 omitidas
```

Artefacto:

```text
desarrollo-aceptacion-integral
artifact id: 8667374072
SHA-256: a1b022885921276b765c16020f1aea39606a0f597e44b5ae5abc26a7ad522a16
```

La evidencia certifica dos intentos SMTP, un fallo temporal, un único mensaje guardado y un PDF A4 válido de 126302 bytes.

Detalle: `docs/FASE7_CERTIFICACION_CORREO.md`.

### Límite y pendiente obligatorio

La idempotencia actual es local al proceso; una versión distribuida persistente queda como mejora futura.

Para cerrar la Fase 7 en sentido estricto falta configurar credenciales SMTP reales exclusivamente en `solqaryn-api-desarrollo`, enviar a un buzón controlado y comprobar:

- recepción en bandeja de entrada;
- comportamiento en spam;
- remitente y Reply-To;
- HTML y texto plano;
- PDF adjunto correcto;
- un solo correo ante doble clic;
- historial y logs de Render Desarrollo.

Producción no debe usarse ni modificarse para esta validación.

## FASE 8 — Validación completa automatizada — COMPLETA Y CERTIFICADA; EXTERNA PENDIENTE

La auditoría transversal automatizada se ejecutó sobre MySQL 8.4 y SMTP temporales y descartables. Incluyó compilación, pruebas unitarias, migraciones, seguridad HTTP, permisos, aislamiento, facturación, variantes, inventario, cargas masivas, PDF, correo aislado, accesibilidad, teclado, responsive extremo, rendimiento controlado, consola y auditoría de logs/secretos.

Commit funcional: `688cbd195e720d8f9c1d04d28c287c7c934035f2`.

- `Desarrollo - Compilación y pruebas`, run `30474905738`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30474905564`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30474905571`: **success**.
- `Fase 8 - Validación completa automatizada`, run `30474905679`: **success**.

```text
Playwright integral: 81 aprobadas, 0 fallos
Suite especializada Fase 8: 7 aprobadas, 0 fallos
```

Artefacto `8733300881`, digest SHA-256 `b0b5962f4230dc90039c744d767bd6e5ef87f011c3ceb5e54d8e29d537a62aa0`.

Se corrigieron nombres accesibles en controles Angular Material, etiquetas de Configuración y el desbordamiento móvil de Cargas masivas.

Permanecen pendientes las validaciones externas y físicas de correo real, Render, Vercel, Aiven, Cloudinary, WhatsApp, dispositivos e impresoras. Detalle: `docs/FASE8_VALIDACION_COMPLETA_AUTOMATIZADA.md`.

## FASE 9 — Informe final — COMPLETA

Se consolidaron cambios, problemas y soluciones, matriz de cumplimiento, riesgos, responsabilidades del propietario y un plan preparatorio de liberación y rollback.

Entregables:

```text
docs/FASE9_INFORME_FINAL.md
docs/FASE9_MATRIZ_CUMPLIMIENTO.md
docs/FASE9_CHECKLIST_VALIDACIONES_EXTERNAS.md
docs/FASE9_PLAN_LIBERACION_Y_ROLLBACK.md
.github/checkpoints/phase-9
```

Dictamen:

```text
DESARROLLO Y AUTOMATIZACIÓN: APROBADOS
VALIDACIONES EXTERNAS: PENDIENTES
PRODUCCIÓN: NO APTO TODAVÍA / NO AUTORIZADO
```

La Fase 9 está documentalmente completa. El siguiente paso no es una nueva fase de código: Javier Mejía debe completar el checklist externo o aceptar formalmente excepciones. Solo después podrá reconsiderarse la aptitud para Producción.

## Ciclo funcional complementario 2026

Este ciclo conserva la numeración funcional solicitada durante la ampliación de Solqaryn. No sustituye ni reescribe la numeración histórica del plan de cierre anterior.

### FASE 6 administrativa — Permisos, auditoría y reportes — COMPLETA Y CERTIFICADA

Se consolidó el acceso total implícito e inmutable del administrador, el diagnóstico de usuarios y roles, los permisos sensibles, los indicadores administrativos, las alertas, el resumen de auditoría, la bitácora detallada y las exportaciones CSV/XLSX protegidas.

Commit funcional: `4e590f48ce8297318b61717a0da3525224ce3c1e`.

- `Desarrollo - Compilación y pruebas`, run `30445998761`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30445998912`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30445999042`: **success**.

La aceptación verificó la matriz administrativa, un rol limitado real, respuestas 403, exportaciones sin credenciales, navegación, responsive, consola y contraste. No se requirió migración de base de datos.

Detalle: `docs/FASE6_PERMISOS_AUDITORIA_REPORTES.md`.

### FASE 7 complementaria — Validación integral y cierre — COMPLETA Y CERTIFICADA

Se cerraron el desglose fiscal exacto con descuento separado, la numeración de ventas y facturas segura ante concurrencia, la matriz de variantes 4/3/3, el stock consolidado, las ventas, compras y anulaciones por color, el costo de envío, la exoneración auditada, los pagos parciales y totales, el PDF A4, el correo SMTP aislado y los errores estructurados de cargas masivas.

Commit funcional: `183696e3b25904172ca2857e193a9d6fc04961b6`.

- `Desarrollo - Compilación y pruebas`, run `30464538356`: **success**.
- `Desarrollo - aceptación funcional integral`, run `30464538385`: **success**.
- `Fase 2 - Auditoría de configuración y dependencias`, run `30464538838`: **success**.

```text
75 pruebas totales
75 aprobadas
0 fallos
```

Artefacto `8729297367`, digest SHA-256 `67b159329b0f56cf84fbe8e469da59f8ac737e10214c2c06559e79747776e507`.

No se requirió una migración nueva. La validación usó MySQL 8.4 y SMTP temporales y descartables. No se utilizó ni modificó Producción.

Detalle: `docs/FASE7_VALIDACION_INTEGRAL.md`.

## Regla de publicación

Completar estas fases no autoriza automáticamente el merge ni el despliegue productivo. Antes de cualquier operación sobre Producción se exige respaldo verificable, estrategia de migración única, ventana de mantenimiento, responsables, rollback y autorización expresa de Javier Mejía.