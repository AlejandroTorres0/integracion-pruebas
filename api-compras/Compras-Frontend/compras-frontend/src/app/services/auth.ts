import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface RegisterRequest {
  email: string;
  password: string;
  repeatPassword: string;
  firstName: string;
  lastName: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  // private apiUrl = 'https://localhost:7248/api/auth/register'; // 🔧 Ajustá el puerto de tu API
  private apiUrl = 'http://localhost:5001/api/auth/register';
  private clientSecret = '66ff9787-4fa5-46b3-b546-4ccbe604d233';

 

  // 🆕 MÉTODO DE REGISTRO (crea usuario en tu API y en Keycloak)
  registerUser(request: RegisterRequest): Promise<string> {
    console.log('🧾 Enviando solicitud de registro al backend...');

    return this.http.post(this.apiUrl, request, { responseType: 'text' })
      .toPromise()
      .then(response => {
        console.log('✅ Usuario registrado correctamente:', response);
        return 'Registro exitoso';
      })
      .catch(error => {
        console.error('❌ Error en el registro:', error);
        throw error;
      });
  }

  // ===== MÉTODOS DE AUTENTICACIÓN MANUAL =====
  
  async exchangeCodeForToken(code: string): Promise<string> {
    console.log('🔄 AuthService - Intercambiando código por token...');
    
    const tokenUrl = 'http://localhost:8080/realms/ds-2025-realm/protocol/openid-connect/token';
    const redirectUri = 'http://localhost:4200/keycloak-callback';
    
    const body = new URLSearchParams();
    body.set('grant_type', 'authorization_code');
    body.set('client_id', 'grupo-10');
    body.set('client_secret', this.clientSecret);
    body.set('code', code);
    body.set('redirect_uri', redirectUri);

    try {
      console.log('📤 Enviando request a Keycloak...');
      
      const response: any = await this.http.post(tokenUrl, body.toString(), {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        }
      }).toPromise();

      console.log('✅ Token obtenido correctamente');
      return response.access_token;
      
    } catch (error) {
      console.error('❌ Error intercambiando código por token:', error);
      throw error;
    }
  }

  // ===== MÉTODOS DE ESTADO =====

  isLoggedIn(): boolean {
    return this.checkManualAuth();
  }

  private checkManualAuth(): boolean {
    const token = localStorage.getItem('auth_token');
    if (!token) {
      return false;
    }

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const isExpired = Date.now() >= payload.exp * 1000;
      
      if (isExpired) {
        localStorage.removeItem('auth_token');
        return false;
      }

      return true;
    } catch (error) {
      return false;
    }
  }

  // ===== MÉTODOS DE LOGIN/LOGOUT =====

  login(): void {
    console.log('🔑 AuthService - Redirigiendo a Keycloak...');
    
    const redirectUri = encodeURIComponent('http://localhost:4200/keycloak-callback');
    const loginUrl = `http://localhost:8080/realms/ds-2025-realm/protocol/openid-connect/auth?client_id=grupo-10&redirect_uri=${redirectUri}&response_type=code&scope=openid`;
    
    window.location.href = loginUrl;
  }

  logout(): void {
    console.log('🚪 AuthService - Cerrando sesión...');
    localStorage.removeItem('auth_token');
    const logoutUrl = `http://localhost:8080/realms/ds-2025-realm/protocol/openid-connect/logout`;
    window.location.href = logoutUrl;
  }

  // ===== MÉTODOS DE INFORMACIÓN DE USUARIO =====

  getToken(): string {
    return localStorage.getItem('auth_token') || '';
  }

  
  getUserName(): string {
    const token = this.getToken();
    if (!token) return '';

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      console.log('🧾 Payload del token en getUserName:', payload);

      // probamos varios campos típicos de Keycloak
      const name =
        payload.preferred_username ||
        payload.name ||
        `${payload.given_name || ''} ${payload.family_name || ''}`.trim() ||
        payload.email ||
        payload.client_id ||
        '';

      return name;
    } catch (error) {
      console.error('❌ Error parseando token en getUserName:', error);
      return '';
    }
  }
 
  /*
  getUserName(): string {
    return 'NOMBRE-DE-PRUEBA';
  }
  */

  saveManualToken(token: string): void {
    localStorage.setItem('auth_token', token);
    console.log('💾 Token guardado en localStorage');
  }

  // ===== MÉTODO DE DEBUG =====

async debugAuthStatus(): Promise<string> {
  const token = this.getToken();
  const isLoggedIn = this.isLoggedIn();
  
  if (!token) {
    return '❌ No hay token en localStorage';
  }

  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const isExpired = Date.now() >= payload.exp * 1000;
    
    return `Token: ${token ? '✅' : '❌'} | ` +
           `Autenticado: ${isLoggedIn ? '✅' : '❌'} | ` +
           `Expirado: ${isExpired ? '✅' : '❌'} | ` +
           `Usuario: ${this.getUserName()}`;
           
  } catch (error: unknown) {
    return `Error: ${this.getErrorMessage(error)}`;
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
}