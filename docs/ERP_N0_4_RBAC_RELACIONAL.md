# ERP-N0.4 — Consolidación RBAC relacional

Fecha de cierre técnico: 2026-08-11  
Plan rector: `PLAN_MAESTRO_ERP_V5`  
Rama: `Desarrollo`  
Estado: **✅ CIERRE TÉCNICO COMPLETADO / PENDIENTE SOLO RECERTIFICACIÓN DEL COMMIT DOCUMENTAL**

---

## 1. Objetivo y autoridad definitiva

ERP-N0.4 elimina la doble autoridad persistida del subsistema de autorización y deja una única fuente de verdad:

```text
Usuario.RolId -> Rol -> RolPermiso.PermisoId -> Permiso
```

Los enums `ModuloSistema` y `AccionPermiso` son identificadores técnicos tipados. No son una autoridad paralela.

Reglas finales:

- un usuario autoriza únicamente mediante grants persistidos `RolPermiso`;
- ausencia de grant = denegación;
- no existe persistencia `Permitido=false` en el modelo final;
- `EsAdministrador` **NO es un bypass de autorización**;
- los administradores acceden porque poseen grants relacionales explícitos;
- roles no administradores mantienen una matriz plenamente administrable.

---

## 2. Semántica final del rol Administrador

Se cerró la ambigüedad entre edición de matriz y seed de administradores.

Contrato definitivo:

1. `EsAdministrador` permanece como metadato de rol y marcador de invariancia/bootstrap.
2. `PermisoService.TienePermisoAsync` no contiene `if (EsAdministrador) return true` ni equivalente; siempre consulta el repositorio relacional.
3. Todo rol activo con `EsAdministrador=true` debe conservar grants explícitos para **todo el catálogo activo**.
4. `SeedPermisoService` crea o restaura de forma idempotente cualquier grant administrativo explícito faltante.
5. `PermisoService.UpdateMatrizAsync` rechaza una solicitud para un rol administrador cuando el conjunto de permisos concedidos no coincide exactamente con el catálogo activo.
6. El rechazo ocurre **antes** de `ReemplazarMatrizPorRolIdAsync`; por tanto, una actualización inválida no muta ningún grant.
7. Un rol no administrador sí puede agregar y retirar grants normalmente.
8. Una matriz administrativa válida conserva el mismo estado semántico después de reiniciar: el seed no cambia nada cuando los grants completos ya existen.

Esta semántica mantiene autorización fail-closed y evita que una reducción temporal de la matriz se restaure silenciosamente en el siguiente arranque.

---

## 3. Causa del M13 rojo previo y corrección

Durante la primera certificación funcional de N0.4, M13 quedó rojo en Playwright aunque el gate RBAC dedicado estaba verde.

La causa fue doble:

### 3.1 E2E legacy destructivo

`fase6-reportes-administrativos.spec.ts` todavía esperaba la semántica anterior de un Administrador con permisos "implícitos e inmutables". El E2E enviaba un `PUT` real para reducir la matriz y esperaba `HTTP 400`.

Con N0.4, la matriz ya estaba representada por grants explícitos y el endpoint aceptó el cambio. El test falló después de haber mutado la matriz compartida del administrador, provocando 403 en pruebas posteriores.

Corrección aplicada:

- el E2E dejó de mutar la matriz del administrador;
- ahora valida de forma no destructiva que el administrador posee grants relacionales explícitos.

### 3.2 Catálogo de Facturación incompleto frente al runtime

Al desaparecer el bypass implícito, se hicieron visibles permisos que controllers de Facturación ya exigían pero que no estaban en `CatalogoPermisosBase`.

El catálogo quedó alineado con el contrato real de runtime:

```text
Facturacion:Ver
Facturacion:Exportar
Facturacion:Imprimir
Facturacion:Compartir
Facturacion:Administrar
Facturacion:Aplicar
Facturacion:Anular
Facturacion:CambiarEstado
```

No se añadió ningún bypass ni se relajó la autorización.

La corrección funcional anterior quedó certificada en:

```text
HEAD: c35ed520c55e960d1d6e8aa6da1539612995f3b8
N0.4: 31533496253 — SUCCESS
M13:  31533496201 — SUCCESS
Playwright: 107 passed / 0 failed / 0 skipped
```

---

## 4. Guarda permanente catálogo RBAC vs permisos de runtime

Archivo:

```text
backend/tests/Solqaryn.Tests/N04CatalogoPermisosRuntimeTests.cs
```

La guarda ya no se limita a Facturación. Mediante reflexión sobre el assembly API inspecciona controllers y acciones protegidas que declaran:

```text
RequierePermisoAttribute
RequiereAlgunoPermisoAttribute
```

Cada combinación `(ModuloSistema, AccionPermiso)` utilizada por runtime debe existir en:

```text
CatalogoPermisosBase.Definicion
```

Si un controller exige una combinación imposible de seedear/conceder, el test falla mostrando el permiso y origen exactos.

Resultado del cierre funcional:

```text
Permisos runtime ausentes del catálogo: 0
Permisos inventados para satisfacer la guarda: 0
```

Se mantienen además aserciones específicas de Facturación para su contrato crítico.

---

## 5. Migración física N0.4

Migración certificada:

```text
20260811174745_N0_4_ConsolidarRbacRelacional
```

Archivo:

```text
backend/src/Infrastructure/Migrations/20260811174745_N0_4_ConsolidarRbacRelacional.cs
```

La migración realiza backfill defensivo de roles/permisos, elimina filas legacy `Permitido=false`, falla cerrado ante datos no representables, retira columnas RBAC legacy y conserva la autoridad relacional final.

Columnas físicas retiradas:

```text
Usuarios.Rol
RolPermisos.Rol
RolPermisos.Modulo
RolPermisos.Accion
RolPermisos.Permitido
```

El administrador histórico se preserva y queda asociado relacionalmente al rol `ADMINISTRADOR`.

---

## 6. Preflight y postcheck

### Preflight

```text
backend/scripts/preflight-erp-n0-4-rbac.sql
```

Comprueba, entre otros:

- usuarios sin rol representable;
- `RolId` huérfanos;
- grants legacy permitidos sin rol o permiso resoluble;
- combinaciones legacy sin equivalente relacional;
- duplicados incompatibles con `(RolId, PermisoId)`.

Resultado certificado: `BloqueosN04 = 0`.

### Postcheck

```text
backend/scripts/postdeploy-erp-n0-4-rbac.sql
```

Comprueba:

- ausencia física de columnas RBAC legacy;
- FKs e índices finales;
- `RolId`/`PermisoId` obligatorios;
- cero huérfanos;
- cero duplicados `(RolId, PermisoId)`;
- grants administrativos completos.

Resultado certificado: `BloqueosN04 = 0`.

---

## 7. Aislamiento definitivo del workflow N0.4

Workflow:

```text
.github/workflows/erp-n0-4-ci.yml
ERP-N0.4 - Certificación RBAC relacional
```

La aplicación de migración dejó de ejecutar `database update` sin target. El comando exacto es:

```bash
dotnet ef database update 20260811174745_N0_4_ConsolidarRbacRelacional \
  --project src/Infrastructure/Solqaryn.Infrastructure.csproj \
  --startup-project src/API/Solqaryn.API.csproj \
  --context AppDbContext
```

Inmediatamente después, el workflow falla cerrado salvo que `__EFMigrationsHistory` cumpla:

```text
20260811032000_N0_3_ConsolidarProductoVariante = exactamente 1
20260811174745_N0_4_ConsolidarRbacRelacional = exactamente 1
MigrationId > N0.4 = 0
```

Consecuencia: cuando existan N0.5, N0.6 o posteriores, este workflow seguirá certificando **únicamente N0.4** y no adelantará accidentalmente el esquema.

---

## 8. Pruebas automatizadas del cierre

Cobertura N0.4 relevante:

- usuario sin scope => denegación fail-closed;
- `EsAdministrador=true` sin grant explícito => denegado;
- grant explícito => autorizado;
- administrador normal => grants explícitos completos;
- reducción de matriz de administrador => rechazada;
- rechazo => cero llamadas a reemplazo de matriz;
- rol normal => puede agregar y retirar grants;
- seed => idempotente;
- grant administrativo faltante por alteración externa => restaurado por seed;
- matriz administrativa válida => mismo estado semántico tras nuevo arranque;
- modelo EF sin autoridad legacy persistida;
- catálogo base cubre toda combinación declarada por atributos de autorización del API.

Certificación funcional más reciente previa a esta actualización documental:

```text
HEAD funcional: c3c0689c23b8c2d2111550b506632a2b5304efed
N0.4 Run:      31536798883 — SUCCESS
Backend:       289 passed / 0 failed / 0 skipped
Build:         0 warnings / 0 errors
Preflight:     SUCCESS
N0.4 exacta:   SUCCESS
Aislamiento:   N0.3=1 / N0.4=1 / posteriores=0
Postcheck:     SUCCESS
Snapshot EF:   CONSISTENTE
```

---

## 9. M13 transversal del cierre funcional

Sobre el mismo HEAD funcional `c3c0689c23b8c2d2111550b506632a2b5304efed`:

```text
M13 Run: 31536798929 — SUCCESS
Runtime, seguridad HTTP y Playwright integral: SUCCESS
Dictamen automatizado M13: SUCCESS
Playwright: 107 passed / 0 failed / 0 skipped
Artifact runtime: m13-runtime-e2e / ID 9119428621
```

El E2E administrativo no destructivo pasó dentro de esos 107 casos.

---

## 10. Trazabilidad del commit documental final

Git no permite que un commit documental contenga anticipadamente su propio SHA y los Run IDs que GitHub Actions generará después de publicarlo.

Por ello, esta versión del documento registra como evidencia incorporada el **último HEAD funcional completamente certificado** (`c3c0689c...`). El commit que actualiza exclusivamente este documento debe ser recertificado inmediatamente con los mismos gates N0.4 y M13.

La evidencia exacta del HEAD documental final queda en:

- historial de GitHub Actions;
- PR #2 `Desarrollo -> main`;
- reporte final de cierre de ERP-N0.4.

No se considera cerrado N0.4 si esa recertificación documental no termina verde.

---

## 11. Seguridad y aislamiento

Todas las certificaciones N0.4 usan MySQL 8.4 efímero de GitHub Actions.

No se ha realizado ninguna de estas acciones:

- modificar `main`;
- crear ramas adicionales;
- mergear PR #2;
- habilitar auto-merge;
- desplegar;
- ejecutar N0.4 contra Aiven Producción;
- ejecutar este cierre contra Aiven Desarrollo;
- modificar secretos, credenciales, dominios, servicios o activos productivos.

Producción permanece congelada y fuera del alcance.

---

## 12. Archivos principales del cierre N0.4

```text
backend/src/Application/Services/PermisoService.cs
backend/src/Application/Common/CatalogoPermisosBase.cs
backend/src/Infrastructure/Services/SeedPermisoService.cs
backend/src/Infrastructure/Migrations/20260811174745_N0_4_ConsolidarRbacRelacional.cs
backend/scripts/preflight-erp-n0-4-rbac.sql
backend/scripts/postdeploy-erp-n0-4-rbac.sql
backend/tests/Solqaryn.Tests/N04RbacRelacionalTests.cs
backend/tests/Solqaryn.Tests/N04AdministradorSemanticaTests.cs
backend/tests/Solqaryn.Tests/N04CatalogoPermisosRuntimeTests.cs
backend/tests/Solqaryn.Tests/SeedPermisoServiceTests.cs
frontend/e2e/fase6-reportes-administrativos.spec.ts
.github/workflows/erp-n0-4-ci.yml
docs/ERP_N0_4_RBAC_RELACIONAL.md
```

---

## 13. Dictamen técnico previo a recertificación documental

```text
AUTORIDAD RBAC ÚNICA Y RELACIONAL:             ✅
BYPASS EsAdministrador:                        AUSENTE ✅
GRANTS ADMINISTRATIVOS EXPLÍCITOS:             ✅
REDUCCIÓN DE MATRIZ ADMINISTRADOR:              RECHAZADA SIN MUTACIÓN ✅
ROL NORMAL ADMINISTRABLE:                       ✅
SEED IDEMPOTENTE / REARRANQUE COHERENTE:        ✅
GUARDA CATÁLOGO VS ATRIBUTOS RUNTIME:           0 FALTANTES ✅
MIGRACIÓN N0.4 AISLADA POR TARGET:              ✅
MIGRACIONES POSTERIORES EN GATE N0.4:           0 ✅
PREFLIGHT / POSTCHECK:                           ✅
SNAPSHOT EF:                                    CONSISTENTE ✅
BACKEND FUNCIONAL:                              289/289 ✅
PLAYWRIGHT FUNCIONAL:                           107/107 ✅
M13 FUNCIONAL:                                  SUCCESS ✅
PRODUCCIÓN TOCADA:                              NO ✅
MAIN MODIFICADA:                                NO ✅
ERP-N0.5 INICIADO:                              NO ✅
```

La declaración definitiva **`✅ ERP-N0.4 = 100% CERRADO / CERTIFICADO`** corresponde únicamente después de recertificar el commit documental final sobre `Desarrollo`.
