# I8-008.F — Preparación de caso: autoridad global por ámbito

Estado: PREPARAR CASO. Proyecto SOLQARYN, rama dev, Plan Comercial Inicial V3. Autor físico: Javier :50, TASK_ID `6af16bbad26881918c283abe06e1f32a`. No equivale a certificación de seguridad ni a cierre LISTO.

## Condición medible elegida

Un rol con `Ambito=Plataforma` y `EsAdministrador=true` que carece de empresa verificada **no debe ganar acceso implícito** a los datos de la empresa 41. El negativo rector contrasta con un rol estrictamente empresarial de EmpresaId 41 consultando el propio ámbito y con un administrador de la empresa 41 intentando acceder a la empresa 42. No se utilizarán usuarios ni credenciales reales.

## Fixture y resultado esperado (no PASS)

| Rol sintético | Empresa del rol | Empresa objetivo | EsAdministrador | Resultado esperado antes de consultar recursos |
| --- | --- | --- | --- | --- |
| Empresa | 41 | 41 | No | Candidato con contexto concordante; aún requiere autorización real |
| Empresa | 41 | 42 | Sí | DENEGAR por empresa cruzada |
| Plataforma | Sin empresa verificada | 41 | No | DENEGAR sin tenant autorizado |
| Plataforma | Sin empresa verificada | 41 | Sí | DENEGAR; el flag global no sustituye el contexto |

La regresión `I8008FPrepararCasoAmbitoGlobalFixtureTests` valida solo que el **fixture** esté bien formado y exprese esas expectativas. **No ejecuta** el servicio de autorización ni solicitudes HTTP/JWT; su ejecución verde NO demuestra aislamiento, RBAC, auditoría ni acceso efectivo. La validación futura deberá ejecutar la ruta real del servicio/API con identidad, tenant A/B y permisos controlados; verificar respuesta negativa uniforme y ausencia de lecturas/escrituras indebidas. No ampliar `I8-008.F` a una implementación del roadmap.

## Impacto y límites

- Solo un test xUnit de datos sintéticos, esta descripción y nota aditiva del changelog; no modifica API, Domain, Application, Infrastructure, DTO, base de datos, migraciones, JWT ni frontend.
- No cambia el proveedor EF/MySQL, workflows, redes, secretos, QA, main o producción.
- CI exact HEAD y revisión independiente son obligatorias para integración. La predecesora I8-008.A está VALIDANDO al preparar el caso; COLA!G=NO impide merge/LISTO hasta su cierre.
- Rollback: revert protegido selectivo del fixture, este protocolo y la nota aditiva, preservando el historial y los cambios concurrentes.
