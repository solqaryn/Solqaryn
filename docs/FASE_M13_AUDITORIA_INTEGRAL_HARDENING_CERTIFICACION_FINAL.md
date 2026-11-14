# FASE M13 — Auditoría integral, hardening y certificación final

Fecha de certificación: **2026-08-10**  
Rama funcional certificada: **`Desarrollo`**  
HEAD funcional certificado: **`19539c72d3a617d95bb3c03dfbde5f6b212ca1de`**  
Producción: **FUERA DE ALCANCE / NO TOCADA**  
Estado: **COMPLETADA / CERTIFICADA AUTOMÁTICAMENTE**

---

## 1. Objetivo y criterio de cierre

M13 constituye el gate final del Plan Maestro de Mejoras Empresariales M0–M13. Su objetivo fue auditar transversalmente Solqaryn, corregir los hallazgos P0/P1 demostrados y emitir un dictamen basado exclusivamente en evidencia reproducible de Desarrollo.

El cierre exige distinguir expresamente:

- **AUTOMATIZADO Y COMPROBADO**: aquello demostrado por compilación, pruebas, MySQL descartable, Playwright, auditorías y GitHub Actions reales.
- **VALIDACIÓN EXTERNA/FÍSICA PENDIENTE**: infraestructura de terceros, dispositivos, impresión, correo real u otras verificaciones que no deben inferirse a partir del CI.

M13 no autoriza merge, despliegue productivo ni modificación de Producción.

---

## 2. Alcance auditado

Se revisaron los dominios exigidos por el Plan Maestro:

1. arquitectura y separación por capas;
2. normalización e integridad relacional;
3. migraciones EF Core y `AppDbContextModelSnapshot`;
4. seguridad de aplicación y superficie HTTP;
5. autenticación, autorización y RBAC;
6. transacciones;
7. concurrencia e idempotencia;
8. preservación de históricos y snapshots;
9. búsquedas y rendimiento controlado;
10. UX/UI y accesibilidad;
11. backups y restore certificados en Desarrollo descartable;
12. logs y exposición de secretos;
13. código duplicado/muerto y marcadores de deuda técnica;
14. dependencias y vulnerabilidades;
15. facturación, inventario, clientes y finanzas dentro de la regresión integral.

---

## 3. Gate M13 permanente

Workflow permanente:

`.github/workflows/m13-certificacion-final.yml`

La certificación se ejecuta en cinco gates independientes y fail-closed:

- **Secretos, higiene y dependencias**;
- **Backend, MySQL, migraciones, snapshot y upgrade**;
- **Frontend TypeScript, lint y producción**;
- **Runtime, seguridad HTTP y Playwright integral**;
- **Docker, aislamiento y backup certificado vigente**.

El dictamen solo puede emitirse cuando todos los gates terminan satisfactoriamente.

### Ejecución canónica — SHA exacto de Desarrollo

- Evento: `push`.
- Run: **`31427995355`**.
- HEAD: **`19539c72d3a617d95bb3c03dfbde5f6b212ca1de`**.
- Resultado: **SUCCESS**.

Esta ejecución es la certificación canónica porque `github.sha` coincide exactamente con el HEAD funcional de `Desarrollo`.

### Ejecución de integración PR

- Evento: `pull_request`.
- Run: **`31428000318`**.
- Head del PR: **`19539c72d3a617d95bb3c03dfbde5f6b212ca1de`**.
- Base: `main` congelada.
- Resultado: **SUCCESS**.

Esta ejecución valida la integración del PR #2 sin fusionarlo.

---

## 4. Backend, arquitectura y calidad

La certificación ejecutó:

- restore .NET;
- build **Release**;
- warnings tratados como error en el gate M13;
- pruebas unitarias y contrato no integración;
- integración MySQL sobre esquema real generado por EF;
- validación de reglas de negocio críticas ya cubiertas por las fases anteriores.

No quedaron fallos P0/P1 abiertos en la automatización final.

---

## 5. MySQL, migraciones e integridad relacional

Se certificó con **MySQL 8.4** descartable y modo administrado estricto, incluyendo `sql_require_primary_key=ON`.

El gate demostró:

- historial completo de migraciones desde cero;
- integración MySQL sobre el esquema actual;
- generación y ejecución de SQL forward completo e idempotente;
- construcción de un esquema anterior representativo;
- siembra de datos legacy representativos;
- evolución del esquema anterior al actual;
- preservación histórica después del upgrade;
- coherencia entre migraciones, modelo actual y snapshot EF.

Los hardenings previos para Aiven —incluidos preflights con PK y recuperación de DDL parcialmente aplicado— permanecieron cubiertos por regresión.

---

## 6. Seguridad, autenticación y RBAC

El runtime M13 validó en una API real de Staging/Desarrollo descartable:

- rutas protegidas;
- autenticación inválida `401`;
- autorización fail-closed;
- headers de seguridad;
- Swagger no expuesto cuando el entorno lo exige;
- mensajes de error sin detalles internos sensibles;
- auditoría de logs sin secretos detectados.

### Cancelaciones HTTP esperadas

M13 detectó que `OperationCanceledException` producidas por peticiones canceladas por el cliente podían terminar registradas como `Error no controlado` y contaminar observabilidad con falsos 500.

Se corrigió `ExceptionHandlingMiddleware` para diferenciar cancelaciones esperadas de errores reales y se agregó prueba unitaria de regresión.

Resultado final de auditoría de runtime:

- `hallazgosFatales`: **0**;
- `hallazgosSecretos`: **0**;
- resultado: **aprobado**.

---

## 7. Inventario — hallazgo P1 cerrado

La auditoría detectó un defecto funcional real en el filtro de Movimientos de Inventario:

`m.Tipo.ToString() == tipo`

se ejecutaba dentro de un `IQueryable`, expresión que Pomelo/MySQL no podía traducir de manera segura y que podía terminar en respuesta 500.

Corrección aplicada:

- parsear previamente `TipoMovimientoInventario`;
- comparar el enum directamente dentro de LINQ;
- valor de filtro inválido => resultado vacío fail-closed;
- ninguna conversión `ToString()` no traducible permanece en ese filtro.

El defecto quedó cerrado antes de la certificación canónica.

---

## 8. Frontend, UX/UI y accesibilidad

M13 ejecutó:

- instalación reproducible mediante lockfile;
- TypeScript;
- lint;
- build de producción Angular;
- navegador Chromium real;
- suite Playwright integral.

### Resultado Playwright final

Artifact canónico `m13-runtime-e2e`:

- tests totales: **107**;
- failures: **0**;
- errors: **0**;
- skipped: **0**;
- resultado: **107/107 aprobadas**.

Durante M13 se eliminaron dos fuentes de falsa inestabilidad del skip-link de accesibilidad. Tanto la prueba histórica de Fase 8 como la de M10 usan ahora un sentinel exclusivo de E2E para fijar el origen del foco antes de `Tab`, sin retries y sin modificar código productivo.

También se actualizaron pruebas históricas que todavía simulaban contratos anteriores a variantes/autocomplete remoto. No se relajaron las aserciones funcionales actuales.

---

## 9. SMTP, PDF, consola y logs

Después de Playwright, el mismo job ejecutó el validador vigente de runtime.

Resultado:

- SMTP efímero: **aprobado**;
- PDF adjunto: **aprobado**;
- auditoría de logs: **aprobada**;
- secretos detectados: **0**;
- hallazgos fatales: **0**.

El reporte final registró únicamente advertencias operativas no fatales; ninguna cumplió los patrones de bloqueo.

---

## 10. Dependencias y vulnerabilidades

### .NET / NuGet

`dotnet list package --vulnerable`:

- Domain: sin paquetes vulnerables conocidos;
- Application: sin paquetes vulnerables conocidos;
- Infrastructure: sin paquetes vulnerables conocidos;
- API: sin paquetes vulnerables conocidos;
- Tests: sin paquetes vulnerables conocidos.

Resultado: **0 vulnerabilidades NuGet conocidas en las fuentes consultadas por el gate**.

Existen paquetes marcados por NuGet como `Legacy`/deprecated —entre ellos componentes de FluentValidation ASP.NET, IdentityModel y xUnit v2—. Se documentan como deuda de mantenimiento y no como P0/P1 abierto porque el mismo gate no reporta vulnerabilidades conocidas para ellos y su sustitución puede implicar cambios mayores de compatibilidad.

### npm

Auditoría de runtime/producción:

- **0 vulnerabilidades**.

Auditoría de tooling de desarrollo:

- quedan advisories transitivos asociados a Angular CLI/build tooling;
- el remediador disponible exige `npm audit fix --force` con upgrades mayores/breaking;
- M13 deliberadamente no aplicó `--force` ni saltos mayores ciegos.

Estos advisories quedan clasificados como deuda de actualización del toolchain, separados del runtime de producción y fuera de P0/P1 del cierre automatizado.

---

## 11. Auditoría estática y deuda técnica

Artifact `m13-auditoria-estatica-dependencias`:

- archivos rastreados por el escáner: **860**;
- `blockingFindings`: **0**;
- resultado: **PASS**.

Se conservaron hallazgos informativos de marcadores de deuda técnica para evolución posterior. No constituyen bloqueo de seguridad ni funcionalidad certificado.

El escáner fue endurecido durante M13 para evitar falsos positivos de palabras como `password` en lockfiles o elementos HTML mientras mantiene detecciones de patrones realmente sensibles.

---

## 12. Docker, aislamiento y backups

El gate de infraestructura verificó:

- build/configuración Docker de Desarrollo;
- invariantes de aislamiento de entornos;
- ausencia de modificaciones a Producción;
- existencia y vigencia del drill M11 certificado.

M13 no realizó restore ni migración sobre Producción.

La certificación de backup referenciada es la de M11 en entorno descartable; el backup operativo de una infraestructura externa concreta continúa sujeto a secretos/autorizaciones y se mantiene separado de la certificación automática.

---

## 13. Hallazgos M13 cerrados

Durante la fase se detectaron y resolvieron, entre otros:

1. falsos positivos iniciales del escáner estático;
2. pruebas históricas que todavía usaban contratos anteriores de selección de productos;
3. no determinismo del foco inicial del navegador en pruebas de skip-link;
4. clasificación incorrecta de cancelaciones HTTP como errores no controlados;
5. filtro no traducible `Tipo.ToString()` de Movimientos de Inventario;
6. diferencias de nombres de logs al reutilizar el validador de Fase 8 desde M13;
7. trazabilidad entre SHA de `pull_request` sintético y SHA real de `Desarrollo`, resuelta usando el run `push` como certificación canónica.

No se añadieron retries para ocultar fallos y no se degradaron gates para conseguir un resultado verde.

---

## 14. Evidencia canónica

### Artifact de dictamen final

- Nombre: `m13-certificacion-final`;
- ID: **`9078318762`**;
- SHA-256: **`44e200583b533b951835b1728fb6bbb93ae527831b69c87a1bed64ac078b4bed`**.

Contenido certificado:

```text
FASE=M13
RESULTADO=AUTOMATIZADO_Y_COMPROBADO
P0_P1_ABIERTOS=0
PRODUCCION_TOCADA=false
VALIDACION_EXTERNA_FISICA=PENDIENTE
COMMIT=19539c72d3a617d95bb3c03dfbde5f6b212ca1de
RUN=31427995355
```

### Otros artifacts M13

- `m13-runtime-e2e` — ID `9078312564` — SHA-256 `224388c96554948ae0b7695f9687344ecbdf6b3aa437082e2718444aebb995e2`;
- `m13-backend-base` — ID `9078157936` — SHA-256 `0b3e186fa6c197d8b7fe30bfb35db243af585acd689287907351747bc296ae68`;
- `m13-auditoria-estatica-dependencias` — ID `9077961369` — SHA-256 `fc12378a22ef888b71350cc4d24ae5e2d642ff0977dd91e46049d32166a7d622`;
- `m13-infra-backup` — ID `9077958536` — SHA-256 `a7dff2641e08e4c2d9db354d8dd77424c01eb3460a036b7187c1e66a8cdefa05`.

---

## 15. Dictamen

### AUTOMATIZADO Y COMPROBADO

**APROBADO.**

- M13: completada y certificada automáticamente;
- P0/P1 automatizados abiertos: **0**;
- Backend/Frontend/MySQL/E2E/seguridad/Docker/backups de drill: gates verdes;
- Producción tocada: **false**;
- Plan Maestro M0–M13: funcionalmente completado en `Desarrollo`.

### VALIDACIÓN EXTERNA/FÍSICA PENDIENTE

La certificación M13 **NO** afirma ni sustituye:

- aceptación manual del propietario/usuarios;
- dispositivos físicos Android/iPhone/tablet;
- impresión física/POS;
- WhatsApp en teléfono real;
- correo externo real fuera del SMTP efímero;
- estado/credenciales de servicios externos no comprobados por el gate;
- autorización de liberación a Producción.

### PRODUCCIÓN

**NO CERTIFICADA PARA DESPLIEGUE POR ESTA FASE Y NO MODIFICADA.**

El PR #2 debe permanecer abierto y en borrador hasta una autorización posterior expresa. M13 no autoriza merge a `main`, auto-merge ni despliegue productivo.

---

## 16. Cierre del Plan Maestro

Con M13 certificada, la secuencia empresarial queda:

`M0 ✅ -> M0.B ✅ -> M1 ✅ -> M2 ✅ -> M3 ✅ -> M4 ✅ -> M5 ✅ -> M6 ✅ -> M7 ✅ -> M8 ✅ -> M9 ✅ -> M10 ✅ -> M11 ✅ -> M12 ✅ -> M13 ✅`

**No existe una fase M14 dentro del Plan Maestro vigente.** Cualquier trabajo posterior debe tratarse como nuevo plan, mantenimiento, validación externa/física o proceso formal de liberación, sin reinterpretar M13 como permiso para Producción.
