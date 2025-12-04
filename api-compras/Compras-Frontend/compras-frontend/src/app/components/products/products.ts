import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth';
import { ApiService } from '../../services/api';
// Si NO usás ProductsListComponent en el HTML, podés borrar esta línea
import { ProductsListComponent } from '../products-list/products-list';

@Component({
  selector: 'app-products',
  standalone: true,
  // si usás <app-products-list> en el HTML, dejalo en imports, sino sacalo
  imports: [CommonModule, RouterModule, ProductsListComponent],
  templateUrl: './products.html',   // 👈 ruta al HTML
  styleUrls: ['./products.css']     // 👈 ruta al CSS
})
export class ProductsComponent implements OnInit {
  private authService = inject(AuthService);
  private apiService = inject(ApiService);

  userName: string = '';
  compras: any[] = [];
  loadingCompras: boolean = false;

  constructor() {
    console.log('🟣 CONSTRUCTOR de ProductsComponent ejecutado');
  }

  ngOnInit() {
    console.log('🟢 ngOnInit de ProductsComponent ejecutado');
    this.userName = this.authService.getUserName();
    console.log('👉 userName en ProductsComponent:', this.userName);
    this.loadCompras();
  }

  loadCompras() {
    this.loadingCompras = true;
    this.apiService.getOrderHistory().subscribe({
      next: (compras: any) => {
        this.compras = compras;
        this.loadingCompras = false;
      },
      error: (error: any) => {
        console.error('Error cargando compras:', error);
        this.compras = [];
        this.loadingCompras = false;
      }
    });
  }

  getStatusClass(estado: string): string {
    if (!estado) return '';
    switch (estado.toUpperCase()) {
      case 'PENDIENTE': return 'status-pendiente';
      case 'ENVIADO': return 'status-enviado';
      case 'ENTREGADO': return 'status-entregado';
      case 'CANCELADO': return 'status-cancelado';
      default: return '';
    }
  }
}
