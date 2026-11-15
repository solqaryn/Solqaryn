# SOLQARYN V5 FINAL — C05-04.M03.01: reglas de dominio de ligereza UI y aislamiento

**Autor:** PRIMARY DEV 02, TASK_ID 6af16bb9492c8191b8e8c7d606f11507; `solqaryn/Solqaryn@dev`; Scope Lock STRICT. **Entrega efectiva:** C# .NET 10 Domain y xUnit, sin afirmar medición CWV ni aplicación de rendimiento visual.

## Alcance real de M03.01

Se incorpora `V5UiLightnessDomainRules`, guardia de prevalidación server-side para evidencia de carga inicial y caché segura por tenant. Impide convertir roles/tenant enviados por navegador en autorización, reutilizar caché A→B, servir respuestas de revisiones obsoletas, precargar rutas privadas sin permiso o silenciar 401/403. Exige identidad, membresía y permiso backend acreditados, ownership persistido, invalidación por revocación, SHA de artefacto coincidente y métricas positivas derivadas de build verificado.

Los límites declarados reflejan `frontend/angular.json`: bundle initial aviso 650kb/error 750kb, estilo por componente aviso 16kb/error 32kb. El dominio representa esta política en bytes para clasificación relativa; la exactitud de la medición y la unidad del builder Angular siguen requiriendo evidencia del CLI real. **No convierte umbrales en mediciones** ni relaja budgets cuando CI falla. Ausencia de medición, exceso y advertencia devuelven códigos distintos. Un snapshot nominal devuelve únicamente `EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED`, jamás permiso de acceso o CWV aprobadas.

## Pruebas y seguridad

Se agregan ocho tests xUnit de precondición, tenant A/B, invalidación, flags de cliente, prefetch privado, 401/403, filtración JWT/PII, SHA discordante, presupuesto excedido/advertencia y revisiones obsoletas. Los casos reales de performance mobile/desktop, LCP/INP/CLS, navegador, transferencia y deduplicación HTTP siguen sin ejecutarse por esta entrega; deberán probarse en etapas de integración/UI pertinentes.

No se modifica Angular, CSS, API, DI, JWT real, EF/MySQL, migraciones, configuración de budgets, datos de tenants, secrets, proveedores, eventos o QA/main/PROD. M02.01–06 cuentan con LISTO y A5 independiente; H/I sin owner y COLA G/O=SI, DAG F=VALIDADO/G=SI comprobaron aptitud antes del claim exclusivo.

## Verificación y salida

A0: código/contrato static y diff local; A1: xUnit positivas y negativas; A2: pertenencia A/B, permisos, caché, revocación, JWT/PII; A3: build backend .NET 10 y seguridad GitHub-hosted; A4: commit dev fast-forward CAS, Scope Lock exact-SHA y V5 CHANGELOG, checks backend/seguridad pertinentes y readback PLAN/COLA/DAG/BITACORA; A5: Task Javier distinta. Pasar VALIDANDO antes de declarar PASS A1–A4. Un check queued/in_progress/cancelled/skipped/failure requerido no es PASS. Rollback normal git revert con revisión de HEAD concurrente.
