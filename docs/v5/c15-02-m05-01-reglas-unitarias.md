# C15-02.M05.01 — reglas unitarias de fuente local de arquitectura

Alcance atómico de PLAN_MAESTRO D2222: ejecutar una prueba unitaria/de regla reproducible. R1 test-only en Angular 22.2.1, sin modificar el componente de producción, API, JWT, RBAC, MySQL, providers ni despliegues. E2222 es la meta acumulativa de M05, no certifica Hostinger, Cloudflare, R2, DNS, migraciones ni el cierre de fase.

- A0: el diff se limita a especificación Angular/Vitest, workflow dirigido GitHub-hosted, este contrato y CHANGELOG; comportamiento runtime inalterado.
- A1: cuatro reglas adversariales positivas, negativas, frontera (16 válidos / 17 rechazados), IDs duplicados, recuperación por reintento y rechazo de IDs malformados sin exposición de datos internos.
- A2: salida no privilegiada, cero secretos renderizados y cierre seguro del error; JWT firmado, RBAC, tenant A/B, HTTP real NO_APLICA como certificación porque no se modifica runtime.
- A3: Vitest Angular TestBed y build/lint reales en GitHub-hosted del SHA publicado; no se simula MySQL 8.4 ni se declara integración de proveedor.
- A4: requerir job dirigido SUCCESS, Scope Lock exact-SHA SUCCESS, V5 CHANGELOG y checks pertinentes de frontend; SKIPPED/FAIL/CANCELLED/IN_PROGRESS no acreditan PASS.
- A5: revisión independiente por otra Task Javier de la allowlist, no por DEV03 autor. Registrar límites y errores históricos sin borrarlos.

Referencia metodológica: angular/angular @ 6808966993bf2b2d455d9b6245e960a59a01b9a9, skills/dev-skills/angular-developer/SKILL.md. No se instaló código externo.

Rollback: git revert normal del commit nuevo bajo gobernanza, nunca force-push. CI pendiente: mantener VALIDANDO y conservar ID/SHA/RUN hasta PASS causal; no publicar QA/main/PROD.
