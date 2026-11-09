## Adenda actual 2026-11-09 — 6 Javier + 2 Alex, V13 vigente sin alterar cuatro techos
Task Javier nueva :10 ID 6af16bb9492c8191b8e8c7d606f11507 y nueva :50 ID 6af16bbad26881918c283abe06e1f32a, distintas de las Tasks Alex :10/:50. La nueva autorización protegida entra en vigor sólo tras review independiente, required CI exact-HEAD, merge a dev y readback del Sheet. Las dos nuevas permanecen PAUSADAS/NO_GO durante integración. Su misión posterior es idéntica a las otras cuatro Javier: recuperar exclusivamente los 52 PR VALIDANDO originales por FIFO, sin nuevos scopes, falsa aprobación ni falsos LISTO. No modificar las Tasks Alex, sus permisos, ni QA/main/PROD.

# SOLQARYN — Separación de cuentas GitHub para trabajo gestionado desde ChatGPT

**Decisión expresa del propietario: 31 de octubre de 2026.**
**Repositorio:** `solqaryn/Solqaryn`. **Rama protegida:** `dev`.


### Distribucion y revision por TASK_ID — contrato 15/15/15, 2026-11-05

Cada Task autorizada puede trabajar scopes distintos dentro del limite 15/15/15. Las revisiones se distribuyen con scripts/automation-review-routing.mjs, sin reservas en Sheets ni preasignaciones; un mismo scope nunca se revisa por la Task autora, no se editan PR ajenos activos y los gates GitHub siguen protegidos.



## Reserva operativa para revisión en VALIDANDO (PR #3854)

Una entrega VALIDANDO preserva autora y PR, pero no crea bloqueo persistente de fila. La revisión corresponde a una Task diferente según ruta determinista; cada Task verifica GitHub y evita intervención ante un actor efectivamente ocupado.


## Protocolo vigente de revisión técnica por TASK_ID — orden 2026-11-04

La unidad de autoría funcional de una microtarea comercial es el `TASK_ID` físico autorizado y su evidencia de `PLAN_MAESTRO`/`BITACORA`, no la coincidencia del login GitHub. Una Task habilitada **distinta de la autora** puede revisar técnica y sustantivamente el PR de una Task que use la misma cuenta GitHub, comprobar diff, aislamiento multitenant/RBAC, checks y pruebas exact-SHA, registrar `TECH_REVIEW_PASS` o `REQUEST_CHANGES` con `TASK_ID_AUTOR != TASK_ID_REVISOR`, SHA y readback. Si todo cumple y los permisos y reglas protegidas lo permiten, la Task revisora puede efectuar el merge y el cierre `LISTO` después de CI postmerge exact-SHA y readback de `PLAN_MAESTRO`, `COLA` (fórmulas intactas) y `BITACORA`. **La Task autora no puede emitir aprobación técnica cruzada ni marcar como LISTO su propia microtarea.** Un traspaso de edición requiere lease libre, consentimiento/propiedad válida y ausencia de edición simultánea; revisión solo lectura no otorga permiso de escritura.

La validación cruzada por `TASK_ID` es **técnica**, no un GitHub `APPROVED` formal de otra identidad. GitHub sigue identificando a las cuatro Tasks Javier como `jmejia31` y a las dos Alex como un solo login de su cuenta. No falsificar cuentas, reviews ni firmar aprobaciones propias. PR comerciales ordinarios aprovechan `AUTO_CERT=PASS` confiable y los checks reales sin pedir rutinariamente revisión humana; los PR protegidos de gobierno/validadores o un ruleset que exija aprobación formal conservan sus controles por identidad GitHub realmente independiente. No solicitar de forma predeterminada a `morales35alex` que apruebe trabajo de Javier, ni a `jmejia31` que apruebe trabajo de Alex. La regla es simétrica y no activa ni reprograma Tasks de Alex, que permanecen inactivas hasta nueva orden. Se preservan `PROJECT_SCOPE_LOCK=STRICT`, WIP 6/2/1, rama base `dev`, veto a autoaprobación formal, prohibición de force-push y límites de PROD.

## Identidades predeterminadas

- **Ejecutora institucional:** `solqaryn-platform`. Es la única conexión GitHub que este asistente emplea para crear ramas, agregar o editar código, redactar documentos, registrar commits, abrir PR y ejecutar cambios de mantenimiento. Todos los commits nuevos de un mismo PR deben proceder de esa identidad, con autoría auténtica.
- **Revisor asignado:** `jmejia31` (Javier). Su función para esos PR es leer diff, CI exact-SHA, seguridad, pruebas y evidencias, emitir comentarios, solicitar correcciones o registrar `APPROVED` cuando el cambio realmente cumpla las condiciones y su identidad sea independiente de todos los contribuyentes del PR.
- No alternar entre las conexiones `solqaryn-platform` y `jmejia31` para redactar o publicar commits del mismo trabajo. No usar `jmejia31` para corregir el PR que después deba aprobar. Si hay que corregirlo, la ejecutora institucional hace el commit y la revisión se renueva sobre el SHA final.

## Actualización de identidad — 2026-11-04

A petición expresa del propietario se creó `PRIMARY_DEV_04` de Javier al minuto :30 con ID `6aeacb7820ac8191b7d768a6f33dfa09`, diferente del ID retirado de Alex :30. El censo propuesto tras autorización protegida es seis Tasks: Javier cuatro (:00/:20/:30/:40) y Alex dos (:10/:50). La nueva Task está PAUSADA preventivamente y solo se habilita después de revisión formal independiente sobre SHA final, CI/Scope Lock, merge a dev y readback institucional. No cambia el reparto manual `solqaryn-platform` autora y `jmejia31` revisor ni la prohibición de autoaprobar el propio PR.

## Criterio anterior de separación (2026-11-03)

La regla `solqaryn-platform` autora y `jmejia31` revisor aplica a **los PR gestionados manualmente por este asistente**, no a las cinco Tasks físicas de ChatGPT. Cada Task opera únicamente con la identidad GitHub efectivamente autenticada y sus propios permisos, sin suplantación. Se permite **autoevaluación técnica** (pruebas, análisis, depuración y corrección) y se prohíbe **autocertificación independiente** cuando la política exige un tercero. El revisor que corrige código en una entrega ajena pierde la independencia respecto de esa entrega, pero puede continuar contribuyendo si tiene permiso; deberá intervenir otro revisor independiente cuando sea obligatorio. `REVIEW_FIRST → RECOVER → CLAIM` permanece obligatorio. Ninguna de estas aclaraciones activa Tasks ni elimina protecciones o aprobaciones excepcionales.

## Revisión genuina y cierre

La revisión independiente es una comprobación real: no usar cuentas, tokens o sesiones ajenas para simular a otro participante; no enviar `APPROVED` sin inspección y criterio propios. En los PR de `solqaryn-platform`, el revisor predeterminado es `jmejia31`. Un resultado de CI no debe presentarse como aprobación humana y viceversa. GitHub conserva sus checks, bloqueo de force-push y merge protegido.

Un PR solo se cierra cuando se acreditan checks exact-SHA, revisión válida cuando el gobierno la exige, merge protegido y lectura posterior de las fuentes institucionales afectadas. Si Javier figura como contribuyente de un PR, no puede ser su revisor independiente. La revisión de Javier en esos casos no satisface el control.

## PR anteriores y cambios futuros

El presente reparto rige **nuevas intervenciones de este asistente** a partir de esta orden. No modifica el autor ni reescribe la historia de PR anteriores: #3803, #3806, #3807, #3808 y #3809 fueron creados por `jmejia31`; #3809 además contiene contribuciones de `solqaryn-platform`. Ese PR no puede recibir una revisión independiente válida de ninguna de esas dos identidades. Debe conservar su evidencia y recibir evaluación de un tercero que no haya contribuido, o revisarse mediante un mecanismo de gobierno explícitamente autorizado y protegido. No hacer cherry-picks o cambios de metadatos con la finalidad de fingir autoría independiente.

Este contrato define **exclusivamente los roles de las conexiones GitHub usadas en intervenciones manuales de este asistente**; no asigna privilegios funcionales permanentes a ninguna Task programada ni cambia cuotas, autorizaciones comerciales o propiedad de cuentas ChatGPT. **Las cinco Tasks del allowlist comercial vigente tienen paridad funcional condicionada a sus permisos efectivos e identidad autenticada**: cualquiera puede seleccionar, desarrollar, probar su propio trabajo, revisar trabajo ajeno, detectar y corregir defectos, recuperar y cerrar scopes comerciales en `dev` dentro de los gates autorizados. `PRIMARY` y `SECONDARY` son nombres de inventario, no prohibiciones de desarrollo. La Task que corrige un PR pasa a ser contribuyente y no puede aprobarlo formalmente como revisor independiente; cuando se exige revisión independiente, esta corresponde a un login GitHub distinto de todos los contribuyentes. La certificación técnica automática de PR comerciales ordinarios corresponde al Scope Lock/CI confiable del SHA exacto, sin aprobación humana rutinaria; los cambios de gobierno, seguridad y validadores requeridos mantienen revisión formal realmente independiente. Las tres Tasks de Javier se observaron pausadas y su reactivación requerirá orden expresa y readback físico; una futura actualización de sus prompts no puede conservar `REVIEWER_ONLY` como restricción funcional general. La autorización comercial sigue limitada a cinco IDs, sin el antiguo slot Alex :30; excluir un registro no demuestra su baja física.

La asignación de cuentas **solo cambia por una nueva orden expresa del propietario**, aplicada con PR protegido y pruebas; no es una regla que obligue a mantener 3+3 Tasks.

## Impacto, pruebas y reversión

- Afecta a la autoría de PR, la separación de contribuyentes, el control de revisión y la documentación de gobierno.
- No cambia frontend, backend de negocio, API/DTO, migraciones, datos, infraestructura, entornos, credenciales ni privilegios efectivos.
- Validación: autenticación de ambas conexiones; autor del PR institucional; no commits de Javier; petición de revisión desde la cuenta institucional; self-tests del verificador y checks obligatorios sobre SHA exacto.
- Reversión: PR protegido con instrucción expresa del propietario, sin alterar historial ni desactivar protecciones.
