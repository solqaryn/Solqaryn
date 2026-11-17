# C08-04.M06.03 — Limitaciones, riesgos abiertos y decisiones del lote sectorial

**Autor:** PRIMARY DEV 03 — JAVIER (TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`). **Rama:** `solqaryn/Solqaryn@dev`. **PROJECT_SCOPE_LOCK:** STRICT. **Clasificación de esta entrega:** R0 documental; los cambios futuros a autorización/tenant/integraciones son R2 y no quedan autorizados por este documento.

## A0 — Alcance y evidencia causal

La entrega **solo documenta** el estado y los límites de las cuatro capacidades sectoriales informativas. No implementa activación, proveedores, API, base de datos, permisos ni ruta nueva. Fuentes inspeccionadas en el HEAD `43ce84f67d5d944cdba7bc22b5ebcee7a8518bfc`:

- `frontend/src/app/features/extensiones-sectoriales/sector-modules-readonly.component.ts`: cuatro capacidades tipadas y `estadoPresentacion='vacio'`, sin conexión a proveedor ni habilitación de módulos.
- `frontend/src/app/app.routes.ts`: ruta `/extensiones-sectoriales` protegida en el cliente por `authGuard`. **Un guard de frontend no certifica autorización backend**.
- `frontend/e2e/c08-04-m05-06-regression.spec.ts`: cuatro casos Chromium con sesión/tenant-context **sintéticos**.
- Fuente de pruebas `2239d9a85e9846c8ba5f95f3eb2c955c91e07efb`, run GitHub Actions `43987704049`, job `133720024703`, **4/4 PASS**. De 17 check-runs de ese SHA, **15 success** y **2 reintentos auxiliares skipped** (no se acreditan como PASS). Scope Lock y V5 success en SHA exacto. Los blobs del test `a3466cd8f55e274d6f1b8ccd3a9a1a8d1b628423`, workflow `4590226a16662a9ace2df0361b897d2450e77b2d` y documento `73975ce503817aa4b9d7c1864e847440a76fe9d7` permanecían idénticos en el HEAD de base.
- Esta revisión previa **sí** tiene A5 independiente por Task DEV03 sobre el trabajo DEV02, pero ello no certifica la funcionalidad global sectorial ni permite publicar en QA/PROD.

## Matriz de capacidades y decisiones

| Capacidad visible | Comportamiento actual verificable | Decisión | Evidencia futura para activación |
| --- | --- | --- | --- |
| Reservas | Tarjeta «No conectado» sin API ni formularios | Seguir como vista previa no operativa | Recursos/sucursales validados por backend, autorización y aislamiento tenant A/B |
| Membresías | Tarjeta informativa, no concede roles | No derivar permisos del cliente | Membresía y RBAC server-side, revocación y denegaciones negativas |
| Catálogo | Tarjeta informativa, no crea entidades | No duplicar catálogo/productos | Propiedad por empresa, consistencia con inventario y API real |
| Sitios por rubro | Tarjeta informativa, sin endpoints | Un host/URL no acredita acceso | Tenancy y permisos comprobados por servidor y pruebas HTTP reales |

## Riesgos abiertos y no certificados

1. **Autorización real (R2):** el `authGuard` y las respuestas `page.route` no prueban JWT válido, RBAC de servidor, IDOR/BOLA ni aislamiento entre empresas. **Estado: NO_CERTIFICADO.** Requerir pruebas HTTP reales con dos tenants, usuarios/roles distintos y revocación.
2. **Fuente de capacidades (R2):** las cuatro tarjetas están declaradas estáticamente; no existe un inventario de habilitación firmado por backend. **NO_CONECTADO** sigue siendo la presentación válida. No sustituir por `localStorage` ni conceder permisos por señales Angular.
3. **Persistencia y migraciones (R2):** esta superficie no toca EF Core/MySQL. MySQL 8.4, restauración y migraciones quedan **NO_APLICA para este diff R0**; no se certifica el sistema de datos por compilar Angular.
4. **Accesibilidad integral:** Chromium acreditó Tab desde foco inicial, anillo de al menos 3 px, área de 44 px y reflow sin scroll horizontal a 320, 375, 768 y 1280 px; no prueba un informe WCAG global, lector de pantalla ni contraste de todas las vistas. **Cobertura global: NO_MEDIDO.**
5. **Rendimiento real:** no se recopilaron Core Web Vitals, LCP/INP/CLS, p95/p99, tiempo a interacción, tasa de error, tráfico ni costos bajo carga. **NO_MEDIDO (numerador/denominador/muestra ausentes).** No inferir SLO productivo de 4 pruebas.
6. **Denegaciones sintéticas:** los escenarios 403/503 prueban redirección de UX a login, ocultamiento de tarjetas y diagnóstico y ausencia de métodos mutadores, pero no un 403/503 de proveedor autenticado real. **Seguridad HTTP: NO_CERTIFICADA.**
7. **Concurrencia/despliegues:** los checks GitHub ejecutados por SHA no garantizan que un cambio posterior conserve blobs; verificar source/test/config actual antes de cada A5 posterior. No usar skipped/cancelled/in_progress como PASS.

## Criterios y salidas

- **A0 (PASS documental):** este inventario de límites, origen de datos y decisiones corresponde exclusivamente a `C08-04.M06.03`; sin interfaces API, DTO, ORM, secrets, permisos, rutas o runtime modificados.
- **A1 (NO_APLICA al diff documental):** no se añadieron asserts de código; se revisan resultados reales anteriores **4/4** solamente como evidencia de límites, no como pruebas ejecutadas por este commit.
- **A2 (NO_APLICA runtime):** documentadas fronteras JWT/RBAC/tenant y riesgos abiertos; no se certifica seguridad de producción.
- **A3 (NO_APLICA runtime):** sin MySQL, API, proveedor ni browser nuevo; la ejecución original de Chromium corresponde a otro SHA.
- **A4 (PENDIENTE hasta comprobación post-publicación):** comparar dos blobs documentales, `PROJECT_SCOPE_LOCK=STRICT`, procedencia V5 y checks pertinentes en SHA exacto o equivalencia probada; readback PLAN_MAESTRO/BITACORA/COLA/DAG con candado V17. Si CI externa sigue en progreso, conservar VALIDANDO, propietario y RUN/SHA.
- **A5:** revisión independiente **PENDIENTE** de una Task Javier distinta de DEV03; jamás aprobar por autoría propia, ni convertir A5 documental en certificación de capacidades o liberar M06.06 antes de puertas reales.

**Rollback:** nuevo commit `git revert` de esta entrega documental (sin force-push/reset), preservar BITACORA histórica. No hacer cambios a `qa`, `main`, PROD, cuentas o datos productivos.
