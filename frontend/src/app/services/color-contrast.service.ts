import { Injectable } from '@angular/core';

interface RgbColor {
  r: number;
  g: number;
  b: number;
}

@Injectable({ providedIn: 'root' })
export class ColorContrastService {
  readonly normalTextRatio = 4.5;

  foregroundFor(background: string): '#ffffff' | '#111827' | '#000000' {
    const bg = this.parseHex(background);
    if (!bg) return '#ffffff';

    const candidates = ['#ffffff', '#111827', '#000000'] as const;
    return candidates
      .map(color => ({ color, ratio: this.contrast(this.parseHex(color)!, bg) }))
      .sort((a, b) => b.ratio - a.ratio)[0].color;
  }

  ensureReadable(foreground: string, backgrounds: string[]): string {
    const fg = this.parseHex(foreground);
    const bgs = backgrounds
      .map(value => this.parseHex(value))
      .filter((value): value is RgbColor => value !== null);

    if (!fg || bgs.length === 0) return foreground;
    if (bgs.every(bg => this.contrast(fg, bg) >= this.normalTextRatio)) return foreground;

    return ['#111827', '#000000', '#ffffff']
      .map(color => ({
        color,
        minimum: Math.min(...bgs.map(bg => this.contrast(this.parseHex(color)!, bg)))
      }))
      .sort((a, b) => b.minimum - a.minimum)[0].color;
  }

  contrastBetween(foreground: string, background: string): number | null {
    const fg = this.parseHex(foreground);
    const bg = this.parseHex(background);
    return fg && bg ? this.contrast(fg, bg) : null;
  }

  private contrast(a: RgbColor, b: RgbColor): number {
    const light = Math.max(this.luminance(a), this.luminance(b));
    const dark = Math.min(this.luminance(a), this.luminance(b));
    return (light + 0.05) / (dark + 0.05);
  }

  private luminance(color: RgbColor): number {
    const channel = (value: number): number => {
      const normalized = value / 255;
      return normalized <= 0.04045
        ? normalized / 12.92
        : Math.pow((normalized + 0.055) / 1.055, 2.4);
    };
    return 0.2126 * channel(color.r) + 0.7152 * channel(color.g) + 0.0722 * channel(color.b);
  }

  private parseHex(value: string | null | undefined): RgbColor | null {
    const normalized = value?.trim();
    if (!normalized) return null;

    const short = /^#([0-9a-f]{3})$/i.exec(normalized);
    if (short) {
      const values = short[1].split('').map(hex => Number.parseInt(`${hex}${hex}`, 16));
      return { r: values[0], g: values[1], b: values[2] };
    }

    const full = /^#([0-9a-f]{6})$/i.exec(normalized);
    if (!full) return null;
    return {
      r: Number.parseInt(full[1].slice(0, 2), 16),
      g: Number.parseInt(full[1].slice(2, 4), 16),
      b: Number.parseInt(full[1].slice(4, 6), 16)
    };
  }
}
