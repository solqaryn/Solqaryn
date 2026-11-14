# SOLQARYN V5 — C00-02.M05.04: aceptación positiva del ciclo institucional modelado

**Resultado exigido:** ejecutar un caso positivo de aceptación sobre el modelo de trabajo para automatizaciones, sin inventar ejecución nativa de otra Task. La suite `backend/tests/Solqaryn.Tests/Application/V5C0002M0504PositiveAcceptanceTests.cs` prueba el adaptador real `V5WorkItemMemoryAdapter` y las reglas de dominio `V5WorkItemRules`.

**Casos ejecutables:** un actor del allowlist reclama con COLA y DAG abiertos, pasa a VALIDANDO con fingerprint, cierra LISTO sólo con cinco auditorías declaradas y A5_PENDIENTE, y confirma owner/estado mediante lectura del adaptador. Un segundo caso verifica dos IDs independientes bajo autores distintos, sin transferencia de propiedad.

**Límite de evidencia:** el adaptador probado es en memoria, sin CAS de Sheets, persistencia MySQL, endpoint HTTP, JWT vivo, tenant A/B autenticado, revisión A5 ni dispatcher; los booleanos de auditoría son entradas del test y NO certifican pruebas externas ni los estados institucionales. Una Task real sigue requiriendo la validación de los gates y readbacks independientes del Sheet.

**Clasificación de pruebas:** requiere_dotnet=SI (xUnit, build/test del SHA exacto); requiere_node_angular=NO (sin modificación UI); requiere_mysql=NO (no se modifica persistencia); requiere_docker=NO; requiere_e2e=NO; requiere_security=SI (no elevar autorización ni autoría y preservar separación A5); requiere_scope_lock=SI. A0 diff, test, secretos y CHANGELOG; A1 casos positivos y estado; A2 límites y propietarios; A3 build/test .NET y regresiones; A4 SHA exacto, Scope Lock/procedencia, readback PLAN/COLA/BITACORA y rollback `git revert` normal. A5 independiente posterior por otra Task Javier autorizada; sin QA/main/PROD.
