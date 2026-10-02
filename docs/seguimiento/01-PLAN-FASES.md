# Plan de fases — Prompt de 6 partes (Usuarios/Roles/Permisos, Productos+imágenes, Facturas+WhatsApp+Correo, Colores, Auditoría)

Fecha de creación: 14/07/2026. Ver `00-INICIO.md` para la nota de honestidad
sobre continuidad de sesiones.

## Aclaración de numeración #14 / #15 / #18

El prompt pide localizar "las secciones #14, #15 y #18" pero no existe un
documento de planificación previo con numeración propia. Hay dos
numeraciones candidatas y se cubren ambas:

- **Del prompt original de esta conversación (26 secciones):**
  §14 Eliminación lógica y física · §15 Auditoría avanzada · §18 Transacciones.
- **De este documento nuevo (6 partes):**
  §14 Envío por WhatsApp · §15 Envío por correo · §18 Integración backend.

## Estado real verificado (no supuesto) al iniciar

| Módulo | Estado real |
|---|---|
| Usuarios | CRUD parcial: falta GetById, búsqueda/filtro/paginación, bloqueo/desbloqueo, eliminación |
| Roles | CRUD completo (ya es el estándar construido en fases anteriores) |
| Permisos | Catálogo + matriz completos |
| Productos | CRUD completo, carga de imágenes a Cloudinary, **sin endpoint de descarga** |
| Facturas | Vista HTML imprimible (`@media print`), **sin PDF real generado en backend** |
| WhatsApp | No implementado (verificado con grep, cero resultados) |
| Correo | No implementado (verificado con grep, cero resultados) |
| Colores/tema | Variables CSS fijas en `styles.scss`, **sin panel de administración ni persistencia** |
| Auditoría | Estructura avanzada ya existe (fase 8 de este mismo proyecto) |

## Fases

### Fase 1 — Usuarios al estándar de Roles (referencia obligatoria del prompt)
Backend: `GetByIdAsync`/endpoint detalle, búsqueda+filtro+paginación,
bloqueo/desbloqueo (distinto de activar/desactivar), eliminación lógica con
las mismas protecciones que Roles (no eliminar el último admin activo).
Frontend: vista de detalle real separada del formulario, confirmaciones,
acciones por fila. Responsive desde el inicio.
Criterio de cierre: Usuarios expone las mismas operaciones que Roles.

### Fase 2 — Roles/Permisos: auditoría de seguridad crítica
Verificar en código (no suponer): protección de roles de sistema, bloqueo
de último administrador, invalidación de permisos efectivos al cambiar rol.
Esto ya se implementó en fases previas — esta fase es **verificación y
corrección**, no reconstrucción.

### Fase 3 — Descarga de imágenes de producto
Backend: endpoint de descarga individual y por lote desde Cloudinary,
autorizado, con manejo de imagen inexistente. Frontend: vista de detalle
de producto con galería, descarga, estado vacío.

### Fase 4 — Generación real de PDF de facturas
Backend: generación de PDF real (no solo HTML imprimible) con librería
compatible con .NET, usando los datos reales de la venta/compra.
Frontend: botón de descarga de PDF real.

### Fase 5 — WhatsApp (envío manual asistido, sin API oficial)
Dado que el proyecto no tiene integración con WhatsApp Business API,
se implementa el flujo realista que el propio prompt permite: generar
enlace `wa.me` con mensaje prellenado y el PDF accesible por URL segura,
sin fingir envío automático de adjuntos.

### Fase 6 — Correo (SMTP configurable)
Backend: servicio de correo vía variables de entorno (sin credenciales en
código), adjuntando o enlazando el PDF. Registro de intentos/resultados.

### Fase 7 — Configuración visual y colores centralizados
Backend: entidad de tema (colores), endpoint get/put, persistencia real.
Frontend: panel de administración de colores + servicio de tema que
aplique variables CSS en tiempo de ejecución, con persistencia tras
recargar/cerrar sesión.

### Fase 8 — Pruebas formales (única al final, según la Parte 6 del prompt)
Compilación real de frontend y backend (dentro de las limitaciones de
sandbox ya documentadas), revisión de consola, pruebas manuales guiadas
para el usuario de lo que no se puede automatizar aquí.

## Progreso
Todas las fases: **no iniciadas** hasta este documento. Comenzando Fase 1.

## Revisión de producción — 19/07/2026

La URL pública final del frontend es `https://SOLQARYN.vercel.app/login`.
Se verificó que responde 200 y que el HTML visible ya usa `SOLQARYN`.

Estado actualizado de las secciones solicitadas:

- **#14 WhatsApp:** implementado en código con enlace público temporal,
  mensaje formal y registro de intento. Se reforzó el backend para resolver
  automáticamente la URL pública real desde la petición cuando
  `AppSettings:BackendPublicUrl` falte, sea `localhost` o esté mal configurada.
- **#15 Correo:** implementado en código como envío secundario por SMTP con
  PDF adjunto y registro de resultado. Sigue dependiendo de credenciales SMTP
  reales en variables de entorno; sin esas credenciales no debe declararse
  envío real operativo.
- **#18 Auditoría/Bitácora:** auditoría disponible y protegida. Se amplió la
  cobertura para creación/edición/eliminación de productos, descarga de
  imágenes, descarga de galería ZIP y descarga de PDFs de factura.

Hallazgo de producción: `GET /health` responde correctamente, pero
`GET /tema-visual` devolvió 404 durante la verificación. El código local sí
incluye `TemaVisualController`, por lo que el hallazgo apunta a un despliegue
de Render no alineado con `main` o a una imagen anterior. Se fuerza una nueva
actualización con estos cambios.
# Seguimiento actualizado 20/07/2026

## Fase correctiva integral

Objetivo: cerrar los pendientes del prompt operativo más reciente sin reconstruir
la arquitectura ni borrar datos existentes.

Alcance aplicado:

1. Seguridad de sesión: JWT máximo de 30 minutos, sin tolerancia por clock skew,
   cierre por inactividad en frontend y limpieza de token/permisos.
2. Auditoría sensible: login exitoso, login fallido y token expirado en backend.
3. Roles/permisos: asignación transaccional por rol activo y permiso activo del
   catálogo; el seed no reactiva permisos deshabilitados manualmente.
4. Cálculos: ISV/ISC y descuentos/promociones integrados en compras/ventas y
   snapshots de impuestos.
5. Configuración empresarial: identidad, logo Cloudinary, textos de documentos,
   cabecera, pie, moneda, zona horaria y copyright administrables.
6. Frontend: identidad dinámica en login/layout/facturas, logo institucional,
   cierre por inactividad y vista de factura con descuentos/impuestos.
7. Migración no destructiva: solo agrega columnas e índices necesarios; no recorta
   columnas existentes ni borra datos de producción.

Estado: implementado y validado localmente. Siguiente paso: desplegar desde
`main` y aplicar la migración en producción.
# Seguimiento actualizado 20/07/2026 - correccion operativa post-produccion

## Fase correctiva de operacion

Objetivo: resolver los hallazgos reportados en produccion sin borrar registros:
usuarios ocultos, asignacion de permisos por rol, desbordes visuales,
eliminacion logica de catalogos y un dashboard principal profesional alimentado
desde la base de datos.

Alcance aplicado:

1. Usuarios: fallback de carga en frontend y reparacion no destructiva de
   usuarios marcados accidentalmente como eliminados sin metadatos de
   eliminacion.
2. Roles/permisos: el catalogo de permisos se normaliza al iniciar la API para
   reactivar permisos del sistema requeridos y permitir asignarlos a roles.
3. Categorias, clientes y proveedores: eliminacion logica, con auditoria y sin
   borrado fisico.
4. Impuestos/descuentos: seed idempotente para ISV 15%, ISC 5% y promocion
   SOLQARYN 10%; los calculos siguen realizandose desde backend.
5. Facturas: fallback de logo institucional para que la factura siempre lleve
   membrete cuando no venga logo de configuracion empresarial.
6. Frontend: correccion de desbordes en permisos y configuracion visual.
7. Dashboard: rediseno ejecutivo con tarjetas, graficas, actividad reciente,
   stock critico y auditoria, usando exclusivamente el endpoint de resumen.

Estado: implementado y validado localmente. No se genero migracion nueva porque
los campos de eliminacion logica utilizados ya existen en el modelo vigente.
