# Fase 9 — QuestPDF 2024.3.6 → 2026.9.1

Fecha: 2026-10-07  
Repositorio: `solqaryn/Solqaryn`  
Rama objetivo: `dev`  
PR de trabajo: #3558  
HEAD inicial congelado: `fd0f5429e3709b1ba8a5bb9c5d8ba4b6db7141a8`

## Estado de cierre

- Estado técnico QuestPDF 2026.9.1: **PASS**.
- Estado legal/licencia para integración: **BLOCKED_OWNER_CONFIRMATION**.
- Dictamen vigente mientras falte esa confirmación: `FASE_9_QUESTPDF=STOP`.
- Fase 10: **NO iniciada**.
- `main`, QA y PROD: **NO modificados**.

`QUESTPDF_2026_COMMUNITY_ELIGIBILITY=OWNER_CONFIRMATION_REQUIRED`

La declaración existente `LicenseType.Community` se preserva, pero por sí sola no demuestra elegibilidad bajo los términos vigentes de QuestPDF. El gate permanente de Fase 9 exige confirmación explícita del propietario antes del merge. No se introduce licencia comercial, clave, secreto ni gasto.

## Baseline real 2024.3.6

El baseline se capturó **antes de modificar la referencia QuestPDF**, sobre el commit `3eaf1e6fb41211fe02cdb0afd2500c680c5d09e4`.

- Run: `37659166394` — SUCCESS.
- Artifact: `phase9-questpdf-2024.3.6-baseline`.
- Artifact ID: `11499008809`.
- Digest: `sha256:e544736714a72f3081a00016eddd951169e04dbf6180a15dac4c70bcb1b4869e`.
- QuestPDF informational version: `2024.3.6+61f531012f59c5baa105307f98041e2a2473fbbe`.
- License declarada por runtime: `Community`.
- PDF/UA productivo: no habilitado.
- QR productivo dentro de PDF: no observado.
- Attachments embebidos dentro de PDF: no observados.

Se generaron PDFs reales deterministas para:

- A4;
- Carta;
- Legal;
- Oficio;
- A5;
- POS58;
- POS80;
- reporte administrativo.

Cada factura se generó en variantes corta, larga y Unicode/especiales. Los artefactos baseline quedaron preservados 30 días por GitHub Actions.

## Candidate 2026.9.1

La única actualización NuGet productiva de esta fase es:

`QuestPDF 2024.3.6 → 2026.9.1`

No se modificaron EF Core, provider Oracle, schema, migraciones, Cloudinary, SMTP, JWT, RBAC, tenancy ni cálculos fiscales.

Run técnico candidate inicial:

- Run: `37659725690`.
- `Fase 9 - QuestPDF contratos y unitarias`: SUCCESS.
- `Fase 9 - PDF integración`: SUCCESS.
- `Fase 9 - regresión NuGet`: SUCCESS.
- `Fase 9 - elegibilidad licencia QuestPDF`: FAILURE intencional/fail-closed hasta confirmación del propietario.
- Artifact candidate: `phase9-questpdf-2026.9.1-candidate`.
- Artifact ID: `11499644112`.
- Digest: `sha256:17dc3682d1b73211172fc7c4d383dbe27df82c92be383a1a9c32a6b08ddb0a35`.
- QuestPDF informational version: `2026.9.1+5a1422e15040484498236a5696d3aa9eefad8b10`.

## Comparación before / after

El comparator machine-readable terminó:

`Result=PASS`

con:

- `Errors=[]`;
- las mismas 22 superficies PDF reales;
- mismas familias de fuentes;
- mismos page counts en perfiles de papel;
- anchos térmicos preservados;
- diferencias de tamaño de archivo dentro de frontera;
- caracteres Unicode requeridos preservados;
- ningún glyph tofu `□`;
- extremos de las facturas largas presentes;
- PDF/UA sin activación silenciosa.

### Dimensiones y paginación candidate

| Perfil | Corta | Larga | Páginas largas | Dictamen |
|---|---:|---:|---:|---|
| A4 | 595 × 842 pt | 595 × 842 pt | 3 | PASS |
| Carta | 612 × 792 pt | 612 × 792 pt | 3 | PASS |
| Legal | 612 × 1008 pt | 612 × 1008 pt | 3 | PASS |
| Oficio | 612 × 936 pt | 612 × 936 pt | 3 | PASS |
| A5 | 420 × 595 pt | 420 × 595 pt | 3 | PASS |
| POS58 | 164.5 × 515.5 pt | 164.5 × 1409.5 pt | continuo / 1 | PASS |
| POS80 | 226.75 × 601 pt | 226.75 × 1440 pt | continuo / 1 | PASS |

Baseline térmico largo:

- POS58: `164.5 × 1420.75 pt`.
- POS80: `226.75 × 1410.5 pt`.

Ratios de altura candidate/baseline:

- POS58 largo: `0.9921`.
- POS80 largo: `1.0209`.

No se observó conversión accidental a A4, pérdida de ancho térmico ni desaparición de contenido.

## Render visual before / after

El runner GitHub `ubuntu-latest` de esta ejecución no expone `pdftoppm` preinstalado; no se añadió un runtime package ni una instalación de sistema no pinneada sólo para conseguir verde.

La comparación de render se ejecutó fuera del runtime del producto sobre **los mismos artifacts inmutables de GitHub Actions**, con:

- renderer: `pdftoppm 25.06.0`;
- resolución: 72 DPI;
- PDFs comparados: 22;
- páginas render comparadas: 32;
- máximo RMSE normalizado: `0.243618`;
- máximo delta de densidad no-blanca: `0.005740`;
- errores de frontera: `0`;
- resultado: **PASS**.

La diferencia visual esperada corresponde al motor tipográfico/render actualizado; contenido, geometría contractual, fuentes y densidad visual permanecen dentro de las fronteras definidas.

El script reproducible queda en:

`scripts/phase9_compare_pdf_renders.py`

y no es una dependencia runtime.

## Fuentes y glyphs

Baseline 2024.3.6 embebía:

- Lato-Regular;
- Lato-SemiBold;
- Lato-Bold;
- Lato-Italic.

Candidate 2026.9.1 preserva exactamente esas familias.

Settings candidate observados:

- `UseSystemFonts=False`;
- `ThrowOnMissingFontFamilies=True`;
- `ThrowOnMissingTextGlyphs=True`.

Por tanto:

`FONT_POLICY=PASS`

No se habilitó dependencia accidental de fuentes del sistema ni se relajó el comportamiento fail-closed.

Caracteres dirigidos preservados en los siete perfiles:

- `José Núñez`;
- `Peña & Compañía`;
- `Crédito`;
- `Información`;
- `Dirección`;
- `¿Gracias por su compra?`;
- `¡Vuelva pronto!`;
- `©`;
- `×`;
- `%`;
- `–`;
- `—`.

`SPECIAL_GLYPHS=PASS`

## PDF/UA

Baseline:

- `StructTreeRootObserved=false`;
- `MarkInfoObserved=false`;
- `PdfUaMarkerObserved=false`;
- no existe configuración productiva `PDFUA_Conformance`.

Candidate:

- no habilita PDF/UA;
- no introduce tagging ni compliance silenciosamente.

Clasificación:

`PDFUA=NO_CHANGE_NOT_ENABLED`

No se afirma compliance PDF/UA.

## QR

La auditoría de código productivo no encontró QR generado o embebido en los PDFs de SOLQARYN.

`QR=NOT_APPLICABLE_NO_EXISTING_PDF_QR`

No se creó un QR artificial para esta fase.

## Adjuntos

### PDF adjunto a correo

La superficie existente se preserva y está cubierta por:

- `FacturaCompartirServiceTests`;
- `SmtpEmailServiceTests`;
- pruebas dirigidas Fase 9.

Se preservan:

- `.pdf`;
- `application/pdf`;
- bytes PDF no vacíos;
- factura correcta;
- flujo de envío;
- RBAC/auditoría existentes.

`EMAIL_ATTACHMENT=PASS`

### Attachments embebidos dentro del PDF

No se observó uso productivo de `DocumentAttachment`/attachments embebidos.

`EMBEDDED_PDF_ATTACHMENTS=NOT_APPLICABLE`

## Reporte administrativo

El fixture dirigido genera PDF real desde `ReporteAdministrativoService` con Unicode.

Before:

- 25,396 bytes;
- 1 página;
- 595.5 × 842 pt;
- Lato-Regular + Lato-SemiBold.

After:

- 24,980 bytes;
- 1 página;
- 595 × 842 pt;
- Lato-Regular + Lato-SemiBold;
- texto semántico idéntico según `TextSha256`.

Resultado:

`ADMIN_REPORT_PDF=PASS`

CSV/XLSX continúan fuera del changeset QuestPDF y quedan cubiertos por la regresión existente.

## Concurrencia

`QuestPdfPhase9Tests.Generacion_Concurrente_No_Mezcla_Facturas` genera 12 documentos paralelos con números/clientes distintos y verifica ausencia de contaminación.

Resultado:

`PDF_CONCURRENCY=PASS`

No se mutan settings globales durante generación concurrente.

## Pruebas dirigidas

Sobre QuestPDF 2026.9.1:

- pruebas nuevas Fase 9: `3/3` PASS;
- regresión PDF/share/SMTP/RBAC: `48/48` PASS;
- integración dirigida PDF/tenant: `190/190` PASS;
- Oracle bootstrap fresh: `136` tablas, `pending=0`.

El warning nullable detectado inicialmente en el probe Fase 9 fue corregido sin afectar producto.

## Seguridad NuGet

Candidate:

- 0 vulnerabilidades conocidas según fuentes NuGet para Domain;
- 0 para Application;
- 0 para Infrastructure;
- 0 para API;
- 0 para Infrastructure.Migrations;
- 0 para DatabaseBootstrap;
- 0 para Tests.

Paquetes deprecated productivos:

- 0 en Domain;
- 0 en Application;
- 0 en Infrastructure;
- 0 en API;
- 0 en Infrastructure.Migrations;
- 0 en DatabaseBootstrap.

La matriz de Fase 8 permanece fijada y no fue actualizada dentro de Fase 9.

## Evidencia CI permanente

Workflow:

`.github/workflows/modernization-phase9-questpdf.yml`

Jobs:

- `Fase 9 - QuestPDF contratos y unitarias`;
- `Fase 9 - PDF integración`;
- `Fase 9 - regresión NuGet`;
- `Fase 9 - elegibilidad licencia QuestPDF`;
- `Dictamen Fase 9`.

El dictamen sólo puede emitir `FASE_9_QUESTPDF=PASS` cuando el gate de licencia también sea SUCCESS.

## Pendiente único de integración

La validación técnica de QuestPDF 2026.9.1 está verde. El único bloqueo deliberado es demostrar que la entidad que usa SOLQARYN está autorizada a seleccionar `LicenseType.Community` bajo los términos vigentes de QuestPDF, o disponer de una licencia comercial válida.

Hasta recibir confirmación explícita del propietario:

- no sacar PR #3558 de draft;
- no mergear a `dev`;
- no declarar Fase 9 cerrada;
- no iniciar Fase 10.

## Dictamen vigente

`QUESTPDF_TECHNICAL_VALIDATION=PASS`  
`QUESTPDF_LICENSE_GATE=OWNER_CONFIRMATION_REQUIRED`  
`P0=0`  
`P1=0`  
`FASE_9_QUESTPDF=STOP`
