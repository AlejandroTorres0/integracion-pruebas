import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-logout',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="min-vh-100 d-flex align-items-center justify-content-center bg-light">
      <div class="text-center">
        <div class="spinner-border text-primary mb-3" role="status"></div>
        <h4>Cerrando sesión...</h4>
        <p class="text-muted">Redirigiendo al login</p>
      </div>
    </div>
  `
})
export class LogoutComponent implements OnInit {

  constructor(private router: Router) {}

  ngOnInit() {
    console.log('🚪 Iniciando proceso de logout...');
    this.performLogout();
  }

  private async performLogout() {
    try {
      // 1. Limpiar TODO el almacenamiento local
      localStorage.clear();
      sessionStorage.clear();
      
      // 2. Eliminar cookies de Keycloak de forma agresiva
      this.cleanKeycloakCookies();
      
      // 3. Esperar un momento para que se limpie todo
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // 4. Redirigir a Keycloak logout SIN redirect_uri
      const logoutUrl = 'http://localhost:8080/realms/ds-2025-realm/protocol/openid-connect/logout';
      console.log('📍 Redirigiendo a Keycloak logout...');
      
      // Usar replace para no guardar en historial
      window.location.replace(logoutUrl);
      
    } catch (error) {
      console.error('❌ Error en logout:', error);
      // Fallback: ir al login directamente
      this.router.navigate(['/']);
    }
  }

  private cleanKeycloakCookies() {
    const cookies = document.cookie.split(';');
    const domains = ['', 'localhost', '.localhost'];
    const paths = ['/', '/auth', '/realms'];
    
    cookies.forEach(cookie => {
      const cookieName = cookie.split('=')[0].trim();
      
      domains.forEach(domain => {
        paths.forEach(path => {
          document.cookie = `${cookieName}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=${path}; domain=${domain}`;
        });
      });
    });
  }
}