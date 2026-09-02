import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  template: `
    <div class="panel">
      <h1>Page not found</h1>
      <p class="muted">The page you requested does not exist.</p>
      <a routerLink="/tickets" class="btn">Back to tickets</a>
    </div>
  `,
})
export class NotFoundPage {}
