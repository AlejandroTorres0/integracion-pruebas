import { Injectable, inject } from '@angular/core';
import { KeycloakService } from 'keycloak-angular';

@Injectable({
  providedIn: 'root'
})
export class KeycloakInitService {
  private keycloakService = inject(KeycloakService);
  private isInitialized = false;

  async initialize(): Promise<boolean> {
    if (this.isInitialized) {
      return true;
    }

    console.log('🔑 Inicializando Keycloak...');
    
    try {
      const initialized = await this.keycloakService.init({
        config: {
          url: 'http://localhost:8080',
          realm: 'ds-2025-realm',
          clientId: 'grupo-10'
        },
        initOptions: {
            onLoad: 'check-sso',
            checkLoginIframe: false,
            silentCheckSsoRedirectUri: window.location.origin + '/assets/silent-check-sso.html'
        },
        loadUserProfileAtStartUp: false,
        enableBearerInterceptor: false
      });

      console.log('✅ Keycloak inicializado. Autenticado:', initialized);
      this.isInitialized = true;
      return initialized;
      
    } catch (error: any) {
      console.error('💥 Error inicializando Keycloak:', error);
      this.isInitialized = false;
      return false;
    }
  }

  isKeycloakInitialized(): boolean {
    return this.isInitialized && !!this.keycloakService.getKeycloakInstance();
  }
}