# Certificación Cloudinary QA — 2026-10-01

Estado: **PASS / CERRADO — CREDENCIALES, RUNTIME, PREFIJO Y CLEANUP**

## Runtime certificado

- Servicio Render: `solqaryn-api-qa`
- Rama: `qa`
- Commit de certificación: `4d01204d0303875437edb97a7500ef3312147e77`
- Deploy: `dep-davgkfpsrm7s73bu69r0`
- Estado: `LIVE`
- Entorno ASP.NET: `Staging`

## Contrato Cloudinary QA

- Cloud: `riyrzmob`
- API key dedicada: `solqaryn_qa`
- API key/secret: configurados exclusivamente como secretos externos de Render; sus valores no se registran.
- Prefijo: `solqaryn_qa`

## Certificación runtime

El probe temporal QA-only ejecutado desde el propio runtime validó:

1. autenticación real contra la API de Cloudinary mediante `/ping`;
2. upload de un PNG mínimo únicamente bajo `solqaryn_qa/certification/runtime/`;
3. PublicId bajo el prefijo QA esperado;
4. eliminación inmediata del activo temporal;
5. ausencia de uso de prefijos DEV o PROD.

Evidencia de log:

```text
CLOUDINARY_QA_CERT=PASS cloud=riyrzmob prefix=solqaryn_qa api_authenticated=true upload_prefix=true cleanup=true
```

El PublicId temporal fue `solqaryn_qa/certification/runtime/runtime-probe-4eb5103f116343cda1f04e58d4bb8074` y el mismo probe confirmó `cleanup=true`.

## Retiro del probe

Tras capturar evidencia causal, el código temporal de certificación fue retirado inmediatamente de `dev`. El runtime normal no conserva hooks de diagnóstico ni credenciales en código.

## Alcance

- DEV no fue modificado en infraestructura externa.
- `main` y PROD no fueron modificados.
- No se tocaron activos DEV/PROD.
- No se expusieron API keys, API secrets ni otros secretos.
