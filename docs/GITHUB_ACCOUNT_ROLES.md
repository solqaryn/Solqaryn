# SOLQARYN — Separación de cuentas GitHub para trabajo gestionado desde ChatGPT

**Decisión expresa del propietario: 31 de octubre de 2026.**
**Repositorio:** `solqaryn/Solqaryn`. **Rama protegida:** `dev`.

## Identidades predeterminadas

- **Ejecutora institucional:** `solqaryn-platform`. Es la única conexión GitHub que este asistente emplea para crear ramas, agregar o editar código, redactar documentos, registrar commits, abrir PR y ejecutar cambios de mantenimiento. Todos los commits nuevos de un mismo PR deben proceder de esa identidad, con autoría auténtica.
- **Revisor asignado:** `jmejia31` (Javier). Su función para esos PR es leer diff, CI exact-SHA, seguridad, pruebas y evidencias, emitir comentarios, solicitar correcciones o registrar `APPROVED` cuando el cambio realmente cumpla las condiciones y su identidad sea independiente de todos los contribuyentes del PR.
- No alternar entre las conexiones `solqaryn-platform` y `jmejia31` para redactar o publicar commits del mismo trabajo. No usar `jmejia31` para corregir el PR que después deba aprobar. Si hay que corregirlo, la ejecutora institucional hace el commit y la revisión se renueva sobre el SHA final.

## Revisión genuina y cierre

La revisión independiente es una comprobación real: no usar cuentas, tokens o sesiones ajenas para simular a otro participante; no enviar `APPROVED` sin inspección y criterio propios. En los PR de `solqaryn-platform`, el revisor predeterminado es `jmejia31`. Un resultado de CI no debe presentarse como aprobación humana y viceversa. GitHub conserva sus checks, bloqueo de force-push y merge protegido.

Un PR solo se cierra cuando se acreditan checks exact-SHA, revisión válida cuando el gobierno la exige, merge protegido y lectura posterior de las fuentes institucionales afectadas. Si Javier figura como contribuyente de un PR, no puede ser su revisor independiente. La revisión de Javier en esos casos no satisface el control.

## PR anteriores y cambios futuros

El presente reparto rige **nuevas intervenciones de este asistente** a partir de esta orden. No modifica el autor ni reescribe la historia de PR anteriores: #3803, #3806, #3807, #3808 y #3809 fueron creados por `jmejia31`; #3809 además contiene contribuciones de `solqaryn-platform`. Ese PR no puede recibir una revisión independiente válida de ninguna de esas dos identidades. Debe conservar su evidencia y recibir evaluación de un tercero que no haya contribuido, o revisarse mediante un mecanismo de gobierno explícitamente autorizado y protegido. No hacer cherry-picks o cambios de metadatos con la finalidad de fingir autoría independiente.

Este contrato define **roles de las conexiones GitHub usadas por este asistente**, no cuotas de Tasks, nuevas autorizaciones comerciales ni cambios de propiedad de cuentas ChatGPT. Las tres Tasks de Javier permanecen pausadas y, si se ajustan sus prompts, solo podrán ejercer tareas de revisión tras nueva orden de reactivación. La autorización comercial actual 5/6 y la exclusión de Alex :30 permanecen intactas.

La asignación de cuentas **solo cambia por una nueva orden expresa del propietario**, aplicada con PR protegido y pruebas; no es una regla que obligue a mantener 3+3 Tasks.

## Impacto, pruebas y reversión

- Afecta a la autoría de PR, la separación de contribuyentes, el control de revisión y la documentación de gobierno.
- No cambia frontend, backend de negocio, API/DTO, migraciones, datos, infraestructura, entornos, credenciales ni privilegios efectivos.
- Validación: autenticación de ambas conexiones; autor del PR institucional; no commits de Javier; petición de revisión desde la cuenta institucional; self-tests del verificador y checks obligatorios sobre SHA exacto.
- Reversión: PR protegido con instrucción expresa del propietario, sin alterar historial ni desactivar protecciones.
