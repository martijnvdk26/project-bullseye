import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stats-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stats-panel.html',
  styleUrl: './stats-panel.css',
})
export class StatsPanelComponent {
  // Receives the player object from the parent container
  @Input() player!: any;
}
