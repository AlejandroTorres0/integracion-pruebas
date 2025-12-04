// cartservice-fixed.ts
/*import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';

@Injectable({
  providedIn: 'root'
})
export class CartServiceFixed {
  private apiService = inject(ApiService);
  private cart: any[] = [];
  
  // 🔒 BLOQUEO GLOBAL usando variable global
  private get isLocked(): boolean {
    return (window as any).cartServiceLocked === true;
  }
  
  private setLock(value: boolean) {
    (window as any).cartServiceLocked = value;
  }

  getItems() {
    return this.cart;
  }

  addToCart(product: any) {
    console.log('🎯 addToCart INICIADO - Producto:', product.nombre);
    
    // 🔒 BLOQUEO GLOBAL INDISCUTIBLE
    if (this.isLocked) {
      console.log('🚫 BLOQUEO ACTIVADO - Ignorando clic');
      return;
    }
    
    // ACTIVAR BLOQUEO INMEDIATAMENTE
    this.setLock(true);
    console.log('🔒 BLOQUEO ACTIVADO');

    // Validaciones básicas
    if (product.stockDisponible <= 0) {
      alert('Producto sin stock disponible');
      this.setLock(false);
      return;
    }

    // 1. Agregar al carrito local (SIEMPRE)
    const existingItem = this.cart.find(item => item.id === product.id);
    if (existingItem) {
      if (existingItem.quantity >= product.stockDisponible) {
        alert('No hay suficiente stock disponible');
        this.setLock(false);
        return;
      }
      existingItem.quantity++;
      console.log('📦 Producto existente, cantidad:', existingItem.quantity);
    } else {
      this.cart.push({ ...product, quantity: 1 });
      console.log('📦 Nuevo producto agregado al carrito');
    }

    // 2. LLAMADA ÚNICA al backend
    this.makeSingleBackendCall(product.id, 1);

    alert('✅ ' + product.nombre + ' agregado al carrito');
  }

  private makeSingleBackendCall(productId: number, quantity: number) {
    console.log('📡 INICIANDO LLAMADA BACKEND ÚNICA - Producto:', productId);
    
    // Timeout de seguridad
    const safetyTimeout = setTimeout(() => {
      console.log('⏰ Safety timeout - Liberando bloqueo');
      this.setLock(false);
    }, 8000);

    this.apiService.addToCart(productId, quantity).subscribe({
      next: (response) => {
        clearTimeout(safetyTimeout);
        console.log('✅ BACKEND RESPONSE:', response);
        this.setLock(false);
      },
      error: (error) => {
        clearTimeout(safetyTimeout);
        console.error('❌ BACKEND ERROR:', error);
        this.setLock(false);
      }
    });
  }

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
    this.setLock(false); // 🔓 Liberar bloqueo al limpiar carrito
  }

  // 🔓 Método para liberar bloqueo manualmente si es necesario
  forceUnlock() {
    this.setLock(false);
    console.log('🔓 BLOQUEO LIBERADO MANUALMENTE');
  }
}*/

// cartservice-fixed.ts - VERSIÓN MEJORADA
import { Injectable, inject } from '@angular/core';
import { ApiService } from './api';

@Injectable({
  providedIn: 'root'
})
export class CartServiceFixed {
  private apiService = inject(ApiService);
  private cart: any[] = [];
  
  // 🔒 BLOQUEO GLOBAL usando variable global
  private get isLocked(): boolean {
    return (window as any).cartServiceLocked === true;
  }
  
  private setLock(value: boolean) {
    (window as any).cartServiceLocked = value;
  }

  // ✅ NUEVO MÉTODO: Cargar carrito desde backend
  loadCartFromBackend(): Promise<any[]> {
    return new Promise((resolve, reject) => {
      this.apiService.getCart().subscribe({
        next: (cartResponse: any) => {
          console.log('🛒 Carrito cargado desde backend:', cartResponse);
          
          // Transformar los items del backend al formato que usa el frontend
          this.cart = cartResponse.items.map((item: any) => ({
            id: item.productId,
            nombre: item.product.name,
            descripcion: item.product.description,
            precio: item.product.price,
            stockDisponible: item.product.stock,
            quantity: item.quantity
          }));
          
          console.log('📦 Carrito transformado:', this.cart);
          resolve(this.cart);
        },
        error: (error) => {
          console.error('❌ Error cargando carrito desde backend:', error);
          reject(error);
        }
      });
    });
  }

  getItems() {
    return this.cart;
  }

  addToCart(product: any) {
    console.log('🎯 addToCart INICIADO - Producto:', product.nombre);
    
    // 🔒 BLOQUEO GLOBAL INDISCUTIBLE
    if (this.isLocked) {
      console.log('🚫 BLOQUEO ACTIVADO - Ignorando clic');
      return;
    }
    
    // ACTIVAR BLOQUEO INMEDIATAMENTE
    this.setLock(true);
    console.log('🔒 BLOQUEO ACTIVADO');

    // Validaciones básicas
    if (product.stockDisponible <= 0) {
      alert('Producto sin stock disponible');
      this.setLock(false);
      return;
    }

    // 1. Agregar al carrito local (SIEMPRE)
    const existingItem = this.cart.find(item => item.id === product.id);
    if (existingItem) {
      if (existingItem.quantity >= product.stockDisponible) {
        alert('No hay suficiente stock disponible');
        this.setLock(false);
        return;
      }
      existingItem.quantity++;
      console.log('📦 Producto existente, cantidad:', existingItem.quantity);
    } else {
      this.cart.push({ ...product, quantity: 1 });
      console.log('📦 Nuevo producto agregado al carrito');
    }

    // 2. LLAMADA ÚNICA al backend
    this.makeSingleBackendCall(product.id, 1);

    alert('✅ ' + product.nombre + ' agregado al carrito');
  }

  private makeSingleBackendCall(productId: number, quantity: number) {
    console.log('📡 INICIANDO LLAMADA BACKEND ÚNICA - Producto:', productId);
    
    // Timeout de seguridad
    const safetyTimeout = setTimeout(() => {
      console.log('⏰ Safety timeout - Liberando bloqueo');
      this.setLock(false);
    }, 8000);

    this.apiService.addToCart(productId, quantity).subscribe({
      next: (response) => {
        clearTimeout(safetyTimeout);
        console.log('✅ BACKEND RESPONSE:', response);
        this.setLock(false);
        
        // ✅ ACTUALIZAR carrito desde backend después de agregar
        this.loadCartFromBackend();
      },
      error: (error) => {
        clearTimeout(safetyTimeout);
        console.error('❌ BACKEND ERROR:', error);
        this.setLock(false);
      }
    });
  }

  // ✅ NUEVO MÉTODO: Actualizar cantidad en backend
  updateCartItem(productId: number, quantity: number): Promise<any> {
    return new Promise((resolve, reject) => {
      this.apiService.updateCartItem(productId, quantity).subscribe({
        next: (response) => {
          console.log('✅ Cantidad actualizada en backend:', response);
          resolve(response);
        },
        error: (error) => {
          console.error('❌ Error actualizando cantidad:', error);
          reject(error);
        }
      });
    });
  }

  // ✅ NUEVO MÉTODO: Eliminar item del backend
  removeFromBackend(productId: number): Promise<any> {
    return new Promise((resolve, reject) => {
      this.apiService.removeFromCart(productId).subscribe({
        next: (response) => {
          console.log('✅ Producto eliminado del backend:', response);
          resolve(response);
        },
        error: (error) => {
          console.error('❌ Error eliminando producto:', error);
          reject(error);
        }
      });
    });
  }

  validateQuantity(item: any) {
    if (item.quantity > item.stockDisponible) {
      alert('No puedes agregar más de ' + item.stockDisponible + ' unidades de este producto');
      item.quantity = item.stockDisponible;
    }
    if (item.quantity < 1) {
      item.quantity = 1;
    }
    
    // ✅ SINCRONIZAR CON BACKEND cuando cambia la cantidad
    this.updateCartItem(item.id, item.quantity).then(() => {
      this.loadCartFromBackend(); // Recargar carrito actualizado
    });
  }

  removeFromCart(index: number) {
    const item = this.cart[index];
    console.log('🗑️ Eliminando producto:', item.nombre);
    
    // ✅ ELIMINAR DEL BACKEND
    this.removeFromBackend(item.id).then(() => {
      // Eliminar del carrito local después de éxito en backend
      this.cart.splice(index, 1);
      console.log('✅ Producto eliminado localmente');
    }).catch(error => {
      console.error('❌ No se pudo eliminar del backend, manteniendo local');
    });
  }

  getCartTotal(): number {
    return this.cart.reduce(
      (total, item) => total + (item.precio * item.quantity),
      0
    );
  }

  clearCart() {
    this.cart = [];
    this.setLock(false);
  }

  // 🔓 Método para liberar bloqueo manualmente si es necesario
  forceUnlock() {
    this.setLock(false);
    console.log('🔓 BLOQUEO LIBERADO MANUALMENTE');
  }
}