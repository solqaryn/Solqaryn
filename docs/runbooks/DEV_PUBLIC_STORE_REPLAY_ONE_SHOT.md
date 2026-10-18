# DEV — Certificacion HMAC replay en Render (ejecucion unica)

Alcance: repositorio solqaryn/Solqaryn, rama de destino dev; Render
solqaryn-api-dev (srv-daqvla49v7es738up9vg), Vercel solqaryn-dev.
NO aplica a QA, PROD, main ni solqaryn.com.

## Estado inicial y autoridad

- La suspension/reactivacion de EMPRESA DE PRUEBA B ya se comprobo
  externamente. NO volver a suspenderla solo para este certificado.
- MySQL DEV y los dos bindings activos deben existir antes del ensayo.
- La prueba NO crea empresas, tiendas, usuarios, migraciones ni endpoints.
- Registra exclusivamente dos firmas aleatorias efimeras en el ledger
  PublicStoreProofReplays de Aiven DEV por la ruta ya existente.
- El codigo es candidato hasta CI, revision y despliegue exact-head.

## Activacion (requiere revision y autorizacion operacional)

1. Aprobar CI y merge del PR temporal con base exclusiva dev. Comprobar
   que Render DEV haya publicado el mismo SHA; ninguna promocion QA/PROD.
2. Confirmar en DEV los gates ya certificados:
   PublicStore__EnableMultiEmpresaBinding=true,
   PublicStore__EnableDatabaseBindings=true,
   PublicStore__RequireVerifiedProxyProof=true,
   y que exista PublicStore__ProxySigningKey sin revelar su valor.
3. Solo en el servicio Render DEV, definir
   PublicStore__ReplayLiveCertificationOnStartup=true.
   Hacer el deploy sin mostrar ni copiar la clave, y observar los logs
   de la instancia DEV publicados por Render.
4. El worker valida Environment=Development, RENDER=true,
   Aiven DEV host/puerto/nombre de DB/usuario/TLS, asociacion activa
   solqaryn-dev.vercel.app + tienda-a y empresa activa. Usa la
   clave HMAC existente en memoria, sin exponerla.
5. Tras /health/ready, envia tres solicitudes GET a la instancia
   local de Kestrel: primera firma valida, reenvio de la MISMA firma
   y tercera firma con nonce nuevo. Comprueba respectivamente
   200 / 503 TENANT_DATA_ISOLATION_REQUIRED / 200, el EmpresaId
   correcto y presencia exacta (una fila por firma) en el ledger MySQL.
6. Solo un PASS autenticado en los logs de Render con el SHA correcto
   es evidencia LIVE:
   PUBLIC_STORE_HMAC_REPLAY_LIVE=PASS env=DEV first=200 replay=503
   fresh=200 ledger=PASS company_binding=PASS.
   BLOCKED, errores, ausencia de logs, SHA incorrecto o timeout
   NUNCA son PASS. Correlacionar con el deploy de Render.
7. Inmediatamente restaurar
   PublicStore__ReplayLiveCertificationOnStartup=false y desplegar
   Render DEV. Verificar A=200, B=200, legacy=200; host desconocido
   y endpoint privado siguen rechazados. No cambiar los otros gates.
8. Registrar evidencia auditada y retirar el worker temporal en
   un cambio posterior revisado. Conservar los logs/redacciones y
   ninguna firma completa. No copiar el secreto a GitHub.

## Seguridad, diagnostico y rollback

- El worker se registra solo con flag true + Render + Development;
  revalida todos los limites al ejecutar y nunca expone endpoints.
- El trafico de certificacion usa loopback y metodo GET; no usa
  tokens empresariales, sesiones comerciales ni reconfigura RBAC.
- Respuestas o errores no se imprimen; los logs solo contienen
  estado global, reason type y SHA validado si lo proporciona Render.
- Si la prueba no alcanza PASS, apagar el flag; los gates multitienda
  existentes permanecen como estaban y el servicio legacy no se altera.
- El workflow de readback existente NO marca automaticamente replay
  PASS: su evidencia y este log autenticado deben correlacionarse
  antes de certificar el alcance. No alterar criterios para pintar
  verde un certificado incompleto.
- No ejecutar en QA/PROD aunque se agregase el flag por error:
  Environment, Render y Aiven DEV deben pasar comprobacion exacta.

## Evidencia y diagnóstico 2026-10-18 (antes del ajuste de conexión)

- Merge PR #3664 a dev: db9b4f0702df176f7c9e44470907a63ba1435881. CI del PR: 17/17 PASS; Render y Vercel DEV publicaron el SHA.
- Deploy Render DEV dep-dba50lgm7kps73fgi0vg (flag true): GET loopback /tienda/binding primera respuesta HTTP 200 a las 04:55:22 UTC; segundo uso del mismo proof HTTP 503 a las 04:55:23 UTC. MySQL documentó rechazo por digest duplicado; el worker avanzó a un tercer GET, pero no apareció respuesta final ni marca LIVE=PASS. Las dos lecturas del digest previas al tercer GET se habian ejecutado bajo condiciones estrictas del worker.
- Deploy de apagado dep-dba53odg1s2s73aqfvc0 quedó LIVE a las 04:59:49 UTC. Segundo y único reintento antes del cambio: primera respuesta 200 a las 05:03:17 UTC, segundo GET iniciado sin respuesta final. **No hubo marca LIVE=PASS**. El flag se volvió a apagar; deploy dep-dba56gvbtjjs73dr5tog LIVE a las 05:05:39 UTC.
- La falla de finalización no debe atribuirse sin evidencia a MySQL, Kestrel o conectividad. NO se certifica el escenario completo 200/503/200 con estos logs.
- Ajuste del worker candidato: encabezado Connection: close para cada GET loopback, timeout acotado en readback MySQL y trazas de fase FIRST_PASS, REPLAY_PASS, FRESH_PASS, sin secreto/nonce/firma en logs. Mantener LIVE=PASS condicionado a los tres HTTP y ambos digests persistidos correctamente.
- Un nuevo ensayo se ejecutará solo tras CI exact-head y fusión/revisión formal en dev, sin alterar contratos, host público, QA/PROD ni empresas. Desactivar siempre el flag al terminar, incluso si aparece BLOCKED.

## 2026-10-18 — Revalidación posterior al PR #3665

- El PR #3665 se fusionó en DEV bajo `f6748b7d6be4bca478d50b1f3955fe529c383713`; su tree exacto aprobó 14/14 CI y el commit merge aprobó 20/20 workflows posteriores.
- Render, deploy `dep-dba5hq7btjjs73dsgfgg`: `FIRST_PASS=200 ledger=PASS` y `REPLAY_PASS=503 ledger=PASS` (05:31:56–57 UTC). El tercer GET con firma nueva llegó a insertar digest en MySQL, pero el servidor no registró fin de la solicitud ni `FRESH_PASS`. Una repetición posterior inició una firma nueva y tampoco entregó resultado completo; por tanto `HMAC_REPLAY_LIVE=PASS` **sigue pendiente**.
- Nuevo control causal candidato: esperar 45 s después de `/health/ready` antes del ensayo para evitar la ventana de handoff de Render; emplear un cliente HTTP aislado por petición, cierre explícito, timeout ligado al token, y hasta 3 intentos *únicamente* de transporte para la firma nueva, cada intento con nonce distinto.
- Nunca se reintenta silenciosamente una respuesta 503, identidad equivocada, ledger incorrecto ni fallo de seguridad; cualquier excepción definitiva es `BLOCKED`. Ningún secreto/firma se registra. Solo un `FRESH_PASS` tras una respuesta 200 y digest único permite `LIVE=PASS`.
- El flag `PublicStore__ReplayLiveCertificationOnStartup=false` ya se desplegó sobre Render DEV y el smoke A=200, B=200, legacy=200 y host desconocido=503 se confirmó a las 06:25–26 UTC.
- Este PR de robustecimiento NO certifica por sí mismo: requiere REVIEW_FIRST, CI exact-head, merge solo a dev, prueba real en Render, desactivar flag de nuevo y smoke post-rollback.
