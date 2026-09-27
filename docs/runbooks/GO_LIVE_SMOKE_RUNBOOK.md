# GO LIVE SMOKE — SOLQARYN


> `PROJECT_SCOPE_LOCK=STRICT`
> Alcance operativo: SOLQARYN / `solqaryn/Solqaryn` / `dev`.
## DEV

Destinos canónicos:

- Frontend: `https://solqaryn-dev.vercel.app`
- Backend: `https://solqaryn-api-dev-fxx8.onrender.com`
- Liveness: `/health`
- Readiness: `/health/ready`
- Rama: `dev`

Smoke mínimo:

1. frontend raíz/login;
2. API health/readiness;
3. login autorizado;
4. dashboard empresa;
5. consulta de productos/inventario;
6. factura de prueba;
7. diagnóstico SMTP;
8. envío controlado;
9. carga/lectura Cloudinary;
10. revisión de logs sin errores P0/P1.

## PROD

Se ejecuta el mismo smoke únicamente cuando exista `solqaryn-prod` y después de promover código certificado. No se usan proyectos o dominios legacy.

VariStoreHN puede validarse como primer tenant mediante sus rutas/identidad configuradas dentro de SOLQARYN, no mediante infraestructura antigua.
