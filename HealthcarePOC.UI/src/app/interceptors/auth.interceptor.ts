import { HttpInterceptorFn, HttpHandlerFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next: HttpHandlerFn) => {
  const token = localStorage.getItem('jwtToken'); // Get JWT Token

  if (token) {
    const clonedReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });

    return next(clonedReq);
  }

  return next(req);
};
