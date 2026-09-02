import { InjectionToken } from '@angular/core';

/**
 * Base URL prefix for API calls. Empty string uses relative `/api/...`
 * paths (dev proxy). Override only for non-proxy deployments.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '',
});
