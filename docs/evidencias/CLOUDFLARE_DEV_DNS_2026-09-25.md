# Auditoría Cloudflare DEV / DNS — 2026-09-25

Estado: **CERRADO / N/A COMO DEPENDENCIA CRÍTICA DEV**

## Vercel DEV

Proyecto: `solqaryn-dev`  
Project ID: `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`

Dominios reportados por Vercel:

- `solqaryn-dev.vercel.app`
- `solqaryn-dev-solqaryn.vercel.app`
- `solqaryn-dev-git-dev-solqaryn.vercel.app`

Todos son dominios `vercel.app` administrados por Vercel. No se observa un dominio custom para DEV.

## Render DEV

Servicio: `solqaryn-api-dev`  
Service ID: `srv-daqvla49v7es738up9vg`  
URL canónica: `https://solqaryn-api-dev-fxx8.onrender.com`

Es un hostname `onrender.com` administrado por Render.

## Repositorio

Búsqueda actual:

- configuración/uso Cloudflare: no encontrado;
- `solqaryn.com`: no encontrado;
- ningún hostname custom Cloudflare participa en CORS, BackendPublicUrl o proxy DEV canónico.

Sí existen referencias históricas a hostnames Vercel/Render legacy en documentación antigua, pero no forman parte del runtime DEV canónico certificado.

## Decisión

La cadena DEV actual es:

`solqaryn-dev.vercel.app -> Vercel -> /api proxy -> solqaryn-api-dev-fxx8.onrender.com -> Render`

Cloudflare no está en esa cadena.

Por tanto:

- no hay zona/DNS Cloudflare que migrar para mantener DEV operativo;
- no hay cambio DNS pendiente para cerrar DEV;
- no se modifica Cloudflare durante este cierre;
- la propiedad de la cuenta/zona Cloudflare queda como control separado para futuros dominios propios y/o PROD.

Resultado final: **Cloudflare DEV/DNS = N/A runtime / CERRADO**.
