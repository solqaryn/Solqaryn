import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { NavigationEnd, NavigationStart, Router } from '@angular/router';

interface LayoutShiftEntry extends PerformanceEntry {
  value: number;
  hadRecentInput: boolean;
}

interface InteractionEntry extends PerformanceEntry {
  duration: number;
  interactionId?: number;
}

export interface PerformanceBaselineSnapshot {
  route: string;
  reason: string;
  capturedAtUtc: string;
  documentNavigationTtfbMs: number | null;
  documentLoadMs: number | null;
  documentLcpMs: number | null;
  documentInpMs: number | null;
  documentCls: number;
  routeNavigationMs: number | null;
  observationWindowMs: number;
  requestCount: number;
  apiRequestCount: number;
  transferBytes: number;
  encodedBodyBytes: number;
  apiTransferBytes: number;
  apiTtfbAverageMs: number | null;
  apiTtfbMaxMs: number | null;
  apiDurationAverageMs: number | null;
}

declare global {
  interface Window {
    __SOLQARYN_PERF_BASELINE__?: PerformanceBaselineSnapshot[];
  }
}

@Injectable({ providedIn: 'root' })
export class PerformanceBaselineService {
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly observers: PerformanceObserver[] = [];
  private readonly interactions = new Map<number, number>();

  private started = false;
  private routeStartMs = 0;
  private routeUrl = '/';
  private routeNavigationMs: number | null = null;
  private lcpMs = 0;
  private cls = 0;
  private fallbackInpMs = 0;
  private readonly snapshotTimers: number[] = [];

  start(): void {
    const view = this.document.defaultView;
    if (this.started || !view || !this.enabled(view)) return;

    this.started = true;
    this.routeStartMs = 0;
    this.routeUrl = this.router.url || view.location.pathname;
    this.observeVitals();

    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        this.routeStartMs = view.performance.now();
        this.routeNavigationMs = null;
        this.routeUrl = event.url;
        this.cancelSnapshot(view);
        return;
      }

      if (event instanceof NavigationEnd) {
        this.routeUrl = event.urlAfterRedirects;
        this.routeNavigationMs = this.routeStartMs > 0
          ? this.round(Math.max(0, view.performance.now() - this.routeStartMs))
          : null;
        this.scheduleSnapshot(view, 'navigation');
      }
    });

    if (this.document.readyState === 'complete') {
      this.scheduleSnapshot(view, 'bootstrap');
    } else {
      view.addEventListener('load', () => this.scheduleSnapshot(view, 'load'), { once: true });
    }

    this.document.addEventListener('visibilitychange', () => {
      if (this.document.visibilityState === 'hidden') this.capture(view, 'visibility-hidden');
    });
  }

  private enabled(view: Window): boolean {
    const host = view.location.hostname.toLowerCase();
    const forced = new URLSearchParams(view.location.search).get('perf') === '1';
    return forced
      || host === 'localhost'
      || host === '127.0.0.1'
      || host === 'solqaryn-dev.vercel.app'
      || host.startsWith('solqaryn-dev-');
  }

  private observeVitals(): void {
    if (typeof PerformanceObserver === 'undefined') return;
    const supported = PerformanceObserver.supportedEntryTypes ?? [];

    if (supported.includes('largest-contentful-paint')) {
      const observer = new PerformanceObserver(list => {
        for (const entry of list.getEntries()) this.lcpMs = Math.max(this.lcpMs, entry.startTime);
      });
      observer.observe({ type: 'largest-contentful-paint', buffered: true });
      this.observers.push(observer);
    }

    if (supported.includes('layout-shift')) {
      const observer = new PerformanceObserver(list => {
        for (const raw of list.getEntries()) {
          const entry = raw as LayoutShiftEntry;
          if (!entry.hadRecentInput && Number.isFinite(entry.value)) this.cls += entry.value;
        }
      });
      observer.observe({ type: 'layout-shift', buffered: true });
      this.observers.push(observer);
    }

    if (supported.includes('event')) {
      const observer = new PerformanceObserver(list => {
        for (const raw of list.getEntries()) {
          const entry = raw as InteractionEntry;
          if (!Number.isFinite(entry.duration) || entry.duration <= 0) continue;

          const interactionId = entry.interactionId ?? 0;
          if (interactionId > 0) {
            this.interactions.set(
              interactionId,
              Math.max(this.interactions.get(interactionId) ?? 0, entry.duration)
            );
          } else {
            this.fallbackInpMs = Math.max(this.fallbackInpMs, entry.duration);
          }
        }
      });
      observer.observe({ type: 'event', buffered: true, durationThreshold: 40 } as PerformanceObserverInit);
      this.observers.push(observer);
    }
  }

  private scheduleSnapshot(view: Window, reason: string): void {
    this.cancelSnapshot(view);
    this.snapshotTimers.push(
      view.setTimeout(() => this.capture(view, reason + '-3s'), 3000),
      view.setTimeout(() => this.capture(view, reason + '-settled'), 10_000)
    );
  }

  private cancelSnapshot(view: Window): void {
    while (this.snapshotTimers.length > 0) {
      const timer = this.snapshotTimers.pop();
      if (timer !== undefined) view.clearTimeout(timer);
    }
  }

  private capture(view: Window, reason: string): void {
    const now = view.performance.now();
    const resources = (view.performance.getEntriesByType('resource') as PerformanceResourceTiming[])
      .filter(entry => entry.startTime >= this.routeStartMs);
    const apiResources = resources.filter(entry => this.isApiResource(view, entry));
    const apiTtfb = apiResources
      .map(entry => entry.responseStart > 0 ? entry.responseStart - entry.requestStart : 0)
      .filter(value => value > 0);
    const apiDuration = apiResources
      .map(entry => entry.responseEnd > 0 ? entry.responseEnd - entry.startTime : 0)
      .filter(value => value > 0);
    const navigation = view.performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming | undefined;

    const snapshot: PerformanceBaselineSnapshot = {
      route: this.routeUrl,
      reason,
      capturedAtUtc: new Date().toISOString(),
      documentNavigationTtfbMs: navigation?.responseStart
        ? this.round(navigation.responseStart - navigation.startTime)
        : null,
      documentLoadMs: navigation?.loadEventEnd
        ? this.round(navigation.loadEventEnd - navigation.startTime)
        : null,
      documentLcpMs: this.lcpMs > 0 ? this.round(this.lcpMs) : null,
      documentInpMs: this.inp(),
      documentCls: Math.round(this.cls * 1000) / 1000,
      routeNavigationMs: this.routeNavigationMs,
      observationWindowMs: this.round(Math.max(0, now - this.routeStartMs)),
      requestCount: resources.length,
      apiRequestCount: apiResources.length,
      transferBytes: this.sum(resources, entry => entry.transferSize),
      encodedBodyBytes: this.sum(resources, entry => entry.encodedBodySize),
      apiTransferBytes: this.sum(apiResources, entry => entry.transferSize),
      apiTtfbAverageMs: this.average(apiTtfb),
      apiTtfbMaxMs: apiTtfb.length ? this.round(Math.max(...apiTtfb)) : null,
      apiDurationAverageMs: this.average(apiDuration)
    };

    const history = view.__SOLQARYN_PERF_BASELINE__ ?? [];
    history.push(snapshot);
    if (history.length > 50) history.splice(0, history.length - 50);
    view.__SOLQARYN_PERF_BASELINE__ = history;

    console.info('[SOLQARYN_PERF_BASELINE]', snapshot);
  }

  private isApiResource(view: Window, entry: PerformanceResourceTiming): boolean {
    if (!['fetch', 'xmlhttprequest'].includes(entry.initiatorType)) return false;

    try {
      const url = new URL(entry.name, view.location.origin);
      return url.pathname.startsWith('/api/')
        || url.hostname.endsWith('.onrender.com')
        || (url.hostname === 'localhost' && url.port === '5005');
    } catch {
      return false;
    }
  }

  private inp(): number | null {
    const durations = [...this.interactions.values()].sort((a, b) => b - a);
    if (durations.length > 0) {
      const ignoredWorstInteractions = Math.floor(durations.length / 50);
      return this.round(durations[Math.min(ignoredWorstInteractions, durations.length - 1)]);
    }

    return this.fallbackInpMs > 0 ? this.round(this.fallbackInpMs) : null;
  }

  private average(values: number[]): number | null {
    if (values.length === 0) return null;
    return this.round(values.reduce((total, value) => total + value, 0) / values.length);
  }

  private sum(
    entries: PerformanceResourceTiming[],
    selector: (entry: PerformanceResourceTiming) => number
  ): number {
    return Math.round(entries.reduce((total, entry) => total + Math.max(0, selector(entry) || 0), 0));
  }

  private round(value: number): number {
    return Math.round(value * 10) / 10;
  }
}
