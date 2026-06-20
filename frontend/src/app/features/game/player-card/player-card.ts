import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import checkoutTable from '../../../shared/data/checkout-table.json';

@Component({
  selector: 'app-player-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './player-card.html',
  styleUrl: './player-card.css',
})
export class PlayerCardComponent {
  // Receives the player object from the parent container
  @Input() player!: any;

  // Covers every 3-dart checkout from 2 to 170. "Bogey" scores (169, 168,
  // 166, 165, 163, 162, 159) have no legal finish and simply aren't in the
  // table - see shared/data/checkout-table.json.
  getCheckoutHint(score: number): string {
    return (checkoutTable as Record<string, string>)[score] ?? '';
  }
}
