# SOLQARYN V5 — C05-02.M01.05: criterios observables de aceptación para branding y presets

**Alcance:** preparación M01 de `C05-02 — Branding y presets`. **Autor:** PRIMARY DEV 04 / JAVIER_CHATGPT / TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO fila 762, rama `dev`, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Resultado verificable y límites

Esta unidad define criterios de aceptación **futuros, observables y reproducibles** de logos, temas, identidad visual, presets por rubro, vista previa, publicación, versionado, rollback y aislamiento tenant. Especifica oráculos positivos y negativos que las unidades posteriores deberán implementar y ejecutar; **no son pruebas runtime ejecutadas**. La aceptación documental de M01.05 no acredita que existan hoy preview, presets por rubro, API tenant de temas, revisión CAS, rollback versionado ni una migración MySQL. `TemaVisual` actual es un registro **global**, distinto de una personalización por tenant.

No se cambian fuentes TS/SCSS, backend, API, entidades, configuración, migraciones, proveedores, archivos de usuario, DNS, secretos ni cuentas. No se activan workers, webhooks, servicios, Tasks o pagos. No hay promoción a QA/main/PROD. Una apariencia distinta en cliente **jamás** concede rol, empresa, sucursal o permisos.

## Línea base comprobable y decisiones abiertas

Las fuentes causalmente inventariadas por `C05-02.M01.03` y `C05-02.M01.04` incluyen `backend/src/Domain/Entities/TemaVisual.cs` (modelo global `Id=1`), `backend/src/Infrastructure/Repositories/TemaVisualRepository.cs` (lectura sin filtro EmpresaId), `backend/src/API/Controllers/TemaVisualController.cs` (lectura pública delimitada y mutación protegida), `backend/src/Application/Services/TemaVisualService.cs`, `frontend/src/app/services/theme-applier.service.ts` (saneamiento hex y descarte de respuesta obsoleta) y `frontend/src/app/services/empresa-identidad.service.ts` (versión local). Su existencia no certifica aislamiento de branding ni tests E2E A/B.

Las decisiones D01–D08 identificadas en `c05-02-m01-04-riesgos-supuestos-decisiones-branding.md` (global/tenant, permisos, esquema de presets, versión/CAS, logos/storage, fallo/fallback, accesibilidad y promoción) siguen **PENDIENTE** hasta prueba y autorización específicas. La M01.06 requiere revisar estas decisiones y los A5 predecesores; M01.05 no la desbloquea por sí sola.

## Criterios funcionales positivos futuros — NO_EJECUTADOS

| ID | Acción y precondición de una implementación futura | Oráculo objetivo y evidencia requerida | Capa de prueba |
| --- | --- | --- | --- |
| P01 | Solicitar el tema global para una vista pública admitida | DTO público mínimo, sin credenciales, secretos ni configuración privada | HTTP API + contrato |
| P02 | Cargar un tema allowlisted vigente | Se aplican solo tokens permitidos y estados legibles, con fallback cuando falta token | Angular unit/integration |
| P03 | Cambiar el contexto de empresa A a B con respuestas asincrónicas | Se eliminan marcas A y nunca se aplica una respuesta A tardía sobre B | Angular con carrera controlada |
| P04 | Previsualizar cambios por un editor autorizado, **si se implementa preview** | Vista local aislada y ninguna modificación persistida antes de publicar | Angular + API/BD efímera |
| P05 | Publicar una revisión válida por titular autorizado, **si existe versionado** | Nueva revisión auditable, condición de versión esperada y lectura consistente | API + MySQL efímero |
| P06 | Revertir a una revisión aprobada, **si se implementa rollback histórico** | Estado anterior recuperable con nueva auditoría; reset a defaults no se confunde con rollback | API/BD + E2E |
| P07 | Cambiar un preset tipado permitido sin cambiar IAM | Presentación cambia; permisos, roles y alcance de datos permanecen iguales | API JWT/RBAC A/B |
| P08 | Usar teclado y viewport móvil con un tema válido | Contraste en estados definidos, foco visible y layout sin overflow general a 320 px | Playwright + a11y |

Los P01–P08 son **requisitos de pruebas posteriores**, no PASS de Angular, HTTP, navegador ni MySQL de esta microtarea documental.

## Criterios negativos y de seguridad futuros — NO_EJECUTADOS

| ID | Intento adverso o fallo | Resultado exigido sin filtraciones |
| --- | --- | --- |
| N01 | Mutar tema sin JWT cuando el endpoint exige autenticación | Denegación, sin cambio ni secretos |
| N02 | Mutar con rol autenticado sin permiso | 403 o respuesta autorizada de denegación, sin escritura |
| N03 | Usuario de A modifica/consulta recurso privado de B | Denegación fail-closed, sin referencia cruzada ni cache contaminada |
| N04 | Cliente envía EmpresaId, host, slug o header manipulados para elevar scope | Backend ignora autoridad derivada del cliente; sin escalada |
| N05 | Preset incluye CSS/HTML/JS arbitrario o nombre de variable no allowlisted | Rechazo de entrada, sin inyección DOM ni script |
| N06 | Logo remoto ajeno, MIME falso o tamaño no permitido | Rechazo con motivo seguro; sin exponer almacenamiento ajeno |
| N07 | Tema define contraste insuficiente o color como único feedback | Política de accesibilidad rechaza o aplica fallback seguro |
| N08 | Respuesta de empresa A llega tras cambiar a B/logout | Respuesta descartada; no persiste marca anterior |
| N09 | Dos editores publican sobre la misma revisión esperada | Uno obtiene conflicto; nunca pérdida silenciosa de actualizaciones |
| N10 | Fallo de red/BD a mitad de publicación | Operación consistente/compensada; no hay estado mezclado |
| N11 | GET anónimo retorna detalles reservados de empresa o token | Respuesta pública minimizada, sin PII/secretos |
| N12 | Preview inadvertidamente crea registro persistido | Operación rechazada en test negativo; no side effect |
| N13 | Preset altera menú ocultando permiso y llama API directamente | Backend niega la operación prohibida independientemente de UI |
| N14 | Reversión solicitada a revisión de otra empresa | Denegación y auditoría, sin leakage cross-tenant |
| N15 | Modal, input o navegación pierden foco o etiqueta bajo branding | Falla accesibilidad reproducible; no clasificar como aprobado |
| N16 | CI de regresión aplicable queda SKIPPED, CANCELLED, FAIL o IN_PROGRESS | No atribuir PASS ni promover el scope |

N01–N16 deben producir pruebas efectivas sobre la implementación **cuando ésta exista**. No se declara bug actualmente reproducido ni se crea permiso, endpoint, migración o sistema de versionado por redactar esta matriz.

## Matriz de evidencia para el cierre futuro

- **Backend/API:** pruebas de autorización positiva/negativa con JWT/RBAC, fuente de tenant confiable y A/B; 401/403/404 según contrato existente. Acreditar validación server-side, no solo guards Angular.
- **Persistencia:** fixture MySQL efímero con al menos dos empresas; comprobar propiedad, revisión, concurrencia/rollback y estados de fallo. Nunca escribir datos productivos para validar.
- **Frontend:** Angular 22.2.1, Jasmine/Karma o runner realmente configurado y Playwright cuando corresponda; contrastes, 320/768/1280 px, teclado, foco, loaders y cambio A→B.
- **Observabilidad:** eventos auditables sin PII/secretos, request correlation opaca, errores seguros; no diseñar un dispatcher ni llamarlo operativo.
- **Gates:** diffs y pruebas por SHA exacto, Scope Lock SUCCESS, A0–A4, A5 independiente por TASK_ID Javier distinto; sin autorización automática QA/main/PROD.

## Auditoría de esta unidad exclusivamente documental

**A0 estática:** contrato M01.05, fuentes autorizadas, revisión de que sólo habrá Markdown y no se incrustan secretos. **A1 funcional-documental:** verificar que P01–P08 son positivos falsables y N01–N16 son negativos observables, expresamente NO_EJECUTADOS. **A2 seguridad-documental:** IAM/JWT/RBAC/tenant A/B, tipos allowlisted, PII y recursos por empresa permanecen límites para la implementación; no PASS runtime inferido. **A3 integración-documental:** coherencia con M01.01–04, riesgos D01–D08 y arquitectura .NET 10 / Angular 22.2.1 / MySQL 8.4. **A4 publicación:** commit fast-forward serializado a dev con SHA, Scope Lock y procedencia V5 completados correctamente; PLAN/COLA/BITACORA readback. Compilaciones/test suites ajenas al diff Markdown son NO_APLICA causal, nunca PASS de la feature.

**A5:** revisión independiente posterior; autora y revisora no pueden ser la misma Task. `DAG=SI` significa elegibilidad documental, no autorización de migración ni despliegue.

**Rollback documental:** revertir el commit con `git revert` normal conservando historial; no force-push. Cualquier futura reversión de configuración activa requiere diseño separado, backups, control de revisiones y pruebas.
