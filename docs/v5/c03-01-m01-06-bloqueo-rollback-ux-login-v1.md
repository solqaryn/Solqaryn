# SOLQARYN V5 — C03-01.M01.06 | Gate de bloqueo, rollback y liberacion UX_LOGIN_V1

**ID:** C03-01.M01.06. **Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repositorio:** `solqaryn/Solqaryn@dev`. **Ambito:** preparacion contractual de identidad (riesgo R2 por impacto futuro en autenticacion, pero diff exclusivamente documental). **PROJECT_SCOPE_LOCK=STRICT**.

**Autoridad:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, anexo UX_LOGIN_INTELIGENTE_V1 (L01-L10), y PLAN_MAESTRO!D439/E439, COLA!G/O y DAG_TECNICO del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Alcance atomico de D: **definir condiciones de bloqueo, rollback y liberacion del lote**. E describe el resultado acumulativo de fase, NO capacidades ejecutadas por este documento.

## Contrato vinculante UX_LOGIN_V1 (para implementaciones futuras)

En credenciales solo usuario, contrasena, Ingresar y recuperacion **cuando exista funcionalidad autorizada**. Sin selector de empresa, sucursal, tenantId o rol, sin un segundo POST por doble clic, sin redireccion ciclica. La autoridad proviene siempre de backend .NET con JWT firmado, BCrypt, membresias/roles actuales y aislamiento tenant A/B, nunca de localStorage, URL, host, encabezados ni preferencia de navegador.

| Caso | Condicion verificada en servidor | Resultado observable esperado | Bloqueo / negativo exigido |
| --- | --- | --- | --- |
| L01 | Una empresa activa autorizada | Login unico y panel directo sin selector | Revocacion o cuenta inactiva impide acceso |
| L02 | N empresas y ultima preferida aun autorizada para ese usuario | Entrada directa a la preferida | Una preferencia de otro usuario del navegador no surte efecto |
| L03 | N empresas sin preferencia inequívoca | Una unica eleccion accesible entre membresias activas | No permitir el nombre o ID de empresa arbitrario |
| L04 | Preferencia revocada, vencida o ausente | Limpiar hint y recalcular membresias 0/1/N | No reutilizar permisos/caches de empresa anterior |
| L05 | Cero membresias | 401/403 o salida empresarial segura; consola solo con autoridad global separada | Nunca inferir privilegio global desde rol de empresa |
| L06 | Cambio de empresa desde menu de cuenta con membresias multiples | Revalidar JWT, rol, empresa y permisos; invalidar caches y solicitudes anteriores | Denegar B sin grant, evitar que respuesta tardia A contamine B |
| L07 | Administrador global verificado server-side | Consola global habilitada solo bajo autoridad global genuina | Rechazar claims/hosts/cabeceras falsificados y rol empresarial |
| L08 | Sesion vigente y contexto validado | Panel directo sin pedir nuevamente credenciales | No reusar sesion expirada/revocada |
| L09 | Timeout, JWT vencido, 401/403, replay o clic multiple | Fallo cerrado, mensaje legible, reintento seguro y POST unico | Sin filtracion de tokens, PII o acceso cruzado |
| L10 | UI responsive, teclado, foco y contraste | Navegacion clara/operable en Chromium real | Sin trampa de foco, bucles ni selector oculto que conceda permiso |

## Gate de BLOQUEO: condiciones verificables, no promesas

- **B01 Identidad:** bloquear la implementacion/promocion si credenciales solicitan tenant/empresa, si un usuario con exactamente una membresia recibe intersticial o si las peticiones duplican POST. Contrato L01/L03/L08/L09, no test ejecutado aqui.
- **B02 Seguridad/tenant:** detener el alcance runtime si backend acepta preferencia/localStorage, host spoof, rol de empresa como autoridad global, JWT invalido, membresia revocada, recurso B desde A, o mantiene cachés y peticiones del tenant anterior; exigir casos reales HTTP 401/403, A/B y negacion por defecto.
- **B03 Persistencia/concurrencia:** bloquear cambios de cache/preferencia/sesion sin idempotencia, rollback de fallos y migraciones probadas en MySQL 8.4 efimero cuando se toque EF/Core, sin usar Aiven productivo.
- **B04 Accesibilidad:** no dar PASS al selector excepcional ni al switch con teclado/Tab/foco inaccesible, respuesta tardia, bucle, mensajes opacos, o pruebas Chromium solo simuladas.
- **B05 Evidencia:** no promover ni certificar con CI requerida `skipped`, `cancelled`, `failed`, `queued` o `in_progress`, SHA no causal, mocks como autenticacion real, P0/P1 abiertos, A5 del autor/reparador, omision de readback, o Scope Lock distinto del SHA.
- **B06 Plataforma:** prohibidos QA/main/PROD, secretos, DNS, dominios, pagos reales y activacion MFA/passkeys/SMTP/proveedor no aprobado. El cierre documental M01.06 no equivale a implementar M02-M06 ni cerrar las pruebas L01-L10.

En una falla B01-B06 solo se detiene el ID/superficie afectada; el resto de tareas independientes de dev puede continuar con sus locks y prerequisitos propios. Conservar FAIL historicos, actor, SHA y BITACORA; no forzar formulas de COLA/DAG.

## Plan de rollback y desactivacion segura (sin ejecutarlo en producción)

1. **Esta entrega R2 documental:** revertir su commit con un `git revert` normal sobre `dev`; no force-push, reset ni alteracion retroactiva de BITACORA. Releer owner/estado/CI antes de cualquier nueva transicion.
2. **Contrato futuro de UI:** mantener login basico compatible durante un rollout autorizado, con selector excepcional aislado/detras de configuracion tipada cuando se implemente. La desactivacion de UX no puede conservar autorizacion previamente revocada ni revivir sesion/sucursal/empresa vieja.
3. **Contrato futuro backend:** desactivar de forma conservadora la resolucion automatica si hay error de membresia, proveedor, JWT o persistencia; exigir reautenticacion o denegar acceso. No usar fallback a tenant por defecto, encabezado o preferencia local.
4. **Persistencia futura:** migraciones aditivas, copia/restore y rollback probado con MySQL efimero antes de un cambio real; no borrar historial de membresias, preferencias ni eventos de auditoria ni modificar datos reales para ensayar.
5. **Validacion de rollback futuro:** repetir L04/L05/L06/L09, pruebas negativas JWT/RBAC y A/B, y comprobar que el servicio anterior no filtra datos ni acepta tokens/sesiones invalidadas. CI con SHA y logs causales antes de atribuir un PASS.

## Condicion de liberacion de M01.06 y sucesoras

**Liberar solo la entrega contractual** cuando PLAN D439/E439 y el anexo se reflejen en un documento rastreable: matriz L01-L10, reglas B01-B06, rollback, no-alcance, diff sin secretos, Scope Lock exitoso del commit exacto, readback PLAN/COLA/BITACORA, y A0-A4 proporcionales. En V5, `VALIDANDO -> LISTO` requiere CAS bajo candado V17, y A5 real independiente posteriormente; el owner historico se preserva. El DAG decide si y cuando habilita `C03-01.M02.*` tras confirmar prerequisitos y A5, **nunca** por edit manual de `G/O` o una afirmacion de este texto.

Las fases posteriores tienen entregables distintos: M02 formaliza dominio/DTO/seguridad; M03 implementa backend/autorizacion; M04 UI/selector excepcional; M05 ejecuta L01-L10 con API .NET, MySQL efimero y Playwright cuando corresponda; M06 certifica regresiones, riesgos y rollback. Ninguna de esas fases se anticipa ni se declara aprobada.

## A0-A5 y limites de evidencia de esta microtarea

- **A0 documental:** inspeccion de diff, consistencia de diez casos, seis bloqueos, rollback y no inclusion de secretos; CHANGELOG debe mantener secciones autocontenidas.
- **A1 documental:** matriz positiva (L01/L02/L08) y negativa (L04/L05/L06/L09) con precondicion, oraculo y rechazo falsable; son contratos, no resultados de runtime.
- **A2 inspeccion de seguridad:** JWT firmado, RBAC, tenant A/B, revocacion, session cache, PII y controles contra spoof/replay descritos en fail-closed; ninguna nueva autorizacion ejecutable.
- **A3 integracion:** `NO_APLICA` a ejecutar HTTP/.NET/Angular/Chromium/MySQL por diff Markdown sin cambio runtime; obligatorios en M03-M05 segun casos L01-L10. No se certifica que existan hoy recovery, passkeys/MFA, refresh rotativo, 429 real o revocacion criptografica central.
- **A4:** commit FF/CAS, checks Scope Lock/V5 pertinentes `completed/success` del SHA exacto, PLAN/COLA/BITACORA idempotente y rollback documental normal; no convertir checks ajenos en PASS.
- **A5:** `PENDIENTE` de Task Javier distinta del autor y de cualquier reparador, con revision genuina de positivo/negativo documental y de los limites expuestos; no QA/main/PROD.

**Referencias de contexto sin recertificar:** `docs/v5/c03-01-m01-01-objetivo-autenticacion-recuperacion.md`, `c03-01-m01-02-actores-entradas-salidas-autenticacion.md`, `c03-01-m01-03-inventario-contratos-codigo-evidencia.md`, `c03-01-m01-04-riesgos-supuestos-decisiones-autenticacion.md` y `c03-01-m01-05-aceptacion-autenticacion-recuperacion.md`. Sus listas de oraculos y blobs de antigua inspeccion no equivalen a pruebas presentes de la version actual.

**Resultado:** entrega M01.06 es el **contrato de bloqueo/rollback/liberacion**, no una certificacion de funcionalidad de login inteligente ya implementada. Riesgos tecnicos futuros permanecen abiertos y se resolveran por microtareas posteriores autorizadas.
