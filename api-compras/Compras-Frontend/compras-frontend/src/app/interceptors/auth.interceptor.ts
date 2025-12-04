/*
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { KeycloakService } from 'keycloak-angular';
import { from, of, switchMap } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const keycloakService = inject(KeycloakService);

  // Excluir URLs que no necesitan autenticación
  if (req.url.includes('/assets/') || req.url.includes('keycloak')) {
    return next(req);
  }

  // Verificar si Keycloak está disponible
  const keycloakInstance = keycloakService.getKeycloakInstance();
  if (!keycloakInstance || !keycloakInstance.authenticated) {
    console.log('⚠️ Keycloak no disponible, enviando request sin token');
    return next(req);
  }

  return from(keycloakService.getToken()).pipe(
    switchMap(token => {
      if (token) {
        const clonedReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        });
        return next(clonedReq);
      }
      return next(req);
    })
  );
};
*/
// auth.interceptor.ts
/*import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Solo agregar token a requests a tu API
  if (req.url.includes('localhost:7248')) {
    const token = authService.getToken();
    
    if (token) {
      console.log('✅ Interceptor - Añadiendo token a la request');
      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next(authReq);
    } else {
      console.warn('⚠️ Interceptor - No hay token disponible');
    }
  }

  return next(req);
};*/


import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth';
import { tap } from 'rxjs/operators';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Solo agregar token a requests a tu API
  if (req.url.includes('localhost:7248')) {
    const token = authService.getToken();
    
    if (token) {
      console.log('🛡️ INTERCEPTOR - Añadiendo token a:', req.method, req.url);
      const authReq = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next(authReq).pipe(
        tap(() => console.log('✅ INTERCEPTOR - Request completada:', req.url))
      );
    } else {
      console.warn('⚠️ INTERCEPTOR - No hay token disponible');
    }
  }

  console.log('🛡️ INTERCEPTOR - Pasando request sin modificar:', req.url);
  return next(req);
};