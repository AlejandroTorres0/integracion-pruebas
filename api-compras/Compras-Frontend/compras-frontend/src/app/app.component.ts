import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { AuthService } from './services/auth';
import { ApiService } from './services/api';
import { HeaderComponent } from './components/header/header';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    RouterOutlet, 
    HeaderComponent
  ],
  template: `
    <!-- SIEMPRE mostrar el header -->
    <app-header></app-header>
    
    <!-- Contenido principal -->
    <main class="container-fluid">
      <router-outlet></router-outlet>
    </main>
  `
})
export class AppComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  ngOnInit() {
    console.log('🚀 AppComponent iniciado');
    
    // No necesitamos hacer nada más aquí
    // Keycloak se inicializa automáticamente en el AuthService
  }
}