/*

import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { KeycloakService } from 'keycloak-angular';

export function initializeKeycloak(keycloakService: KeycloakService) {
  return () => {
    console.log('🔑 Inicializando Keycloak...');
    
    return keycloakService.init({
      config: {
        url: 'http://localhost:8080',
        realm: 'ds-2025-realm',
        clientId: 'grupo-10'
      },
      initOptions: {
        onLoad: 'check-sso', // Esto causa la redirección automática
        checkLoginIframe: false,
        silentCheckSsoRedirectUri: window.location.origin + '/assets/silent-check-sso.html' // Agregar esto
      },
      loadUserProfileAtStartUp: false
    }).then(authenticated => {
      console.log('✅ Keycloak listo. Autenticado:', authenticated);
      return authenticated;
    }).catch(error => {
      console.error('❌ Error Keycloak:', error);
      return false;
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    KeycloakService,
    {
      provide: 'APP_INITIALIZER',
      useFactory: initializeKeycloak,
      multi: true,
      deps: [KeycloakService]
    }
  ]
};
*/

import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { KeycloakService } from 'keycloak-angular';

export function initializeKeycloak(keycloakService: KeycloakService) {
  return () => {
    console.log('🔑 Inicializando Keycloak...');
    
    return keycloakService.init({
      config: {
        url: 'http://localhost:8080',
        realm: 'ds-2025-realm',
        clientId: 'grupo-10'
      },
      initOptions: {
        onLoad: 'login-required', // ← CAMBIAR de 'check-sso' a 'login-required'
        checkLoginIframe: false
        // ELIMINAR silentCheckSsoRedirectUri - ya no es necesario
      },
      loadUserProfileAtStartUp: false
    }).then(authenticated => {
      console.log('✅ Keycloak listo. Autenticado:', authenticated);
      return authenticated;
    }).catch(error => {
      console.error('❌ Error Keycloak:', error);
      return false;
    });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    KeycloakService
    // ELIMINAR el APP_INITIALIZER - lo manejaremos manualmente
  ]
};