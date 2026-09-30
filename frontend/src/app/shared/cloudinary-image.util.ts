const CLOUDINARY_UPLOAD_SEGMENT = '/image/upload/';
const RESPONSIVE_WIDTHS = [320, 480, 640, 800] as const;

function normalizarUrl(url: string | null | undefined): string {
  return typeof url === 'string' ? url.trim() : '';
}

function esCloudinaryImage(url: string): boolean {
  try {
    const parsed = new URL(url);
    return parsed.protocol === 'https:'
      && parsed.hostname === 'res.cloudinary.com'
      && parsed.pathname.includes(CLOUDINARY_UPLOAD_SEGMENT);
  } catch {
    return false;
  }
}

export function cloudinaryResponsiveUrl(url: string | null | undefined, width = 800): string {
  const original = normalizarUrl(url);
  if (!original || !esCloudinaryImage(original)) return original;

  const boundedWidth = RESPONSIVE_WIDTHS.reduce(
    (best, candidate) => Math.abs(candidate - width) < Math.abs(best - width) ? candidate : best,
    800
  );
  const markerIndex = original.indexOf(CLOUDINARY_UPLOAD_SEGMENT);
  const prefix = original.slice(0, markerIndex + CLOUDINARY_UPLOAD_SEGMENT.length);
  const suffix = original.slice(markerIndex + CLOUDINARY_UPLOAD_SEGMENT.length);
  return `${prefix}f_auto,q_auto,c_limit,w_${boundedWidth}/${suffix}`;
}

export function cloudinaryResponsiveSrcset(url: string | null | undefined): string | null {
  const original = normalizarUrl(url);
  if (!original || !esCloudinaryImage(original)) return null;
  return RESPONSIVE_WIDTHS
    .map(width => `${cloudinaryResponsiveUrl(original, width)} ${width}w`)
    .join(', ');
}
