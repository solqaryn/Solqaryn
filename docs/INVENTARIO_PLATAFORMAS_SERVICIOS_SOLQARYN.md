# Inventario de plataformas y servicios externos — SOLQARYN

> Estado de referencia: 2026-09-30  
> Repositorio canónico: `solqaryn/Solqaryn`  
> Rama ordinaria: `dev`  
> Identidad corporativa operativa: `solqaryn.platform@outlook.com`

## 1. Propósito

Este documento concentra, en una sola superficie, las plataformas, sitios y servicios externos que SOLQARYN utiliza actualmente para desarrollo, operación, infraestructura, medios, identidad, automatización y administración.

No contiene contraseñas, tokens, API secrets, connection strings ni códigos de recuperación.

Reglas:

- Los accesos deben otorgarse mediante usuarios individuales, equipos, roles o invitaciones cuando el proveedor lo permita.
- No compartir contraseñas, refresh tokens, cookies, sesiones, API secrets ni llaves privadas entre miembros.
- Modelo vigente de acceso humano: Javier Mejía y Alex Morales son los dos miembros del workspace ChatGPT Business `SOLQARYN`. GitHub puede usar sus identidades personales como miembros de la organización; el resto de proveedores se opera por defecto mediante recursos/cuentas corporativas SOLQARYN y conectores autorizados, sin crear membresías personales adicionales salvo necesidad administrativa explícita.
- `main`, PROD, datos productivos, dominios, certificados, secretos e infraestructura productiva siguen requiriendo autorización explícita vigente del propietario para cambios materiales.
- Las cuentas o recursos legacy/personales retirados no forman parte de este inventario.

## 2. Resumen ejecutivo

| # | Plataforma / sitio | Estado | Uso en SOLQARYN | Dependencia del runtime |
|---|---|---|---|---|
| 1 | GitHub | ACTIVO / CRÍTICO | Código fuente, ramas, PR, Actions/CI, Environments, evidencia técnica | Sí, para desarrollo/release |
| 2 | Vercel | ACTIVO / CRÍTICO | Hosting/CDN del frontend Angular DEV y PROD | Sí |
| 3 | Render | ACTIVO / CRÍTICO | Hosting del backend ASP.NET Core DEV y PROD | Sí |
| 4 | Aiven | ACTIVO / CRÍTICO | MySQL administrado para DEV y PROD | Sí |
| 5 | Cloudinary | ACTIVO / CRÍTICO | Almacenamiento y entrega de imágenes/media | Sí |
| 6 | Cloudflare | ACTIVO | DNS y administración de la zona de `solqaryn.com` | Sí para DNS; cutover custom PROD aplazado |
| 7 | Microsoft Outlook.com | ACTIVO / CONFIGURADO | Identidad corporativa y transporte de correo SMTP OAuth2 | Configurado; transporte SMTP real está aplazado |
| 8 | Microsoft identity platform / Entra App Registration | ACTIVO | Registro OAuth2 y emisión/renovación de tokens usados por Outlook SMTP | Sí para OAuth2 de correo |
| 9 | Google Drive / Docs / Sheets | ACTIVO / OPERACIONES | Plan Maestro, notas y superficie administrativa de automatizaciones | No para el runtime de la app; sí para operación VAEP |
| 10 | ChatGPT / OpenAI | ACTIVO / OPERACIONES | Proyecto SOLQARYN, control VAEP, conectores y 10 Tasks/automatizaciones | No como dependencia del runtime actual de la app |
| 11 | TinyFish | ACTIVO / SOPORTE | Navegación/browser automation para tareas administrativas cuando un dashboard requiere interacción web | No |
| 12 | Clover | NO INTEGRADO | Proveedor evaluado/reservado para pagos futuros; actualmente sin API, secretos, webhooks ni flujo productivo | No |

## 3. Plataformas de infraestructura y código

### 3.1 GitHub

**Sitio:** GitHub.  
**Organización/repositorio canónico:** `solqaryn/Solqaryn`.

Uso actual:

- repositorio fuente único;
- rama ordinaria `dev`;
- rama productiva `main`;
- Pull Requests y revisión;
- GitHub Actions / CI;
- Environments `DEV` y `PROD`;
- evidencias, documentación y trazabilidad de cambios.

Recursos relevantes:

- repo: `solqaryn/Solqaryn`;
- environments: `DEV`, `PROD`.

**Acceso Alex Morales:** REQUERIDO.

Recomendación de acceso:

- miembro de la organización/equipo de SOLQARYN;
- acceso al repositorio;
- acceso a Actions y lectura de Environments;
- permisos administrativos sólo si sus funciones realmente lo requieren;
- no entregar secretos de Environment fuera de los mecanismos de GitHub.

---

### 3.2 Vercel

**Sitio:** Vercel.  
**Team corporativo:** SOLQARYN.

Uso actual:

- compilación y hosting del frontend Angular;
- CDN;
- rewrites `/api/*`;
- frontend DEV y PROD.

Proyectos canónicos:

- DEV: `solqaryn-dev`;
- PROD: `solqaryn-prod`.

URLs administradas relevantes:

- DEV: `https://solqaryn-dev.vercel.app`;
- PROD: `https://solqaryn-prod.vercel.app`.

**Acceso Alex Morales:** REQUERIDO.

Debe poder visualizar ambos proyectos, deployments, logs/configuración y settings necesarios para su función. Cambios materiales de PROD continúan sujetos a autorización del propietario.

---

### 3.3 Render

**Sitio:** Render.  
**Workspace operativo:** SOLQARYN.

Uso actual:

- hosting de la API ASP.NET Core;
- variables por entorno;
- health/readiness;
- logs y despliegues backend.

Servicios canónicos:

- DEV: `solqaryn-api-dev`;
- PROD: `solqaryn-api-prod`.

Ambos servicios se mantienen actualmente en Render Free conforme a la decisión vigente. No se usa keep-alive artificial.

**Acceso Alex Morales:** REQUERIDO.

Debe tener acceso nominal al workspace y a ambos servicios. Los secretos no deben copiarse ni compartirse fuera del gestor de variables.

---

### 3.4 Aiven

**Sitio:** Aiven Console.  
**Proyecto:** `solqaryn`.  
**Servicio:** `solqaryn-mysql`.

Uso actual:

- MySQL administrado;
- persistencia principal de SOLQARYN;
- separación lógica DEV/PROD.

Bases canónicas:

- `solqaryn_dev`;
- `solqaryn_prod`.

Los usuarios de aplicación son independientes por entorno.

**Acceso Alex Morales:** REQUERIDO.

El acceso al proyecto puede otorgarse sin entregar manualmente contraseñas de base de datos. Los privilegios DB administrativos deben limitarse a la función que realmente desempeñe.

---

### 3.5 Cloudinary

**Sitio:** Cloudinary Console.  
**Cloud corporativo vigente:** `riyrzmob`.

Uso actual:

- imágenes de productos;
- galerías y media;
- assets empresariales gestionados por la aplicación;
- delivery responsive/optimizado;
- separación por prefijo de entorno.

Prefijos canónicos:

- DEV: `solqaryn_dev`;
- PROD: `solqaryn_prod`.

**Acceso Alex Morales:** REQUERIDO.

Debe utilizar un usuario/equipo propio cuando Cloudinary lo permita. API Key/API Secret no deben copiarse a documentación ni chats.

---

### 3.6 Cloudflare

**Sitio:** Cloudflare Dashboard.  
**Zona:** `solqaryn.com`.

Uso actual:

- ownership/gestión DNS corporativa;
- delegación de `solqaryn.com`;
- futura terminación/cutover del dominio personalizado.

Estado actual:

- Cloudflare está activo y certificado como DNS corporativo;
- el cutover del dominio personalizado hacia PROD permanece deliberadamente aplazado;
- DEV y PROD continúan utilizando las URLs administradas actuales donde corresponda.

**Acceso Alex Morales:** REQUERIDO.

Preferir acceso de miembro limitado a la zona `solqaryn.com`, DNS y funciones estrictamente necesarias. No compartir credenciales del propietario.

## 4. Microsoft: qué usamos realmente

### 4.1 Outlook.com

**Servicio:** Microsoft Outlook.com.  
**Identidad corporativa:** `solqaryn.platform@outlook.com`.

Uso actual:

- identidad corporativa raíz operativa;
- buzón/remitente de SOLQARYN;
- SMTP configurado con OAuth2/Modern Auth;
- host configurado: `smtp-mail.outlook.com:587`;
- STARTTLS obligatorio.

Estado:

- OAuth2/token está configurado;
- la certificación de transporte SMTP real en Render permanece aplazada por la conectividad saliente disponible en el plan Render Free;
- no se usa contraseña SMTP básica;
- no se usa client secret OAuth2 en el runtime actual.

**Acceso Alex Morales:** REQUERIDO PARA ADMINISTRACIÓN CORPORATIVA, pero no mediante intercambio informal de contraseña o refresh tokens.

---

### 4.2 Microsoft identity platform / Microsoft Entra App Registration

SOLQARYN sí utiliza la plataforma de identidad de Microsoft para la aplicación OAuth2 asociada al correo Outlook.

Uso actual:

- Client ID de aplicación Microsoft;
- flujo OAuth2;
- endpoint Microsoft `/consumers/oauth2/v2.0/token`;
- obtención/renovación de access tokens para SMTP Outlook.

**Importante:** esto es el uso actual de “Azure/Microsoft” en SOLQARYN.

**Acceso Alex Morales:** REQUERIDO si tendrá responsabilidad sobre correo, app registration o recuperación OAuth.

### 4.3 Lo que NO estamos usando de Azure actualmente

No existe evidencia canónica de que el runtime actual de SOLQARYN dependa de:

- Azure App Service;
- Azure Database for MySQL;
- Azure SQL;
- Azure Storage;
- Azure Functions;
- Azure Kubernetes Service;
- Azure Key Vault como gestor actual de secretos.

La base de datos es Aiven, el backend es Render, el frontend es Vercel y los medios están en Cloudinary.

Por lo tanto, **Azure no es actualmente nuestro proveedor de hosting**. Microsoft/Entra se usa para identidad OAuth2 y Outlook.

### 4.4 Microsoft Graph

Microsoft Graph aparece como dirección futura posible dentro del roadmap de comunicaciones, pero **no forma parte del runtime vigente certificado**. El correo actual usa Outlook SMTP + OAuth2.

## 5. Google y documentación operativa

### 5.1 Google Drive / Google Docs / Google Sheets

**Cuenta corporativa Google:** asociada a `solqaryn.platform@outlook.com`.

Uso actual:

- almacenamiento documental corporativo;
- Plan Maestro único;
- notas de desarrollo;
- Sheet operativo de las diez automatizaciones;
- backups administrativos históricos no ejecutables.

Carpeta corporativa de trabajo:

`https://drive.google.com/drive/folders/15OxnbJj36wTmYP2UctBKf8z_ovNxf6e8`

Documentos/superficies principales:

- `PLAN MAESTRO SOLQARYN`;
- `Notas SOLQARYN_DEV.docx`;
- `SOLQARYN - PLAN MAESTRO DE AUTOMATIZACIONES`;
- `SOLQARYN - AUTORIDAD OPERATIVA VAEP`.

**Acceso Alex Morales:** REQUERIDO.

Debe compartir la carpeta/documentos con su identidad propia y asignarle el nivel de edición necesario. No utilizar sesiones compartidas.

### 5.2 Google Cloud Platform

No existe evidencia canónica actual de que SOLQARYN utilice GCP como hosting, base de datos, storage, functions o infraestructura del runtime.

Usamos Google Drive/Docs/Sheets, no GCP como proveedor de infraestructura de la aplicación.

## 6. IA, automatización y herramientas de operación

### 6.1 ChatGPT / OpenAI

Uso actual:

- proyecto de trabajo SOLQARYN;
- controlador VAEP;
- conectores a proveedores;
- automatizaciones/Tasks programadas;
- revisión, desarrollo, QA, evidencias y coordinación operativa.

Las diez automatizaciones canónicas permanecen actualmente PAUSADAS hasta autorización explícita. Reparto vigente: Javier Mejía = cinco Primary `:00/:12/:24/:36/:48`; Alex Morales = cinco Supervisor `:05/:17/:29/:41/:53`.

**Acceso Alex Morales:** REQUERIDO al workspace ChatGPT Business `SOLQARYN` con su identidad personal. Las conexiones/plugins que consumen datos de SOLQARYN deben autenticarse contra recursos corporativos SOLQARYN; no contra cuentas personales de Javier o Alex.

### 6.2 TinyFish

Uso actual:

- browser automation de apoyo;
- navegación de dashboards o flujos web que requieren interacción visual/autenticada;
- apoyo en operaciones administrativas cuando un conector directo no resuelve el flujo completo.

**No es dependencia del frontend/backend de SOLQARYN.**

**Acceso Alex Morales:** REQUERIDO si debe ejecutar los mismos flujos administrativos asistidos por navegador. Debe utilizar su acceso autorizado y no cookies/sesiones compartidas.

## 7. Pagos

### 7.1 Clover

Estado canónico:

`NOT_INTEGRATED_CERTIFIED`

Actualmente:

- no hay API Clover en el código;
- no hay cliente Clover;
- no hay webhook Clover;
- no hay secretos Clover requeridos por Render;
- no hay migración Clover;
- no hay flujo productivo que dependa de Clover.

Clover se conserva únicamente como proveedor potencial/futuro dentro del ecosistema de decisiones de pago.

**Acceso Alex Morales:** NO ES NECESARIO PARA OPERAR EL RUNTIME ACTUAL. Si el propietario desea que Alex tenga paridad administrativa completa del ecosistema, se le puede otorgar acceso nominal, pero no debe tratarse como una dependencia activa.

## 8. Tecnologías importantes que NO son cuentas/plataformas externas separadas

Estas tecnologías forman parte del stack, pero no requieren una plataforma corporativa adicional equivalente a Vercel/Render/Aiven:

- Angular 20;
- Angular Material;
- ASP.NET Core 8;
- .NET 8;
- MySQL;
- EF Core 8 / Pomelo;
- JWT / BCrypt;
- QuestPDF;
- Playwright;
- Chromium;
- Brotli/Gzip;
- Git.

Su operación está contenida en el repositorio, CI o infraestructura ya enumerada.

## 9. Matriz de acceso para Alex Morales

| Plataforma | Modelo de acceso Alex | Motivo |
|---|---|---|
| GitHub | MIEMBRO PERSONAL | Código, PR, CI y documentación dentro de la organización SOLQARYN |
| ChatGPT Business | MIEMBRO PERSONAL | Workspace SOLQARYN y cinco Supervisor VAEP |
| Google Drive / Docs / Sheets | CONECTOR A CUENTA CORPORATIVA | Plan Maestro, Sheet y notas bajo `solqaryn.platform@outlook.com` |
| Vercel | CORPORATIVO / SIN MIEMBRO PERSONAL POR DEFECTO | Operación mediante recursos/conectores SOLQARYN; membresía directa sólo si se necesita administración manual |
| Render | CORPORATIVO / SIN MIEMBRO PERSONAL POR DEFECTO | Backend y configuración mediante recursos/conectores SOLQARYN |
| Aiven | CORPORATIVO / SIN MIEMBRO PERSONAL POR DEFECTO | MySQL SOLQARYN; acceso individual sólo si se autoriza administración directa |
| Cloudinary | CORPORATIVO / SIN MIEMBRO PERSONAL POR DEFECTO | Media administrada desde la cuenta SOLQARYN |
| Cloudflare | CORPORATIVO / SIN MIEMBRO PERSONAL POR DEFECTO | DNS/zona SOLQARYN |
| Outlook.com / Microsoft | CORPORATIVO | Identidad `solqaryn.platform@outlook.com`; no crear otra identidad para Alex |
| Microsoft Entra / App Registration | CORPORATIVO | App OAuth2 de SOLQARYN |
| TinyFish | SOPORTE EN CHATGPT | Navegación asistida con sesiones corporativas autorizadas |
| Clover | NO NECESARIO ACTUALMENTE | No integrado al runtime vigente |

## 10. Checklist seguro para provisionar a Alex Morales

1. Mantener sólo las identidades humanas necesarias: Javier y Alex en ChatGPT Business; ambos pueden ser miembros personales de GitHub. No crear membresías personales adicionales en proveedores externos salvo necesidad administrativa explícita.
2. No reutilizar usuarios personales legacy.
3. No enviar por chat contraseñas, tokens, API secrets, cookies, connection strings ni códigos MFA.
4. Aplicar mínimo privilegio inicialmente y ampliar sólo cuando su función lo requiera.
5. Separar acceso DEV y PROD donde el proveedor lo permita.
6. Verificar que Alex pueda entrar y leer los recursos correctos antes de otorgar permisos destructivos.
7. Mantener cambios de PROD sujetos a la autorización operativa vigente de SOLQARYN.
8. Documentar únicamente nombres de recursos y roles; nunca secretos.
9. Revocar cualquier acceso temporal cuando deje de ser necesario.
10. Si una plataforma no soporta colaboración nominal adecuada, definir primero un mecanismo corporativo antes de compartir credenciales raíz.

## 11. Clasificación final

### Runtime crítico

- GitHub;
- Vercel;
- Render;
- Aiven;
- Cloudinary;
- Cloudflare/DNS.

### Identidad y comunicaciones

- Outlook.com;
- Microsoft identity platform / Entra App Registration.

### Gobierno, documentación y automatización

- Google Drive / Docs / Sheets;
- ChatGPT;
- TinyFish.

### Futuro / no integrado

- Clover.

## 12. Fuentes canónicas revisadas

Este inventario se construyó a partir del estado vigente de:

- `PROJECT_CONTEXT.md`;
- `ARCHITECTURE.md`;
- `docs/PROJECT_SCOPE_LOCK.md`;
- `AGENTS.md`;
- `docs/RENDER_ENVIRONMENT_CONTRACT.md`;
- `docs/DETALLES_PENDIENTES.md`;
- `docs/FASE7_CERTIFICACION_CORREO.md`;
- `docs/evidencias/PROD_CIERRE_FINAL_2026-09-28.md`;
- `docs/evidencias/CLOVER_PROD_CERTIFICACION_2026-09-27.md`;
- estado operativo actual de VAEP/Google Drive conectado a SOLQARYN.

Si una plataforma cambia de estado, este archivo debe actualizarse sin convertir evidencia histórica en autoridad operativa.
