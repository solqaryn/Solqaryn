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
