import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavShellComponent } from './layout/nav-shell.component';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavShellComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Mattei Grid';
  auth = inject(AuthService);
}
