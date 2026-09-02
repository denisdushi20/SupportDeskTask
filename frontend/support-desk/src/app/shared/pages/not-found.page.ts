import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  template: `
    <div class="panel error-state">
      <h1 class="error-state__title">Page not found</h1>
      <p class="error-state__desc">The page you requested does not exist.</p>
      <a routerLink="/tickets" class="btn">Back to tickets</a>
    </div>
  `,
})
export class NotFoundPage {}
