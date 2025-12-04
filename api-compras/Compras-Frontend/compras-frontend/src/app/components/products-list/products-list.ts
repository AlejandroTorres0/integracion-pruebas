import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api';
//import { CartService } from '../../services/cartservice';
import { CartServiceFixed } from '../../services/cartservice-fixed';

@Component({
  selector: 'app-products-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './products-list.html',
  styleUrl: './products-list.css'
})
export class ProductsListComponent implements OnInit {
  private apiService = inject(ApiService);
  private cartService = inject(CartServiceFixed);
  private clickCounter: number = 0;

  searchTerm: string = '';
  products: any[] = [];
  filteredProducts: any[] = [];
  loading: boolean = false;
  
  // 🔒 CONTROL MEJORADO DE ESTADO DE BOTONES
  addingProductId: number | null = null;
  private lastClickTime = 0;
  public isAddingToCart = false;

  constructor() {
    console.log('🎯 ProductsListComponent CONSTRUCTOR ejecutado');
    console.log('🔧 CartService inyectado:', !!this.cartService);
    console.log('🔧 ApiService inyectado:', !!this.apiService);
  }

  ngOnInit() {
    console.log('🔄 ProductsListComponent ngOnInit ejecutado');
    this.loadProducts();
  }

  loadProducts() {
    console.log('📦 ProductsListComponent loadProducts ejecutado');
    this.loading = true;
    this.apiService.getProducts().subscribe({
      next: (products: any) => {
        console.log('✅ Productos cargados:', products.length);
        console.log('📋 Primer producto:', products[0]);
        this.products = products;
        this.filteredProducts = products;
        this.loading = false;
      },
      error: (error: any) => {
        console.error('❌ Error cargando productos:', error);
        this.products = [];
        this.filteredProducts = [];
        this.loading = false;
      }
    });
  }

  addToCart(product: any) {
    // 🔒 PROTECCIÓN 1: Debouncing - mínimo 3 segundos entre clics
    const now = Date.now();
    if (now - this.lastClickTime < 3000) {
      console.log('⏳ BLOQUEADO - Espera 3 segundos entre clics');
      return;
    }
    this.lastClickTime = now;

    // 🔒 PROTECCIÓN 2: Evitar múltiples ejecuciones simultáneas
    if (this.isAddingToCart) {
      console.log('🚫 BLOQUEADO - Ya hay una operación en progreso');
      return;
    }

    // 🔒 PROTECCIÓN 3: Evitar clics múltiples en el mismo producto
    if (this.addingProductId === product.id) {
      console.log('📋 BLOQUEADO - Este producto ya se está agregando');
      return;
    }

    this.isAddingToCart = true;
    this.addingProductId = product.id;
    
    console.log('🎯 BOTÓN CLICKEADO - INICIANDO PROCESO...');
    console.log('📦 Producto:', product.nombre, 'ID:', product.id);
    
    if (!product.id) {
      console.error('❌ Producto no tiene ID:', product);
      this.resetAddState();
      return;
    }
    
    // Agregar al carrito
    this.cartService.addToCart(product);
    console.log('✅ Llamada a cartService iniciada');

    // 🔓 LIBERAR después de tiempo suficiente para completar la operación
    setTimeout(() => {
      this.resetAddState();
      console.log('🔓 Estado de agregado liberado');
    }, 5000); // 5 segundos para permitir que la llamada HTTP complete
  }

  // 🔓 MÉTODO PARA RESETEAR EL ESTADO
  private resetAddState() {
    this.isAddingToCart = false;
    this.addingProductId = null;
  }

  testDebug() {
    console.log('🎯 DEBUG BUTTON CLICKEADO - Componente FUNCIONA');
    alert('¡El componente TypeScript funciona!');
  
    if (this.filteredProducts.length > 0) {
      console.log('📦 Productos disponibles:', this.filteredProducts);
      this.addToCart(this.filteredProducts[0]);
    }
  }

  // ... el resto de tus métodos permanece igual
  searchProducts() {
    console.log('🔍 Buscando productos:', this.searchTerm);
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      this.filteredProducts = this.products.filter(product =>
        product.nombre.toLowerCase().includes(term) ||
        product.descripcion.toLowerCase().includes(term) ||
        (product.categorias &&
          product.categorias.some((cat: any) =>
            cat.nombre.toLowerCase().includes(term)
          ))
      );
      console.log('📊 Resultados de búsqueda:', this.filteredProducts.length);
    } else {
      this.filteredProducts = this.products;
    }
  }

  getProductImage(product: any): string {
    if (product.imagenes && product.imagenes.length > 0) {
      return product.imagenes[0].url;
    }
    return 'https://via.placeholder.com/300x200?text=Sin+Imagen';
  }

  onImageError(event: any) {
    event.target.src = 'https://via.placeholder.com/300x200?text=Imagen+No+Disponible';
  }
}
