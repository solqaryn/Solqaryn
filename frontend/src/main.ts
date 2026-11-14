import { registerLocaleData } from '@angular/common';
import localeEsHn from '@angular/common/locales/es-HN';
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

registerLocaleData(localeEsHn, 'es-HN');

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
