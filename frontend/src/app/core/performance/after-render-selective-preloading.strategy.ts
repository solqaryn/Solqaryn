import { ApplicationRef, Injectable, inject } from '@angular/core';
import { PreloadingStrategy, Route, Router } from '@angular/router';
import { Observable, filter, of, switchMap, take, timer } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AfterRenderSelectivePreloadingStrategy implements PreloadingStrategy {
  private readonly appRef = inject(ApplicationRef);
  private readonly router = inject(Router);

  preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
    if (route.data?.['preloadAfterRender'] !== true) return of(null);
    if (!this.router.url.startsWith('/tienda')) return of(null);

    return this.appRef.isStable.pipe(
      filter(Boolean),
      take(1),
      switchMap(() => timer(250)),
      switchMap(() => load())
    );
  }
}
