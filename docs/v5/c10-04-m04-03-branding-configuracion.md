# SOLQARYN V17 — C10-04.M04.03: branding y configuracion reactiva segura
**Autor:** PRIMARY DEV 02 JAVIER `6af16bb9492c8191b8e8c7d606f11507`. **Riesgo:** R2 por pertenecer a una ruta privada de continuidad. **Ambito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT, PLAN fila 1570, candado `2000001570`.

## A0 — cambio concreto
Amplia exclusivamente la vista privada `continuidad-operativa-v5.component.ts` con un token Angular de configuracion PRESENTACIONAL reactiva `CONTINUIDAD_PRESENTACION_V5`. Admite dos marcas tipadas `solqaryn` / `institucional` y dos densidades `normal` / `compacta`. Un `Signal<unknown>` puede cambiar ambos valores sin reconstruir la ruta. Todo objeto extra, tipo invalido o texto arbitrario se rechaza y vuelve al estilo seguro `SOLQARYN/normal`. No se aceptan CSS, HTML, enlaces, valores de roles o identificadores de tenant desde el token. No se agregan dependencias ni componentes de terceros: Angular standalone y CSS encapsulado existente.

La configuracion no tiene autoridad para alterar sesion, guard, JWT, RBAC, tenants, jobs, R2, restauracion ni proveedores; los controles documentales siguen sujetos a la sesion de plataforma vigente y se ocultan cuando esta se revoca. Tampoco hay persistencia de la preferencia o valor remoto no verificado.

## A1 / A2 — pruebas reales requeridas
Vitest en `continuidad-presentacion-v5.spec.ts`: seleccion y reactividad de variantes permitidas, negativa de HTML/CSS no confiable, fronteras de objetos extra/nulos/arrays/enums, y ausencia de control protegido sin sesion. Se exige conservar la suite anterior de estados, la ruta con guard y la regresion Chromium de anonimo/403/503. No atribuir PASS hasta que GitHub Actions termine SUCCESS en SHA exacto. El backend sigue siendo la autoridad de permisos.

## A3 — alcance de integracion
Se requiere `npm ci`, Vitest, lint+build Angular 22.2.1 y Playwright/Chromium localhost con API SINTETICA del escenario anterior, para verificar el render DOM real. JWT HTTP autentico, MySQL 8.4, EF, proveedores cloud, transacciones y permisos live NO_APLICA: el diff no los modifica ni los prueba.

## A4 / A5 — cierre y reversibilidad
Entregar un commit fast-forward CAS con este documento, codigo, pruebas y CHANGELOG autocontenido; validar `verify-solqaryn-project-scope`, procedencia V5 y check dirigido `C10-04.M04.03 - Vitest, Angular y Chromium` en SHA exacto antes de LISTO. Registrar PLAN/COLA/DAG/BITACORA con readback y liberar candado V17 solo tras transicion segura. Si CI queda queued/in_progress, conservar VALIDANDO/owner y documentar run/check. A5 por otra Task Javier distinta de la autora, nunca autocertificacion. Rollback: `git revert` normal; no tocar QA/main/PROD.
