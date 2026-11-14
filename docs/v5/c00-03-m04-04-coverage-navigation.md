# C00-03.M04.04 — navegación y acción única en matriz de cobertura V5

**Prueba ejecutable:** dos casos adicionales Playwright Chromium para la ruta protegida `/plataforma/cobertura-v5`. Un caso recorre cobertura → administración → cobertura, verifica la matriz con cinco encabezados y ausencia de formularios y escrituras. El segundo retiene un GET de sesión **sintético** y comprueba botón desactivado durante la petición, respuesta 403 genérica sin exponer detalles internos y reintento deliberado que recupera consulta solo lectura.

**Delimitación:** formularios comerciales son NO_APLICA porque la matriz es de consulta y expresamente no crea contratos ni modifica PLAN/COLA/BITACORA. Las respuestas mock no acreditan grant JWT/MySQL real. Se conservan tests previos de guard sin token/403; autorización real la decide servidor.

**Tecnologías y cierre:** requiere_node_angular=SI, requiere_e2e=SI, requiere_security=SI (negativos UI, no RBAC real), requiere_scope_lock=SI; requiere_dotnet/mysql/docker=NO_APLICA pues no se cambia API/DB. A0 revisión estática, A1/A2/A3 Chrome + lint/build, A4 exact-SHA, A5 pendiente de otra Task Javier. Reversión con git revert no destructivo; no QA/main/PROD.
