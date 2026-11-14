# SOLQARYN V5 — C05-02.M01.03: inventario de código y evidencia de branding

**Unidad:** C05-02.M01.03, preparación M01 / inventario de contratos y código existente sin suponer certificación. **Autor:** PRIMARY DEV 01 / JAVIER_CHATGPT / TASK_ID \`6adbb9fff1548191a1bd5b30d9e9984a\`. **Referencia:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive \`1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA\`, PLAN_MAESTRO fila 760. **Rama:** \`solqaryn/Solqaryn@dev\`. **Fecha:** 2026-11-14. **Scope Lock:** STRICT.

## Qué se ha verificado y qué no

Este documento registra las capacidades **que se leen realmente en blobs Git** antes de inventar un editor, un preset por rubro o un historial de branding. Inspeccionar una declaración no equivale a ejecutar API, MySQL, Playwright, auditoría o protección cross-tenant. La finalidad es separar la configuración **global** de tema que existe hoy de la personalización por tenant/versionado que todavía requiere diseño y validación. No se crean migraciones, contratos, servicios ni tests en este cambio documental.

## Inventario trazable: Domain, Application, API, Infrastructure y frontend

| Capa / archivo | Blob observado | Hecho inspeccionado y límite de certeza |
| --- | --- | --- |
| Domain \`backend/src/Domain/Entities/TemaVisual.cs\` | \`c17fd1a3398ac1676f09a69e4c9b20f94aa20855\` | Entidad con 15 campos de color, marca \`Id=1\`, fecha y usuario de actualización; el comentario la define **global**, no por empresa. |
| Application \`backend/src/Application/Services/TemaVisualService.cs\` | \`f92ee8e34e88ca8e611a81d7f00623e823f7b10b\` | \`GetAsync/UpdateAsync/RestaurarPredeterminadoAsync\`, regex hex, guardado y \`IAuditoriaService\`; \`Restaurar\` vuelve a colores de clase, no restaura revisión versionada. |
| API \`backend/src/API/Controllers/TemaVisualController.cs\` | \`a33991fbf941c1fc3048dd30425f6342a0b0c6a4\` | \`GET /tema-visual\` público con caché para login, \`PUT\` y \`POST /restaurar\` con \`[Authorize]\` y permiso \`Configuracion/Editar\`. No se inspeccionó ejecución HTTP. |
| Infrastructure \`backend/src/Infrastructure/Repositories/TemaVisualRepository.cs\` | \`350604fe85fb5361d52f7c28cb61f3a36789f6c6\` | \`FirstOrDefaultAsync\` sobre \`TemasVisuales\`, operaciones simples \`Add/Update/SaveChanges\`; no expone filtro \`EmpresaId\` en este repositorio. |
| EF \`backend/src/Infrastructure/Persistence/Configurations/TemaVisualConfiguration.cs\` | \`0700a9cecb85c6e3e91a011fbd44539107b64627\` | Tabla \`TemaVisual\`, PK Id, longitudes de color, seed \`Id=1\`; no configura propiedad/índice por tenant ni tabla de versiones. |
| Frontend DTO \`frontend/src/app/core/models/tema-visual.model.ts\` | \`9d99a1da70c9c6da408d9a564e91512150b4aedd\` | DTO con 15 campos y \`CAMPOS_TEMA\` mapea 15 claves a variables CSS; más \`fechaActualizacion\` opcional. |
| Cliente HTTP \`frontend/src/app/services/tema-visual.service.ts\` | \`588aa46fc4c10f8d6be85628e919a7bd89f366ac\` | Cliente \`get/update/restaurar\`; no endpoint de versiones/presets. |
| Aplicador \`frontend/src/app/services/theme-applier.service.ts\` | \`cbccc2795cb1b010f74ce5b2b4dc1fb20f0b41f3\` | Limpia tokens y respuestas obsoletas, regex hex, foreground con contraste; no acredita no-regresión de toda la UI. |
| Tests Angular existentes \`frontend/src/app/services/theme-applier.service.spec.ts\` | \`99529c4f26a075f58d1b0a08f6817c4d42003814\` | Archivo tiene casos de aplicar valores permitidos, rechazar CSS inválido y carreras A→B; solo **inspeccionados**, no ejecutados en esta unidad. |
| Contraste \`frontend/src/app/services/color-contrast.service.ts\` | \`2b789f4af6c345068dc689c727313b859f438364\` | Umbral nominal de texto 4.5 y cálculo de foreground; requiere contraste de estados reales. |
| Configuración empresa \`frontend/src/app/services/empresa-configuracion.service.ts\` | \`0e4f127bc2a7dce504a7318546231fb0776903fe\` | Rutas logo, configuración pública y por tenant; una URL \`tenant/{empresaId}\` no es evidencia de autorización ni fallo confirmado. |
| Identidad visible \`frontend/src/app/services/empresa-identidad.service.ts\` | \`88526c37ad3d50dc454e8e23ce7899818a0ada72\` | Signals, contexto y protección ante respuestas tardías, no decisión de permisos. |
| Design system \`frontend/src/styles/_enterprise-design-system.scss\` | \`6a188590af9a958bff2e3e0375c0c46cdb2b14e6\` | Tokens de tipografía, espacio, objetivos táctiles y responsive; no certifica presets ni un builder. |

**Lectura correcta:** son **13 blobs distintos**. Los 15 campos de colores de \`TemaVisual\` no son 15 revisiones ni un esquema de presets. La lectura Git no ejecuta tests. La implementación actual es global para \`TemaVisual\`; no cabe certificar tema personalizado y aislado por empresa a partir de este inventario.

## Contratos existentes y brechas reales (no necesariamente vulnerabilidades)

1. **Tema global frente a tenant.** Entity/Repository/EF muestran \`Id=1\` y consulta \`FirstOrDefaultAsync\` sin \`EmpresaId\`. Si se habilita personalización por empresa en una microfase posterior, requiere modelo de propiedad, filtros y pruebas tenant A/B; no atribuir aislamiento donde no existe esa dimensión.
2. **Restauración ≠ versionado.** \`RestaurarPredeterminadoAsync\` reasigna colores iniciales y audita operación, pero no hay aquí almacenamiento de revisiones ni CAS de versión esperada para publicación.
3. **Lectura pública deliberada.** \`GET /tema-visual\` es anónimo por pantallas de login; hoy son colores globales. No añadir a esa respuesta logos privados, configuración tenant, claims de sesión ni secretos.
4. **Autorización de escritura presente en superficie HTTP.** \`[Authorize]\` + \`[RequierePermiso]\` cubren PUT/restaurar declarativamente; se deben probar revocaciones, errores, roles, y que endpoints futuros tengan la misma frontera.
5. **Validación de colores.** Application y frontend aceptan formato hex; comprobar diferencias de formato, contraste insuficiente, semántica del color y rutas de error en runtime. No afirmar que regex certifica WCAG.
6. **Identidad/logo empresariales en otro contrato.** Los endpoints de \`EmpresaConfiguracionService\` requieren verificación backend de empresa/archivo y manejo de caché; este inventario no certifica esos controladores, repositorios o almacenamiento.

## Evidencia positiva y negativa a reproducir DESPUÉS

**Positivos P01–P06 futuros:** P01 GET anónimo devuelve solo paleta global segura; P02 PUT autorizado aplica 15 colores hex sin CSS; P03 restaurar predeterminado audita y devuelve valores default; P04 ThemeApplier limpia tokens al cambiar contexto; P05 usuario empresarial ve identidad/logo de empresa permitida; P06 builder futuro versiona preview y revisión autorizada preservando accesibilidad.

**Negativos N01–N12 futuros:** N01 PUT sin JWT; N02 PUT con permiso revocado; N03 color CSS \`url(...)\`; N04 parámetro de campo inesperado; N05 fuga de identidad A→B; N06 actualización de logo ajeno; N07 respuesta tardía del tenant A; N08 bypass de RBAC desde tema; N09 publicación de preset sin revisión/CAS; N10 recuperación parcial con error de almacenamiento; N11 contraste insuficiente; N12 caché pública conteniendo datos internos del tenant. Los escenarios del builder se refieren a **trabajo futuro**, no capacidades probadas.

## Impacto, rollback y gates

**Impacto de esta entrega:** solo nuevo Markdown y encabezado+viñetas juntos en \`CHANGELOG_AI.md\`. No cambia contratos API/DTO, estilos Angular, base de datos, migraciones, permisos, JWT, jobs, integraciones, URLs o datos. El análisis de eventual cambio por empresa exige posteriormente revisión arquitectónica, modelo de aislamiento, pruebas .NET/MySQL/Angular y rollback causal.

**Preflight:** \`PLAN_MAESTRO!H760/I760\`, \`COLA!G760/O760\`, \`DAG_TECNICO!A721:G721\`, y exclusión de escritor concurrente. No liberar M01.06 ni fases posteriores por esta preparación documental.

**A0:** diff, 13 blobs y ausencia de secretos añadidos. **A1:** seis positivos y doce negativos **especificados**, no ejecutados. **A2:** fronteras JWT/RBAC, tenant A/B y público vs privado inspeccionadas en código sin fingir tests. **A3:** comparación Domain→Application→API→EF→Angular por blobs, sin ejecutar MySQL/HTTP. **A4:** publicación fast-forward en dev, Scope Lock exact-SHA y check V5 CHANGELOG SUCCESS, readback institucional. Checks backend/frontend/security irrelevantes para el Markdown: NO_APLICA solo con evidencia; no declarar PASS de pruebas runtime. **A5:** evaluación independiente por otro TASK_ID Javier.

Rollback documental: \`git revert\` normal, nunca force-push. QA/main/PROD, datos reales, producción, workers, webhooks, eventos y Tasks nuevas están fuera del alcance.
