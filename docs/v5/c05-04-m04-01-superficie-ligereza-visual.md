# C05-04.M04.01 — superficie privada de revisión de ligereza UI (DEV)

Autor: PRIMARY DEV 06 JAVIER; TASK_ID `6af16bbad26881918c283abe06e1f32a`. Repositorio `solqaryn/Solqaryn`, rama `dev`, Scope Lock STRICT. PLAN fila 848; COLA G/O=SI y DAG F=VALIDADO/G=SI, predicado M03.01–06 y sus A5 acreditados. Riesgo V15 R1: UI Angular 22.2.1 sin cambios en backend, JWT ni datos.

## A0 — implementación, dependencias y limitaciones
Se extiende la superficie **ya protegida** `/plataforma`, sin agregar rutas, privilegios ni componentes nuevos. Un `<details>` agrupa tres criterios de rendimiento perceptual: tamaño de bundle, LCP/CLS objetivo y prevención de llamadas duplicadas. El botón local con Signal muestra vista compacta, `aria-pressed`, foco visible, meta táctil 44 px y distribución responsive. La sección no emite escrituras ni consulta datos institucionales; preserva el guard `plataformaGuard` y la autoridad del backend. No se afirma haber reducido bytes, medido Core Web Vitals ni haber medido latencia real de la red. La compilación y pruebas externas son requisitos de A4.

Dependencias directas: `PlataformaComponent` y `PlataformaSesionService`, Angular Material existente. El directorio global de empresas sigue su ciclo previo sin cambios. No introduce endpoints, DTO, migraciones, entidades, trabajos, proveedores, secretos, caché, SSR o nuevos permisos.

## A1 — pruebas que deben fallar ante regresión
La prueba Chromium incluye acceso positivo, denegación anónima y 403 sintético, no divulgación del mensaje privado, navegación con Tab/Enter, tres criterios reales en DOM, reducción/expansión sin escritura HTTP y ausencia de overflow a 320/375/768/1280 px. Una prueba de simulación 403 no acredita JWT real ni aislamiento A/B de MySQL.

## A2 y A3 — frontera y ejecución
A2: mantiene el guard global preexistente; superficie condicionada a `sesion()` confirmada. La denegación sintética y el caso anónimo se ejecutan en Chromium; no se inventan roles empresariales. A3: Playwright/Chromium en GitHub-hosted Angular 22.2.1 y build/lint CI exact-SHA. MySQL/EF/JWT HTTP/provider externo: NO_APLICA porque no cambia interfaces ni persistencia. Métricas CWV reales: NO_MEDIDO, no PASS.

## A4/A5 y rollback
A4 requiere `verify-solqaryn-project-scope`, V5/procedencia, frontend lint/build y Chromium dirigido en `completed/success` sobre el SHA publicado, y readback PLAN/BITACORA. Si CI está queued/in_progress, conservar `VALIDANDO` con owner y RUN, sin marcar LISTO. A5 requiere revisor de Task Javier distinta. Rollback: `git revert` normal del commit de esta unidad; ningún cambio a QA/main/PROD.

Referencias aplicadas: governance canónica y SKREF-10 Angular Developer del origen `angular/angular` pin `6808966993bf2b2d455d9b6245e960a59a01b9a9` (solo directrices compatibles).
