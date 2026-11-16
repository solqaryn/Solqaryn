# SOLQARYN V17 — C15-01.M04.03 configuración visual segura de cobertura

Autor PRIMARY DEV 03 JAVIER; Task ID `6adc2e4bd8a48191ac7d8641ed95a1a3`; PLAN R2182, V17 candado `2000002182`. Rama dev, `PROJECT_SCOPE_LOCK=STRICT`.

## A0 / arquitectura y reversión
Riesgo R2 conservador (ruta privada). Se implementa un InjectionToken local opcional `COBERTURA_PRESENTACION_V5` con lista blanca tipada de dos marcas visuales (institucional/neutra) y dos densidades (regular/compacta). Ausencia de configuración conserva presentación institucional previa; valores arbitrarios o malformados caen en neutra/regular. Los objetos son inmutables y el DOM solo recibe atributos y clases constantes de lista blanca. No se altera sesión, plataformaGuard, tenancy, JWT/RBAC, APIs/DTO, Domain/Application/Infrastructure, MySQL/EF, datos, permisos, resultados institucionales, aprobaciones ni rutas. Reversión mediante git revert normal sobre dev; nunca reset ni fuerza.

## A1–A3 oráculos causales
Cuatro nuevas pruebas Vitest: positivos/valores frontera, objeto inválido e inyección HTML, visual neutro compacto con NO_MEDIDO y cero controles de escritura, revocación sin lectura privada. Se preservan pruebas existentes de componente, guard, carga/error y cuatro dimensiones. Integración pertinente: GitHub-hosted Angular lint/build y Chromium Playwright de ruta de cobertura, cuyo workflow sigue ejecutando los escenarios anónimo/403, responsive, teclado, contraste y denegación. No se simula PASS ni se certifica JWT HTTP real, tenant backend A/B, MySQL o proveedores: NO_APLICA por alcance de presentación local.

## A4 / A5 y concurrencia
Exigir CI requerida completed/success en SHA publicado o equivalente auditado de blobs, Scope Lock, V5/CHANGELOG, check dirigido y readback PLAN/COLA/BITACORA; un run QUEUED/IN_PROGRESS/CANCELLED/SKIPPED no prueba PASS. Conservar únicamente esta microtarea EN_PROGRESO hasta publicación; si CI externa sigue ejecutándose, pasarla a VALIDANDO con RUN/SHA, propietario y entrega completa para permitir siguiente CLAIM. Revisor A5 debe ser otra de las cinco Tasks Javier, nunca la Task autora. No publicar QA/main/PROD ni manipular métricas institucionales.
