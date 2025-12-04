/*
import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div class="card shadow-lg" style="width: 100%; max-width: 400px;">
        <div class="card-body p-5 text-center">
          <div class="mb-4">
            <h1 class="display-4 text-primary">🔐</h1>
            <h2 class="card-title">Portal de Compras</h2>
            <p class="text-muted">Inicia sesión para continuar</p>
          </div>
          
          <div class="d-grid gap-3">
            <button class="btn btn-primary btn-lg" (click)="login()" [disabled]="isLoading">
              <span *ngIf="!isLoading">🔑 Iniciar Sesión con Keycloak</span>
              <span *ngIf="isLoading">
                <span class="spinner-border spinner-border-sm me-2"></span>
                Redirigiendo...
              </span>
            </button>
            
            <button class="btn btn-outline-secondary" (click)="checkStatus()">
              🔄 Verificar Estado
            </button>
          </div>
          
          <div class="mt-4">
            <p class="small" [class.text-success]="debugInfo.includes('✅')" 
                          [class.text-danger]="debugInfo.includes('❌')"
                          [class.text-muted]="!debugInfo.includes('✅') && !debugInfo.includes('❌')">
              {{ debugInfo }}
            </p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  debugInfo: string = 'Presiona "Verificar Estado"';
  isLoading: boolean = false;

  async ngOnInit() {
    console.log('🔍 LoginComponent iniciado');
    await this.checkAuthStatus();
  }

  async checkAuthStatus() {
    try {
      this.debugInfo = 'Verificando estado...';
      const isLoggedIn = await this.authService.isLoggedIn();
      
      if (isLoggedIn) {
        this.debugInfo = '✅ Ya estás autenticado. Redirigiendo...';
        console.log('✅ Usuario ya autenticado, redirigiendo a compras...');
        setTimeout(() => {
          this.router.navigate(['/compras']);
        }, 1000);
      } else {
        this.debugInfo = '❌ No estás autenticado. Haz clic en "Iniciar Sesión"';
      }
    } catch (error: any) { // ← CORREGIDO: agregar tipo 'any'
      this.debugInfo = `💥 Error: ${error.message || error}`;
      console.error('Error verificando autenticación:', error);
    }
  }

  async login() {
    this.isLoading = true;
    this.debugInfo = 'Iniciando proceso de login...';
    console.log('🔑 Click en botón de login principal');
    
    try {
      await this.authService.login(); // ← CORREGIDO: usar authService directamente
      this.debugInfo = '✅ Redirigiendo a Keycloak...';
    } catch (error: any) { // ← CORREGIDO: agregar tipo 'any'
      this.isLoading = false;
      this.debugInfo = `❌ Error: ${error.message || error}`;
      console.error('Error en login:', error);
    }
  }

  async checkStatus() {
    await this.checkAuthStatus();
  }
}
*/

import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div class="card shadow-lg" style="width: 100%; max-width: 400px;">
        <div class="card-body p-5 text-center">
          <div class="mb-4">
            <h1 class="display-4 text-primary">🔐</h1>
            <h2 class="card-title">Portal de Compras</h2>
            <p class="text-muted">Inicia sesión para continuar</p>
          </div>
          
          <div class="d-grid gap-3">
            <button class="btn btn-primary btn-lg" (click)="login()" [disabled]="isLoading">
              <span *ngIf="!isLoading">🔑 Iniciar Sesión</span>
              <span *ngIf="isLoading">
                <span class="spinner-border spinner-border-sm me-2"></span>
                Redirigiendo...
              </span>
            </button>


            
            <button class="btn btn-outline-primary" (click)="goToRegister()">
              📝 Registrarse
            </button>
          </div>
          
          <div class="mt-4">
            <div class="alert alert-info small">
              <strong>Debug Info:</strong><br>
              {{ debugInfo }}
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  debugInfo: string = 'Inicializando...';
  isLoading: boolean = false;

  async ngOnInit() {
    console.log('🔍 LoginComponent iniciado');
    await this.checkAuthStatus();
  }


  async checkAuthStatus() {
    try {
      this.debugInfo = 'Verificando estado...';
      
      const isLoggedIn = this.authService.isLoggedIn();
      const debugStatus = await this.authService.debugAuthStatus();
      
      this.debugInfo = debugStatus;
      
      if (isLoggedIn) {
        this.debugInfo += '<br>✅ Redirigiendo a compras...';
        setTimeout(() => {
          this.router.navigate(['/compras']);
        }, 1000);
      }
    } catch (error: unknown) {
      this.debugInfo = `Error: ${this.getErrorMessage(error)}`;
    }
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    } else if (typeof error === 'string') {
      return error;
    } else {
      return 'Error desconocido';
    }
  }

  login() {
    this.isLoading = true;
    this.debugInfo = 'Iniciando proceso de login...';
    
    try {
      this.authService.login();
      // La redirección la maneja el servicio automáticamente
    } catch (error: unknown) {
      this.isLoading = false;
      this.debugInfo = `Error en login: ${this.getErrorMessage(error)}`;
    }
  }

  async checkStatus() {
    await this.checkAuthStatus();
  }

  goToRegister() {
    this.router.navigate(['/register']);
  }
}