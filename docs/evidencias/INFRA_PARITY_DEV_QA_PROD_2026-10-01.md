# Certificación de paridad de infraestructura DEV / QA / PROD — 2026-10-01

Estado: **EN RECUPERACIÓN CONTROLADA**. Este documento registra únicamente evidencia viva obtenida durante la reconciliación actual. No contiene valores secretos.

## Contrato objetivo

La paridad exigida es estructural y fail-closed:

- GitHub Environment: 4 variables MySQL + 3 secretos operativos por entorno.
- Aiven: mismo servicio corporativo y endpoint; base/usuario exclusivos por entorno; TLS requerido; sin grants cruzados ni privilegios administrativos.
- Render: mismo runtime Docker, Dockerfile, health contract y exactamente 28 claves administradas; sólo cambian identidad de entorno, URLs, prefijos y secretos propios.
- Cloudinary: cloud corporativo común, credenciales segregadas y prefijo `solqaryn_dev`, `solqaryn_qa` o `solqaryn_prod`.
- Vercel: un proyecto corporativo por entorno, misma configuración de build/routing y Production Branch `dev`, `qa` o `main`.
- Promoción Git: `dev -> qa -> main`.

## DEV — evidencia viva

### GitHub + Aiven

Run de paridad: `36954462879` — **SUCCESS**.

Marcadores obtenidos sin revelar secretos:

```text
DEV_GITHUB_REQUIRED_STRUCTURE=PASS vars=4 secrets=3
DEV_AIVEN_ENDPOINT_CONTRACT=PASS
DEV_AIVEN_DB_BINDING=PASS
DEV_AIVEN_LEAST_PRIVILEGE=PASS
DEV_AIVEN_CROSS_ACCESS=DENY_PASS
DEV_AIVEN_CONTROL_PLANE=PASS
DEV_BACKUP_PASSPHRASE=PASS
```

La API de GitHub denegó al `GITHUB_TOKEN` el inventario administrativo de nombres (`403`), por lo que el gate certifica presencia/consumo de las 7 entradas requeridas, pero la exclusión de entradas adicionales requiere readback administrativo del Environment.

### Render

El deploy de certificación `dep-davh89id0e5s73800o9g` falló cerrado antes de reemplazar la instancia sana.

El runtime detectó **36** claves administradas frente a las **28** canónicas. No faltan claves; existen ocho residuos explícitamente retirados por el contrato:

```text
AppSettings__LogoPublicUrl
Smtp__CorreoRemitente
Smtp__CorreoRespuesta
Smtp__OAuth2Scope
Smtp__OAuth2TokenEndpoint
Smtp__PasswordSmtp
Smtp__RequiereAutenticacion
Smtp__UsarSsl
```

No se leyeron ni persistieron sus valores. La documentación canónica ya define esas ocho entradas como redundantes/legacy y prohíbe `Smtp__PasswordSmtp` en el runtime OAuth2.

La instancia previa permaneció disponible y `/health/ready` continuó respondiendo `database=connected`.

**Estado DEV Render:** bloqueado de certificación hasta eliminar únicamente esos ocho nombres en el dashboard de Render y repetir el deploy.

## QA — evidencia viva

### GitHub + Aiven

Run de paridad: `36954576441` — **SUCCESS**.

```text
QA_GITHUB_REQUIRED_STRUCTURE=PASS vars=4 secrets=3
QA_AIVEN_ENDPOINT_CONTRACT=PASS
QA_AIVEN_DB_BINDING=PASS
QA_AIVEN_LEAST_PRIVILEGE=PASS
QA_AIVEN_CROSS_ACCESS=DENY_PASS
QA_AIVEN_CONTROL_PLANE=PASS
QA_BACKUP_PASSPHRASE=PASS
```

El inventario administrativo de nombres también requiere readback de UI por la restricción `403` del token de Actions.

### Render

Deploy `dep-davh760u01pc73eomtg0` sobre `qa@89079c0b8959227673a7167f573f35b8d66a6aae`: **LIVE**.

```text
RENDER_ENV_CONTRACT=PASS
environment=Staging
managed_keys=28
keyset_fingerprint=F6E354A5B1015490
shared_fingerprint=4F62914442078B8C
```

Readiness posterior: `database=connected`.

### Cloudinary

Certificación runtime previa de la misma sesión: **PASS** en cloud `riyrzmob`, prefijo `solqaryn_qa`, autenticación real, upload bajo namespace QA y cleanup inmediato. Evidencia detallada en `docs/evidencias/CLOUDINARY_QA_CERTIFICACION_2026-10-01.md`.

### Vercel

El deployment Git más reciente de QA está READY, pero no está promovido como target `production`. El alias canónico `solqaryn-qa.vercel.app` continúa apuntando a un deployment anterior y `/api/health/ready` devuelve 503, mientras el deployment QA nuevo responde correctamente.

**Estado QA:** backend/Aiven/Cloudinary certificados; Vercel canonical alias/Production Branch pendiente de reconciliación.

## PROD — evidencia viva

Cloudinary PROD ya dispone de certificación runtime persistida en `docs/evidencias/CLOUDINARY_PROD_CERTIFICACION_2026-09-27.md`: `CLOUDINARY_PROD_CERT=PASS`, cloud `riyrzmob`, prefijo `solqaryn_prod` y autenticación API real sin exposición de credenciales. La migración productiva certificada conserva además 351 URLs/public IDs esperados en Cloudinary.

Render PROD continúa `live` sobre `main@d6d967f42bad99613bfb4c19621800877c63a71d`. Tras despertar el servicio Free:

```text
/health       -> status=ok
/health/ready -> database=connected
```

La certificación de paridad estricta no se declara todavía para PROD: el contrato nuevo se promociona únicamente después de cerrar QA conforme al flujo `dev -> qa -> main`. Deben certificarse en PROD los mismos gates GitHub/Aiven, las 28 claves Render y el probe Cloudinary PROD antes del cierre.

## Bloqueos externos actuales

1. **Render DEV:** eliminar ocho variables legacy detectadas arriba; el API conectado permite merge/replace pero no eliminación individual segura sin releer secretos, por lo que no se usa `replace=true`.
2. **Vercel QA:** fijar Production Branch a `qa`, promover el deployment correcto y comprobar paridad de Root Directory/Framework/Build/Output/Install con DEV y PROD.
3. **GitHub Environments:** readback administrativo de nombres/conteos para demostrar que no existen extras además de las 4 variables + 3 secretos requeridos.
4. Tras cerrar 1–3: promover infraestructura QA -> main y ejecutar en PROD los gates exact-head de GitHub/Aiven/Render/Vercel. Cloudinary PROD ya cuenta con certificación runtime propia; sólo se repetirá un write/delete si el HEAD promovido cambia el adaptador o las credenciales productivas.

No se considera `LISTO` hasta cerrar estos puntos con evidencia viva.
