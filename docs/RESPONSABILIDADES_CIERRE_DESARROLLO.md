# Responsabilidades vigentes para DEV y promoción a PROD

## Identidad

- Plataforma: SOLQARYN.
- Repositorio: `solqaryn/Solqaryn`.
- Rama ordinaria: `dev`.
- Servicio Render DEV: `solqaryn-api-dev`.
- Servicio Render PROD: `solqaryn-api-prod`.
- Base DEV: `solqaryn_dev`.
- Base PROD: `solqaryn_prod`.
- Cuenta corporativa operativa: `solqaryn.platform@outlook.com`.

## Regla de trabajo

Las correcciones, experimentos y certificaciones se realizan primero en DEV. `main`, PROD y datos productivos no se modifican sin autorización explícita vigente del propietario.

## Variables y secretos

Render DEV y PROD deben respetar el mismo contrato de nombres. Los valores dependientes del entorno permanecen separados y los secretos no se copian entre entornos.

La fuente única del inventario y justificación de variables es:

`docs/RENDER_ENVIRONMENT_CONTRACT.md`

## Correo

DEV y PROD usan Outlook.com + OAuth2/Modern Auth. Cada entorno mantiene su propio refresh token. No se usa autenticación SMTP por contraseña ni client secret OAuth2.

DEV se considera certificado para correo solamente después de:

1. despliegue correcto;
2. health correcto;
3. diagnóstico `SMTP_OK`;
4. envío real controlado de factura;
5. recepción y PDF verificados.

## Promoción

Una vez DEV esté certificado y no existan defectos P0/P1, el propietario puede autorizar la promoción a `main`. Después se repiten en PROD las comprobaciones correspondientes sin convertir PROD en laboratorio.
