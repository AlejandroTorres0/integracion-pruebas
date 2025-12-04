/*
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { KeycloakService } from 'keycloak-angular';

@Component({
  selector: 'app-keycloak-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div class="text-center">
        <div class="spinner-border text-primary mb-3" role="status"></div>
        <h4>Autenticación completada</h4>
        <p class="text-muted">Redirigiendo al portal...</p>
        <p class="small text-success">{{ statusMessage }}</p>
        <button class="btn btn-primary btn-sm mt-3" (click)="redirectNow()">
          Ir ahora al Portal
        </button>
      </div>
    </div>
  `
})
export class KeycloakCallbackComponent implements OnInit {
  private keycloakService = inject(KeycloakService);
  private router = inject(Router);

  statusMessage: string = 'Procesando...';

  async ngOnInit() {
    console.log('🔄 Callback recibido');
    console.log('📍 URL:', window.location.href);
    
    // Simplemente redirigir después de un tiempo - Keycloak ya procesó la autenticación
    this.statusMessage = '✅ Autenticación exitosa';
    
    setTimeout(() => {
      this.redirectToPortal();
    }, 2000);
  }

  redirectToPortal() {
    console.log('📍 Redirigiendo a /compras...');
    // Limpiar la URL del callback para evitar bucles
    window.history.replaceState({}, document.title, '/');
    this.router.navigate(['/compras']);
  }

  redirectNow() {
    this.redirectToPortal();
  }
}
*/

import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-keycloak-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div class="text-center">
        <div class="spinner-border text-primary mb-3" role="status"></div>
        <h4>Procesando autenticación</h4>
        <p class="text-muted">{{ statusMessage }}</p>
        <p class="small text-info">{{ debugInfo }}</p>
      </div>
    </div>
  `
})
export class KeycloakCallbackComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  statusMessage: string = 'Obteniendo código de autorización...';
  debugInfo: string = '';

  async ngOnInit() {
    console.log('🔄 KeycloakCallback - Iniciando procesamiento manual');
    
    try {
      await this.processAuthentication();
    } catch (error) {
      console.error('❌ Error en callback:', error);
      this.statusMessage = 'Error en autenticación';
      this.debugInfo = this.getErrorMessage(error);
      setTimeout(() => this.redirectToLogin(), 3000);
    }
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    } else if (typeof error === 'string') {
      return error;
    } else {
      return 'Error desconocido en la autenticación';
    }
  }

  private async processAuthentication() {
    // Obtener código de la URL
    const code = this.route.snapshot.queryParamMap.get('code');
    const error = this.route.snapshot.queryParamMap.get('error');
    
    this.debugInfo = `Code: ${code ? '✅' : '❌'} | Error: ${error || 'None'}`;

    if (error) {
      throw new Error(`Error de Keycloak: ${error}`);
    }

    if (!code) {
      throw new Error('No se recibió código de autorización');
    }

    this.statusMessage = 'Intercambiando código por token...';
    
    // Usar el AuthService para obtener el token
    const token = await this.authService.exchangeCodeForToken(code);
    
    if (!token) {
      throw new Error('No se pudo obtener el token');
    }

    // Guardar el token
    this.authService.saveManualToken(token);
    
    this.statusMessage = '✅ ¡Autenticación exitosa! Redirigiendo...';
    this.debugInfo = 'Token guardado correctamente';
    
    // Redirigir inmediatamente
    setTimeout(() => {
      this.redirectToPortal();
    }, 500);
  }

  private redirectToPortal() {
    console.log('📍 Redirigiendo a página principal...');
    window.history.replaceState({}, document.title, '/');
    this.router.navigate(['/compras']);
  }

  private redirectToLogin() {
    this.router.navigate(['/']);
  }
}