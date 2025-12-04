import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  // private apiUrl = 'https://localhost:7248/api'; // Tu backend .NET original
  private apiUrl = 'http://localhost:5001/api'; // Backend en Docker


  // Método para hacer requests autenticados de manera SÍNCRONA con el token
  private authenticatedRequest<T>(url: string, method: string = 'GET', data?: any): Observable<T> {
    // Obtenemos el token directamente (es un string, no una promesa ni observable)
    const token = this.authService.getToken();
    
    const options = {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    };

    const fullUrl = `${this.apiUrl}${url}`;

    switch (method.toUpperCase()) {
      case 'GET':
        return this.http.get<T>(fullUrl, options);
      case 'POST':
        return this.http.post<T>(fullUrl, data, options);
      case 'PUT':
        return this.http.put<T>(fullUrl, data, options);
      case 'DELETE':
        return this.http.delete<T>(fullUrl, options);
      default:
        return this.http.get<T>(fullUrl, options);
    }
  }

  // ========== PRODUCTOS ==========
  getProducts(): Observable<any[]> {
    return this.authenticatedRequest('/product');
  }

  getProduct(id: number): Observable<any> {
    return this.authenticatedRequest(`/product/${id}`);
  }

  // ========== CARRITO ==========
  getCart(): Observable<any> {
    return this.authenticatedRequest('/shopcart');
  }

  addToCart(productId: number, quantity: number): Observable<any> {
    console.log('🚀 API SERVICE - Iniciando addToCart');
    
    const token = this.authService.getToken();
    console.log('🔑 API SERVICE - Token obtenido');
    
    const options = {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    };

    const fullUrl = `${this.apiUrl}/shopcart`;
    console.log('📡 API SERVICE - URL:', fullUrl);

    return this.http.post(fullUrl, { productId, quantity }, options).pipe(
      tap(response => console.log('✅ API SERVICE - Response recibida')),
      catchError(error => {
        console.error('❌ API SERVICE - Error:', error);
        return throwError(() => error);
      }),
      finalize(() => console.log('🏁 API SERVICE - addToCart finalizado'))
    );
  }

  updateCartItem(productId: number, quantity: number): Observable<any> {
    return this.authenticatedRequest('/shopcart', 'PUT', {
      productId,
      quantity
    });
  }

  removeFromCart(productId: number): Observable<any> {
    return this.authenticatedRequest(`/shopcart/${productId}`, 'DELETE');
  }

  clearCart(): Observable<any> {
    return this.authenticatedRequest('/shopcart', 'DELETE');
  }

  // ========== PEDIDOS ==========
  createOrder(orderData: any): Observable<any> {
    console.log('🚀 API SERVICE - Creando orden:', orderData);
    
    const token = this.authService.getToken();
    
    const options = {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    };

    const fullUrl = `${this.apiUrl}/shopcart/checkout`;
    
    return this.http.post(fullUrl, orderData, options).pipe(
      tap(response => console.log('✅ API SERVICE - Orden creada:', response)),
      catchError(error => {
        console.error('❌ API SERVICE - Error creando orden:', error);
        return throwError(() => error);
      }),
      finalize(() => console.log('🏁 API SERVICE - createOrder finalizado'))
    );
  }

  getOrderHistory(): Observable<any[]> {
    return this.authenticatedRequest('/shopcart/history');
  }

  getOrder(id: number): Observable<any> {
    return this.authenticatedRequest(`/shopcart/history/${id}`);
  }

  cancelOrder(id: number): Observable<any> {
    return this.authenticatedRequest(`/shopcart/history/${id}`, 'DELETE');
  }

  // ========== USUARIO ==========
  getUserProfile(): Observable<any> {
    return this.authenticatedRequest('/user/profile');
  }

  createUserProfile(profileData: any): Observable<any> {
    return this.authenticatedRequest('/user/profile', 'POST', profileData);
  }

  updateUserProfile(profileData: any): Observable<any> {
    return this.authenticatedRequest('/user/profile', 'PUT', profileData);
  }

  // ========== SINCronización con Keycloak ==========
  syncUser(userData: any): Observable<any> {
    // Este endpoint lo debes crear en tu backend para sincronizar usuarios de Keycloak
    return this.authenticatedRequest('/user/sync', 'POST', userData);
  }
}
