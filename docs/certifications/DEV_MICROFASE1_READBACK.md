# SOLQARYN — Microfase 1 DEV: certificado compuesto y readback vigente

## Estado y limite del certificado

La identificacion publica multiempresa en **DEV** tiene evidencia criptografica
HMAC real y evidencias de suspension/reactivacion A/B. No autoriza checkout
multitienda, ampliar rutas comerciales, ni promocionar QA/PROD.

El certificado vivo utiliza dos fuentes diferentes, sin confundirlas:

1. **Ejecutado en cada readback:** version 8.4.8 y estructura MySQL DEV,
   unicidad de host+tienda, exactamente dos empresas activas con GET de
   binding positivo y EmpresaId correcto, tienda legacy HTTP 200, clave
   desconocida HTTP 503, checkout no certificado HTTP 503, peticion directa
   sin prueba HTTP 503 y readiness del backend.
2. **Atestiguado historicamente por fuentes revisadas:** intento 5, empresa
   suspendida rechazada por HTTP 503; intento 6, las dos empresas nuevamente
   activas; y tres ensayos dentro de Render DEV del mismo proof HMAC
   `200 → 503 → 200` con ledger MySQL y tenant correcto.

## Fuentes y gobernanza

- Certificado versionado: `docs/certifications/dev-public-store-microfase1.json`.
- Revisor automatico fail-closed:
  `scripts/certification/verify_dev_public_store_evidence.py`.
- Tests de alteracion, origen, guardas y falsa prueba:
  `scripts/certification/test_verify_dev_public_store_evidence.py`.
- Acta original, creada por miembro corporativo y no editada:
  [PR #3666, comentario 6251764256](https://github.com/solqaryn/Solqaryn/pull/3666#issuecomment-6251764256).
- Readback historico:
  [GitHub Actions run 39084418982](https://github.com/solqaryn/Solqaryn/actions/runs/39084418982),
  intentos 5/6, jobs 117567817345 y 117568173016.
- Render DEV: SHA `49195f34d6c00e0c0b7684465619af0a96afeba5`,
  tres log IDs fijados en el certificado, HMAC LIVE PASS (primera 200,
  replay 503, nueva 200, ledger PASS), servicio de Render expresamente DEV.
- Apagado del ejecutor efimero: deploy
  `dep-dbaeb65g1s2s73bpkbdg`, LIVE.

Los intentos antiguos conservaran su conclusion historica BLOCKED, que no se
reescribe ni se elimina por motivos de auditoria. El workflow vigente **no**
presenta esas comprobaciones como ejecutadas en el nuevo run: las identifica
como `PASS_ATTESTED`. Eso no implica una deuda funcional.

## Mecanismo vigente

El workflow `.github/workflows/dev-public-store-live-readback.yml` se
ejecuta manualmente sobre `dev` y, para validar cambios al propio
certificador/evidencia, mediante un evento push limitado a esos archivos.
No se engancha a cada cambio backend: evitar bloqueo de despliegue de
migraciones por `Render checksPass`.

En los PR que modifican el certificador, el workflow ejecuta ahora la comprobacion de procedencia GitHub **online** (sin secretos de base de datos ni conexiones Aiven), además de los tests fail-closed. Una falla se localiza por etapa, sin imprimir tokens, firma HMAC ni errores de CLI sin filtrar.

Al ejecutarse sobre `dev`, primero verifica:

- repositorio, rama y Aiven DEV aprobados; secretos fuera de logs;
- origen corporativo en GitHub del acta de evidencia (ID numerico del autor, no la etiqueta contextual de afiliacion), comentario no
  modificado, PR fusionado en `dev`, SHA/IDs/logs HMAC;
- que el commit atestiguado es ancestro del HEAD y que los archivos
  de firma/proxy/middleware/ledger-replay/binding son identicos byte a byte;
- tests unitarios de condiciones que deben fallar cerradas.

Despues lee **en vivo** el esquema/indice MySQL y rutas HTTP. No crea,
suspende ni reactiva empresas; no reutiliza secretos; no genera firmas.
Si las guardas no han cambiado y todo el readback en vivo es correcto:
`MICROFASE1_LIVE_CERT=PASS_ATTESTED` y conclusion del workflow = success.
Si falla una comprobacion actual, el origen del acta, o cambian los archivos
criptograficos, devuelve FAIL o BLOCKED, nunca marca PASS ficticio.

## Limpieza del ensayo temporal

El worker `DevPublicStoreReplayCertificationHostedService`, su registro en
Program, sus tests de arranque, su configuracion por defecto y el runbook de
activacion han sido retirados del repositorio. El flag externo en Render quedo
**false** y no tiene consumidor de codigo desde este cambio. No debe
reactivarse ni crear una API de firmas. Se conserva la prueba auditada; no
se conserva infraestructura de ensayo innecesaria.

## Repeticion autorizada

Si cambia el guard HMAC, proxy signer o resolver de binding, el readback
se detiene por discrepancia de codigo: se requiere una **nueva** prueba
autorizada y una nueva atestacion versionada/revisada. No puede reciclar
un certificado historico para un mecanismo nuevo. No reabrir ni falsificar
el run historico y no suspender empresas por rutina.
