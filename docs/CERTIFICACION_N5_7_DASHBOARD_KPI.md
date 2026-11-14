# N5.7 — Dashboard ejecutivo · KPIs configurables — Certificación

Autoridad operativa: `docs/VAEP_AUTHORITY.md`  
Rama: `Desarrollo`  
Alcance: `N5.7.A`–`N5.7.H` — Crear KPIs configurables  
Functional head certificado de la última delta funcional: `14df0c1c9aeb045033eb8d3020a396c487d862dc`

## Resultado funcional

N5.7 mantiene el dashboard real existente como autoridad de valores y añade únicamente configuración persistida de presentación sobre un catálogo cerrado de métricas ya source-backed. No existe lenguaje de fórmulas, SQL del usuario, motor BI paralelo ni una segunda autoridad financiera.

La configuración permite habilitar/deshabilitar KPIs soportados, ordenar su presentación y definir una etiqueta visible opcional. Las claves desconocidas fallan cerrado. La configuración efectiva respeta precedencia de usuario sobre rol y conserva el scope/autorización existente del Dashboard.

## Contrato HTTP vigente

La superficie autenticada vive bajo `dashboard/kpis` y requiere el permiso existente `Dashboard/Ver`:

- `GET /dashboard/kpis/configuracion`: devuelve la configuración efectiva del usuario/rol actual.
- `PUT /dashboard/kpis/configuracion`: reemplaza la configuración propia del usuario; rechaza `MetricKey` duplicada/desconocida, orden negativo y etiquetas mayores de 150 caracteres.
- `GET /dashboard/kpis/resueltos`: proyecta únicamente KPIs habilitados y ordenados, usando los valores obtenidos desde `DashboardService.GetResumenAsync`; no evalúa fórmulas aportadas por el usuario.

El catálogo cerrado inicialmente certificado cubre ingresos, ventas, compras, productos, unidades, valor de inventario, utilidad bruta, balance operativo, cuentas por cobrar, cuentas por pagar y productos con stock bajo, siempre sujeto a las autoridades y permisos existentes.

## Persistencia y rollback

`DashboardKpiConfiguracion` persiste ownership exclusivo por usuario o rol, `MetricKey`, estado habilitado, orden y etiqueta visible. La migración N5.7.C y el snapshot EF quedaron reconciliados y el gate de pending-model-changes quedó en verde. No se ejecutó migración sobre Producción.

Rollback seguro: retirar/revertir la configuración y migración únicamente mediante un cambio forward controlado o restauración compatible después de verificar ausencia/preservación de datos configurados. No improvisar DDL destructivo ni borrar configuraciones de usuario/rol sin evidencia y respaldo apropiados.

## Seguridad

La configuración no amplía acceso al Dashboard ni a sus datos. Las lecturas/escrituras se resuelven desde el usuario autenticado; una configuración de otro usuario no se convierte en autoridad. El fallback de rol no sobreescribe una configuración propia del usuario. Claves de métricas fuera del catálogo fallan cerrado.

N5.7.F quedó certificado por `REVIEW_FIRST` y recuperación directa del controller con P0=0/P1=0. No se creó R2 material ni R3.

## QA y gates

Evidencia causal reutilizada para `N5.7.G`:

- Full application validation `34480731619`, job `102882578149`, `SUCCESS` sobre `14df0c1c9aeb045033eb8d3020a396c487d862dc`: restore/build/tests backend, API con migraciones MySQL, lint/build frontend y browser E2E.
- Configuration/dependency audit `34480731567`, `SUCCESS` sobre el mismo functional head: hardening de configuración, auditoría de vulnerabilidades .NET y auditoría npm high/critical.
- Persistencia N5.7.C: `vaep/evidence/fragments/N5.7.C_LISTO_REAL_20260910T1202Z.json`, con migración presente, snapshot efectivo alineado y `pendingModelChanges=false`.
- Seguridad N5.7.F: `vaep/evidence/fragments/N5.7.F_LISTO_REAL_20260910T1314Z.json`, con `REVIEW_FIRST=MATERIAL_ACCEPTED` y P0/P1=0.
- QA/regresión/CI N5.7.G: `vaep/evidence/fragments/N5.7.G_LISTO_REAL_20260910T1322Z.json`.

Benchmark de performance dedicado: `NOT_APPLICABLE` para N5.7 porque esta capacidad configura presentación sobre el path existente y no introduce motor de consultas/BI/agregación nuevo. La ruta de aplicación completa sí quedó cubierta por el gate funcional anterior.

## Guardrails de cierre

- `main`: no tocada.
- Producción: no tocada.
- Secrets: no tocados.
- PR #2: no merge.
- R3: no creado.
- Filler/busywork: no creado.
- Resultado tardío Jules superseded: evidencia únicamente cuando aplica.
- `LISTO_REAL`: sólo mediante receipts y gates causales verificables.

## Continuidad

Con A–G certificados, `N5.7.H` puede cerrar exclusivamente después de reconciliar el rollup documental/estado requerido por COLA. Después del cierre H, promover inmediatamente `N5.8.A — Exportaciones / Auditoría y preflight` y mantener `N5.8.B/N5.8.C` prearmados dependency-gated, conforme a `CLOSURE_CHAIN_SAME_RUN` y sin filler.
