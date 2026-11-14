# Fase 6 — Permisos, auditoría y reportes administrativos

## Estado

**Completada y certificada en la rama `Desarrollo`.**

Esta certificación no autoriza despliegues, fusiones ni cambios sobre Producción. El PR oficial continúa abierto y en borrador.

## Objetivo

Consolidar la administración de privilegios y convertir la auditoría existente en una herramienta operativa de control, incorporando diagnósticos y reportes exportables sin exponer credenciales ni datos técnicos sensibles.

## Alcance implementado

### 1. Acceso total implícito del administrador

- El rol dinámico marcado como administrador obtiene todos los permisos publicados en `CatalogoPermisosBase`.
- Los permisos nuevos se reflejan inmediatamente, aunque una instalación anterior no posea todavía una fila persistida para cada combinación.
- La matriz del administrador no puede reducirse desde la API.
- Los roles no administrativos continúan dependiendo exclusivamente de su matriz persistida.

### 2. Diagnóstico de usuarios y accesos

El reporte identifica por usuario:

- rol dinámico asignado;
- condición administrativa;
- estado de usuario y rol;
- bloqueo o eliminación lógica;
- permisos efectivos;
- permisos sensibles;
- estado final de acceso.

No se incluyen contraseñas, hashes, tokens ni secretos.

### 3. Diagnóstico de roles y privilegios

El reporte consolida:

- usuarios asignados;
- permisos efectivos;
- módulos con acceso;
- permisos sensibles;
- porcentaje de cobertura;
- nivel de privilegio;
- estado de configuración;
- detalle de módulo y acción.

Los niveles de privilegio son indicadores administrativos; no modifican automáticamente las matrices.

### 4. Resumen administrativo

La pantalla muestra:

- usuarios registrados, habilitados, bloqueados, eliminados y privilegiados;
- roles activos, sin permisos y sin usuarios;
- cantidad de permisos catalogados;
- actividad de auditoría por período;
- éxitos, rechazos y errores;
- alertas administrativas que requieren revisión.

El período máximo consultable es de 366 días.

### 5. Auditoría consolidada

La vista de Auditoría integra dos bloques:

1. reportes y diagnóstico administrativo;
2. bitácora cronológica detallada.

La bitácora conserva filtros por:

- módulo;
- acción;
- resultado;
- fecha desde y hasta;
- usuario, descripción o motivo.

También reconoce explícitamente resultados `Exito`, `Error`, `Rechazado` y `Denegado`.

### 6. Exportaciones

Formatos habilitados:

- CSV con BOM UTF-8;
- XLSX.

Tipos de reporte:

- usuarios y accesos;
- roles y permisos;
- auditoría.

Controles de exportación:

- neutralización de fórmulas en CSV y XLSX;
- IP enmascarada en la exportación de auditoría;
- exclusión de valores anteriores y nuevos sin procesar;
- máximo de 50,000 eventos por exportación de auditoría;
- registro de cada exportación en la propia bitácora.

## Endpoints

```text
GET /reportes-administrativos/resumen
GET /reportes-administrativos/usuarios-accesos
GET /reportes-administrativos/roles-permisos
GET /reportes-administrativos/auditoria-resumen
GET /reportes-administrativos/exportar/{tipo}?formato=csv|xlsx
```

Todos los endpoints:

- requieren autenticación;
- exigen permisos del módulo `ReportesAdministrativos`;
- realizan además una verificación administrativa explícita.

## Permisos incorporados

```text
ReportesAdministrativos:Ver
ReportesAdministrativos:Exportar
```

No se requirió una migración de base de datos. Los enums de módulo se almacenan como texto y el catálogo de permisos se sincroniza mediante el servicio de seed existente.

## Interfaz

La interfaz se integró en la ruta administrativa existente `/auditoria` e incluye:

- indicadores ejecutivos;
- alertas;
- pestaña de usuarios y accesos;
- pestaña de roles y permisos;
- pestaña de actividad de auditoría;
- búsqueda y filtros;
- detalle expandible de permisos;
- exportaciones CSV y Excel;
- diseño responsive para escritorio y teléfono.

## Compatibilidad MySQL

La consolidación de permisos evita `COUNT(DISTINCT)` sobre objetos compuestos, operación que Pomelo/MySQL no traduce de forma estable. Se consulta únicamente la proyección mínima de la matriz y la deduplicación se efectúa en memoria sobre un conjunto acotado por roles y permisos.

## Evidencia automatizada

Commit funcional certificado:

```text
4e590f48ce8297318b61717a0da3525224ce3c1e
```

Ejecuciones aprobadas:

```text
Desarrollo - Compilación y pruebas:             30445998761 — success
Desarrollo - aceptación funcional integral:      30445998912 — success
Auditoría de configuración y dependencias:       30445999042 — success
```

La aceptación comprobó:

- matriz administrativa completa e inmutable;
- resumen de usuarios, roles, privilegios y auditoría;
- diagnóstico de acceso efectivo;
- rol limitado con un único permiso;
- exportaciones CSV y XLSX válidas;
- ausencia de contraseñas y hashes en exportaciones;
- denegación HTTP 403 a usuarios no administrativos;
- interfaz de reportes y bitácora;
- ausencia de desbordamiento horizontal;
- navegación sin errores de JavaScript;
- usabilidad móvil;
- contraste en tema claro y oscuro;
- regresión integral de módulos, facturación, PDF y correo SMTP aislado.

## Condiciones preservadas

- Única rama modificada: `Desarrollo`.
- `main` permanece congelada.
- No se crearon ramas.
- No se fusionó el PR #2.
- No se habilitó auto-merge.
- No se desplegó.
- No se aplicaron migraciones ni cambios en Producción.
- No se modificaron credenciales, dominios, bases de datos ni servicios productivos.
