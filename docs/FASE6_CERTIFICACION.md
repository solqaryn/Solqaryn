# Fase 6 — Certificación técnica de Solqaryn

Fecha de certificación: 22 de julio de 2026  
Rama: `agent/mejoras-solqaryn`  
Pull Request: `#1`  
Base protegida: `main`

## Resultado

La Fase 6 queda **completa y certificada** en la rama de trabajo. No se aplicaron migraciones a Aiven, no se desplegó producción y no se fusionó el Pull Request.

## Ejecución certificada

- Workflow: `Solqaryn CI`.
- Run ID: `29929548226`.
- Commit evaluado: `d22d0d9fde81c7efd15b9728d4e7fd587f51a2ef`.
- Resultado del frontend: aprobado.
- Resultado del backend: aprobado.
- Resultado de pruebas: **66 ejecutadas, 66 aprobadas, 0 fallidas**.

El workflow generó y confirmó posteriormente el SQL revisable en la misma rama. Ese commit no altera código funcional; incorpora únicamente el artefacto SQL generado.

## Certificaciones automatizadas

### Entity Framework Core

- Migración detectada: `20260722142118_Fase6SeguridadFacturacionPerfil`.
- El método `Up()` contiene únicamente operaciones aditivas permitidas.
- No se detectaron:
  - `DropTable`.
  - `DropColumn`.
  - `DeleteData`.
  - `AlterColumn`.
  - renombramientos destructivos.
- `dotnet ef migrations has-pending-model-changes` finalizó correctamente.
- `AppDbContextModelSnapshot` está alineado con las entidades y configuraciones actuales.

### SQL forward

Archivo:

```text
docs/migraciones/004_fase6_seguridad_facturacion_perfil.sql
```

Intervalo exacto:

```text
20260720101639_Fases18SesionConfigPermisosCalculos
→ 20260722142118_Fase6SeguridadFacturacionPerfil
```

El script:

- inicia una transacción;
- agrega columnas nuevas;
- crea la tabla `CompraDocumentos`;
- crea índices nuevos;
- registra la migración en `__EFMigrationsHistory`;
- finaliza con `COMMIT`;
- no contiene `DROP`, `TRUNCATE` ni `DELETE FROM`.

### Backend

- Restauración NuGet: aprobada.
- Build `Release`: aprobado.
- Pruebas: 66/66 aprobadas.

Cobertura relevante incorporada:

- permisos y alcance administrativo;
- aislamiento financiero;
- confirmación y anulación de compras y ventas;
- stock e inventario;
- eliminación lógica de productos, categorías, clientes y proveedores;
- conservación de imágenes al eliminar lógicamente un producto;
- impuesto incluido sin doble suma;
- impuesto adicional;
- descuento aplicado antes del impuesto;
- impuesto incluido en compras;
- hash SHA-256 de enlaces públicos;
- vencimiento y límite de accesos;
- revocación de enlaces;
- acceso al perfil mediante el `UsuarioId` autenticado;
- unicidad del nombre de usuario;
- normalización de datos del perfil;
- rechazo de contraseñas débiles;
- cambio de contraseña con validación de la contraseña actual.

### Frontend

- `npm ci`: aprobado.
- `npm run build:prod`: aprobado.
- Compilación de Angular 20: aprobada.
- Se corrigieron importaciones de Angular Material y proyección de contenido en botones antes de la certificación final.

## Cambios de esquema de la Fase 6

### Ventas

- `Eliminado`.
- `EliminadoPorUsuarioId`.
- `FechaEliminacion`.

### Compras

- `Eliminado`.
- `EliminadoPorUsuarioId`.
- `FechaEliminacion`.

### Productos

- `Activo` con valor predeterminado `true`.
- `Eliminado`.
- `EliminadoPorUsuarioId`.
- `FechaEliminacion`.

### Usuarios

- `FotoPerfilUrl`.
- `FotoPerfilPublicId`.

### Snapshots fiscales

- `VentaImpuestos.IncluidoEnPrecioSnapshot`.
- `CompraImpuestos.IncluidoEnPrecioSnapshot`.

### Comprobantes de compras

Nueva tabla `CompraDocumentos`, con relación restrictiva hacia `Compras` y eliminación lógica propia.

## Aplicación futura en Aiven

La migración no debe aplicarse hasta la Fase 7. Antes de hacerlo:

1. Verificar nuevamente el respaldo automático y la exportación SQL local.
2. Confirmar que la base seleccionada es la productiva correcta.
3. Revisar el SQL forward guardado.
4. Elegir una sola estrategia:
   - ejecutar el SQL revisado; o
   - habilitar temporalmente las migraciones al iniciar el backend.
5. No utilizar ambas estrategias en el mismo despliegue.
6. Comprobar `__EFMigrationsHistory` y el estado del servicio después de la aplicación.

## Pendiente para la Fase 7

La certificación automatizada no sustituye las pruebas productivas reales. Permanecen pendientes:

- Preview controlado.
- Matriz Administrador/Vendedor/rol personalizado.
- SMTP real con Gmail.
- PDF real con logo.
- WhatsApp y enlace público desde teléfono e incógnito.
- Cloudinary para perfil y comprobantes de compra.
- Responsividad visual en dispositivos representativos.
- Aplicación controlada de la migración en Aiven.
- Validación posterior de Render y Vercel.
- Autorización del propietario antes de fusionar a `main`.
