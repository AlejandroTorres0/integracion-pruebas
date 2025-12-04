// cartservice.ts - SOLUCIÓN NUCLEAR
import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private apiService = inject(ApiService);
  private cart: any[] = [];
  
  // 🔒 PROTECCIÓN NUCLEAR
  private isAddingToCart = false;
  private pendingRequest: any = null;
  private requestTimeout: any = null;

  getItems() {
    return this.cart;
  }

  addToCart(product: any) {
    console.log('🛒 addToCart INICIADO - Producto:', product.nombre, 'ID:', product.id);
    
    // 🔒 BLOQUEO ABSOLUTO - Solo una operación a la vez
    if (this.isAddingToCart) {
      console.log('🚫 BLOQUEO NUCLEAR - Operación en progreso, IGNORANDO');
      return;
    }

    this.isAddingToCart = true;
    console.log('🔒 BLOQUEO ACTIVADO - Iniciando proceso...');

    if (product.stockDisponible <= 0) {
      alert('Producto sin stock disponible');
      this.unlock();
      return;
    }

    // 1. Agregar al carrito local
    const existingItem = this.cart.find(item => item.id === product.id);
    if (existingItem) {
      if (existingItem.quantity >= product.stockDisponible) {
        alert('No hay suficiente stock disponible');
        this.unlock();
        return;
      }
      existingItem.quantity++;
      console.log('📦 Producto existente, cantidad aumentada a:', existingItem.quantity);
    } else {
      this.cart.push({
        ...product,
        quantity: 1
      });
      console.log('📦 Nuevo producto agregado al carrito local');
    }

    // 2. CANCELAR CUALQUIER REQUEST PENDIENTE
    if (this.pendingRequest) {
      console.log('🛑 Cancelando request pendiente anterior');
      this.pendingRequest.unsubscribe();
      this.pendingRequest = null;
    }

    // 3. SOLO UNA llamada al backend
    this.addToBackendCartNuclear(product.id, 1);

    alert('✅ ' + product.nombre + ' agregado al carrito');
  }

  addToBackendCartNuclear(productId: number, quantity: number) {
    console.log('🚀 INICIANDO LLAMADA HTTP ÚNICA - Producto:', productId);
    
    // 🔒 TIMEOUT DE SEGURIDAD
    this.requestTimeout = setTimeout(() => {
      console.log('⏰ TIMEOUT DE SEGURIDAD - Liberando bloqueo');
      this.unlock();
    }, 10000);

    // 🔒 GUARDAR LA SUSCRIPCIÓN PARA PODER CANCELARLA
    this.pendingRequest = this.apiService.addToCart(productId, quantity).subscribe({
      next: (response) => {
        console.log('✅ LLAMADA HTTP COMPLETADA EXITOSAMENTE');
        this.clearTimeout();
        this.unlock();
      },
      error: (error) => {
        console.warn('⚠️ ERROR HTTP:', error.message);
        this.clearTimeout();
        this.unlock();
      },
      complete: () => {
        console.log('🎯 HTTP COMPLETE');
        this.clearTimeout();
      }
    });
  }

  private clearTimeout() {
    if (this.requestTimeout) {
      clearTimeout(this.requestTimeout);
      this.requestTimeout = null;
    }
  }

  private unlock() {
    this.isAddingToCart = false;
    this.pendingRequest = null;
    console.log('🔓 BLOQUEO LIBERADO - Listo para nueva operación');
  }


  // ... los otros métodos permanecen igual
  validateQuantity(item: any) {
    if (item.quantity > item.stockDisponible) {
      alert('No puedes agregar más de ' + item.stockDisponible + ' unidades de este producto');
      item.quantity = item.stockDisponible;
    }
    if (item.quantity < 1) {
      item.quantity = 1;
    }
  }

  removeFromCart(index: number) {
    this.cart.splice(index, 1);
  }

  getCartTotal(): number {
    return this.cart.reduce(
      (total, item) => total + (item.precio * item.quantity),
      0
    );
  }

  clearCart() {
    this.cart = [];
  }
}