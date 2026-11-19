# C15-02.M04.01 — superficie privada de revisión de arquitectura (V20)
Proyecto: SOLQARYN. Rama: dev. PROJECT_SCOPE_LOCK=STRICT. Autor: DEV01, task 6adbb9fff1548191a1bd5b30d9e9984a.

## Criterio de alcance A0
Ruta Angular standalone `/plataforma/revision-arquitectura-v20`, protegida mediante `plataformaGuard`, sin endpoint nuevo ni cambios backend. La vista modela cinco superficies por revisar: hosting, Cloudflare/DNS, R2, MySQL y proveedores. Todo aparece `NO_VERIFICADO`: los diagramas, contratos, secretos excluidos y procedimientos de rollback son requisitos, no resultados.

No se conecta ni cambia hosting, cuentas, DNS, despliegue, migraciones, secretos, empresas ni permisos. La autorización efectiva sigue en el backend `GET /plataforma/sesion`; el guard revalida, el componente además oculta información al revocar la señal. No reemplaza JWT/RBAC real.

## Calidad y regresión
- A1: TestBed verifica ruta única protegida, cinco superficies y revocación/ocultamiento sin formularios.
- A2: Chromium prueba ausencia de acceso anónimo/403 y no expone texto sensible; HTTP sintético, nunca JWT real.
- A3: Playwright Chromium con servidor Angular efímero; verificar anchos 320,375,768,1280 sin desbordamiento y cero mutaciones HTTP. No aplica integración MySQL/Cloudflare/R2 porque no hay integración runtime ni cambio de DB.
- A4: exigir GitHub Actions exact-SHA: npm lint/build, Vitest/Chromium dirigido, Scope Lock y procedencia V5 SUCCESS. CI QUEUED/IN_PROGRESS no es PASS. Readback PLAN/COLA/BITACORA.
- A5: otra Task Javier distinta de DEV01 revisa diff, pruebas negativas y límites. No autoaprobación.

Rollback normal por revert del commit publicado en dev, sin force-push. No QA/main/PROD.
