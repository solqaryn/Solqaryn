# Nota de continuidad — honestidad ante todo

Este archivo se crea el 14/07/2026 al recibir el prompt de 6 partes sobre
Usuarios/Roles/Permisos, Productos+imágenes, Facturas+WhatsApp+Correo,
Configuración visual y Auditoría.

**No existían archivos de seguimiento previos en este repositorio.** El
prompt asume "los tres archivos de seguimiento que has venido utilizando",
pero al revisar el repo (`docs/`, raíz, búsqueda global) no se encontró
ningún archivo de plan/seguimiento de sesiones anteriores. Todo el progreso
previo de este proyecto quedó registrado únicamente en los mensajes de
commit de Git (rama `feature/roles-catalogo-dinamico`, 10 commits).

Se crean aquí, desde cero y honestamente, los tres archivos que el
documento pide mantener:
- `01-PLAN-FASES.md` — plan de fases, estado de cada una.
- `02-CIERRE-FASES.md` — reportes de cierre acumulados.
- `03-COMANDOS-INTEGRACION.md` — comandos exactos para que el usuario
  aplique cada fase.

**Restricción de entorno que sigue vigente:** no hay acceso a `nuget.org`
en este sandbox, por lo que `dotnet build`/`dotnet test` del backend no se
pueden ejecutar aquí. El frontend Angular sí se puede compilar
(`npx ng build`) porque `registry.npmjs.org` está permitido, y así se ha
verificado cada fase hasta ahora.
