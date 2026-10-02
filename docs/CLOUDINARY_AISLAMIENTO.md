# Cloudinary — aislamiento entre DEV y PROD

## Objetivo

Evitar que una base Aiven de DEV creada desde un fork productivo pueda eliminar activos reales de PROD.

El identificador oficial del entorno de DEV es `solqaryn_dev`.

## Comportamiento implementado

Cuando `Cloudinary__EnvironmentPrefix=solqaryn_dev`:

- las nuevas imágenes de productos se almacenan bajo `solqaryn_dev/Solqaryn/productos`;
- los nuevos comprobantes se almacenan bajo `solqaryn_dev/Solqaryn/compras`;
- las nuevas fotografías de perfil se almacenan bajo `solqaryn_dev/solqaryn/perfiles`;
- cualquier eliminación cuyo `PublicId` no comience con `solqaryn_dev/` queda bloqueada antes de llamar a Cloudinary.

Esto protege los activos productivos aunque la base de Aiven DEV conserve URLs y `PublicId` históricos de PROD.

Cuando el prefijo está vacío, el comportamiento productivo se conserva sin cambiar las carpetas ni los identificadores actuales.

## Recurso autorizado de DEV

La clave de API creada por el propietario y etiquetada `solqaryn_dev` es la única clave autorizada para el servicio Render de DEV. Sus valores deben permanecer exclusivamente como secretos externos y nunca deben copiarse al repositorio, al PR, a capturas públicas o al chat.

Las otras claves visibles en el panel de Cloudinary no se consideran entornos duplicados: pueden pertenecer a funciones internas de la plataforma. No deben eliminarse sin una auditoría de dependencias en el panel.

## Validación automatizada

`CloudinaryEnvironmentIsolationTests` verifica que DEV no pueda eliminar:

- una imagen productiva de producto;
- un comprobante productivo de compra.

El workflow de DEV también verifica que `render.yaml` declare exactamente el prefijo `solqaryn_dev`.

## Validación externa pendiente

En el entorno real de DEV se debe probar:

1. confirmar que Render usa la clave etiquetada `solqaryn_dev`, sin exponer su valor;
2. subir un activo nuevo y confirmar su prefijo `solqaryn_dev/`;
3. eliminar ese activo de DEV;
4. intentar eliminar una referencia heredada de PROD y confirmar que se rechaza;
5. revisar que ningún activo productivo haya cambiado;
6. verificar almacenamiento, transformaciones y ancho de banda.

La opción más segura sigue siendo usar un product environment o una cuenta Cloudinary completamente separada para DEV. Mientras se comparta el mismo product environment, el prefijo y la clave autorizada son controles obligatorios.

## Evidencia de ownership — 2026-09-25

- Cuenta/perfil: `Solqaryn Platform`
- Email: `solqaryn.platform@outlook.com`
- Cloud name: `riyrzmob`
- Product Environment ID: `7ab9e3e6de660a0b70eb4a5bacf331`
- Estado: `Active`
- Product environments observados: 1
- Media Library observada: contenido de ejemplo únicamente

Estado: ownership del Cloudinary nuevo confirmado. Aún falta certificar la API key usada por Render DEV y realizar un upload real que produzca un PublicId bajo `solqaryn_dev/` antes de retirar cualquier recurso Cloudinary legacy.

## Inventario de API keys — 2026-09-25

La evidencia visual del cloud `riyrzmob` muestra una única API key activa llamada `Root`, creada el 2026-09-22. No existe todavía una key dedicada `solqaryn_dev`.

Decisión operativa:

- conservar `Root` sin usarla como credencial de aplicación;
- crear una API key dedicada denominada `solqaryn_dev`;
- usar esa key exclusivamente en Render DEV;
- no revelar ni registrar el API Secret en chat/repositorio/capturas;
- PROD permanece fuera de alcance.

## API keys canónicas — 2026-09-25

Evidencia visual confirma las siguientes keys activas en `riyrzmob`:

- `Root`: administración/recuperación únicamente;
- `solqaryn_dev`: credencial operativa exclusiva de DEV;
- `solqaryn_prod`: credencial reservada para PROD, aún sin conectar.

No almacenar ni revelar API Secret en Git, documentación, capturas o chat.

## Render DEV conectado — 2026-09-25

La configuración visible de `solqaryn-api-dev` contiene:

- `Cloudinary__CloudName=riyrzmob`;
- `Cloudinary__EnvironmentPrefix=solqaryn_dev`;
- `Cloudinary__ApiKey` con valor oculto;
- `Cloudinary__ApiSecret` con valor oculto.

El redeploy manual `dep-darcsc0jo6nc73fffmtg` terminó `live` y el health posterior respondió HTTP 200. Pendiente final: upload funcional desde la aplicación y validación del locator resultante bajo `riyrzmob/.../solqaryn_dev/`.


## Prueba funcional e inventario legacy — 2026-09-25

### Upload funcional

- Render DEV: deploy `dep-darcsc0jo6nc73fffmtg` terminó `live`.
- Health posterior: `/health/ready` HTTP 200.
- Upload real desde SOLQARYN DEV: PASS.
- Cloud destino: `riyrzmob`.
- Prefijo observado en Media Library: `solqaryn_dev/Solqaryn/productos/empresas/1/`.
- La API pública de tienda devuelve el asset nuevo desde `res.cloudinary.com/riyrzmob/.../solqaryn_dev/...`.

### Inventario read-only de referencias legacy

GitHub Actions run `36182095589` / artifact `cloudinary-dev-legacy-inventario-36182095589`:

- `ProductoImagenes.Url`: 10 filas;
- `ProductoImagenes.PublicId`: 10 filas;
- `CompraDocumentos.Url`: 1 fila;
- `CompraDocumentos.PublicId`: 1 fila;
- `Usuarios.FotoPerfilUrl`: 2 filas;
- `Usuarios.FotoPerfilPublicId`: 2 filas.

Interpretación operacional: 13 filas lógicas de assets continúan enlazadas al Cloudinary legacy. El escaneo fue solo lectura, sobre `solqaryn_dev`, y no tocó Producción.

Estado del punto: **Cloudinary DEV nuevo funcional; cierre global PENDIENTE de migrar 13 assets legacy y volver a escanear hasta cero referencias.**
