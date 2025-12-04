import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-dashboard',
  standalone: true,  // ← Agregar esto
  imports: [CommonModule], // ← Agregar imports necesarios
  template: `
    <div class="dashboard">
      <h1>Bienvenido, {{ userName }}!</h1>
      <p>Email: {{ userEmail }}</p>
      
      <button (click)="loadCompras()">Cargar mis compras</button>
      
      @if (compras.length > 0) {
        <div>
          <h3>Mis Compras:</h3>
          <ul>
            @for (compra of compras; track compra.id) {
              <li>{{ compra.nombre }}</li>
            }
          </ul>
        </div>
      }
    </div>
  `,
  styles: [`
    .dashboard {
      padding: 2rem;
    }
    button {
      padding: 0.5rem 1rem;
      background: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
    }
  `]
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private apiService = inject(ApiService);
  
  userName = '';
  userEmail = '';
  compras: any[] = [];

  ngOnInit() {
    this.userName = this.authService.getUserName();
    //this.userEmail = this.authService.getEmail();
  }

  loadCompras(): void {
  this.apiService.getOrderHistory().subscribe({  // ← CAMBIAR getCompras por getOrderHistory
    next: (compras: any) => {                    // ← Agregar tipo 'any'
      this.compras = compras;
    },
    error: (error: any) => {                     // ← Agregar tipo 'any'
      console.error('Error cargando compras:', error);
    }
  });
}
}